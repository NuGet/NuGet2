using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NuGet
{
    /// <summary>
    /// Sets properties on a project item according to rules set
    /// </summary>
    public class ProjectFileProcessingExecutor
    {
        readonly IProjectFileProcessingProject _project;
        readonly ReadOnlyCollection<IProjectFileProcessor> _processors;

        public ProjectFileProcessingExecutor(IProjectFileProcessingProject project, IEnumerable<IProjectFileProcessor> processors)
        {
            if (project == null)
            {
                throw new ArgumentNullException("project");
            }

            _project = project;

            processors = processors ?? new IProjectFileProcessor[0];
            _processors = new ReadOnlyCollection<IProjectFileProcessor>(processors.ToList());
        }

        public void Process(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            var projectItem = _project.GetItem(path);
            if (projectItem == null)
            {
                return;
            }

            var matchingRules =
                from rule in _processors
                where rule.IsMatch(projectItem)
                select rule;

            foreach (var rule in matchingRules)
            {
                rule.Process(projectItem);
            }
        }
    }
}
