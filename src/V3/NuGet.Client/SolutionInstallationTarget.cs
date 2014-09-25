using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Client
{
    // Right now this is mostly just a marker class, but I do have some ideas for things to put here -anurse.
    public abstract class SolutionInstallationTarget : InstallationTarget
    {
        public override bool IsSolution
        {
            get { return true; }
        }
    }
}
