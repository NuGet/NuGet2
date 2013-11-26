using System;
using System.ComponentModel.Composition;
using System.Reflection;
using EnvDTE80;
using NuGet.VisualStudio.Resources;
using System.Globalization;

namespace NuGet.VisualStudio
{
    [Export(typeof(ISourceControlFileSystemProvider))]
    public class TfsSourceControlFileSystemProviderPicker : ISourceControlFileSystemProvider
    {
        private const string typeName = "NuGet.TeamFoundationServer.TfsSourceControlFileSystemProvider";

        private ISourceControlFileSystemProvider _cachedTfsFileSystemProvider;

        public ISourceControlFileSystem GetFileSystem(string path, SourceControlBindings binding)
        {
            ISourceControlFileSystemProvider tfsProvider = GetUnderlyingTfsProvider();
            return tfsProvider != null ? tfsProvider.GetFileSystem(path, binding) : null;
        }

        private ISourceControlFileSystemProvider GetUnderlyingTfsProvider()
        {
            if (_cachedTfsFileSystemProvider == null)
            {
                string assemblyName;
                if (VsVersionHelper.IsVisualStudio2010)
                {
                    assemblyName = "NuGet.TeamFoundationServer10";
                }
                else if (VsVersionHelper.IsVisualStudio2012)
                {
                    assemblyName = "NuGet.TeamFoundationServer11";
                }
                else if (VsVersionHelper.IsVisualStudio2013)
                {
                    assemblyName = "NuGet.TeamFoundationServer12";
                }
                else
                {
                    var message = string.Format(
                        CultureInfo.CurrentCulture,
                        VsResources.Error_UnsupportedVSVersion,
                        VsVersionHelper.FullVsEdition);
                    throw new InvalidOperationException(message);
                }

                try
                {
                    Assembly assembly = RuntimeHelpers.LoadAssemblySmart(assemblyName);

                    if (assembly != null)
                    {
                        var type = assembly.GetType(typeName, throwOnError: false);
                        if (type != null)
                        {
                            _cachedTfsFileSystemProvider = (ISourceControlFileSystemProvider)Activator.CreateInstance(type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHelper.WriteToActivityLog(ex);
                    _cachedTfsFileSystemProvider = null;
                }
            }

            return _cachedTfsFileSystemProvider;
        }
    }
}