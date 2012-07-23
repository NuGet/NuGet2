using NuGet.ExecutionModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace NuGet.VisualStudio.ExecutionModel
{
    internal class VsProjectProxy : MarshalByRefObject, IProjectProxy
    {
        private readonly IProjectSystem _projectSystem;

        public VsProjectProxy(IProjectSystem projectSystem)
        {
            if (projectSystem == null)
            {
                throw new ArgumentNullException("projectSystem");
            }

            _projectSystem = projectSystem;
        }

        public string TargetFramework
        {
            get { return _projectSystem.TargetFramework.ToString(); }
        }

        public string ProjectName
        {
            get { return _projectSystem.ProjectName; }
        }

        public string Root
        {
            get { return _projectSystem.Root; }
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            _projectSystem.DeleteDirectory(path, recursive);
        }

        public IEnumerable<string> GetFiles(string path, string filter, bool recursive)
        {
            return _projectSystem.GetFiles(path, filter, recursive);
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            return _projectSystem.GetDirectories(path);
        }

        public void DeleteFile(string path)
        {
            _projectSystem.DeleteFile(path);
        }

        public bool FileExists(string path)
        {
            return _projectSystem.FileExists(path);
        }

        public bool DirectoryExists(string path)
        {
            return _projectSystem.DirectoryExists(path);
        }

        public void AddFile(string path, Stream stream)
        {
            _projectSystem.AddFile(path, stream);
        }

        public void AddFrameworkReference(string assemblyName)
        {
            _projectSystem.AddFrameworkReference(assemblyName);
        }

        public void AddReference(string assemblyPath)
        {
            _projectSystem.AddReference(assemblyPath, Stream.Null);
        }

        public bool ReferenceExists(string name)
        {
            return _projectSystem.ReferenceExists(name);
        }

        public void RemoveReference(string name)
        {
            _projectSystem.RemoveReference(name);
        }

        public bool IsBindingRedirectSupported
        {
            get { return _projectSystem.IsBindingRedirectSupported; }
        }
    }
}