namespace NuPack.VisualStudio.Cmdlets {
    using System.Management.Automation;

    /// <summary>
    /// This cmdlet returns the list of project names in the current solution, 
    /// which is used for tab expansion.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Project")]
    public class GetProjectCmdlet : Cmdlet {

        [Parameter(Position=0)]
        public string Name { get; set; }

        protected override void ProcessRecord() {
            if (!string.IsNullOrEmpty(Name)) {
                WriteObject(SolutionManager.Current.GetProject(Name));
            }
            else {
                // if the Name parameter is not specified, output all projects
                WriteObject(SolutionManager.Current.GetProjects(), enumerateCollection: true);
            }
        }
    }
}