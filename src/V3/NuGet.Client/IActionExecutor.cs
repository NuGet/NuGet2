using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    public interface IActionExecutor
    {
        Task ExecuteActions(IEnumerable<PackageActionDescription> actions);
    }
}
