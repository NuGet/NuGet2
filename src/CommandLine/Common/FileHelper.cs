using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace NuGet
{
    internal static class FileHelper
    {
        private const int BytesToRead = sizeof(Int64);

        public static bool AreFilesEqual(Stream s1, Stream s2)
        {
            if (s1 == null)
            {
                throw new ArgumentNullException("s1");
            }
            if (s2 == null)
            {
                throw new ArgumentNullException("s2");
            }

            var iterations = (int)Math.Ceiling((double)s1.Length / BytesToRead);
            return FileAreEqual(s1, s2, iterations);
        }

        public static bool AreFilesEqual(string path1, string path2)
        {
            if (path1 == null)
            {
                throw new ArgumentNullException("path1");
            }
            if (path2 == null)
            {
                throw new ArgumentNullException("path2");
            }

            return FilesAreEqual(new FileInfo(path1), new FileInfo(path2));
        }

        private static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            if (first == null)
            {
                throw new ArgumentNullException("first");
            }
            if (second == null)
            {
                throw new ArgumentNullException("second");
            }

            if (!first.Exists)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, NuGetResources.UnableToFindFile, first.FullName), "first");
            }

            if (!second.Exists)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, NuGetResources.UnableToFindFile, second.FullName), "second");
            }

            if (first.Length != second.Length)
            {
                return false;
            }

            var iterations = (int)Math.Ceiling((double)first.Length / BytesToRead);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                return FileAreEqual(fs1, fs2, iterations);
            }
        }

        private static bool FileAreEqual(Stream s1, Stream s2, int iterations)
        {
            if (s1 == null)
            {
                throw new ArgumentNullException("s1");
            }
            if (s2 == null)
            {
                throw new ArgumentNullException("s2");
            }

            if (s1.Length != s2.Length)
            {
                return false;
            }

            Debug.Assert(iterations >= 0);

            var one = new byte[BytesToRead];
            var two = new byte[BytesToRead];

            for (var i = 0; i < iterations; i++)
            {
                s1.Read(one, 0, BytesToRead);
                s2.Read(two, 0, BytesToRead);

                if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                {
                    return false;
                }
            }
            return true;
        }
    }
}