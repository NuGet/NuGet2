namespace NuGet.WebMatrix.DependentTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.WebMatrix.Extensibility;

    /// <summary>
    /// A mock WebMatrix host 
    /// </summary>
    internal class IWebMatrixHostMock : IWebMatrixHost
    {

        // As of C# 4.0, the compiler generates a warning when an event is never fired (CS0067)
        // we're required to have these events as part of the IWebMatrixHost interface, so this
        // will just disable the warning for this class.
#pragma warning disable 0067

        public void AddDashboardItems(IEnumerable<DashboardItem> dashboardItems)
        {
            throw new NotImplementedException();
        }

        public void AddRibbonItems(IEnumerable<RibbonItem> ribbonItems)
        {
            throw new NotImplementedException();
        }

        public void ApplyPathProtection(IEnumerable<IProtectPathInfo> protectPathInfos)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<ContextMenuOpeningEventArgs> ContextMenuOpening;

        public ISiteItem GetSiteItem(HierarchyId id)
        {
            throw new NotImplementedException();
        }

        public IHostCommands HostCommands
        {
            get { throw new NotImplementedException(); }
        }

        public bool? ShowDialog(string title, object content, DialogSize dialogSize = DialogSize.SizeToContent, MessageBoxButton buttons = MessageBoxButton.OKCancel, MessageBoxResult defaultButton = MessageBoxResult.OK, ICommand[] buttonCommands = null)
        {
            throw new NotImplementedException();
        }

        public void ShowExceptionMessage(string title, string message, Exception exception)
        {
            throw new NotImplementedException();
        }

        public void ShowNotification(string message, string actionMessage = null, Action action = null)
        {
        }

        public void ShowErrorNotification(string message, string actionMessage = null, Action action = null)
        {
        }

        public event EventHandler<TreeItemEventArgs> TreeItemCreated;

        public event EventHandler<TreeItemEventArgs> TreeItemRemoved;

        public string Version
        {
            get { throw new NotImplementedException(); }
        }

        public IWebSite WebSite
        {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler<EventArgs> WebSiteChanged;

        public IWorkspace Workspace
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler<WorkspaceChangedEventArgs> WorkspaceChanged;

        public ICollection<IWorkspace> Workspaces
        {
            get { throw new NotImplementedException(); }
        }

#pragma warning restore 0067

        public void AddContextualRibbonTabs(params RibbonContextualTab[] ribbonTabs)
        {
            throw new NotImplementedException();
        }

        public void RemoveContextualRibbonTabs(params RibbonContextualTab[] ribbonTabs)
        {
            throw new NotImplementedException();
        }

        public string ShowFolderDialog(string startFolderPath, System.Windows.Window parentWindow = null)
        {
            throw new NotImplementedException();
        }


        public IPreferences GetExtensionSpecificPreferences(Guid guid)
        {
            throw new NotImplementedException();
        }


        public string DefaultWebSitePath
        {
            get { throw new NotImplementedException(); }
        }


        public bool? ShowDialog(string title, string message, DialogSize dialogSize = DialogSize.SizeToContent, MessageBoxButton buttons = MessageBoxButton.OKCancel, MessageBoxResult defaultButton = MessageBoxResult.OK, ICommand[] buttonCommands = null)
        {
            throw new NotImplementedException();
        }

        public bool? ShowDialog(string title, UIElement content, DialogSize dialogSize = DialogSize.SizeToContent, MessageBoxButton buttons = MessageBoxButton.OKCancel, MessageBoxResult defaultButton = MessageBoxResult.OK, ICommand[] buttonCommands = null)
        {
            throw new NotImplementedException();
        }

        public IWaitDialog CreateWaitDialog(string title, string message, DialogSize dialogSize = DialogSize.SizeToContent, TimeSpan? delayShow = null, Func<Tuple<bool, string>> onCancel = null)
        {
            throw new NotImplementedException();
        }


        public Logger Logger
        {
            get { throw new NotImplementedException(); }
        }
    }
}
