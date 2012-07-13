using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Resources;

namespace NuGet
{
    public class PhysicalFileSystem : IFileSystem
    {
        private readonly string _root;
        private ILogger _logger;

        public PhysicalFileSystem(string root)
        {
            if (String.IsNullOrEmpty(root))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "root");
            }
            _root = root;
        }

        public string Root
        {
            get
            {
                return _root;
            }
        }

        public ILogger Logger
        {
            get
            {
                return _logger ?? NullLogger.Instance;
            }
            set
            {
                _logger = value;
            }
        }

        public virtual string GetFullPath(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return Root;
            }
            return Path.Combine(Root, path);
        }

        public virtual void AddFile(string path, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            EnsureDirectory(Path.GetDirectoryName(path));

            string fullPath = GetFullPath(path);

            using (Stream outputStream = File.Create(fullPath))
            {
                stream.CopyTo(outputStream);
            }

            WriteAddedFileAndDirectory(path);
        }

        private void WriteAddedFileAndDirectory(string path)
        {
            string folderPath = Path.GetDirectoryName(path);

            if (!String.IsNullOrEmpty(folderPath))
            {
                Logger.Log(MessageLevel.Debug, NuGetResources.Debug_AddedFileToFolder, Path.GetFileName(path), folderPath);
            }
            else
            {
                Logger.Log(MessageLevel.Debug, NuGetResources.Debug_AddedFile, Path.GetFileName(path));
            }
        }

        public virtual void DeleteFile(string path)
        {
            if (!FileExists(path))
            {
                return;
            }

            try
            {
                path = GetFullPath(path);
                File.Delete(path);
                string folderPath = Path.GetDirectoryName(path);
                if (!String.IsNullOrEmpty(folderPath))
                {
                    Logger.Log(MessageLevel.Debug, NuGetResources.Debug_RemovedFileFromFolder, Path.GetFileName(path), folderPath);
                }
                else
                {
                    Logger.Log(MessageLevel.Debug, NuGetResources.Debug_RemovedFile, Path.GetFileName(path));
                }
            }
            catch (FileNotFoundException)
            {

            }
        }

        public virtual void DeleteDirectory(string path)
        {
            DeleteDirectory(path, recursive: false);
        }

        public virtual void DeleteDirectory(string path, bool recursive)
        {
            if (!DirectoryExists(path))
            {
                return;
            }

            try
            {
                path = GetFullPath(path);
                Directory.Delete(path, recursive);
                Logger.Log(MessageLevel.Debug, NuGetResources.Debug_RemovedFolder, path);
            }
            catch (DirectoryNotFoundException)
            {

            }
        }

        public virtual IEnumerable<string> GetFiles(string path, bool recursive)
        {
            return GetFiles(path, null, recursive);
        }

        public virtual IEnumerable<string> GetFiles(string path, string filter, bool recursive)
        {
            path = EnsureTrailingSlash(GetFullPath(path));
            if (String.IsNullOrEmpty(filter))
            {
                filter = "*.*";
            }
            try
            {
                if (!Directory.Exists(path))
                {
                    return Enumerable.Empty<string>();
                }
                return Directory.EnumerateFiles(path, filter, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                .Select(MakeRelativePath);
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            return Enumerable.Empty<string>();
        }

        public virtual IEnumerable<string> GetDirectories(string path)
        {
            try
            {
                path = EnsureTrailingSlash(GetFullPath(path));
                if (!Directory.Exists(path))
                {
                    return Enumerable.Empty<string>();
                }
                return Directory.EnumerateDirectories(path)
                                .Select(MakeRelativePath);
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            return Enumerable.Empty<string>();
        }

        public virtual DateTimeOffset GetLastModified(string path)
        {
            path = GetFullPath(path);
            if (File.Exists(path))
            {
                return File.GetLastWriteTimeUtc(path);
            }
            return Directory.GetLastWriteTimeUtc(path);
        }

        public DateTimeOffset GetCreated(string path)
        {
            path = GetFullPath(path);
            if (File.Exists(path))
            {
                return File.GetCreationTimeUtc(path);
            }
            return Directory.GetCreationTimeUtc(path);
        }

        public DateTimeOffset GetLastAccessed(string path)
        {
            path = GetFullPath(path);
            if (File.Exists(path))
            {
                return File.GetLastAccessTimeUtc(path);
            }
            return Directory.GetLastAccessTimeUtc(path);
        }

        public virtual bool FileExists(string path)
        {
            path = GetFullPath(path);
            return File.Exists(path);
        }

        public virtual bool DirectoryExists(string path)
        {
            path = GetFullPath(path);
            return Directory.Exists(path);
        }

        public virtual Stream OpenFile(string path)
        {
            path = GetFullPath(path);
            return File.OpenRead(path);
        }

        protected string MakeRelativePath(string fullPath)
        {
            return fullPath.Substring(Root.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        protected virtual void EnsureDirectory(string path)
        {
            path = GetFullPath(path);
            Directory.CreateDirectory(path);
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (!path.EndsWith("\\", StringComparison.Ordinal))
            {
                path += "\\";
            }
            return path;
        }
    }
}
