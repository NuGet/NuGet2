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

        protected override void ProcessRecordCore()
        {
            if (All.IsPresent) {
                WriteObject(_solutionManager.GetProjects(), enumerateCollection: true);
            }
            else {
                var projects = new List<Project>();

                // No name specified; return default project
                if (this.Name == null) {
                    projects.Add(_solutionManager.DefaultProject);
                }
                else {
                    foreach (string projectName in this.Name) {
                        
                        // Treat every name as a wildcard; results in simpler code
                        var pattern = new WildcardPattern(projectName, WildcardOptions.IgnoreCase);

                        var matches = from project in _solutionManager.GetProjects() // cached in dictionary, not expensive to call
                                      where pattern.IsMatch(project.Name)
                                      select project;

                        projects.AddRange(matches); // possibly adding empty collection here, but no cost for simpler code.

                        // We only emit non-terminating error record if a non-wildcarded name was not found.
                        // This is consistent with built-in cmdlets that support wildcarded search.
                        // A search with a wildcard that returns nothing should not be considered an error.
                        if ((matches.Count() == 0) && !WildcardPattern.ContainsWildcardCharacters(projectName)) {
                            ErrorHandler.WriteProjectNotFoundError(projectName, terminating: false);
                        }
                    }
                }
                WriteObject(projects, enumerateCollection: true);
            }
        }
    }
}
