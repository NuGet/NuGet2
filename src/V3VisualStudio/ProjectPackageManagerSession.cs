using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using NuGet.Client;

namespace NuGet.Client.VisualStudio
{
    internal class ProjectPackageManagerSession : VsPackageManagerSession
    {
        private Project _project;

        public override string Name
        {
            get { return _project.Name; }
        }

        public ProjectPackageManagerSession(Project project)
        {
            _project = project;
        }

        public override IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            throw new NotImplementedException();
        }

        public override IInstalledPackageList GetInstalledPackageList()
        {
            throw new NotImplementedException();
        }
    }
}
