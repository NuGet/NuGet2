using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.VisualStudio.ClientV3;

namespace NuGet.VisualStudio
{
    public interface IVsRepositoryManager
    {
        PackageSource ActiveSource { get; }
        INuGetRepository GetActiveRepository();
        void ChangeActiveSource(string name);
        IEnumerable<PackageSource> GetEnabledSources();
    }
    
    [Export(typeof(IVsRepositoryManager))]
    public class VsRepositoryManager : IVsRepositoryManager
    {
        private readonly INuGetRepositoryFactory _factory;
        private readonly IVsPackageSourceProvider _sourceProvider;
        
        public PackageSource ActiveSource
        {
            get { return _sourceProvider.ActivePackageSource; }
        }

        public INuGetRepository GetActiveRepository()
        {
            return _factory.Create(_sourceProvider.ActivePackageSource);
        }

        [ImportingConstructor]
        public VsRepositoryManager(IPackageRepositoryFactory factory, IVsPackageSourceProvider sourceProvider)
            : this(new NuGetRepositoryFactory(factory), sourceProvider) { }
        
        public VsRepositoryManager(INuGetRepositoryFactory factory, IVsPackageSourceProvider sourceProvider)
        {
            _factory = factory;
            _sourceProvider = sourceProvider;
        }
        public void ChangeActiveSource(string name)
        {
            _sourceProvider.ActivePackageSource = _sourceProvider.GetEnabledPackageSourcesWithAggregate()
                .FirstOrDefault(ps => ps.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<PackageSource> GetEnabledSources()
        {
            // TODO: Remove aggregate!
            return _sourceProvider.GetEnabledPackageSourcesWithAggregate();
        }
    }
}
