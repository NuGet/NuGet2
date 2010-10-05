using System.Management.Automation;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This cmdlet returns the list of project names in the current solution, 
    /// which is used for tab expansion.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Projects")]
    public class GetProjectCmdlet : Cmdlet {

        protected override void ProcessRecord() {
            WriteObject(SolutionProjectsHelper.Current.GetCurrentProjectNames());
        }

    }
}