using System.Collections.Generic;
using System.Linq;

namespace NuGet
{
    /// <summary>
    /// Simple builder for a <see cref="ProjectFileProcessingExecutor" />
    /// </summary>
    public class ProjectFileProcessingBuilder
    {
        readonly IList<IProjectFileProcessor> _processors;

        public ProjectFileProcessingBuilder(IEnumerable<IProjectFileProcessor> processors)
        {
            _processors = processors == null
                              ? new List<IProjectFileProcessor>()
                              : processors.ToList();
        }

        public ProjectFileProcessingBuilder Clone()
        {
            return new ProjectFileProcessingBuilder(_processors);
        }

        public ProjectFileProcessingBuilder WithProcessor(IProjectFileProcessor processor)
        {
            _processors.Add(processor);

            return this;
        }

        public ProjectFileProcessingBuilder WithProcessors(IEnumerable<IProjectFileProcessor> processors)
        {
            _processors.AddRange(processors);

            return this;
        }

        public ProjectFileProcessingExecutor Build(IProjectFileProcessingProject project)
        {

            return new ProjectFileProcessingExecutor(project,_processors);
        }

        private static readonly object LockObject = new object();
        private static volatile ProjectFileProcessingBuilder _default;

        public static ProjectFileProcessingBuilder Default
        {
            get
            {
                if (_default == null)
                {
                    lock (LockObject)
                    {
                        if (_default == null)
                        {
                            _default = new ProjectFileProcessingBuilder(null);
                        }
                    }
                }

                return _default;
            }
            set
            {
                _default = value;
            }
        }
    }
}