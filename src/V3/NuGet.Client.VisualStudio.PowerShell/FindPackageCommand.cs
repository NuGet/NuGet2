using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;
using System;
using System.Management.Automation;

namespace NuGet.Client.VisualStudio.PowerShell
{
    /// <summary>
    /// FindPackage is identical to GetPackage except that FindPackage filters packages only by Id and does not consider description or tags.
    /// </summary>
    /// TODO List:
    /// 1. Reimplement this command after spec regarding to Get-Package is done.
    /// 2. Consider replacing Get-Package -ListAvailable with Find-Package.
    [Cmdlet(VerbsCommon.Find, "Package2", DefaultParameterSetName = "Default")]
    [OutputType(typeof(IPackage))]
    public class FindPackageCommand : PackageListBaseCommand
    {
        private const int MaxReturnedPackages = 30;
        private IProductUpdateService _productUpdateService;
        private bool _hasConnectedToHttpSource;

        public FindPackageCommand()
            : base()
        {
            _productUpdateService = ServiceLocator.GetInstance<IProductUpdateService>();
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public virtual string Id { get; set; }

        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        public string Version { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Remote")]
        [Alias("Online", "Remote")]
        public SwitchParameter ListAll { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Updates")]
        public SwitchParameter latest { get; set; }

        /// <summary>
        /// Determines if an exact Id match would be performed with the Filter parameter. By default, FindPackage returns all packages that starts with the
        /// Filter value.
        /// </summary>
        [Parameter]
        public SwitchParameter ExactMatch { get; set; }

        protected override void ProcessRecordCore()
        {
            // Since this is used for intellisense, we need to limit the number of packages that we return. Otherwise,
            // typing InstallPackage TAB would download the entire feed.
            First = MaxReturnedPackages;
            Preprocess();
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
            CheckForNuGetUpdate();
        }

        private void CheckForNuGetUpdate()
        {
            _hasConnectedToHttpSource |= UriHelper.IsHttpSource(Source);
            if (_productUpdateService != null && _hasConnectedToHttpSource)
            {
                _productUpdateService.CheckForAvailableUpdateAsync();
            }
        }
    }
}
