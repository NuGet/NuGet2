using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LinqExpression = System.Linq.Expressions.Expression;

namespace NuPack.Dialog.Providers {
    internal class UpdatePackagesProvider : OnlinePackagesProvider {
        private ResourceDictionary _resources;
        private object _mediumIconDataTemplate;

        public UpdatePackagesProvider(ResourceDictionary resources)
            : base(resources, false) {
            _resources = resources;
        }

        public override string Name {
            get {
                // TODO: Localize this string
                return "Updates";
            }
        }

        public override object MediumIconDataTemplate {
            get {
                if (_mediumIconDataTemplate == null) {
                    _mediumIconDataTemplate = _resources["OnlineUpdateTileTemplate"];
                }
                return _mediumIconDataTemplate;
            }
        }

        public override IQueryable<IPackage> GetQuery() {
            return GetUpdateQuery().AsQueryable();
        }

        private IEnumerable<IPackage> GetUpdateQuery() {
            List<IPackage> localPackages = ProjectManager.LocalRepository.GetPackages().ToList();

            if (!localPackages.Any()) {
                yield break;
            }

            // Filter packages by what we currently have installed
            var parameterExpression = LinqExpression.Parameter(typeof(IPackage));
            LinqExpression body = localPackages.Select(package => GetEqualsExpression(parameterExpression, package.Id))
                                               .Aggregate(LinqExpression.OrElse);

            var filterExpression = LinqExpression.Lambda<Func<IPackage, bool>>(body, parameterExpression);

            // These are the packages that we need to look at for potential updates.
            IDictionary<string, IPackage> remotePackages = PackageManager.SourceRepository.GetPackages()
                                                                         .Where(filterExpression)
                                                                         .ToList()
                                                                         .GroupBy(package => package.Id)
                                                                         .ToDictionary(package => package.Key, 
                                                                                       package => package.OrderByDescending(p => p.Version).First());

            foreach (var package in localPackages) {
                IPackage newestAvailablePackage;
                if (remotePackages.TryGetValue(package.Id, out newestAvailablePackage) && newestAvailablePackage.Version > package.Version) {
                    yield return newestAvailablePackage;
                }
            }
        }

        /// <summary>
        /// Builds the expression: package.Id.ToLower() == "somepackageid"
        /// </summary>
        private static LinqExpression GetEqualsExpression(LinqExpression parameterExpression, string packageId) {
            // package.Id
            var propertyExpression = LinqExpression.Property(parameterExpression, "Id");
            // .ToLower()
            var toLowerExpression = LinqExpression.Call(propertyExpression, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            // == localPackage.Id
            return LinqExpression.Equal(toLowerExpression, LinqExpression.Constant(packageId.ToLower()));
        }

    }
}