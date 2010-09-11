using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Effects;
//using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Microsoft.VisualStudio.PlatformUI;
//using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.Internal.Performance;
using NuPack.Dialog.PackageManagerUI.Providers;
using NuPack.Dialog.Providers;
using NuPack.Dialog.ToolsOptionsUI;
using NuPack.VisualStudio;

namespace NuPack.Dialog.PackageManagerUI
{
    /// <summary>
    /// Interaction logic for PackageManagerWindow.xaml
    /// </summary>
    public partial class PackageManagerWindow : DialogWindow, INotifyPropertyChanged
    {
        
        //private CompositionContainer container;
        private const string F1Keyword = "vs.ExtensionManager";

        //private InstalledVsExtensionsProvider installedProvider;
        private OnlinePackageProvider _onlinePackageProvider;
        private OnlinePackagesProvider _onlinePackagesProvider;
        private InstalledPackagesProvider _installedPackagesProvider;
        private RecentPackagesProvider _recentPackagesProvider;
            
        ///// <summary>
        ///// Constructor for the Extension Manager Window
        ///// </summary>
        public PackageManagerWindow()
            : base(F1Keyword)
        {

            InitializeComponent();

            InstalledPackagesProvider _installedPackagesProvider = new InstalledPackagesProvider(Resources);
            this.explorer.Providers.Add(_installedPackagesProvider);

            RecentPackagesProvider _recentPackagesProvider = new RecentPackagesProvider(Resources);
            this.explorer.Providers.Add(_recentPackagesProvider);

            OnlinePackagesProvider _onlinePackagesProvider = new OnlinePackagesProvider(Resources, false);
            this.explorer.Providers.Add(_onlinePackagesProvider);
            this.explorer.SelectedProvider = _onlinePackagesProvider;

        }

        protected void Window_Loaded(object sender, EventArgs e)
        {
        }


        void HandleRequestNavigate(object sender, RoutedEventArgs e) 
        {
        }

        private void ExecutedUninstallExtension(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void CanExecuteUninstallExtension(object sender, CanExecuteRoutedEventArgs e)
        {
        }

        private void ExecutedToggleExtensionEnabledState(object sender, ExecutedRoutedEventArgs e)
        {
        
        }

        private void CanExecuteToggleExtensionEnabledState(object sender, CanExecuteRoutedEventArgs e)
        {
        }


        private void ExecutedUpdateExtension(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void CanExecuteUpdateExtension(object sender, CanExecuteRoutedEventArgs e)
        {
        
        }

        
        private void ExecutedRestartVisualStudio(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void CanExecuteRestartVisualStudio(object sender, CanExecuteRoutedEventArgs e)
        {
        }

        
        private void ExecutedClose(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void CanExecuteClose(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        
        private void ExecutedSelectOnlineProvider(object sender, ExecutedRoutedEventArgs e)
        {
        }
        
        private void ExecutedShowOptionsPage(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
            (Utilities.ServiceProvider as NuPackPackage).ShowOptionPage(typeof(ToolsOptionsPage));
        }

        private void ExecutedDownloadExtension(object sender, ExecutedRoutedEventArgs e)
        {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null)
            {
                return;
            }

            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null)
            {
                return;
            }

            OnlinePackagesProvider provider = control.SelectedProvider as OnlinePackagesProvider;
            if (provider == null)
            {
                return;
            }

            provider.Install(selectedItem.Id, new Version(selectedItem.Version));
        }

        private void CanExecuteDownloadExtension(object sender, CanExecuteRoutedEventArgs e)
        {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null)
            {
                e.CanExecute = false;
                return;
            }
            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null)
            {
                e.CanExecute = false;
                return;
            }

            if (selectedItem.IsInstalled)
            {
                //Don't allow the download command on extensions that are already installed.
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
            return;
        }

        private void ExecutedFocusOnSearchBox(object sender, ExecutedRoutedEventArgs e)
        {
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnNotifyPropertyChanged(string propertyName)
        {
        }
    }
}
