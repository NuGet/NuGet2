using NuGet.Client;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NuGet.Common
{
    public class SelfUpdater
    {
        private const string NuGetCommandLinePackageId = "NuGet.CommandLine";
        private const string NuGetExe = "NuGet.exe";

        public IConsole Console { get; set; }      
        private IPackageSourceProvider _sourceProvider;
        private ICollection<string> _source;

        public SelfUpdater(IPackageSourceProvider sourceProvider, ICollection<string> source)
        {
            _sourceProvider = sourceProvider;
            _source = source;
        }
        public void UpdateSelf()
        {
            Assembly assembly = typeof(SelfUpdater).Assembly;
            var version = GetNuGetVersion(assembly) ?? new NuGetVersion(assembly.GetName().Version);
            SelfUpdate(assembly.Location, version);
        }

        internal void SelfUpdate(string exePath, NuGetVersion version)
        {
            Console.WriteLine(LocalizedResourceManager.GetString("UpdateCommandCheckingForUpdates"), NuGetConstants.DefaultFeedUrl);
            SourceRepository sourceRepository = SourceRepositoryHelper.CreateSourceRepository(_sourceProvider, _source);

            var package = sourceRepository.GetPackageMetadataById(NuGetCommandLinePackageId).Result.LastOrDefault();
            if (package == null || version >= new NuGetVersion(package[Properties.Version].ToString()))
            {
                Console.WriteLine(LocalizedResourceManager.GetString("UpdateCommandNuGetUpToDate"));
            }
            else
            {
                Console.WriteLine(LocalizedResourceManager.GetString("UpdateCommandUpdatingNuGet"), package[Properties.Version].ToString());
                var downloader = new PackageDownloader();
                PackageName packageName = new PackageName(package[Properties.PackageId].ToString(), new SemanticVersion(package[Properties.Version].ToString()));
                string downloadUriStr = package[Properties.PackageContent].ToString();
                Uri downloadUri;
                IPackage packageContent;
                if (!Uri.TryCreate(downloadUriStr, UriKind.Absolute, out downloadUri))
                {
                    Console.WriteLine("invalid download Uri");
                    return;
                }

                using (var targetStream = new MemoryStream())
                {
                    downloader.DownloadPackage(
                        new HttpClient(downloadUri),
                        packageName,
                        targetStream);

                    targetStream.Seek(0, SeekOrigin.Begin);
                    packageContent = new ZipPackage(targetStream);
                }
                // Get NuGet.exe file from the package
                IPackageFile file = packageContent.GetFiles().FirstOrDefault(f => Path.GetFileName(f.Path).Equals(NuGetExe, StringComparison.OrdinalIgnoreCase));
                // If for some reason this package doesn't have NuGet.exe then we don't want to use it
                if (file == null)
                {
                    throw new CommandLineException(LocalizedResourceManager.GetString("UpdateCommandUnableToLocateNuGetExe"));
                }

                // Get the exe path and move it to a temp file (NuGet.exe.old) so we can replace the running exe with the bits we got 
                // from the package repository
                string renamedPath = exePath + ".old";
                Move(exePath, renamedPath);

                // Update the file
                UpdateFile(exePath, file);
                Console.WriteLine(LocalizedResourceManager.GetString("UpdateCommandUpdateSuccessful"));
            }
        }

        protected virtual void UpdateFile(string exePath, IPackageFile file)
        {
            using (Stream fromStream = file.GetStream(), toStream = File.Create(exePath))
            {
                fromStream.CopyTo(toStream);
            }
        }

        protected virtual void Move(string oldPath, string newPath)
        {
            try
            {
                if (File.Exists(newPath))
                {
                    File.Delete(newPath);
                }
            }
            catch (FileNotFoundException)
            {

            }

            File.Move(oldPath, newPath);
        }
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want this method to throw.")]
        internal static NuGetVersion GetNuGetVersion(ICustomAttributeProvider assembly)
        {
            try
            {
                var assemblyInformationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                return new NuGetVersion(assemblyInformationalVersion.InformationalVersion);
            }
            catch
            {
                // Don't let GetCustomAttributes throw.
            }
            return null;
        }

    }
}
