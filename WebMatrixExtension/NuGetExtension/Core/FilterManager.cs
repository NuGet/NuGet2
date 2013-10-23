using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Extensibility;
using NuGet;
using NuGet.WebMatrix.Data;

namespace NuGet.WebMatrix
{
    internal class FilterManager
    {
        private ListViewFilter _installedFilter;
        private ListViewFilter _updatesFilter;
        private ListViewFilter _disabledFilter;

        private VirtualizingListViewFilter _allFilter;

        // Task scheduler for executing tasks on the primary thread
        private TaskScheduler _scheduler;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FilterManager"/> class.
        /// </summary>
        internal FilterManager(NuGetModel model, TaskScheduler scheduler, INuGetGalleryDescriptor descriptor)
        {
            Debug.Assert(model != null, "Model must not be null");
            Debug.Assert(scheduler != null, "TaskScheduler must not be null");
            this.Model = model;

            Filters = new ObservableCollection<IListViewFilter>();

            _installedFilter = new ListViewFilter(Resources.Filter_Installed, string.Format(Resources.Filter_InstalledDescription, descriptor.PackageKind), supportsPrerelease: false);
            _installedFilter.FilteredItems.SortDescriptions.Clear();

            _updatesFilter = new ListViewFilter(Resources.Filter_Updated, string.Format(Resources.Filter_UpdatedDescription, descriptor.PackageKind), supportsPrerelease: true);
            _updatesFilter.FilteredItems.SortDescriptions.Clear();

            _disabledFilter = new ListViewFilter(Resources.Filter_Disabled, string.Format(Resources.Filter_DisabledDescription, descriptor.PackageKind), supportsPrerelease: false);
            _disabledFilter.FilteredItems.SortDescriptions.Clear();

            _scheduler = scheduler;
        }

        internal ObservableCollection<IListViewFilter> Filters
        {
            get;
            private set;
        }

        public NuGetModel Model
        {
            get;
            private set;
        }

        internal ListViewFilter InstalledFilter
        {
            get
            {
                return _installedFilter;
            }
        }

        private VirtualizingListViewFilter AllFilter
        {
            get
            {
                return _allFilter;
            }
        }

        private ListViewFilter DisabledFilter
        {
            get
            {
                return _disabledFilter;
            }
        }

        public void UpdateFilters()
        {
            // populate the installed packages first, followed by disabled filter (other categories depend on this information)
            var populateInstalledTask = StartPopulatingInstalledAndDisabledFilters();
            populateInstalledTask.Wait();

            // Start populating the filters
            var populateFiltersTask = StartPopulatingAllAndUpdateFilters();
            populateFiltersTask.Wait();
        }

        private Task StartPopulatingInstalledAndDisabledFilters()
        {
            return Task.Factory.StartNew(() =>
            {
                // after we get the installed packages, use them to populate the 'installed' and 'disabled' filters
                var installedTask = Task.Factory
                    .StartNew<IEnumerable<PackageViewModel>>(GetInstalledPackages, TaskCreationOptions.AttachedToParent);

                installedTask.ContinueWith(
                        UpdateInstalledFilter, 
                        CancellationToken.None,
                        TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.OnlyOnRanToCompletion,
                        this._scheduler);

                installedTask.ContinueWith(
                        UpdateDisabledFilter,
                        CancellationToken.None,
                        TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.OnlyOnRanToCompletion,
                        this._scheduler);
            });
        }

        private Task StartPopulatingAllAndUpdateFilters()
        {
            // the child tasks here are created with AttachedToParent, the outer task will not
            // complete until all children have.
            return Task.Factory.StartNew(() =>
            {
                // each of these operations is a two-step process
                // 1. Get the packages
                // 2. Create view models and add to filters
                Task.Factory
                    .StartNew(UpdateTheAllFilter, TaskCreationOptions.AttachedToParent);

                Task.Factory
                    .StartNew<IEnumerable<IPackage>>(GetUpdatePackages, TaskCreationOptions.AttachedToParent)
                    .ContinueWith(
                        UpdateUpdatesFilter(),
                        CancellationToken.None,
                        TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.OnlyOnRanToCompletion,
                        this._scheduler);
            })
            .ContinueWith(AddFilters, this._scheduler);
        }

        private void AddFilters(Task task)
        {
            Filters.Clear();

            // always show the 'all' filter
            Filters.Add(_allFilter);

            if (_updatesFilter.Count > 0)
            {
                Filters.Add(_updatesFilter);
            }

            // always show the installed filter
            Filters.Add(_installedFilter);

            if (_disabledFilter.Count > 0)
            {
                Filters.Add(_disabledFilter);
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }

        private void UpdateTheAllFilter()
        {
            // updating the 'all' filter can take a matter of seconds -- so only update when it's timed out
            if (this._allFilter == null)
            {
                this._allFilter = new VirtualizingListViewFilter(
                    Resources.Filter_All,
                    Resources.Filter_AllDescription,
                    (p) => new PackageViewModel(this.Model, p as IPackage, PackageViewModelAction.InstallOrUninstall));

                this.AllFilter.Sort = (p) => p.DownloadCount;

                if (!String.IsNullOrWhiteSpace(this.Model.FeedSource.FilterTag))
                {
                    this.AllFilter.Filter = FilterManager.BuildTagFilterExpression(this.Model.FeedSource.FilterTag);
                }

                this.AllFilter.PackageManager = this.Model.PackageManager;
            }
        }

        private void UpdateInstalledFilter(Task<IEnumerable<PackageViewModel>> task)
        {
            var installed = task.Result;

            _installedFilter.Items.Clear();
            foreach (var viewModel in installed)
            {
                _installedFilter.Items.Add(new ListViewItemWrapper()
                {
                    Item = viewModel,
                    SearchText = viewModel.SearchText,
                    Name = viewModel.Name,
                });
            }
        }

        private void UpdateDisabledFilter(Task<IEnumerable<PackageViewModel>> task)
        {
            var installed = task.Result;

            _disabledFilter.Items.Clear();
            foreach (var viewModel in installed)
            {
                if (!viewModel.IsEnabled)
                {
                    _disabledFilter.Items.Add(new ListViewItemWrapper()
                    {
                        Item = viewModel,
                        SearchText = viewModel.SearchText,
                        Name = viewModel.Name,
                    });
                }
            }
        }

        private Action<Task<IEnumerable<IPackage>>> UpdateUpdatesFilter()
        {
            return (task) =>
            {
                if (task.Result == null)
                {
                    return;
                }

                _updatesFilter.Items.Clear();
                var packages = task.Result;
                foreach (var package in packages)
                {
                    var packageViewModel = new PackageViewModel(this.Model, package, PackageViewModelAction.Update);
                    _updatesFilter.Items.Add(new ListViewItemWrapper()
                    {
                        Item = packageViewModel,
                        SearchText = packageViewModel.SearchText,
                        Name = packageViewModel.Name,
                    });
                }
            };
        }

        /// <summary>
        /// Filters the given set of packages on the given tag. If the filter tag is null or whitespace, 
        /// all packages are returned. (Case-Insensitive)
        /// </summary>
        /// <param name="packages">Input packages</param>
        /// <param name="filterTag">The tag to filter</param>
        /// <returns>The set of packages containing the given tag tag</returns>
        /// <remarks>
        /// This implementation (IQueryable) is based on the nature of the NuGet remote package service.
        /// The filter clause applied here is pushed up to the server, which will dramatically increase the
        /// performance. If you tweak the body of this function, expect to find things that work locally,
        /// and fail when hitting the server-side.
        /// </remarks>
        public static IQueryable<IPackage> FilterOnTag(IQueryable<IPackage> packages, string filterTag)
        {
            Debug.Assert(packages != null, "Packages cannot be null");
            if (string.IsNullOrWhiteSpace(filterTag))
            {
                return packages;
            }

            // we're doing this padding because we don't get to call string.split
            // when this is running on a remote package list (inside the lambda)
            //
            // the tag value on the package is considered untrusted input, so we make sure
            // it has a leading and trailing space, as does the search text.
            // it's also possible that package.Tags might be delimited by spaces and commas
            // like: ' foo, bar '
            string loweredFilterTag = filterTag.ToLowerInvariant().Trim();
            string loweredPaddedFilterTag = " " + loweredFilterTag + " ";
            string loweredCommaPaddedFilterTag = " " + loweredFilterTag + ", ";
            return packages
                .Where(package => package.Tags != null)
                .Where(package => 
                    (" " + package.Tags.ToLower().Trim() + " ").Contains(loweredPaddedFilterTag)
                    || (" " + package.Tags.ToLower().Trim() + " ").Contains(loweredCommaPaddedFilterTag));
        }

        public static Expression<Func<IPackage, bool>> BuildTagFilterExpression(string filterTag)
        {
            // we're doing this padding because we don't get to call string.split
            // when this is running on a remote package list (inside the lambda)
            //
            // the tag value on the package is considered untrusted input, so we make sure
            // it has a leading and trailing space, as does the search text.
            // it's also possible that package.Tags might be delimited by spaces and commas
            // like: ' foo, bar '
            string loweredFilterTag = filterTag.ToLowerInvariant().Trim();
            string loweredPaddedFilterTag = " " + loweredFilterTag + " ";
            string loweredCommaPaddedFilterTag = " " + loweredFilterTag + ", ";

            return (package) => package.Tags != null &&
                    ((" " + package.Tags.ToLower().Trim() + " ").Contains(loweredPaddedFilterTag) || 
                    (" " + package.Tags.ToLower().Trim() + " ").Contains(loweredCommaPaddedFilterTag));
        }

        private IEnumerable<PackageViewModel> GetInstalledPackages()
        {
            var installed = FilterOnTag(this.Model.GetInstalledPackages().AsQueryable(), this.Model.FeedSource.FilterTag);

            //// From the installed tab, the only possible operation is uninstall and update is NOT supported
            //// For this reason, retrieving the remote package is not worthwhile
            //// Plus, Downloads count will not be shown in installed tab, which is fine
            //// Note that 'ALL' tab continues to support all applicable operations on a selected package including 'Update'

            IEnumerable<PackageViewModel> viewModels;
            viewModels = installed.Select((local) => new PackageViewModel(
                this.Model,
                local,
                true,
                PackageViewModelAction.InstallOrUninstall));

            return viewModels;
        }

        private IEnumerable<IPackage> GetUpdatePackages()
        {
            IEnumerable<IPackage> allPackages = this.Model.GetPackagesWithUpdates();
            return FilterOnTag(allPackages.AsQueryable(), this.Model.FeedSource.FilterTag);
        }
    }
}
