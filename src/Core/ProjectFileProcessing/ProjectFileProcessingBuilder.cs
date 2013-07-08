using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NuGet
{
    /// <summary>
    ///     <para>
    ///         Simple builder for a <see cref="ProjectFileProcessingExecutor" />
    ///     </para>
    /// </summary>
    public class ProjectFileProcessingBuilder
    {
        readonly IList<IProjectFileProcessor> _processors;

        public ProjectFileProcessingBuilder(
            IEnumerable<IProjectFileProcessor> processors)
        {
            _processors = processors == null
                              ? new List<IProjectFileProcessor>()
                              : processors.ToList();
        }

        public ProjectFileProcessingBuilder Clone()
        {
            return new ProjectFileProcessingBuilder(_processors);
        }

        public ProjectFileProcessingBuilder WithProcessor(
            IProjectFileProcessor processor)
        {
            _processors.Add(processor);

            return this;
        }

        public ProjectFileProcessingExecutor Build(
            IProjectFileProcessingProject propertiesProject)
        {
            return new ProjectFileProcessingExecutor(
                propertiesProject,
                _processors);
        }

        static readonly object LockObject = new object();
        static volatile ProjectFileProcessingBuilder _default;

        public static ProjectFileProcessingBuilder Default
        {
            get
            {
                if (_default == null)
                    lock (LockObject)
                    {
                        if (_default == null)
                            return _default = new ProjectFileProcessingBuilder(null);
                    }

                return _default;
            }
            set { _default = value; }
        }
    }
}