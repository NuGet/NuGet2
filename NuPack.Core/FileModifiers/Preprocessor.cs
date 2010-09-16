using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace NuPack {
    public class Preprocessor : IPackageFileModifier {
        private static readonly Regex _tokenRegex = new Regex(@"\$(?<propertyName>\w+)\$");

        public void Modify(IPackageFile file, string targetPath, ProjectSystem projectSystem) {            
            var content = Preprocess(file, projectSystem);

            projectSystem.AddFile(targetPath, stream => Write(stream, content));
        }

        private void Write(Stream stream, string preprocessed) {
            var writer = new StreamWriter(stream);
            writer.Write(preprocessed);
            writer.Flush();
        }

        private string Preprocess(IPackageFile file, ProjectSystem projectSystem) {
            using (StreamReader reader = new StreamReader(file.Open())) {
                string text = reader.ReadToEnd();
                return _tokenRegex.Replace(text, match => ReplaceToken(match, projectSystem));
            }
        }

        private string ReplaceToken(Match match, ProjectSystem projectSystem) {
            string propertyName = match.Groups["propertyName"].Value;
            if (String.IsNullOrEmpty(propertyName)) {
                // Throw an exception here
                throw new InvalidOperationException("Property name is null at index:" + match.Index);
            }
            return projectSystem.GetPropertyValue(propertyName);
        }

        public void Revert(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, ProjectSystem projectSystem) {            
            string originalContent = Preprocess(file, projectSystem);

            if (projectSystem.FileExists(targetPath)) {
                // We need to do this so we don't try to delete the file while we have it opened
                uint checksum = 0;
                using (Stream stream = projectSystem.OpenFile(targetPath)) {
                    checksum = Crc32.Calculate(stream);
                }

                if (checksum == Crc32.Calculate(originalContent)) {
                    projectSystem.DeleteFile(targetPath);
                }
            }
        }
    }
}
