using System.IO;

namespace NuGet.VisualStudio {
    internal static class FileHelper {
        public static void CopyAllFiles(string sourceDirectory, string targetDirectory) {
            foreach (string file in Directory.EnumerateFiles(sourceDirectory)) {
                File.Copy(file, Path.Combine(targetDirectory, Path.GetFileName(file)), overwrite: true);
            }
        }
    }
}