using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// <para>Sets properties on a project item according to rules set</para>
    /// </summary>
    public class ProjectFileProcessingExecutor
    {
        readonly IProjectFileProcessingProject _propertiesProject;
        readonly ReadOnlyCollection<IProjectFileProcessor> _processors;

        public ProjectFileProcessingExecutor(
            IProjectFileProcessingProject propertiesProject,
            IEnumerable<IProjectFileProcessor> processors)
        {
            if (propertiesProject == null) throw new ArgumentNullException("propertiesProject");

            _propertiesProject = propertiesProject;

            processors = processors ?? new IProjectFileProcessor[] { };
            _processors = new ReadOnlyCollection<IProjectFileProcessor>(processors.ToList());
        }

        public void Process(string targetPath)
        {
            if (targetPath == null) throw new ArgumentNullException("targetPath");

            var projectItem = _propertiesProject.GetItem(targetPath);

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
