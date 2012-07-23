using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.ExecutionModel
{
    public class InstallContext : MarshalByRefObject
    {
        public string InstallPath { get; private set; }
        public IPackage Package { get; private set; }
        public IProjectProxy Project { get; private set; }

        public InstallContext(string installPath, IPackage package, IProjectProxy project)
        {
            InstallPath = installPath;
            Package = package;
            Project = project;
        }
    }
}