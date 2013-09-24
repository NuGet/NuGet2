using NuGet.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NuGet
{
    /// <summary>
    /// Simple token replacement system for content files.
    /// </summary>
    public class Preprocessor : IPackageFileTransformer
    {
        private static readonly Regex _tokenRegex = new Regex(@"\$(?<propertyName>\w+)\$");

        public void TransformFile(IPackageFile file, string targetPath, IProjectSystem projectSystem)
        {
            ProjectSystemExtensions.TryAddFile(projectSystem, targetPath, () => Process(file, projectSystem).AsStream());
        }

        public void RevertFile(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, IProjectSystem projectSystem)
        {
            Func<Stream> streamFactory = () => Process(file, projectSystem).AsStream();
            FileSystemExtensions.DeleteFileSafe(projectSystem, targetPath, streamFactory);
        }

        internal static string Process(IPackageFile file, IPropertyProvider propertyProvider)
        {
            using (var stream = file.GetStream())
            {
                return Process(stream, propertyProvider, throwIfNotFound: false);
            }
        }

        public static string Process(Stream stream, IPropertyProvider propertyProvider, bool throwIfNotFound = true)
        {
            // Fix for bug https://nuget.codeplex.com/workitem/3174, source code transformations must support BOM
            byte[] bytes = stream.ReadAllBytes();
            string text = Encoding.UTF8.GetString(bytes);
            return _tokenRegex.Replace(text, match => ReplaceToken(match, propertyProvider, throwIfNotFound));
        }

        private static string ReplaceToken(Match match, IPropertyProvider propertyProvider, bool throwIfNotFound)
        {
            string propertyName = match.Groups["propertyName"].Value;
            var value = propertyProvider.GetPropertyValue(propertyName);
            if (value == null && throwIfNotFound)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.TokenHasNoValue, propertyName));
            }
            return value;
        }
    }
}
