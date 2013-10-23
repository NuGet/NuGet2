using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Core;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Utility;
using NuGet;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// Interface that represents a custom NuGet-style gallery.
    /// </summary>
    public interface INuGetExtensionGallery
    {
        /// <summary>
        /// Shows a custom NuGet-style gallery.
        /// </summary>
        /// <param name="gallery">A gallery descriptor for the gallery to be shown</param>
        /// <param name="installRoot">Root path for installing packages.</param>
        /// <returns>Task that shows the gallery.</returns>
        Task<bool?> ShowGallery(
            INuGetGalleryDescriptor gallery,
            string installRoot);
    }

    /// <summary>
    /// MEF-usable class that creates a NuGet-style gallery.
    /// </summary>
    [Export(typeof(INuGetExtensionGallery))]
    internal class NuGetGallery : INuGetExtensionGallery
    {
        internal static TaskScheduler GetCurrentTaskScheduler()
		{
            TaskScheduler scheduler = null;
            try
            {
                // the scheduler should be the current Sync Context
                scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            }
            catch (InvalidOperationException)
            {
                scheduler = TaskScheduler.Default;
            }

            return scheduler;
        }

        [Import]
        private IWebMatrixHost Host
        {
            get;
            set;
        }

        /// <summary>
        /// Shows a custom NuGet-style gallery.
        /// </summary>
        /// <param name="galleryId">A gallery descriptor for the gallery</param>
        /// <param name="installRoot">Root path for installing packages.</param>
        /// <returns>Task that shows the gallery.</returns>
        public Task<bool?> ShowGallery(
            INuGetGalleryDescriptor descriptor,
            string installRoot)
        {
            var feedSource = new FeedSource(descriptor.FeedUri, descriptor.FeedName)
            {
                IsBuiltIn = true,
                FilterTag = descriptor.FilterTag,
            };

            var preferences = this.Host.GetExtensionSpecificPreferences(descriptor.PreferencesStore);
            IFeedSourceStore feedSourceStore;

            // we special case the NuGet gallery to use the system-wide NuGet store
            feedSourceStore = new NuGetFeedSourceStore(preferences);

            // Customize the custom gallery
            var packageSourcesViewModel = new PackageSourcesViewModel(new PackageSourcesModel(feedSource, feedSourceStore));

            var viewModel = new NuGetViewModel(
                descriptor,
                this.Host,
                packageSourcesViewModel,
                (sourceUrl, siteRoot) => new NuGetPackageManager(
                        sourceUrl,
                        siteRoot,
                        this.Host),
                installRoot,
                GetCurrentTaskScheduler());

            viewModel.ShouldShowPrereleaseFilter = true;

            return Task.Factory.StartNew<bool?>(() =>
            {
                var view = new NuGetView(descriptor.DialogTitle);

                // show the gallery view
                view.DataContext = viewModel;

                return this.Host.ShowDialog(null, view);
            },
            CancellationToken.None,
            TaskCreationOptions.None,
            GetCurrentTaskScheduler());
        }
    }
}
