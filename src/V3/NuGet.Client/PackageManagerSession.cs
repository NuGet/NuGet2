using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NuGet.Client
{
    public abstract class PackageManagerSession
    {
        public abstract string Name { get; }
        public abstract NuGet.Client.PackageSource ActiveSource { get; }

        public abstract IEnumerable<NuGet.Client.PackageSource> GetAvailableSources();
        public abstract IEnumerable<FrameworkName> GetSupportedFrameworks();
        public abstract IPackageSearcher CreateSearcher();
        public abstract IInstalledPackageList GetInstalledPackageList();
        
        /// <summary>
        /// Changes the active package source to the one specified. WARNING: This call does
        /// NOT affect instances of <see cref="IPackageSearcher"/> retrieved via <see cref="CreateSearcher"/>.
        /// After calling this method, consumers will need to retrieve a new <see cref="IPackageSearcher"/>
        /// via <see cref="CreateSearcher"/> in order to search the new active source.
        /// </summary>
        /// <param name="newSourceName">The name of the source to change to</param>
        /// <exception cref="ArgumentException">The specified source does not exist in the available sources, as defined by <see cref="GetAvailableSources"/></exception>
        public abstract void ChangeActiveSource(string newSourceName);

        public virtual void ChangeActiveSource(NuGet.Client.PackageSource newSource)
        {
            ChangeActiveSource(newSource.Name);
        }

        public abstract IActionResolver CreateActionResolver();
        public abstract IActionExecutor CreateActionExecutor();
    }
}
