using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio.ClientV3
{
    public interface INuGetRepository
    {
        IPackageSearcher CreateSearcher();
    }
}
