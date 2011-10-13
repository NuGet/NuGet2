using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet
{
    internal class NullFileSystem : IFileSystem
    {
        private static readonly NullFileSystem _instance = new NullFileSystem();

        private NullFileSystem()
        {
            // Private constructor for a singleton
        }

        public static NullFileSystem Instance
        {
            get { return _instance; }
        }

        public ILogger Logger
        {
            get;
            set;
        }

        public string Root
        {
            get { return String.Empty; }
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            // Do nothing
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetFiles(string path, string filter)
        {
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            return Enumerable.Empty<string>();
        }

        public string GetFullPath(string path)
        {
            return path;
        }

        public void DeleteFile(string path)
        {
            // Do nothing
        }

        public bool FileExists(string path)
        {
            return false;
        }

        public bool DirectoryExists(string path)
        {
            return false;
        }

        public void AddFile(string path, Stream stream)
        {
            // Do nothing
        }

        public Stream OpenFile(string path)
        {
            return Stream.Null;
        }

        public DateTimeOffset GetLastModified(string path)
        {
            return DateTimeOffset.MinValue;
        }

        public DateTimeOffset GetCreated(string path)
        {
            return DateTimeOffset.MinValue;
        }
    }
}
