using Microsoft.VisualStudio.Shell;
using NuGet.Client.Resolution;
using NuGet.Versioning;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Text;

namespace NuGet.Client.VisualStudio.PowerShell
{
    // TODO List:
    // 1. Figure out support for Install-Package and Update-Package in terms of Path and -Safe support
    public class PackageInstallBaseCommand : PackageActionBaseCommand
    {
        private ResolutionContext _context;
        private bool _readFromPackagesConfig;
        private bool _readFromDirectPackagePath;

        public PackageInstallBaseCommand(
            IVsPackageSourceProvider packageSourceProvider,
            IPackageRepositoryFactory packageRepositoryFactory,
            SVsServiceProvider svcServiceProvider,
            IVsPackageManagerFactory packageManagerFactory,
            ISolutionManager solutionManager,
            IHttpClientEvents clientEvents)
            : base(packageSourceProvider, packageRepositoryFactory, svcServiceProvider, packageManagerFactory, solutionManager, clientEvents, PackageActionType.Install)
        {
        }

        [Parameter, Alias("Prerelease")]
        public SwitchParameter IncludePrerelease { get; set; }

        [Parameter]
        public SwitchParameter IgnoreDependencies { get; set; }

        [Parameter]
        public Client.FileConflictAction FileConflictAction { get; set; }

        [Parameter]
        public DependencyBehavior? DependencyVersion { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            this.PackageActionResolver = new ActionResolver(ActiveSourceRepository, ResolutionContext);
        }

        protected override void PreprocessProjectAndIdentities()
        {
            ParseUserInputForId();
            this.Identities = GetIdentitiesForResolver();
        }

        /// <summary>
        /// Get Identities for Resolver. Can be a single Identity for Install/Uninstall-Package.
        /// or multiple identities for Install/Update-Package.
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<PackageIdentity> GetIdentitiesForResolver(bool isSafe = false)
        {
            IEnumerable<PackageIdentity> identityList = Enumerable.Empty<PackageIdentity>();
            if (_readFromPackagesConfig)
            {
                identityList = CreatePackageIdentitiesFromPackagesConfig();
            }
            else if (_readFromDirectPackagePath)
            {
                identityList = CreatePackageIdentityFromNupkgPath();
            }
            else
            {
                identityList = GetPackageIdentityForResolver(isSafe);
            }
            return identityList;
        }

        /// <summary>
        /// Returns single package identity for resolver when Id is specified
        /// </summary>
        /// <returns></returns>
        private List<PackageIdentity> GetPackageIdentityForResolver(bool requireSafe)
        {
            PackageIdentity identity = null;

            // If Version is specified by commandline parameter
            if (!string.IsNullOrEmpty(Version))
            {
                identity = new PackageIdentity(Id, NuGetVersion.Parse(Version));
                if (!_readFromDirectPackagePath)
                {
                    identity = Client.PackageRepositoryHelper.ResolvePackage(ActiveSourceRepository, V2LocalRepository, identity, IncludePrerelease.IsPresent);
                }
            }
            else
            {
                // For Install-Package and Update-Package
                Version = PowerShellPackage.GetLastestVersionForPackage(ActiveSourceRepository, Id, IncludePrerelease.IsPresent, null, requireSafe);
                identity = new PackageIdentity(Id, NuGetVersion.Parse(Version));
            }

            return new List<PackageIdentity>() { identity };
        }

        /// <summary>
        /// Parse user input for Id parameter. 
        /// Id can be the name of a package, path to packages.config file or path to .nupkg file.
        /// </summary>
        private void ParseUserInputForId()
        {
            if (!String.IsNullOrEmpty(Id))
            {
                if (Id.ToLowerInvariant().EndsWith(NuGet.Constants.PackageReferenceFile))
                {
                    _readFromPackagesConfig = true;
                }
                else if (Id.ToLowerInvariant().EndsWith(NuGet.Constants.PackageExtension))
                {
                    _readFromDirectPackagePath = true;
                }
            }
        }

        /// <summary>
        /// Return list of package identities parsed from packages.config
        /// </summary>
        /// <returns></returns>
        private IEnumerable<PackageIdentity> CreatePackageIdentitiesFromPackagesConfig()
        {
            List<PackageIdentity> identities = new List<PackageIdentity>();
            IEnumerable<PackageIdentity> parsedIdentities = null;

            try
            {
                // Example: install-package2 https://raw.githubusercontent.com/NuGet/json-ld.net/master/src/JsonLD/packages.config
                if (Id.ToLowerInvariant().StartsWith("http"))
                {
                    string text = ReadPackagesConfigFileContentOnline(Id).Replace("???", "");
                    PackagesConfigReader reader = new PackagesConfigReader();
                    parsedIdentities = reader.GetPackages(text);
                }
                else
                {
                    using (FileStream stream = new FileStream(Id, FileMode.Open))
                    {
                        PackagesConfigReader reader = new PackagesConfigReader(stream);
                        parsedIdentities = reader.GetPackages();
                        if (stream != null)
                        {
                            stream.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(MessageLevel.Error, Resources.Cmdlet_FailToParsePackages, Id, ex.Message);
            }

            foreach (PackageIdentity identity in parsedIdentities)
            {
                PackageIdentity resolvedIdentity = Client.PackageRepositoryHelper.ResolvePackage(ActiveSourceRepository, V2LocalRepository, identity, IncludePrerelease.IsPresent);
                identities.Add(resolvedIdentity);
            }
            return identities;
        }

        private IEnumerable<PackageIdentity> CreatePackageIdentityFromNupkgPath()
        {
            PackageIdentity identity = null; 
            if (UriHelper.IsHttpSource(Id))
            {
                throw new NotImplementedException();
            }
            else
            {
                try
                {
                    string fullPath = Path.GetFullPath(Id);
                    Source = Path.GetDirectoryName(fullPath);
                    var package = new OptimizedZipPackage(fullPath);
                    if (package != null)
                    {
                        Id = package.Id;
                        Version = package.Version.ToString();
                    }
                    identity = new PackageIdentity(Id, NuGetVersion.Parse(Version));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            return new List<PackageIdentity>() { identity };
        }

        /// <summary>
        /// Read the content of the file via HttpWebRequest
        /// </summary>
        /// <param name="url"></param>
        private string ReadPackagesConfigFileContentOnline(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Read data via the response stream
            Stream resStream = response.GetResponseStream();
            string tempString = null;
            StringBuilder stringBuilder = new StringBuilder();

            int bytesToRead = 10000;
            byte[] buffer = new Byte[bytesToRead];
            int count = 0;

            do
            {
                // Fill the buffer with data
                count = resStream.Read(buffer, 0, buffer.Length);

                // Make sure we read some data
                if (count != 0)
                {
                    // Translate from bytes to ASCII text
                    tempString = Encoding.ASCII.GetString(buffer, 0, count);

                    // Continue building the string
                    stringBuilder.Append(tempString);
                }
            }
            while (count > 0); // Any more data to read?
            resStream.Close();

            // Return content
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Resolution Context for the command
        /// </summary>
        public ResolutionContext ResolutionContext
        {
            get
            {
                _context = new ResolutionContext();
                _context.DependencyBehavior = GetDependencyBehavior();
                _context.AllowPrerelease = IncludePrerelease.IsPresent;
                // If Version is prerelease, automatically allow prerelease (i.e. append -Prerelease switch).
                if (!string.IsNullOrEmpty(Version) && PowerShellPackage.IsPrereleaseVersion(Version))
                {
                    _context.AllowPrerelease = true;
                }
                return _context;
            }
        }

        public override FileConflictAction ResolveFileConflict(string message)
        {
            if (FileConflictAction == FileConflictAction.Overwrite)
            {
                return Client.FileConflictAction.Overwrite;
            }

            if (FileConflictAction == FileConflictAction.Ignore)
            {
                return Client.FileConflictAction.Ignore;
            }

            return base.ResolveFileConflict(message);
        }

        private DependencyBehavior GetDependencyBehavior()
        {
            if (IgnoreDependencies.IsPresent)
            {
                return DependencyBehavior.Ignore;
            }
            else if (DependencyVersion.HasValue)
            {
                return DependencyVersion.Value;
            }
            else
            {
                return DependencyBehavior.Lowest;
            }
        }
    }
}
