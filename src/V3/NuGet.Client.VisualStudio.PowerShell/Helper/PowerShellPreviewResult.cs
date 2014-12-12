
namespace NuGet.Client.VisualStudio.PowerShell
{
    public class PowerShellPreviewResult
    {
        public string Id { get; set; }

        public string Action { get; set; }

        public string ProjectName { get; set; }
    }

    public enum PowerShellPackageAction
    {
        Install,
        Uninstall,
        Update,
    }
}
