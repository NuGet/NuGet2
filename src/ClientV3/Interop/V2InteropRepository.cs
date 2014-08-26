using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio.ClientV3.Interop
{
    public class V2InteropRepository : INuGetRepository
    {
        private readonly IPackageRepository _repository;

        public V2InteropRepository(IPackageRepository repository)
        {
            _repository = repository;
        }

        public IPackageSearcher CreateSearcher()
        {
            return new V2InteropSearcher(_repository);
        }
    }
}
