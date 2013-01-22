namespace NuGet
{
    using System;
    using System.Linq;

    public abstract class PackageRepositoryBase : IPackageRepository
    {
        private PackageSaveProperties _packageSave;

        protected PackageRepositoryBase()
        {
            _packageSave = PackageSaveProperties.Nupkg;
        }

        public abstract string Source { get; }


        public PackageSaveProperties PackageSave 
        {
            get { return _packageSave; }
            set
            {
                if (value == PackageSaveProperties.None)
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
