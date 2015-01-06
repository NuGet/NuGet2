using NuGet.ProjectManagement;
using NuGet.Resolver;
using System.Collections.Generic;
using Resx = NuGet.Client.VisualStudio.UI.Resources;

namespace NuGet.Client.VisualStudio.UI
{
    public class Options
    {
        public Options()
        {
            ShowPreviewWindow = true;
            CreateFileConflictActions();
            CreateDependencyBehaviors();
        }
        
        private void CreateFileConflictActions()
        {
            _fileConflicActions = new[]
            {
                new FileConflictActionItem(Resx.Resources.FileConflictAction_Prompt, FileConflictAction.PromptUser),
                new FileConflictActionItem(Resx.Resources.FileConflictAction_IgnoreAll, FileConflictAction.IgnoreAll),
                new FileConflictActionItem(Resx.Resources.FileConflictAction_OverwriteAll, FileConflictAction.OverwriteAll)
            };

            SelectedFileConflictAction = _fileConflicActions[0];
        }

        private void CreateDependencyBehaviors()
        {
            _dependencyBehaviors = new[] 
            {
                new DependencyBehaviorItem(Resx.Resources.DependencyBehavior_IgnoreDependencies, DependencyBehavior.Ignore),
                new DependencyBehaviorItem(Resx.Resources.DependencyBehavior_Lowest, DependencyBehavior.Lowest),
                new DependencyBehaviorItem(Resx.Resources.DependencyBehavior_HighestPatch, DependencyBehavior.HighestPatch),
                new DependencyBehaviorItem(Resx.Resources.DependencyBehavior_HighestMinor, DependencyBehavior.HighestMinor),
                new DependencyBehaviorItem(Resx.Resources.DependencyBehavior_Highest, DependencyBehavior.Highest),
            };
            SelectedDependencyBehavior = _dependencyBehaviors[1];
        }

        private FileConflictActionItem[] _fileConflicActions;

        public IEnumerable<FileConflictActionItem> FileConflictActions
        {
            get
            {
                return _fileConflicActions;
            }
        }

        public FileConflictActionItem SelectedFileConflictAction
        {
            get;
            set;
        }

        private DependencyBehaviorItem[] _dependencyBehaviors;

        public IEnumerable<DependencyBehaviorItem> DependencyBehaviors
        {
            get
            {
                return _dependencyBehaviors;
            }
        }

        public DependencyBehaviorItem SelectedDependencyBehavior
        {
            get;
            set;
        }

        public bool ShowPreviewWindow
        {
            get;
            set;
        }

        public bool RemoveDependencies
        {
            get;
            set;
        }

        public bool ForceRemove
        {
            get;
            set;
        }
    }
}
