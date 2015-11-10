namespace NuGet.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Threading;

    using Microsoft.DiaSymReader;

    using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;
    using System.Globalization;
    using System.Security;
    using System.Diagnostics.CodeAnalysis;

    internal static class PdbHelper
    {
        public static IEnumerable<string> GetSourceFileNames(IPackageFile pdbFile)
        {
            using (var stream = new StreamAdapter(pdbFile.GetStream()))
            {
                var reader = CreateNativeSymReader(stream);

                return reader.GetDocuments()
                    .Select(doc => doc.GetName())
                    .Where(IsValidSourceFileName);
            }
        }

        private static ISymUnmanagedReader3 CreateNativeSymReader(IStream pdbStream)
        {
            object symReader = null;
            var guid = default(Guid);

            if (IntPtr.Size == 4)
            {
                NativeMethods.CreateSymReader32(ref guid, out symReader);
            }
            else
            {
                NativeMethods.CreateSymReader64(ref guid, out symReader);
            }

            var reader = (ISymUnmanagedReader3)symReader;
            var hr = reader.Initialize(new DummyMetadataImport(), null, null, pdbStream);
            Marshal.ThrowExceptionForHR(hr);
            return reader;
        }

        private class DummyMetadataImport : IMetadataImport { }

        private static bool IsValidSourceFileName(string sourceFileName)
        {
            return !string.IsNullOrEmpty(sourceFileName) && !IsTemporaryCompilerFile(sourceFileName);
        }

        private static bool IsTemporaryCompilerFile(string sourceFileName)
        {
            //the VB compiler will include temporary files in its pdb files.
            //the source file name will be similar to 17d14f5c-a337-4978-8281-53493378c1071.vb.
            return sourceFileName.EndsWith("17d14f5c-a337-4978-8281-53493378c1071.vb", StringComparison.OrdinalIgnoreCase);
        }

        [SuppressUnmanagedCodeSecurity]
        private static class NativeMethods
        {
            [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
            [DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymReader")]
            internal extern static void CreateSymReader32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

            [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
            [DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymReader")]
            internal extern static void CreateSymReader64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);
        }

        /// <summary>
        /// Wrap a Stream so it's usable where we need an IStream
        /// </summary>
        sealed class StreamAdapter : IStream, IDisposable
        {
            Stream _stream;

            [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Justification = "need to validate that part.")]
            IntPtr _pcbData = Marshal.AllocHGlobal(8); // enough to store long/int64, can be shared since we don't support multithreaded access to one file.

            private bool _disposed = false;

            /// <summary>
            /// Create a new adapter around the given stream.
            /// </summary>
            /// <param name="wrappedStream">The stream to wrap.</param>
            public StreamAdapter(Stream wrappedStream)
            {
                _stream = wrappedStream;
            }

            ~StreamAdapter()
            {
                Dispose(false);
            }

            public void Clone(out IStream ppstm)
            {
                throw new NotSupportedException();
            }

            public void Commit(int grfCommitFlags)
            {
            }

            public void LockRegion(long libOffset, long cb, int dwLockType)
            {
                throw new NotSupportedException();
            }

            public void Revert()
            {
                throw new NotSupportedException();
            }

            public void UnlockRegion(long libOffset, long cb, int dwLockType)
            {
                throw new NotSupportedException();
            }

            public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
            {
                throw new NotSupportedException();
            }

            public void Read(byte[] pv, int cb, IntPtr pcbRead)
            {
                var count = _stream.Read(pv, 0, cb);
                if (pcbRead != IntPtr.Zero)
                {
                    Marshal.WriteInt32(pcbRead, count);
                }
            }

            public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
            {
                var origin = (SeekOrigin)dwOrigin;
                var pos = _stream.Seek(dlibMove, origin);
                if (plibNewPosition != IntPtr.Zero)
                {
                    Marshal.WriteInt64(plibNewPosition, pos);
                }
            }

            public void SetSize(long libNewSize)
            {
                _stream.SetLength(libNewSize);
            }

            public void Stat(out STATSTG pstatstg, int grfStatFlag)
            {
                pstatstg = new STATSTG
                {
                    type = 2,
                    cbSize = _stream.Length,
                    grfMode = 0
                };

                if (_stream.CanRead && _stream.CanWrite)
                {
                    pstatstg.grfMode |= 2;
                }
                else if (_stream.CanWrite && !_stream.CanRead)
                {
                    pstatstg.grfMode |= 1;
                }
            }

            public void Write(byte[] pv, int cb, IntPtr pcbWritten)
            {
                _stream.Write(pv, 0, cb);
                if (pcbWritten != IntPtr.Zero)
                {
                    Marshal.WriteInt32(pcbWritten, cb);
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if(_disposed)
                {
                    return;
                }

                if (disposing)
                {
                    Interlocked.Exchange(ref _stream, null)?.Close();

                    var data = Interlocked.Exchange(ref _pcbData, IntPtr.Zero);
                    if (data != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(_pcbData);
                    }

                    _disposed = true;           
                }
            }
        }
    }

    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces", Justification = "need to validate that's ok to suppress.")]
    [ComImport, Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
    public interface IMetadataImport { }

    static class SymUnmanagedReaderExtensions
    {
        // Excerpt of http://source.roslyn.io/#Roslyn.Test.PdbUtilities/Shared/SymUnmanagedReaderExtensions.cs

        private const int E_FAIL = unchecked((int)0x80004005);
        private const int E_NOTIMPL = unchecked((int)0x80004001);

        private delegate int ItemsGetter<in TEntity, in TItem>(TEntity entity, int bufferLength, out int count, TItem[] buffer);

        private static string ToString(char[] buffer)
        {
            if (buffer.Length == 0)
                return string.Empty;

            Debug.Assert(buffer[buffer.Length - 1] == 0);
            return new string(buffer, 0, buffer.Length - 1);
        }

        private static void ValidateItems(int actualCount, int bufferLength)
        {
            if (actualCount != bufferLength)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Read only {0} of {1} items.", actualCount, bufferLength));
            }
        }

        private static TItem[] GetItems<TEntity, TItem>(TEntity entity, ItemsGetter<TEntity, TItem> getter)
        {
            int count;
            var hr = getter(entity, 0, out count, null);
            ThrowExceptionForHR(hr);
            if (count == 0)
                return new TItem[0];

            var result = new TItem[count];
            hr = getter(entity, count, out count, result);
            ThrowExceptionForHR(hr);
            ValidateItems(count, result.Length);
            return result;
        }

        public static ISymUnmanagedDocument[] GetDocuments(this ISymUnmanagedReader reader)
        {
            return GetItems(reader, (ISymUnmanagedReader a, int b, out int c, ISymUnmanagedDocument[] d) => a.GetDocuments(b, out c, d));
        }

        internal static string GetName(this ISymUnmanagedDocument document)
        {
            return ToString(GetItems(document, (ISymUnmanagedDocument a, int b, out int c, char[] d) => a.GetUrl(b, out c, d)));
        }

        internal static void ThrowExceptionForHR(int hr)
        {
            // E_FAIL indicates "no info".
            // E_NOTIMPL indicates a lack of ISymUnmanagedReader support (in a particular implementation).
            if (hr < 0 && hr != E_FAIL && hr != E_NOTIMPL)
            {
                Marshal.ThrowExceptionForHR(hr, new IntPtr(-1));
            }
        }
    }
}