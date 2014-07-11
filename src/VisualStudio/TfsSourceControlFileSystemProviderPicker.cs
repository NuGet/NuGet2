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

#if VS10 || VS11
                if (VsVersionHelper.IsVisualStudio2010)
                {
                    assemblyName = "NuGet.TeamFoundationServer10";
                }
                else 
                {
                    // VS 2012
                    assemblyName = "NuGet.TeamFoundationServer11";
                }
#endif

#if VS12
                assemblyName = "NuGet.TeamFoundationServer12";
#endif

#if VS14
                assemblyName = "NuGet.TeamFoundationServer14";
#endif

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