using System;

namespace NuGet.ExecutionModel
{
    public class UninstallContext : MarshalByRefObject
    {
        public string UninstallPath { get; private set; }
        public IPackage Package { get; private set; }
        public IProjectProxy Project { get; private set; }

        public UninstallContext(string uninstallPath, IPackage package, IProjectProxy project)
        {
            UninstallPath = uninstallPath;
            Package = package;
            Project = project;
        }
    }
}