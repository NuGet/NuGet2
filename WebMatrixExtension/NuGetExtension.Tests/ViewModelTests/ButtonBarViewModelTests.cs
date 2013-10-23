using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Extensibility;
using NuGet.WebMatrix.Data;
using NuGet.WebMatrix.Tests.Utilities;
using System.Windows.Input;
using Xunit;
using Moq;

namespace NuGet.WebMatrix.Tests.ViewModelTests
{
    
    public class ButtonBarViewModelTests
    {
        public ButtonBarViewModelTests()
        {
            NuGetModel.ClearCache();

            this.CloseCommand = new Mock<ICommand>().Object;

            this.Descriptor = new Mock<INuGetGalleryDescriptor>();

            // makes sure tests report a failure if an exception is caught by the view model
            this.Host = new Mock<IWebMatrixHost>();
            this.Host
                .Setup(host => host.ShowExceptionMessage(null, null, null))
                .Throws(new Exception("Test tried to report and error to the user"));

            this.PackageManager = new InMemoryPackageManager();

            var feedSource = new FeedSource("http://oo.com", "test source");
            var feedSourceStore = new InMemoryFeedSourceStore(feedSource);
            this.PackageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource, feedSourceStore));

            this.ReadonlyDestination = Path.GetFullPath(@".\DO_NOT_WRITE_HERE\");
        }

        /// <summary>
        /// When nothing is selected in the view, then only the cancel button should be shown
        /// </summary>
        [Fact]
        public void NoPackageSelected()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                Assert.Equal(new ButtonViewModel[] { buttonBarViewModel.CloseButton }, buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// When a package that is not installed is selected, the install and cancel buttons should be shown
        /// </summary>
        [Fact]
        public void InstallablePackageSelected()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("installable");
                this.PackageManager.RemotePackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, package.Id);

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.InstallButton, buttonBarViewModel.CloseButton }, 
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// When a package that is installed is selected, the uninstall and cancel buttons should be shown
        /// on any filter
        /// </summary>
        [Fact]
        public void InstalledPackageSelectedInstalledFilter()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("installed");
                this.PackageManager.InstalledPackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, package.Id);

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.UninstallButton, buttonBarViewModel.CloseButton },
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// When a package that is installed is selected, the uninstall and cancel buttons should be shown
        /// on any filter
        /// </summary>
        [Fact]
        public void InstalledPackageSelectedAllFilter()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("installed");
                this.PackageManager.RemotePackages.Add(package);
                this.PackageManager.InstalledPackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, package.Id);

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.UninstallButton, buttonBarViewModel.CloseButton },
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// When a package has updates available, it should show the 'update' 'uninstall' and 'cancel' buttons,
        /// regardless of which filter is selected.
        /// </summary>
        [Fact]
        public void UpdatablePackageSelectedAllFilter()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var installedPackage = PackageFactory.Create("update", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installedPackage);

                var remotePackage = PackageFactory.Create("update", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remotePackage);
                
                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, installedPackage.Id);

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.UpdateButton, buttonBarViewModel.UninstallButton, buttonBarViewModel.CloseButton },
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// When a package has updates available, it should show the 'update' 'uninstall' and 'cancel' buttons,
        /// regardless of which filter is selected.
        /// </summary>
        [Fact]
        public void UpdatablePackageSelectedUpdatesFilter()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var installedPackage = PackageFactory.Create("update", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installedPackage);

                var remotePackage = PackageFactory.Create("update", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remotePackage);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Updated, installedPackage.Id);

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.UpdateButton, buttonBarViewModel.UninstallButton, buttonBarViewModel.CloseButton },
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// When more than 1 package has updates available, it should show the 'updateAll' 'update' 'uninstall' and 'cancel' buttons,
        /// only when the updates filter is selected
        /// </summary>
        [Fact]
        public void UpdateAllShownUpdatablePackageSelectedUpdatesFilter()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var installedPackage1 = PackageFactory.Create("update1", new Version(1, 0));
                var installedPackage2 = PackageFactory.Create("update2", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installedPackage1);
                this.PackageManager.InstalledPackages.Add(installedPackage2);

                var remotePackage1 = PackageFactory.Create("update1", new Version(2, 0));
                var remotePackage2 = PackageFactory.Create("update2", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remotePackage1);
                this.PackageManager.RemotePackages.Add(remotePackage2);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Updated, installedPackage1.Id);

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.UpdateAllButton, buttonBarViewModel.UpdateButton, buttonBarViewModel.UninstallButton, buttonBarViewModel.CloseButton },
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// When All filter is selected, 'updateAll' should not be shown
        /// even when more than 1 package has updates available
        /// </summary>
        [Fact]
        public void UpdateAllNotShownIfAllFilterIsSelected()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var installedPackage1 = PackageFactory.Create("update1", new Version(1, 0));
                var installedPackage2 = PackageFactory.Create("update2", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installedPackage1);
                this.PackageManager.InstalledPackages.Add(installedPackage2);

                var remotePackage1 = PackageFactory.Create("update1", new Version(2, 0));
                var remotePackage2 = PackageFactory.Create("update2", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remotePackage1);
                this.PackageManager.RemotePackages.Add(remotePackage2);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, installedPackage1.Id);
                Assert.False(buttonBarViewModel.ActionButtons.Contains(buttonBarViewModel.UpdateAllButton));
                Assert.True(buttonBarViewModel.ActionButtons.Contains(buttonBarViewModel.UpdateButton));
            }
        }

        /// <summary>
        /// When Installed filter is selected, 'updateAll' should not be shown
        /// even when more than 1 package has updates available
        /// </summary>
        [Fact]
        public void UpdateAllNotShownIfInstalledFilterIsSelected()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var installedPackage1 = PackageFactory.Create("update1", new Version(1, 0));
                var installedPackage2 = PackageFactory.Create("update2", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installedPackage1);
                this.PackageManager.InstalledPackages.Add(installedPackage2);

                var remotePackage1 = PackageFactory.Create("update1", new Version(2, 0));
                var remotePackage2 = PackageFactory.Create("update2", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remotePackage1);
                this.PackageManager.RemotePackages.Add(remotePackage2);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, installedPackage1.Id);
                Assert.False(buttonBarViewModel.ActionButtons.Contains(buttonBarViewModel.UpdateAllButton));
                Assert.True(buttonBarViewModel.ActionButtons.Contains(buttonBarViewModel.UpdateButton));
            }
        }

        /// <summary>
        /// When only 1 package has updates available, 'updateAll' button should not be shown
        /// even when the updates filter is selected
        /// </summary>
        [Fact]
        public void UpdateAllNotShownWhenOnly1PackageHasUpdates()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var installedPackage1 = PackageFactory.Create("update1", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installedPackage1);

                var remotePackage1 = PackageFactory.Create("update1", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remotePackage1);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Updated, installedPackage1.Id);

                Assert.False(buttonBarViewModel.ActionButtons.Contains(buttonBarViewModel.UpdateAllButton));
                Assert.True(buttonBarViewModel.ActionButtons.Contains(buttonBarViewModel.UpdateButton));
            }
        }

        /// <summary>
        /// A disablable package should have the enable option as well as the others
        /// </summary>
        [Fact]
        public void DisablablePackageSelected()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                this.PackageManager.SupportsEnableDisable = true;

                var installedPackage = PackageFactory.Create("disable", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installedPackage);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, installedPackage.Id);

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.DisableButton, buttonBarViewModel.UninstallButton, buttonBarViewModel.CloseButton },
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// An enablable package should have the disable option as well as the others
        /// </summary>
        [Fact]
        public void EnablablePackageSelected()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                this.PackageManager.SupportsEnableDisable = true;

                var installedPackage = PackageFactory.Create("enable", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installedPackage);
                this.PackageManager.DisabledPackages.Add(installedPackage);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, installedPackage.Id);

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.EnableButton, buttonBarViewModel.UninstallButton, buttonBarViewModel.CloseButton },
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// When the 'details' page is shown, the 'yes' and 'no' buttons should be shown
        /// </summary>
        [Fact]
        public void DetailsPageButtons()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("installable");
                this.PackageManager.RemotePackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, package.Id);

                Assert.True(buttonBarViewModel.InstallButton.Command.CanExecute(PackageViewModelAction.InstallOrUninstall));
                thread.Invoke(() => { buttonBarViewModel.InstallButton.Command.Execute(PackageViewModelAction.InstallOrUninstall); });
                buttonBarViewModel.NuGetViewModel.WaitUntilComplete();

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.YesButton, buttonBarViewModel.NoButton },
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// When the 'license' page is shown, the 'accept' and 'decline' buttons should be shown
        /// </summary>
        [Fact]
        public void LicensePageButtons()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("installable");
                this.PackageManager.RemotePackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, package.Id);

                Assert.True(buttonBarViewModel.InstallButton.Command.CanExecute(PackageViewModelAction.InstallOrUninstall));
                thread.Invoke(() => { buttonBarViewModel.InstallButton.Command.Execute(PackageViewModelAction.InstallOrUninstall); });
                buttonBarViewModel.NuGetViewModel.WaitUntilComplete();

                Assert.True(buttonBarViewModel.YesButton.Command.CanExecute(null));
                thread.Invoke(() => { buttonBarViewModel.YesButton.Command.Execute(null); });
                buttonBarViewModel.NuGetViewModel.WaitUntilComplete();

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.AcceptButton, buttonBarViewModel.DeclineButton },
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// When the 'uninstall' page is shown, the 'yes' and 'no' buttons should be shown
        /// </summary>
        [Fact]
        public void UninstallPageButtons()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("installed");
                this.PackageManager.InstalledPackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, package.Id);

                Assert.True(buttonBarViewModel.UninstallButton.Command.CanExecute(null));
                thread.Invoke(() => { buttonBarViewModel.UninstallButton.Command.Execute(null); });
                buttonBarViewModel.NuGetViewModel.WaitUntilComplete();

                Assert.Equal(
                    new ButtonViewModel[] { buttonBarViewModel.YesButton, buttonBarViewModel.NoButton },
                    buttonBarViewModel.ActionButtons);
            }
        }

        /// <summary>
        /// Creates a NuGetViewModel and ButtonBarViewModel for testing
        /// </summary>
        private ButtonBarViewModel CreateViewModel(TemporaryDispatcherThread thread)
        {
            ButtonBarViewModel buttonBarViewModel = null;
            NuGetViewModel nuGetViewModel = null;
            thread.Invoke(() =>
            {
                nuGetViewModel = new NuGetViewModel(
                    this.Descriptor.Object,
                    this.Host.Object,
                    this.PackageSourcesViewModel,
                    (uri, site) => this.PackageManager,
                    this.ReadonlyDestination,
                    thread.Scheduler);

                buttonBarViewModel = new ButtonBarViewModel(nuGetViewModel, this.CloseCommand);
            });
            nuGetViewModel.WaitUntilComplete();

            return buttonBarViewModel;
        }

        private void SelectPackage(TemporaryDispatcherThread thread, NuGetViewModel viewModel, string filterName, string packageId)
        {
            this.SelectFilter(thread, viewModel, filterName);

            object packageItem;
            if (viewModel.SelectedFilter is Data.VirtualizingListViewFilter)
            {
                packageItem = viewModel.SelectedFilter.FilteredItems
                    .OfType<Data.VirtualizingListEntry>()
                    .Where(entry => entry.Item != null && ((PackageViewModel)entry.Item).Id == packageId)
                    .FirstOrDefault();
            }
            else
            {
                packageItem = viewModel.SelectedFilter.FilteredItems
                    .OfType<ListViewItemWrapper>()
                    .Where(item => item.Item != null && ((PackageViewModel)item.Item).Id == packageId)
                    .FirstOrDefault();
            }

            Assert.NotNull(packageItem);

            thread.Invoke(() =>
            {
                viewModel.SelectedItem = packageItem;
            });

            Assert.Equal(packageItem, viewModel.SelectedItem);
            Assert.NotNull(viewModel.SelectedPackage);
            Assert.Equal<string>(packageId, viewModel.SelectedPackage.Id);
        }

        private void SelectFilter(TemporaryDispatcherThread thread, NuGetViewModel viewModel, string filterName)
        {
            var filterToSelect = viewModel.Filters.Where(f => f.Name == filterName).FirstOrDefault();
            Assert.NotNull(filterToSelect);

            thread.Invoke(() =>
            {
                viewModel.SelectedFilter = filterToSelect;
            });
            Assert.Equal(filterName, viewModel.SelectedFilter.Name);
        }

        private ICommand CloseCommand
        {
            get;
            set;
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

        private InMemoryPackageManager PackageManager
        {
            get;
            set;
        }

        private PackageSourcesViewModel PackageSourcesViewModel
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
