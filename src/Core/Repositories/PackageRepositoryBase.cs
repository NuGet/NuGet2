using System;
using System.Linq;

namespace NuGet
{
    public abstract class PackageRepositoryBase : IPackageRepository
    {
        private PackageFileTypes _packageSave;

        protected PackageRepositoryBase()
        {
            _packageSave = PackageFileTypes.Nupkg;
        }

        public abstract string Source { get; }


        public PackageFileTypes FilesToSave 
        {
            get { return _packageSave; }
            set
            {
                if (value == PackageFileTypes.None)
                {
                    throw new ArgumentException("PackageSave cannot be set to None");
                }

                _packageSave = value;
            }
        }

        public abstract IQueryable<IPackage> GetPackages();

        public abstract bool SupportsPrereleasePackages { get; }

        public virtual void AddPackage(IPackage package)
        {
            throw new NotSupportedException();
        }

        public virtual void RemovePackage(IPackage package)
        {
            throw new NotSupportedException();
        }
    }
}
