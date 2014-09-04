using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client.VisualStudio
{
    public interface IUserInterfaceService
    {
        bool PromptForLicenseAcceptance(IEnumerable<JObject> packages);
    }
}
