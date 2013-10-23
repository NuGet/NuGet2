using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.WebMatrix.Core;
using Microsoft.WebMatrix.Core.SQM;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Utility;
using NuGet;

namespace NuGet.WebMatrix
{
    internal class NuGetModel
    {
        private static HashSet<string> _tempFilesToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _cleanupAdded = false;
        private static object _lock = new object();

        private readonly static Dictionary<Tuple<string, FeedSource, bool>, NuGetModel> _cache = new Dictionary<Tuple<string, FeedSource, bool>, NuGetModel>();

        private readonly IWebMatrixHost _webMatrixHost;
        private readonly string _packageKind;
        private readonly int _galleryId;

        internal static void ClearCache()
        {
            lock (_cache)
            {
                _cache.Clear();
            }
        }

        public static NuGetModel GetModel(INuGetGalleryDescriptor descriptor, IWebMatrixHost webMatrixHost, FeedSource remoteSource, bool includePrerelease = false)
        {
            return GetModel(descriptor, webMatrixHost, remoteSource, null, null, TaskScheduler.Default);
        }

        public static NuGetModel GetModel(INuGetGalleryDescriptor descriptor, IWebMatrixHost webMatrixHost, FeedSource remoteSource, string destination, Func<Uri, string, INuGetPackageManager> packageManagerCreator, TaskScheduler scheduler, bool includePrerelease = false)
        {
            if (destination == null)
            {
                var siteRoot = webMatrixHost.WebSite == null ? null : webMatrixHost.WebSite.Path;

                if (String.IsNullOrWhiteSpace(siteRoot))
                {
                    Debug.Fail("The NuGetModel needs a site with a physical path");
                    return null;
                }

                destination = siteRoot;
            }

            NuGetModel model;
            lock (_cache)
            {
                var key = new Tuple<string, FeedSource, bool>(destination, remoteSource, includePrerelease);
                if (_cache.TryGetValue(key, out model))
                {
                    model.FromCache = true;
                }
                else
                {
                    INuGetPackageManager packageManager;
                    if (packageManagerCreator == null)
                    {
                        packageManager = new NuGetPackageManager(remoteSource.SourceUrl, destination, webMatrixHost);
                    }
                    else
                    {
                        packageManager = packageManagerCreator(remoteSource.SourceUrl, destination);
                    }

                    model = new NuGetModel(descriptor, webMatrixHost, remoteSource, destination, packageManager, scheduler);
                    packageManager.IncludePrerelease = includePrerelease;
                    _cache[key] = model;
                }
            }

            Debug.Assert(model != null, "model should be created");

            return model;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:NuGetModel"/> class.
        /// <remarks>This is internal for tests. Callers inside of the NuGet module should used NuGetDataCache::GetModel to 
        /// take advantage of cachein</remarks>
        /// </summary>
        internal NuGetModel(INuGetGalleryDescriptor descriptor, IWebMatrixHost host, FeedSource remoteSource, string destination, INuGetPackageManager packageManager, TaskScheduler scheduler)
        {
            Debug.Assert(host != null, "webMatrixHost must not be null");
            Debug.Assert(remoteSource != null, "remoteSource must not be null");
            Debug.Assert(remoteSource.SourceUrl != null, "remoteSource.SourceUrl must not be null");

            this.Descriptor = descriptor;
            this.Scheduler = scheduler;

            FeedSource = remoteSource;
            _webMatrixHost = host;
            _packageKind = descriptor.PackageKind;
            _galleryId = descriptor.GalleryId;

            this.PackageManager = packageManager;

            this.FilterManager = new FilterManager(this, this.Scheduler, descriptor);
        }

        internal INuGetGalleryDescriptor Descriptor
        {
            get;
            private set;
        }

        internal FilterManager FilterManager
        {
            get;
            private set;
        }

        internal bool FromCache
        {
            get;
            private set;
        }

        internal INuGetPackageManager PackageManager
        {
            get;
            private set;
        }

        internal TaskScheduler Scheduler
        {
            get;
            private set;
        }

        public IEnumerable<string> UninstallPackage(IPackage package, bool inDetails)
        {
            Exception exception = null;
            IEnumerable<string> result = null;

            try
            {
                result = this.PackageManager.UninstallPackage(package);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                // Log the result of the uninstall
                var telemetry = WebMatrixTelemetryServiceProvider.GetTelemetryService();
                if (telemetry != null)
                {
                    string appId = _webMatrixHost.WebSite.ApplicationIdentifier;
                    telemetry.LogPackageUninstall(_galleryId, package.Id, appId, exception, inDetails, isFeatured: false, isCustomFeed: !FeedSource.IsBuiltIn);
                }
            }

            if (_webMatrixHost != null)
            {
                _webMatrixHost.ShowNotification(string.Format(Resources.Notification_Uninstalled, _packageKind, package.Id));
            }

            return result;
        }

        public IEnumerable<string> InstallPackage(IPackage package, bool inDetails)
        {
            Exception exception = null;
            IEnumerable<string> result = null;

            try
            {
                result = this.PackageManager.InstallPackage(package);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                // Log the result of the install
                var telemetry = WebMatrixTelemetryServiceProvider.GetTelemetryService();

                if (telemetry != null)
                {
                    string appId = _webMatrixHost.WebSite.ApplicationIdentifier;
                    telemetry.LogPackageInstall(_galleryId, package.Id, appId, exception, inDetails, isFeatured: false, isCustomFeed: !FeedSource.IsBuiltIn);
                }
            }

            if (_webMatrixHost != null)
            {
                string message = string.Format(Resources.Notification_Installed, _packageKind, package.Id);

                ShowMessageAndReadme(package, message);
            }

            return result;
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "It's safe to dispose the stream twice")]
        private void ShowMessageAndReadme(IPackage package, string message)
        {
            var files = package.GetContentFiles();

            string filePath = null;

            if (files != null)
            {
                var readme = files.FirstOrDefault(
                                (file) => file.Path.Equals("ReadMe.txt", StringComparison.OrdinalIgnoreCase));

                if (readme == null)
                {
                    readme = files.FirstOrDefault(
                    (file) => file.Path.IndexOf("ReadMe.txt", StringComparison.OrdinalIgnoreCase) >= 0);
                }

                string text = null;

                if (readme != null)
                {
                    try
                    {
                        using (var stream = readme.GetStream())
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                text = reader.ReadToEnd();
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var tempFilePath = Path.GetTempFileName();

                            lock (_lock)
                            {
                                if (!_cleanupAdded)
                                {
                                    var dispatcher = TryGetDispatcher();

                                    if (dispatcher != null)
                                    {
                                        dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)AddExitCleanUp);
                                    }
                                }
                            }

                            File.WriteAllText(tempFilePath, text);

                            filePath = tempFilePath;

                            lock (_lock)
                            {
                                _tempFilesToDelete.Add(filePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Fail(ex.ToString());
                    }
                }
            }

            if (filePath != null)
            {
                _webMatrixHost.ShowNotification(message, Resources.ClickForReadme, () => ShowReadMe(filePath, true));
            }
            else
            {
                _webMatrixHost.ShowNotification(message);
            }
        }

        private void AddExitCleanUp()
        {
            lock (_lock)
            {
                if (!_cleanupAdded)
                {
                    var current = Application.Current;

                    if (current != null)
                    {
                        current.Exit += ApplicationExitCleanupHandler;
                        _cleanupAdded = true;
                    }
                }
            }
        }

        private void ApplicationExitCleanupHandler(object sender, ExitEventArgs e)
        {
            string[] list = null;

            lock (_lock)
            {
                if (_tempFilesToDelete != null)
                {
                    list = _tempFilesToDelete.ToArray();
                    _tempFilesToDelete = null;
                }
            }

            if (list != null)
            {
                foreach (var file in list)
                {
                    if (File.Exists(file))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private Dispatcher TryGetDispatcher()
        {
            var current = Application.Current;

            if (current != null)
            {
                var dispatcher = current.Dispatcher;

                if (dispatcher != null && !dispatcher.HasShutdownStarted)
                {
                    return dispatcher;
                }
            }

            return null;
        }

        private void ShowReadMe(string filePath, bool deleteAfterReading)
        {
            Debug.Assert(File.Exists(filePath), "Where is the file");

            if (File.Exists(filePath))
            {
                var host = _webMatrixHost as IWebMatrixHostInternal;

                var editorService = host.ServiceProvider.GetService<EditorService>();

                editorService.FileEditorModule.OpenFile(filePath, OpenMode.OpenInEditor);

                if (deleteAfterReading)
                {
                    var dispatcher = TryGetDispatcher();

                    if (dispatcher != null)
                    {
                        dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                            (Action)(() => TryDeleteFile(filePath)));
                    }
                }
            }
        }

        private void TryDeleteFile(string filePath)
        {
            try
            {
                lock (_lock)
                {
                    File.Delete(filePath);
                    _tempFilesToDelete.Remove(filePath);
                }
            }
            catch
            {
            }
        }

        public IEnumerable<string> UpdatePackage(IPackage package, bool inDetails)
        {
            Exception exception = null;
            IEnumerable<string> result;

            try
            {
                result = this.PackageManager.UpdatePackage(package);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                // Log the result of the update
                var telemetry = WebMatrixTelemetryServiceProvider.GetTelemetryService();
                if (telemetry != null)
                {
                    string appId = _webMatrixHost.WebSite.ApplicationIdentifier;
                    telemetry.LogPackageUpdate(_galleryId, package.Id, appId, exception, inDetails, isFeatured: false, isCustomFeed: !FeedSource.IsBuiltIn);
                }
            }

            if (_webMatrixHost != null)
            {
                var message = string.Format(Resources.Notification_Updated, _packageKind, package.Id);

                ShowMessageAndReadme(package, message);
            }

            return result;
        }

        public IEnumerable<string> UpdateAllPackages(bool inDetails)
        {
            Exception exception = null;
            IEnumerable<string> result;

            try
            {
                result = this.PackageManager.UpdateAllPackages();
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                // Log the result of the update
                var telemetry = WebMatrixTelemetryServiceProvider.GetTelemetryService();
                if (telemetry != null)
                {
                    string appId = _webMatrixHost.WebSite.ApplicationIdentifier;
                    //telemetry.LogPackageUpdate(_galleryId, package.Id, appId, exception, inDetails, isFeatured: false, isCustomFeed: !FeedSource.IsBuiltIn);
                }
            }

            if (_webMatrixHost != null)
            {
                var message = string.Format(Resources.Notification_UpdatedAll);

                _webMatrixHost.ShowNotification(message);
            }

            return result;
        }

        public bool IsPackageInstalled(IPackage package)
        {
            return this.PackageManager.IsPackageInstalled(package);
        }

        public IQueryable<IPackage> GetInstalledPackages()
        {
            return this.PackageManager.GetInstalledPackages();
        }

        public IEnumerable<IPackage> GetPackagesWithUpdates()
        {
            return this.PackageManager.GetPackagesWithUpdates();
        }

        public FeedSource FeedSource
        {
            get;
            private set;
        }

        public IEnumerable<IPackage> FindPackages(IEnumerable<string> packageIds)
        {
            return this.PackageManager.FindPackages(packageIds);
        }

        public IEnumerable<IPackage> FindDependenciesToBeInstalled(IPackage package)
        {
            return this.PackageManager.FindDependenciesToBeInstalled(package);
        }
    }
}
