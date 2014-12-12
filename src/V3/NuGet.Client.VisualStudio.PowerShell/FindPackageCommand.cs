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
    public class FindPackageCommand : GetPackageCommand
    {
        private const int MaxReturnedPackages = 30;

        public FindPackageCommand()
            : base()
        {
        }

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
            base.ProcessRecordCore();
        }

        protected override void LogCore(MessageLevel level, string formattedMessage)
        {
            // We don't want this cmdlet to print anything
        }
    }
}
