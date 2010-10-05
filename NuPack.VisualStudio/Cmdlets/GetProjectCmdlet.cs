namespace NuPack.VisualStudio.Cmdlets {
    using System.Management.Automation;
    using System.Linq;

    /// <summary>
    /// This cmdlet returns the list of project names in the current solution, 
    /// which is used for tab expansion.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Projects")]
    public class GetProjectCmdlet : Cmdlet {

        protected override void ProcessRecord() {
            WriteObject(from p in SolutionManager.Current.GetProjects()
                        select p.Name);
        }

    }
}