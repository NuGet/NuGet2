using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using EnvDTE80;

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
                var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                string assemblyName = VsVersionHelper.IsVisualStudio2010 ? "NuGet.TeamFoundationServer10.dll" : "NuGet.TeamFoundationServer.dll";

                try
                {
                    var assembly = Assembly.LoadFrom(Path.Combine(assemblyDirectory, assemblyName));
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