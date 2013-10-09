using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Extensibility;

namespace NuGet.WebMatrix
{
    /// <summary>
    /// An ICommandTarget implementation for the NuGet gallery
    /// </summary>
    internal class NuGetCommandTarget : ICommandTarget
    {
        public NuGetCommandTarget(ModuleGlobals globals)
        {
            this.Globals = globals;

            this.OpenNuGetGalleryCommand = this.Globals.CommandManager.AddCommand(
                this,
                NuGetCommands.OpenNuGetGalleryCommandId,
                Resources.GalleryCommandName,
                Resources.GalleryCommandTooltip,
                Resources.nuget_32.ConvertToImageSource(),
                Resources.nuget_32.ConvertToImageSource());
        }

        public Microsoft.WebMatrix.Core.Command OpenNuGetGalleryCommand
        {
            get;
            private set;
        }

        private IHostCommands Commands
        {
            get
            {
                return this.Globals.Host.HostCommands;
            }
        }

        private IWebSite CurrentSite
        {
            get
            {
                return this.Host == null ? null : this.Host.WebSite;
            }
        }

        private ModuleGlobals Globals
        {
            get;
            set;
        }

        private IWebMatrixHost Host
        {
            get
            {
                return this.Globals.Host;
            }
        }

        public CommandStatus CanExecute(ICommandId commandId, object parameter)
        {
            if (commandId == null || commandId.GroupId != NuGetCommands.GroupId)
            {
                return CommandStatus.UnSupported;
            }

            if (commandId == NuGetCommands.OpenNuGetGalleryCommandId)
            {
                return CommandStatus.EnabledAndSupported;
            }
            else
            {
                Debug.Fail("Unknown CommandId detected.");
                return CommandStatus.UnSupported;
            }
        }

        public void Execute(ICommandId commandId, object parameter)
        {
            if (commandId == null || commandId.GroupId != NuGetCommands.GroupId)
            {
                return;
            }

            if (commandId == NuGetCommands.OpenNuGetGalleryCommandId)
            {
                this.OpenNuGetGallery();
            }
        }

        private void OpenNuGetGallery()
        {
            try
            {
                INuGetGalleryDescriptor descriptor = GalleryDescriptors.NuGet;
                Task<bool?> galleryTask = this.Globals.Gallery.ShowGallery(descriptor, this.Host.WebSite.Path);

                galleryTask.ContinueWith((task) =>
                {
                    Exception exception = null;

                    if (task.IsFaulted)
                    {
                        exception = task.Exception.Flatten().InnerException;
                    }
                    else
                    {
                        try
                        {
                            // Send "Refresh" command to update the file tree
                            var refreshCommand = this.Host.HostCommands.GetCommand(CommonCommandIds.GroupId, (int)CommonCommandIds.Ids.Refresh);

                            // Passing true to the command forces a refresh from the root of the tree
                            if (refreshCommand.CanExecute(true))
                            {
                                refreshCommand.Execute(true);
                            }
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                    }

                    if (exception != null)
                    {
                        this.Host.ShowExceptionMessage(Resources.String_Error, Resources.String_ErrorOccurred, exception);
                    }
                },
                NuGetExtension.GetCurrentTaskScheduler());
            }
            catch (Exception ex)
            {
                this.Host.ShowExceptionMessage(
                    Resources.String_Error,
                    Resources.String_ErrorOccurred,
                    ex);
            }
        }
    }
}
