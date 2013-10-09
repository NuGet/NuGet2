using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Extensibility;
using NuGet.WebMatrix.Tests.Utilities;
using NuGet.WebMatrix.Data;
using Xunit;
using Moq;

namespace NuGet.WebMatrix.Tests.ViewModelTests
{
    
    public class NuGetViewModelTests
    {
        public NuGetViewModelTests()
        {
            NuGetModel.ClearCache();

            this.Descriptor = new Mock<INuGetGalleryDescriptor>();

            // makes sure tests report a failure if an exception is caught by the view model
            this.Host = new Mock<IWebMatrixHost>();
            this.Host
                .Setup(host => host.ShowExceptionMessage(null, null, null))
                .Throws(new Exception("Test tried to report and error to the user"));

            this.ReadonlyDestination = Path.GetFullPath(@".\DO_NOT_WRITE_HERE\");
        }

        /// <summary>
        /// Tests creation of the view model based on a mock
        /// </summary>
        [Fact]
        public void CreateViewModel()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var feedSource = new FeedSource("http://oo.com", "test source");
                var feedSourceStore = new InMemoryFeedSourceStore(feedSource);
                var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource, feedSourceStore));

                var packageManager = new InMemoryPackageManager();

                NuGetViewModel viewModel = null;
                thread.Dispatcher.Invoke((Action)(() =>
                {
                    viewModel = new NuGetViewModel(
                        this.Descriptor.Object,
                        this.Host.Object,
                        packageSourcesViewModel,
                        (uri, site) => packageManager,
                        this.ReadonlyDestination,
                        thread.Scheduler);
                }));
                viewModel.WaitUntilComplete();

                Assert.False(viewModel.Loading, "The view model should be done loading");
                Assert.Equal<FeedSource>(feedSource, viewModel.SelectedFeedSource);
            }
        }

        /// <summary>
        /// The selected feed source is maintained by the PackageSourcesViewModel, and should be the source
        /// chosen by ViewModel when loaded
        /// </summary>
        [Fact]
        public void SelectedFeedSourceUsed()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var feedSource = new FeedSource("http://oo.com", "test source");
                var sources = new FeedSource[] { new FeedSource("http://fake.com", "this shouldn't be selected"), feedSource };
                var feedSourceStore = new InMemoryFeedSourceStore(feedSource, sources);

                var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource, feedSourceStore));

                var packageManager = new InMemoryPackageManager();

                NuGetViewModel viewModel = null;
                thread.Dispatcher.Invoke((Action)(() =>
                {
                    viewModel = new NuGetViewModel(
                        this.Descriptor.Object,
                        this.Host.Object,
                        packageSourcesViewModel,
                        (uri, site) => packageManager,
                        this.ReadonlyDestination,
                        thread.Scheduler);
                }));

                viewModel.WaitUntilComplete();

                Assert.Equal<FeedSource>(packageSourcesViewModel.ActiveFeedSource, viewModel.SelectedFeedSource);
            }
        }

        [Fact]
        public void FirstFilterSelected()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var feedSource = new FeedSource("http://oo.com", "test source");
                var feedSourceStore = new InMemoryFeedSourceStore(feedSource);

                var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource, feedSourceStore));

                var packageManager = new InMemoryPackageManager();

                NuGetViewModel viewModel = null;
                thread.Dispatcher.Invoke((Action)(() => {
                    viewModel = new NuGetViewModel(
                        this.Descriptor.Object,
                        this.Host.Object,
                        packageSourcesViewModel,
                        (uri, site) => packageManager,
                        this.ReadonlyDestination,
                        thread.Scheduler);
                }));

                viewModel.WaitUntilComplete();

                Assert.Equal<IListViewFilter>(viewModel.Filters[0], viewModel.SelectedFilter);
            }
        }

        /// <summary>
        /// Tests that we can safely change the selected feed source. 
        /// </summary>
        /// <remarks>
        /// This test doesn't actually verify that the data is different
        /// </remarks>
        [Fact]
        public void SelectAnotherSource()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var feedSource1 = new FeedSource("http://1.com", "source1");
                var feedSource2 = new FeedSource("http://2.com", "source2");
                var sources = new FeedSource[] { feedSource1, feedSource2 };
                var feedSourceStore = new InMemoryFeedSourceStore(feedSource1, sources);

                var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource1, feedSourceStore));

                var packageManager = new InMemoryPackageManager();

                NuGetViewModel viewModel = null;
                thread.Dispatcher.Invoke((Action)(() =>
                {
                    viewModel = new NuGetViewModel(
                        this.Descriptor.Object,
                        this.Host.Object,
                        packageSourcesViewModel,
                        (uri, site) => packageManager,
                        this.ReadonlyDestination,
                        thread.Scheduler);
                }));
                viewModel.WaitUntilComplete();

                Assert.Equal<FeedSource>(feedSource1, viewModel.SelectedFeedSource);

                thread.Dispatcher.Invoke((Action)(() =>
                {
                    viewModel.SelectedFeedSourceItem = feedSource2;
                }));
                viewModel.WaitUntilComplete();

                Assert.Equal<FeedSource>(feedSource2, viewModel.SelectedFeedSource);
            }
        }

        /// <summary>
        /// Selecting another feed source should hide the details page
        /// </summary>
        [Fact]
        public void SelectAnotherSourceHidesDetails()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var feedSource1 = new FeedSource("http://1.com", "source1");
                var feedSource2 = new FeedSource("http://2.com", "source2");
                var sources = new FeedSource[] { feedSource1, feedSource2 };
                var feedSourceStore = new InMemoryFeedSourceStore(feedSource1, sources);

                var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource1, feedSourceStore));

                var packageManager = new InMemoryPackageManager();
                packageManager.RemotePackages.Add(PackageFactory.Create("select me"));

                NuGetViewModel viewModel = null;
                thread.Dispatcher.Invoke((Action)(() =>
                {
                    viewModel = new NuGetViewModel(
                        this.Descriptor.Object,
                        this.Host.Object,
                        packageSourcesViewModel,
                        (uri, site) => packageManager,
                        this.ReadonlyDestination,
                        thread.Scheduler);
                }));
                viewModel.WaitUntilComplete();

                Assert.Equal<FeedSource>(feedSource1, viewModel.SelectedFeedSource);

                var firstFilter = viewModel.Filters[0];
                var itemToSelect = firstFilter.FilteredItems.OfType<object>().First();
                thread.Invoke(() =>
                {
                    viewModel.SelectedItem = itemToSelect;

                    // show the details page
                    viewModel.IsDetailsPaneVisible = true;
                });

                thread.Dispatcher.Invoke((Action)(() =>
                {
                    viewModel.SelectedFeedSourceItem = feedSource2;
                }));
                viewModel.WaitUntilComplete();

                Assert.Equal<FeedSource>(feedSource2, viewModel.SelectedFeedSource);
                Assert.False(viewModel.IsDetailsPaneVisible, "The details pane should be hidden now.");
            }
        }

        [Fact]
        public void SelectAnotherFilter()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var feedSource1 = new FeedSource("http://1.com", "source1");
                var feedSource2 = new FeedSource("http://2.com", "source2");
                var sources = new FeedSource[] { feedSource1, feedSource2 };
                var feedSourceStore = new InMemoryFeedSourceStore(feedSource1, sources);

                var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource1, feedSourceStore));

                var packageManager = new InMemoryPackageManager();

                NuGetViewModel viewModel = null;
                thread.Dispatcher.Invoke((Action)(() =>
                {
                    viewModel = new NuGetViewModel(
                        this.Descriptor.Object,
                        this.Host.Object,
                        packageSourcesViewModel,
                        (uri, site) => packageManager,
                        this.ReadonlyDestination,
                        thread.Scheduler);
                }));
                viewModel.WaitUntilComplete();

                Assert.True(viewModel.Filters.Count >= 2, "This test needs at least 2 filters");
                Assert.Equal<IListViewFilter>(viewModel.Filters[0], viewModel.SelectedFilter);

                viewModel.SelectedFilter = viewModel.Filters[1];
                Assert.Equal<IListViewFilter>(viewModel.Filters[1], viewModel.SelectedFilter);
            }
        }

        /// <summary>
        /// Selecting another filter when the details page is open should hide it
        /// </summary>
        [Fact]
        public void SelectAnotherFilterHidesDetails()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var feedSource1 = new FeedSource("http://1.com", "source1");
                var feedSource2 = new FeedSource("http://2.com", "source2");
                var sources = new FeedSource[] { feedSource1, feedSource2 };
                var feedSourceStore = new InMemoryFeedSourceStore(feedSource1, sources);

                var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource1, feedSourceStore));

                var packageManager = new InMemoryPackageManager();
                packageManager.RemotePackages.Add(PackageFactory.Create("select me"));

                NuGetViewModel viewModel = null;
                thread.Dispatcher.Invoke((Action)(() =>
                {
                    viewModel = new NuGetViewModel(
                        this.Descriptor.Object,
                        this.Host.Object,
                        packageSourcesViewModel,
                        (uri, site) => packageManager,
                        this.ReadonlyDestination,
                        thread.Scheduler);
                }));
                viewModel.WaitUntilComplete();

                var firstFilter = viewModel.Filters[0];
                Assert.True(viewModel.Filters.Count >= 2, "This test needs at least 2 filters");
                Assert.Equal<IListViewFilter>(firstFilter, viewModel.SelectedFilter);

                var itemToSelect = firstFilter.FilteredItems.OfType<object>().First();
                thread.Invoke(() => { 
                    viewModel.SelectedItem = itemToSelect;

                    // show the details page
                    viewModel.IsDetailsPaneVisible = true;
                });

                viewModel.SelectedFilter = viewModel.Filters[1];
                viewModel.WaitUntilComplete();
                Assert.Equal<IListViewFilter>(viewModel.Filters[1], viewModel.SelectedFilter);
                Assert.False(viewModel.IsDetailsPaneVisible, "The details page should be hidden");
            }
        }

        /// <summary>
        /// Load a source a package, one of the filters should have a package
        /// </summary>
        [Fact]
        public void SourceWithPackages()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var feedSource = new FeedSource("http://1.com", "source1");
                var feedSourceStore = new InMemoryFeedSourceStore(feedSource);

                var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource, feedSourceStore));

                var packageManager = new InMemoryPackageManager();
                packageManager.RemotePackages.Add(PackageFactory.Create("package1"));

                NuGetViewModel viewModel = null;
                thread.Dispatcher.Invoke((Action)(() =>
                {
                    viewModel = new NuGetViewModel(
                        this.Descriptor.Object,
                        this.Host.Object,
                        packageSourcesViewModel,
                        (uri, site) => packageManager,
                        this.ReadonlyDestination,
                        thread.Scheduler);
                }));
                viewModel.WaitUntilComplete();

                Assert.True(viewModel.Filters.Any(f => f.Count > 0), "One of the filters should have some items");
            }
        }

        /// <summary>
        /// Load a source with packages, select a filter and package
        /// </summary>
        /// <remarks>
        /// A package will never be selected by default, that's a function of the UI
        /// </remarks>
        [Fact]
        public void SelectAPackage()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var feedSource = new FeedSource("http://1.com", "source1");
                var feedSourceStore = new InMemoryFeedSourceStore(feedSource);

                var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource, feedSourceStore));

                var packageManager = new InMemoryPackageManager();
                packageManager.RemotePackages.Add(PackageFactory.Create("package1"));

                NuGetViewModel viewModel = null;
                thread.Invoke(() =>
                {
                    viewModel = new NuGetViewModel(
                        this.Descriptor.Object,
                        this.Host.Object,
                        packageSourcesViewModel,
                        (uri, site) => packageManager,
                        this.ReadonlyDestination,
                        thread.Scheduler);
                });
                viewModel.WaitUntilComplete();

                var filterToSelect = viewModel.Filters.First(f => f.Count > 0);
                if (viewModel.SelectedFilter != filterToSelect)
                {
                    viewModel.SelectedFilter = filterToSelect;
                }

                Assert.Equal<IListViewFilter>(filterToSelect, viewModel.SelectedFilter);
                Assert.Null(viewModel.SelectedItem);

                var itemToSelect = filterToSelect.FilteredItems.OfType<object>().First();
                thread.Invoke(() => { viewModel.SelectedItem = itemToSelect; });
                Assert.Equal(itemToSelect, viewModel.SelectedItem);
                Assert.NotNull(itemToSelect);
            }
        }

        /// <summary>
        /// Validates change of prerelease filter from default to IncludePrerelease
        /// </summary>
        [Fact]
        public void SelectPrereleaseFilter()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var feedSource = new FeedSource("http://oo.com", "test source");
                var feedSourceStore = new InMemoryFeedSourceStore(feedSource);
                var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource, feedSourceStore));

                var packageManager = new InMemoryPackageManager();

                NuGetViewModel viewModel = null;
                thread.Dispatcher.Invoke((Action)(() =>
                {
                    viewModel = new NuGetViewModel(
                        this.Descriptor.Object,
                        this.Host.Object,
                        packageSourcesViewModel,
                        (uri, site) => packageManager,
                        this.ReadonlyDestination,
                        thread.Scheduler);
                }));
                viewModel.WaitUntilComplete();

                Assert.Equal<string>(Resources.Prerelease_Filter_StableOnly, viewModel.SelectedPrereleaseFilter);

                viewModel.SelectedPrereleaseFilter = Resources.Prerelease_Filter_IncludePrerelease;
                Assert.Equal<string>(Resources.Prerelease_Filter_IncludePrerelease, viewModel.SelectedPrereleaseFilter);
            }
        }

        private Mock<INuGetGalleryDescriptor> Descriptor
        {
            get;
            set;
        }

        private Mock<IWebMatrixHost> Host
        {
            get;
            set;
        }

        private string ReadonlyDestination
        {
            get;
            set;
        }
    }
}
