
namespace NuGet.PowerShell.Commands
{
    internal static class ProgressActivityIds
    {

        // represents the activity Id for the Get-Package command to report its progress
        public const int GetPackageId = 1;

        // represents the activity Id for download progress operation
        public const int DownloadPackageId = 2;
    }
}
