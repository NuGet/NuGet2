using System.Collections.Generic;
using System.Linq;

namespace NuGet
{
    /// <summary>
    /// <para>Simple builder for a <see cref="ProjectFileProcessingExecutor"/></para>
    /// </summary>
    public class ProjectFileProcessingBuilder
    {
        readonly IEnumerable<IProjectFileProcessor> _processors;

        public ProjectFileProcessingBuilder(
            IEnumerable<IProjectFileProcessor> processors)
        {
            _processors = processors ??
                          new IProjectFileProcessor[] {};
        }

        public ProjectFileProcessingBuilder WithProcessor(
            IProjectFileProcessor processor)
        {
            return
                new ProjectFileProcessingBuilder(
                    _processors.Concat(new[] {processor}));
        }

        public ProjectFileProcessingExecutor Build(
            IProjectFileProcessingProject propertiesProject)
        {
            return new ProjectFileProcessingExecutor(
                propertiesProject,
                _processors);
        }
    }
}