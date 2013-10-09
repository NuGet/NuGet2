using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Utility;

namespace NuGet.WebMatrix.Data
{
    /// <summary>
    /// A list implementation that lazily fetches elements from an IQueryable source
    /// </summary>
    /// <remarks>
    /// Entries in the VirtualizingList are always of type VirtualizingListEntry (instead of the type of 
    /// the underlying data)-- the reason for this is that fundamentally WPF has some tricky bugs inside 
    /// ICollectionView and ItemsControl.
    /// 
    /// In particular there are issues when using INotifyCollectionChanged and maintaining keyboard focus
    /// for which there is no suitable workaround. (If you try to send an 'update' to the currently focused
    /// item, keyboard focus will go to the beginning of the list on the next operation).
    ///
    /// The approach instead is to bind to the Item property of and provide one data-template keyed on x:Type
    /// VirtualizingListPlaceholder, and another data-template keyed on the real domain object. VirtualizingListEntry
    /// will notify when the real item is available, and the template will swap.
    /// </remarks>
    public class VirtualizingList : IList
    {
        private object _chunkLock;

        /// <summary>
        /// The maximum number of concurrent requests to allowed
        /// </summary>
        /// <remarks>
        /// We throttle the number of concurrent requests allowed to the IQueryable api, because each
        /// call is a blocking operation. Even though it occurs in a task, it still results in a blocked
        /// thread. Too many blocked threads will starve the app and cause the UI to hang.
        /// </remarks>
        private const int RequestConcurrency = 2;

        public VirtualizingList(IQueryable<object> source, int chunkSize, Func<object, object> itemFactory = null)
        {
            this.Source = source;
            this.ChunkSize = chunkSize;
            this.ItemFactory = itemFactory;
            
            this.Count = source.Count();
            this.Scheduler = new LimitedConcurrencyScheduler(RequestConcurrency);

            this.PlaceholderValue = new VirtualizingListPlaceholder();
            this.Chunks = new Chunk[(this.Count + (this.ChunkSize - 1)) / this.ChunkSize];
            this._chunkLock = new object();

            // wait for the first chunk if we have any data -- this is useful for testability and
            // helps frontload the waiting time... the list isn't interesting to a user if all of 
            // the items say 'loading...'
            if (this.Chunks.Length > 0)
            {
                var firstChunk = this.FetchChunkAsyncIfNeeded(0);
                firstChunk.Task.Wait();
            }
        }

        private Chunk[] Chunks
        {
            get;
            set;
        }

        public int ChunkSize
        {
            get;
            private set;
        }

        public int Count
        {
            get;
            private set;
        }

        public bool IsFixedSize
        {
            get { return true; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public Func<object, object> ItemFactory
        {
            get;
            private set;
        }

        public object PlaceholderValue
        {
            get;
            private set;
        }

        private TaskScheduler Scheduler
        {
            get;
            set;
        }

        public IQueryable<object> Source
        {
            get;
            private set;
        }

        public object SyncRoot
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the value at the specified position
        /// </summary>
        /// <param name="index">The index to retrieve</param>
        /// <returns>The value at the specified position, which may be a placeholder</returns>
        /// <remarks>This method will begin the download of a chunk if needed, but will not block waiting for the download
        /// of the chunk data.</remarks>
        public object this[int index]
        {
            get
            {
                if (!this.IsValidIndex(index))
                {
                    throw new ArgumentOutOfRangeException();
                }

                int chunkIndex = index / this.ChunkSize;
                Chunk chunk = this.GetChunk(chunkIndex);

                if (chunk == null)
                {
                    chunk = this.FetchChunkAsyncIfNeeded(chunkIndex);
                }

                if (chunk.Task.IsFaulted)
                {
                    throw chunk.Task.Exception;
                }
                else
                {
                    int localIndex = index % this.ChunkSize;
                    Debug.Assert(localIndex >= 0 && localIndex < chunk.Entries.Length, "localIndex is out of range");
                    return chunk.Entries[localIndex];
                }
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool Contains(object value)
        {
            return this.IndexOf(value) >= 0;
        }

        /// <summary>
        /// Returns the index of a given value in the list
        /// </summary>
        /// <param name="value">The value to search for</param>
        /// <returns>The index of the given value, or -1 if it's not found</returns>
        /// <remarks>
        /// This method only searches data that is already contained in the list, and does not
        /// fetch any new data
        /// </remarks>
        public int IndexOf(object value)
        {
            lock (this._chunkLock)
            {
                foreach (var chunk in this.Chunks)
                {
                    // it's alright if a chunk is null here, we don't eagerly fetch data
                    // in IndexOf
                    if (chunk == null)
                    {
                        continue;
                    }

                    VirtualizingListEntry[] entries = chunk.Entries;
                    if (entries != null)
                    {
                        for (int i = 0; i < entries.Length; i++)
                        {
                            if (entries[i].Equals(value))
                            {
                                return i + (this.ChunkSize * chunk.Index);
                            }
                        }
                    }
                }

                return -1;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new VirtualizingListEnumerator(this);
        }

        /// <summary>
        /// Gets the chunk if it has already been created
        /// </summary>
        /// <param name="chunkIndex">The index of the chunk to return (range: [0 - Count/ChunkSize]</param>
        /// <returns>The chunk, which may contain placeholder values, or null if the chunk hasn't been created</returns>
        /// <remarks>This method does not start a download if the chunk hasn't been created yet.</remarks>
        private Chunk GetChunk(int chunkIndex)
        {
            Debug.Assert(chunkIndex >= 0 && chunkIndex < this.Chunks.Length, "chunkIndex is  out of range");

            lock (this._chunkLock)
            {
                return this.Chunks[chunkIndex];
            }
        }

        /// <summary>
        /// Queues the fetch of a chunk if needed
        /// </summary>
        /// <param name="chunkIndex">The index of the chunk to return (range: [0 - Count/ChunkSize]</param>
        /// <returns>The chunk, which may contain placeholder values</returns>
        /// <remarks>This method does not block on retrieving the chunk data</remarks>
        private Chunk FetchChunkAsyncIfNeeded(int chunkIndex)
        {
            Debug.Assert(chunkIndex >= 0 && chunkIndex < this.Chunks.Length, "chunkIndex is  out of range");

            lock (this._chunkLock)
            {
                Chunk chunk = this.Chunks[chunkIndex];
                if (chunk == null)
                {
                    chunk = new Chunk(this.ChunkSize, chunkIndex, this.PlaceholderValue);
                    chunk.Task = Task.Factory.StartNew(this.BeginFetchChunk, chunk, CancellationToken.None, TaskCreationOptions.None, this.Scheduler);

                    this.Chunks[chunkIndex] = chunk;
                }

                return chunk;
            }
        }

        /// <summary>
        /// Worker method to download a chunk
        /// </summary>
        /// <param name="state">The chunk object to download</param>
        /// <remarks>
        /// This is blocking and should be called on a task
        /// </remarks>
        private void BeginFetchChunk(object state)
        {
            Chunk chunk = (Chunk)state;
            var data = this.Source
                .Skip(chunk.Index * this.ChunkSize)
                .Take(this.ChunkSize)
                .ToArray();

            int i = 0;
            foreach (var item in data)
            {
                if (this.ItemFactory == null)
                {
                    chunk.Entries[i].Item = item;
                }
                else
                {
                    chunk.Entries[i].Item = this.ItemFactory(item);
                }

                i++;
            }
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < this.Count;
        }

        #region Not Implemented (this is read only)

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// Represents a unit of data to be loaded on demand
        /// </summary>
        private class Chunk
        {
            public Chunk(int chunkSize, int index, object placeholderValue)
            {
                this.Index = index;

                this.Entries = new VirtualizingListEntry[chunkSize];
                for (int i = 0; i < chunkSize; i++)
                {
                    this.Entries[i] = new VirtualizingListEntry();
                    this.Entries[i].Item = placeholderValue;
                }
            }

            public int Index
            {
                get;
                private set;
            }

            public Task Task
            {
                get;
                set;
            }

            public VirtualizingListEntry[] Entries
            {
                get;
                set;
            }
        }

        /// <summary>
        /// An IEnumerator implementation for the VirtualizingList
        /// </summary>
        /// <remarks>
        /// This enumerator is used deep within the CollectionView implementation, but only in a few
        /// circumstances (to check whether the list is empty or not). The implementation here is 
        /// complete, though it's not really used in full.
        /// </remarks>
        private class VirtualizingListEnumerator : IEnumerator
        {
            public VirtualizingListEnumerator(VirtualizingList list)
            {
                this.List = list;

                this.CurrentIndex = -1;
            }

            public Chunk CurrentChunk
            {
                get;
                private set;
            }

            public int CurrentChunkIndex
            {
                get;
                private set;
            }

            public int CurrentIndex
            {
                get;
                private set;
            }

            public VirtualizingList List
            {
                get;
                private set;
            }

            public object Current
            {
                get
                {
                    if (!this.List.IsValidIndex(this.CurrentIndex))
                    {
                        throw new IndexOutOfRangeException();
                    }

                    int chunkIndex = this.CurrentIndex / this.List.ChunkSize;
                    if (this.CurrentChunkIndex == chunkIndex && this.CurrentChunk != null)
                    {
                        // do nothing, we already have the current chunk
                    }
                    else
                    {
                        this.CurrentChunkIndex = chunkIndex;
                        this.CurrentChunk = this.List.GetChunk(chunkIndex);
                    }

                    if (this.CurrentChunk == null)
                    {
                        this.CurrentChunk = this.List.FetchChunkAsyncIfNeeded(chunkIndex);
                    }

                    if (this.CurrentChunk.Task.IsFaulted)
                    {
                        throw this.CurrentChunk.Task.Exception;
                    }
                    else
                    {
                        int localIndex = this.CurrentIndex % this.List.ChunkSize;
                        Debug.Assert(localIndex >= 0 && localIndex < this.CurrentChunk.Entries.Length, "localIndex is out of range");
                        return this.CurrentChunk.Entries[localIndex];
                    }
                }
            }

            public bool MoveNext()
            {
                this.CurrentIndex++;
                return this.List.IsValidIndex(this.CurrentIndex);
            }

            public void Reset()
            {
                this.CurrentIndex = -1;
            }
        }
    }
}
