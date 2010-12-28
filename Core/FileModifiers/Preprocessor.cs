using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NuGet.Resources;

namespace NuGet {
    /// <summary>
    /// Simple token replacement system for content files.
    /// </summary>
    public class Preprocessor : IPackageFileTransformer {
        private static readonly Regex _tokenRegex = new Regex(@"\$(?<propertyName>\w+)\$");

        public void TransformFile(IPackageFile file, string targetPath, IProjectSystem projectSystem) {
            if (!projectSystem.FileExists(targetPath)) {
                using (Stream stream = Process(file, projectSystem).AsStream()) {
                    projectSystem.AddFile(targetPath, stream);
                }
            }
        }

        public void RevertFile(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, IProjectSystem projectSystem) {
            Func<Stream> streamFactory = () => Process(file, projectSystem).AsStream();
            FileSystemExtensions.DeleteFileSafe(projectSystem, targetPath, streamFactory);
        }
        
        private static string Process(IPackageFile file, IProjectSystem projectSystem) {
            string text = file.GetStream().ReadToEnd();
            return _tokenRegex.Replace(text, match => ReplaceToken(match, projectSystem));
        }

        private static string ReplaceToken(Match match, IProjectSystem projectSystem) {
            string propertyName = match.Groups["propertyName"].Value;
            var value = projectSystem.GetPropertyValue(propertyName);
            if (value == null) {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.TokenHasNoValue, propertyName));
            }
            return value;
        }
    }
}
