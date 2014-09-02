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

            // Why not just use OfType<PackageActionDescriptionWrapper>? Because I WANT this to fail
            // if it is given any actions to execute that were not PackageActionDescriptionWrappers!
            var executableActions = actions.Where(a => a.ActionType != PackageActionType.AcceptLicense);

            executor.Execute(executableActions.Cast<PackageActionDescriptionWrapper>().Select(w => w.ResolverAction));
            return Task.FromResult(0);
        }
    }
}
