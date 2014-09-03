using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client.Interop
{
    public class V2InteropActionExecutor : IActionExecutor
    {
        public Task ExecuteActions(IEnumerable<PackageActionDescription> actions)
        {
            var executor = new NuGet.Resolver.ActionExecutor();

            executor.Execute(actions.Cast<PackageActionDescriptionWrapper>().Select(w => w.ResolverAction));
            return Task.FromResult(0);
        }
    }
}
