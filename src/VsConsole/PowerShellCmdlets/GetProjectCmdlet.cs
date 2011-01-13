using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using EnvDTE;
using NuGet.VisualStudio;

namespace NuGet.Cmdlets
{
    /// <summary>
    /// This cmdlet returns the list of project names in the current solution, 
    /// which is used for tab expansion.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Project", DefaultParameterSetName = ParameterAttribute.AllParameterSets)]
    [OutputType(typeof(Project))]
    public class GetProjectCmdlet : NuGetBaseCmdlet {
        private readonly ISolutionManager _solutionManager;

        public GetProjectCmdlet()
            : this(ServiceLocator.GetInstance<ISolutionManager>())
        {
        }

        public GetProjectCmdlet(ISolutionManager solutionManager) : base(solutionManager, null) {
            _solutionManager = solutionManager;
        }

        [Parameter(Mandatory=true, Position = 0, ParameterSetName = "ByName")]
        [ValidateNotNullOrEmpty]
        public string[] Name { get; set; }

        [Parameter(Mandatory=true, ParameterSetName = "All")]
        public SwitchParameter All { get; set; }

        protected override void ProcessRecordCore() {
            if (!SolutionManager.IsSolutionOpen) {
                ErrorHandler.ThrowSolutionNotOpenTerminatingError();
            }

            if (All.IsPresent) {
                WriteObject(_solutionManager.GetProjects(), enumerateCollection: true);
            }
            else {
                // No name specified; return default project (if not null)
                if (this.Name == null) {
                    if (_solutionManager.DefaultProject != null) {
                        WriteObject(_solutionManager.DefaultProject);
                    }
                }
                else {
                    // get all projects matching name(s) - handles wildcards
                    WriteObject(GetProjectsByName(this.Name), enumerateCollection: true);
                }
            }
        }
    }
}
