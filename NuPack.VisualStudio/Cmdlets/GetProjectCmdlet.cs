using System.Management.Automation;

namespace NuPack.VisualStudio.Cmdlets {
    /// <summary>
    /// This cmdlet returns the list of project names in the current solution, 
    /// which is used for tab expansion.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Project", DefaultParameterSetName = "Single")]
    public class GetProjectCmdlet : Cmdlet {

        [Parameter(Position = 0, ParameterSetName = "Single")]
        public string Name { get; set; }

        [Parameter(Position = 0, ParameterSetName = "All")]
        public SwitchParameter All { get; set; }

        protected override void ProcessRecord() {
            if (All.IsPresent) {
                WriteObject(SolutionManager.Current.GetProjects(), enumerateCollection: true);
            }
            else {
                string projectName = Name;
                if (string.IsNullOrEmpty(projectName)) {
                    // if the Name parameter is not specified, get the default project name
                    projectName = SolutionManager.Current.DefaultProjectName;
                }
                WriteObject(SolutionManager.Current.GetProject(projectName));
            }
        }
    }
}