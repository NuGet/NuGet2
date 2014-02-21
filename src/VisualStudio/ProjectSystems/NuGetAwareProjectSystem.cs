using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.ProjectSystem.Interop;

namespace NuGet.VisualStudio
{
    class NuGetAwareProjectSystem : IProjectSystem, INuGetPackageManager
    {
        INuGetPackageManager _nugetProject;
        Project _project;
        ILogger _logger;

        public NuGetAwareProjectSystem(Project project)
        {
            _project = project;
            _nugetProject = project.ToNuGetProjectSystem();
        }

        public System.Runtime.Versioning.FrameworkName TargetFramework
        {
            get 
            {
                return null;                
            }
        }

        public string ProjectName
        {
            get { return _project.Name; }
        }

        public void AddReference(string referencePath, System.IO.Stream stream)
        {
            throw new NotImplementedException();
        }

        public void AddFrameworkReference(string name)
        {
            throw new NotImplementedException();
        }

        public bool ReferenceExists(string name)
        {
            throw new NotImplementedException();
        }

        public void RemoveReference(string name)
        {
            throw new NotImplementedException();
        }

        public bool IsSupportedFile(string path)
        {
            throw new NotImplementedException();
        }

        public string ResolvePath(string path)
        {
            throw new NotImplementedException();
        }

        public bool IsBindingRedirectSupported
        {
            get 
            { 
                return false;
            }
        }

        public void AddImport(string targetFullPath, ProjectImportLocation location)
        {
            throw new NotImplementedException();
        }

        public void RemoveImport(string targetFullPath)
        {
            throw new NotImplementedException();
        }

        public bool FileExistsInProject(string path)
        {
            return _project.ContainsFile(path);
        }

        public string Root
        {
            get { throw new NotImplementedException(); }
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetFiles(string path, string filter, bool recursive)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            throw new NotImplementedException();
        }

        public string GetFullPath(string path)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(string path)
        {
            throw new NotImplementedException();
        }

        public void DeleteFiles(IEnumerable<IPackageFile> files, string rootDir)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string path)
        {
            if (string.Equals(path, "packages.config", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            throw new NotImplementedException();
        }

        public bool DirectoryExists(string path)
        {
            throw new NotImplementedException();
        }

        public void AddFile(string path, System.IO.Stream stream)
        {
            throw new NotImplementedException();
        }

        public void AddFile(string path, Action<System.IO.Stream> writeToStream)
        {
            throw new NotImplementedException();
        }

        public void AddFiles(IEnumerable<IPackageFile> files, string rootDir)
        {
            throw new NotImplementedException();
        }

        public void MakeFileWritable(string path)
        {
            throw new NotImplementedException();
        }

        public void MoveFile(string source, string destination)
        {
            throw new NotImplementedException();
        }

        public System.IO.Stream CreateFile(string path)
        {
            throw new NotImplementedException();
        }

        public System.IO.Stream OpenFile(string path)
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset GetLastModified(string path)
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset GetCreated(string path)
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset GetLastAccessed(string path)
        {
            throw new NotImplementedException();
        }

        public dynamic GetPropertyValue(string propertyName)
        {
            throw new NotImplementedException();
        }

        public ILogger Logger
        {
            get
            {
                return _logger;
            }
            set
            {
                _logger = value;
            }
        }

        public bool CanSupport(string optionName, NuGetOperation operation)
        {
            return _nugetProject.CanSupport(optionName, operation);
        }

        public Task<IReadOnlyCollection<object>> GetInstalledPackagesAsync(CancellationToken cancellationToken)
        {
            return _nugetProject.GetInstalledPackagesAsync(cancellationToken);
        }

        public Task InstallPackageAsync(INuGetPackageMoniker package, IReadOnlyDictionary<string, object> options, System.IO.TextWriter logger, IProgress<INuGetPackageInstallProgress> progress, CancellationToken cancellationToken)
        {
            return _nugetProject.InstallPackageAsync(package, options, logger, progress, cancellationToken);
        }

        public Task UninstallPackageAsync(INuGetPackageMoniker package, IReadOnlyDictionary<string, object> options, System.IO.TextWriter logger, IProgress<INuGetPackageInstallProgress> progress, CancellationToken cancellationToken)
        {
            return _nugetProject.UninstallPackageAsync(package, options, logger, progress, cancellationToken);
        }
    }
}