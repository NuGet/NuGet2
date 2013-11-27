using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.WebMatrix.Extensibility;
using NuGet.WebMatrix.Data;
using NuGet.WebMatrix.Tests.Utilities;
using Xunit;
using Moq;

namespace NuGet.WebMatrix.Tests.ViewModelTests
{
    
    public class EndToEndViewModelTests
    {
        public EndToEndViewModelTests()
        {
            NuGetModel.ClearCache();

            this.CloseCommand = new Mock<ICommand>().Object;

            this.Descriptor = new Mock<INuGetGalleryDescriptor>();

            // makes sure tests report a failure if an exception is caught by the view model
            this.Host = new Mock<IWebMatrixHost>();
            this.Host
                .Setup(host => host.ShowExceptionMessage(null, null, null))
                .Throws(new Exception("Test tried to report an error to the user"));

            this.PackageManager = new InMemoryPackageManager();

            var feedSource = new FeedSource("http://oo.com", "test source");
            var feedSourceStore = new InMemoryFeedSourceStore(feedSource);
            this.PackageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource, feedSourceStore));

            this.ReadonlyDestination = Path.GetFullPath(@".\DO_NOT_WRITE_HERE\");
        }

        /// <summary>
        /// Cancelling an install from the details page (clicking no), should take you back to the list
        /// </summary>
        public void CancelInstall()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("install");
                this.PackageManager.RemotePackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, package.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.InstallButton, PackageViewModelAction.InstallOrUninstall);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsDetailsPaneVisible, "The details page should be shown.");
                Assert.Equal<PackageViewModelAction>(PackageViewModelAction.InstallOrUninstall, buttonBarViewModel.NuGetViewModel.PackageAction);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.NoButton);

                Assert.False(buttonBarViewModel.NuGetViewModel.IsDetailsPaneVisible, "The details page should not be shown.");
                Assert.False(buttonBarViewModel.NuGetViewModel.IsLicensePageVisible, "The license page should not be shown.");

                Assert.False(this.PackageManager.InstalledPackages.Any(), "Nothing should be installed.");
            }
        }

        /// <summary>
        /// Cancelling an install from the license page (clicking no), should take you back to the list
        /// </summary>
        [Fact]
        public void DeclineEula()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("install");
                this.PackageManager.RemotePackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, package.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.InstallButton, PackageViewModelAction.InstallOrUninstall);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsDetailsPaneVisible, "The details page should be shown.");
                Assert.Equal<PackageViewModelAction>(PackageViewModelAction.InstallOrUninstall, buttonBarViewModel.NuGetViewModel.PackageAction);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.YesButton);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsLicensePageVisible, "The license page should be shown.");

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.DeclineButton);

                Assert.False(buttonBarViewModel.NuGetViewModel.IsDetailsPaneVisible, "The details page should not be shown.");
                Assert.False(buttonBarViewModel.NuGetViewModel.IsLicensePageVisible, "The license page should not be shown.");

                Assert.False(this.PackageManager.InstalledPackages.Any(), "Nothing should be installed.");
            }
        }

        /// <summary>
        /// Select a package from the 'all' view and walk through the installer
        /// </summary>
        [Fact]
        public void InstallPackage()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("install");
                this.PackageManager.RemotePackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, package.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.InstallButton, PackageViewModelAction.InstallOrUninstall);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsDetailsPaneVisible, "The details page should be shown.");
                Assert.Equal<PackageViewModelAction>(PackageViewModelAction.InstallOrUninstall, buttonBarViewModel.NuGetViewModel.PackageAction);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.YesButton);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsLicensePageVisible, "The license page should be shown.");

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.AcceptButton);
                
                var installedPackage = this.PackageManager.InstalledPackages.FirstOrDefault();
                Assert.Equal(package, installedPackage);
            }
        }

        /// <summary>
        /// Select a package from the 'installed' view and walk through the uninstaller
        /// </summary>
        [Fact]
        public void UninstallPackageFromInstalled()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("uninstall");
                this.PackageManager.InstalledPackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, package.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.UninstallButton);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsUninstallPageVisible, "The uninstall page should be shown.");

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.YesButton);

                Assert.False(this.PackageManager.InstalledPackages.Any(), "The package should be uninstalled");
            }
        }

        /// <summary>
        /// Select a package from the 'all' view and walk through the uninstaller
        /// </summary>
        [Fact]
        public void UninstallPackageFromAll()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var package = PackageFactory.Create("uninstall");
                this.PackageManager.InstalledPackages.Add(package);
                this.PackageManager.RemotePackages.Add(package);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, package.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.UninstallButton);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsUninstallPageVisible, "The uninstall page should be shown.");

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.YesButton);

                Assert.False(this.PackageManager.InstalledPackages.Any(), "The package should be uninstalled");
            }
        }

        /// <summary>
        /// Select a package from the 'all' view and walk through the updater
        /// </summary>
        [Fact]
        public void UpdatePackageFromAll()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var installed = PackageFactory.Create("update", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                var remote = PackageFactory.Create("update", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remote);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All, installed.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.UpdateButton, PackageViewModelAction.Update);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsDetailsPaneVisible, "The details page should be shown.");
                Assert.Equal<PackageViewModelAction>(PackageViewModelAction.Update, buttonBarViewModel.NuGetViewModel.PackageAction);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.YesButton);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsLicensePageVisible, "The license page should be shown.");

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.AcceptButton);

                var installedPackage = this.PackageManager.InstalledPackages.FirstOrDefault();
                Assert.Equal(remote, installedPackage);
            }
        }

        /// <summary>
        /// Select a package from the 'updates' view and walk through the updater
        /// </summary>
        [Fact]
        public void UpdatePackageFromUpdates()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var installed = PackageFactory.Create("update", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                var remote = PackageFactory.Create("update", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remote);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Updated, installed.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.UpdateButton, PackageViewModelAction.Update);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsDetailsPaneVisible, "The details page should be shown.");
                Assert.Equal<PackageViewModelAction>(PackageViewModelAction.Update, buttonBarViewModel.NuGetViewModel.PackageAction);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.YesButton);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsLicensePageVisible, "The license page should be shown.");

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.AcceptButton);

                var installedPackage = this.PackageManager.InstalledPackages.FirstOrDefault();
                Assert.Equal(remote, installedPackage);
            }
        }

        /// <summary>
        /// Select a package from the 'installed' view and walk through the updater
        /// </summary>
        [Fact]
        public void UpdatePackageFromInstalled()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var installed = PackageFactory.Create("update", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                var remote = PackageFactory.Create("update", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remote);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, installed.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.UpdateButton, PackageViewModelAction.Update);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsDetailsPaneVisible, "The details page should be shown.");
                Assert.Equal<PackageViewModelAction>(PackageViewModelAction.Update, buttonBarViewModel.NuGetViewModel.PackageAction);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.YesButton);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsLicensePageVisible, "The license page should be shown.");

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.AcceptButton);

                var installedPackage = this.PackageManager.InstalledPackages.FirstOrDefault();
                Assert.Equal(remote, installedPackage);
            }
        }

        /// <summary>
        /// Select a package from the 'updates' view and click 'updateall' to update all packages
        /// </summary>
        [Fact]
        public void UpdateAllPackagesFromUpdates()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                var installed1 = PackageFactory.Create("update1", new Version(1, 0));
                var installed2 = PackageFactory.Create("update2", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed1);
                this.PackageManager.InstalledPackages.Add(installed2);

                var remote1 = PackageFactory.Create("update1", new Version(2, 0));
                var remote2 = PackageFactory.Create("update2", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remote1);
                this.PackageManager.RemotePackages.Add(remote2);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Updated, installed1.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.UpdateAllButton, PackageViewModelAction.UpdateAll);
                Assert.True(buttonBarViewModel.NuGetViewModel.IsLicensePageVisible, "The license page should be shown.");
                Assert.Equal<PackageViewModelAction>(PackageViewModelAction.UpdateAll, buttonBarViewModel.NuGetViewModel.PackageAction);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.AcceptButton);

                var installedPackages = this.PackageManager.InstalledPackages.ToArray();
                Assert.Equal(remote1, installedPackages[0]);
                Assert.Equal(remote2, installedPackages[1]);
            }
        }

        /// <summary>
        /// Enables a package and checks that it's no longer shown as disabled
        /// </summary>
        [Fact]
        public void EnablePackage()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                this.PackageManager.SupportsEnableDisable = true;

                var installed = PackageFactory.Create("enable", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);
                this.PackageManager.DisabledPackages.Add(installed);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, installed.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.EnableButton);

                Assert.False(buttonBarViewModel.NuGetViewModel.IsDetailsPaneVisible, "The details page should not be shown.");
                Assert.False(buttonBarViewModel.NuGetViewModel.IsLicensePageVisible, "The license page should not be shown.");

                Assert.False(this.PackageManager.DisabledPackages.Any(), "No packages should be disabled.");
            }
        }

        /// <summary>
        /// Disables a package and checks that it's no longer shown as enabled
        /// </summary>
        [Fact]
        public void DisablePackage()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                this.PackageManager.SupportsEnableDisable = true;

                var installed = PackageFactory.Create("disable", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, installed.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.DisableButton);

                Assert.False(buttonBarViewModel.NuGetViewModel.IsDetailsPaneVisible, "The details page should not be shown.");
                Assert.False(buttonBarViewModel.NuGetViewModel.IsLicensePageVisible, "The license page should not be shown.");

                Assert.True(this.PackageManager.DisabledPackages.Contains(installed), "The package should be disabled.");

                // check for the package in disabled filter
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Disabled, installed.Id);
            }
        }

        /// <summary>
        /// Select a mandatory package and ensure that uninstall button is disabled and disabled button is enabled
        /// </summary>
        [Fact]
        public void SelectMandatoryPackage()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                this.PackageManager.SupportsEnableDisable = true;

                var installed = PackageFactory.Create("WebMatrixExtensionsGallery", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, installed.Id);

                Assert.False(buttonBarViewModel.NuGetViewModel.UninstallCommand.CanExecute(null), "The uninstall command should be disabled");
                Assert.True(buttonBarViewModel.NuGetViewModel.DisableCommand.CanExecute(null), "The disable command should be enabled");
            }
        }

        /// <summary>
        /// Test for prerelease filter visibility for NuGet Gallery
        /// NuGetViewModel.ShouldShowPrereleaseFilter is set to true
        /// </summary>
        [Fact]
        public void PrereleaseFilterVisibilityForNuGetGallery()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                this.PackageManager.SupportsEnableDisable = true;

                var installed = PackageFactory.Create("update", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                var remote = PackageFactory.Create("update", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remote);

                installed = PackageFactory.Create("disable", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, installed.Id);

                buttonBarViewModel.NuGetViewModel.ShouldShowPrereleaseFilter = true;

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.DisableButton);

                // At this point, there are 2 packages installed; 1 has an update and 1 is disabled

                // Select All Filter and check that the prerelease filter is visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All);
                Assert.Equal<Visibility>(buttonBarViewModel.NuGetViewModel.ShowPrereleaseFilter, Visibility.Visible);

                // Select Disabled Filter and check that the prerelease filter is NOT visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Disabled);
                Assert.NotEqual<Visibility>(buttonBarViewModel.NuGetViewModel.ShowPrereleaseFilter, Visibility.Visible);

                // Select Updates Filter and check that the prerelease filter is visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Updated);
                Assert.Equal<Visibility>(buttonBarViewModel.NuGetViewModel.ShowPrereleaseFilter, Visibility.Visible);

                // Select Installed Filter and check that the prerelease filter is NOT visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed);
                Assert.NotEqual<Visibility>(buttonBarViewModel.NuGetViewModel.ShowPrereleaseFilter, Visibility.Visible);
            }
        }

        /// <summary>
        /// Test for prerelease filter visibility for extension Gallery
        /// NuGetViewModel.ShouldShowPrereleaseFilter is set to false
        /// </summary>
        [Fact]
        public void PrereleaseFilterVisibilityForExtensionsGallery()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                this.PackageManager.SupportsEnableDisable = true;

                var installed = PackageFactory.Create("update", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                var remote = PackageFactory.Create("update", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remote);

                installed = PackageFactory.Create("disable", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, installed.Id);

                buttonBarViewModel.NuGetViewModel.ShouldShowPrereleaseFilter = false;

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.DisableButton);

                // At this point, there are 2 packages installed; 1 has an update and 1 is disabled

                // Select All Filter and check that the prerelease filter is visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All);
                Assert.NotEqual<Visibility>(buttonBarViewModel.NuGetViewModel.ShowPrereleaseFilter, Visibility.Visible);

                // Select Disabled Filter and check that the prerelease filter is NOT visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Disabled);
                Assert.NotEqual<Visibility>(buttonBarViewModel.NuGetViewModel.ShowPrereleaseFilter, Visibility.Visible);

                // Select Updates Filter and check that the prerelease filter is visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Updated);
                Assert.NotEqual<Visibility>(buttonBarViewModel.NuGetViewModel.ShowPrereleaseFilter, Visibility.Visible);

                // Select Installed Filter and check that the prerelease filter is NOT visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed);
                Assert.NotEqual<Visibility>(buttonBarViewModel.NuGetViewModel.ShowPrereleaseFilter, Visibility.Visible);
            }
        }

        /// <summary>
        /// Test for FeedSource Combobox visibility
        /// </summary>
        [Fact]
        public void FeedSourceVisibility()
        {
            using (var thread = new TemporaryDispatcherThread())
            {
                this.PackageManager.SupportsEnableDisable = true;

                var installed = PackageFactory.Create("update", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                var remote = PackageFactory.Create("update", new Version(2, 0));
                this.PackageManager.RemotePackages.Add(remote);

                installed = PackageFactory.Create("disable", new Version(1, 0));
                this.PackageManager.InstalledPackages.Add(installed);

                ButtonBarViewModel buttonBarViewModel = this.CreateViewModel(thread);
                this.SelectPackage(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed, installed.Id);

                this.ClickButton(thread, buttonBarViewModel, buttonBarViewModel.DisableButton);

                // At this point, there are 2 packages installed; 1 has an update and 1 is disabled

                // Select All Filter and check that the prerelease filter is visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_All);
                Assert.Equal<Visibility>(buttonBarViewModel.NuGetViewModel.ShowFeedSourceComboBox, Visibility.Visible);

                // Select Disabled Filter and check that the prerelease filter is NOT visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Disabled);
                Assert.NotEqual<Visibility>(buttonBarViewModel.NuGetViewModel.ShowFeedSourceComboBox, Visibility.Visible);

                // Select Updates Filter and check that the prerelease filter is visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Updated);
                Assert.Equal<Visibility>(buttonBarViewModel.NuGetViewModel.ShowFeedSourceComboBox, Visibility.Visible);

                // Select Installed Filter and check that the prerelease filter is NOT visible
                this.SelectFilter(thread, buttonBarViewModel.NuGetViewModel, Resources.Filter_Installed);
                Assert.NotEqual<Visibility>(buttonBarViewModel.NuGetViewModel.ShowFeedSourceComboBox, Visibility.Visible);
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

        private void ClickButton(TemporaryDispatcherThread thread, ButtonBarViewModel viewModel, ButtonViewModel button, object parameter = null)
        {
            Assert.True(button.Command.CanExecute(parameter), "The button should be enabled.");
            Assert.True(viewModel.ActionButtons.Contains(button), "The button should be visible.");
            thread.Invoke(() => { button.Command.Execute(parameter); });
            viewModel.NuGetViewModel.WaitUntilComplete();
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
