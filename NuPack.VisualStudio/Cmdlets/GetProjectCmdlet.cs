using System.Management.Automation;

namespace NuGet.VisualStudio.Cmdlets {
    /// <summary>
    /// This cmdlet returns the list of project names in the current solution, 
    /// which is used for tab expansion.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Project", DefaultParameterSetName = "Single")]
    public class GetProjectCmdlet : Cmdlet {
        private readonly ISolutionManager _solutionManager;
        
        public GetProjectCmdlet()
            : this(SolutionManager.Current) {

        }

        public GetProjectCmdlet(ISolutionManager solutionManager) {
            _solutionManager = solutionManager;
        }

        [Parameter(Position = 0, ParameterSetName = "Single")]
        public string Name { get; set; }

        [Parameter(Position = 0, ParameterSetName = "All")]
        public SwitchParameter All { get; set; }

        protected override void ProcessRecord() {
            if (All.IsPresent) {
                WriteObject(_solutionManager.GetProjects(), enumerateCollection: true);
            }
            else {
                string projectName = Name;
                if (string.IsNullOrEmpty(projectName)) {
                    // if the Name parameter is not specified, get the default project name
                    projectName = _solutionManager.DefaultProjectName;
                }
                WriteObject(_solutionManager.GetProject(projectName));
            }
        }
    }
}
