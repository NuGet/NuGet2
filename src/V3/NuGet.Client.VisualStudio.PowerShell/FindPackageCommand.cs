using Newtonsoft.Json.Linq;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Runtime.Versioning;

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// FindPackage is identical to GetPackage except that FindPackage filters packages only by Id and does not consider description or tags.
    /// </summary>
    /// TODO List:
    /// 1. Add support for tab expansion, which performs FindPackageById for V2 and goes through the autocomplete endpoint for V3. 
    [Cmdlet(VerbsCommon.Find, "Package")]
    [OutputType(typeof(IPackage))]
    public class FindPackageCommand : PackageListBaseCommand
    {
        private const int MaxReturnedPackages = 30;
        private bool _hasConnectedToHttpSource;
        private IProductUpdateService _productUpdateService;

        public FindPackageCommand()
            : base()
        {
            _productUpdateService = ServiceLocator.GetInstance<IProductUpdateService>();
        }

        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0)]
        public string Id { get; set; }

        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Version { get; set; }

        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Source { get; set; }

        [Parameter]
        public SwitchParameter ListAll { get; set; }

        /// <summary>
        /// Determines if an exact Id match would be performed with the Filter parameter. By default, FindPackage returns all packages that starts with the
        /// Filter value.
        /// </summary>
        [Parameter]
        public SwitchParameter ExactMatch { get; set; }

        protected override void Preprocess()
        {
            this.ActiveSourceRepository = GetActiveRepository(Source);
        }

        protected override void ProcessRecordCore()
        {
            // Since this is used for intellisense, we need to limit the number of packages that we return. Otherwise,
            // typing InstallPackage TAB would download the entire feed.
            First = MaxReturnedPackages;
            Preprocess();

            List<JObject> packagesToDisplay = new List<JObject>();

            if (!string.IsNullOrEmpty(Version))
            {
                JObject searchResult = PowerShellPackage.GetPackageByIdAndVersion(ActiveSourceRepository, Id, Version, IncludePrerelease.IsPresent);
                packagesToDisplay.Add(searchResult);
                WritePackages(packagesToDisplay, VersionType.single);
            }
            else
            {
                if (Id == null)
                {
                    Id = string.Empty;
                }

                packagesToDisplay = GetPackagesFromRemoteSource(Id, Enumerable.Empty<FrameworkName>(), IncludePrerelease.IsPresent, Skip, First).ToList();
                if (ExactMatch.IsPresent)
                {
                    packagesToDisplay = packagesToDisplay.Where(p => string.Equals(p.Value<string>(Properties.PackageId), Id, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (ListAll.IsPresent)
                {
                    WritePackages(packagesToDisplay, VersionType.all);
                }
                else
                {
                    WritePackages(packagesToDisplay, VersionType.latest);
                }
            }
        }

        protected void CheckForNuGetUpdate()
        {
            _hasConnectedToHttpSource |= UriHelper.IsHttpSource(Source);
            if (_productUpdateService != null && _hasConnectedToHttpSource)
            {
                _productUpdateService.CheckForAvailableUpdateAsync();
            }
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
            CheckForNuGetUpdate();
        }
    }
}
