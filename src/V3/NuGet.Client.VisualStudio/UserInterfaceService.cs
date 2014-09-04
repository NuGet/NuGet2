using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client.VisualStudio
{
    [Export(typeof(IUserInterfaceService))]
    public class UserInterfaceService : IUserInterfaceService
    {
        public bool PromptForLicenseAcceptance(IEnumerable<JObject> packages)
        {
            throw new NotImplementedException();
        }
    }
}
