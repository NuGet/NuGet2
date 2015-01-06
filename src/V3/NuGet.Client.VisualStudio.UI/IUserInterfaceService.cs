using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NuGet.Client.VisualStudio.UI
{
    public interface IUserInterfaceService
    {
        bool PromptForLicenseAcceptance(IEnumerable<PackageLicenseInfo> packages);
        void LaunchExternalLink(Uri url);
        void LaunchNuGetOptionsDialog();
    }
}
