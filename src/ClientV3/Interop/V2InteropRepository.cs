using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio.Client.Interop
{
    public class V2InteropRepository : INuGetRepository
    {
        private readonly DataServicePackageRepository _repository;

        public V2InteropRepository(Uri targetSource)
        {
            _repository = new DataServicePackageRepository(targetSource);
        }

        public IPackageSearcher CreateSearcher(params Uri[] requiredResultTypes)
        {
            return new V2InteropSearcher(_repository);
        }
    }
}
