using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using EnvDTE;

namespace NuGet.Dialog
{
    public class FolderNode : ProjectNodeBase
    {
        private readonly ICollection<ProjectNodeBase> _children;
        private bool _suppressPropagatingIsSelectedProperty;
        private bool _isExpanded = true;
        private readonly Project _project;
        private static ImageSource _expandedIcon, _collapsedIcon;

        public FolderNode(Project project, string name, ICollection<ProjectNodeBase> children) 
            : base(name)
        {

            if (children == null)
            {
                throw new ArgumentNullException("children");
            }
            _children = children;
            _project = project;

            if (children.Count > 0)
            {
                foreach (var child in _children)
                {
                    child.Parent = this;
                }
                OnChildSelectedChanged();
                OnChildEnabledChanged();
            }
        }

        public Project Project
        {
            get
            {
                return _project;
            }
        }

        public bool HasProjects
        {
            get
            {
                return Children.Any(p => (p is ProjectNode) || (p as FolderNode).HasProjects);
            }
        }

        public bool IsRootFolder
        {
            get
            {
                return Parent == null;
            }
        }

        public ImageSource Icon
        {
            get
            {
                if (IsRootFolder)
                {
                    return ProjectUtilities.GetSolutionImage();
                }

                if (IsExpanded)
                {
                    if (_expandedIcon == null)
                    {
                        _expandedIcon = ProjectUtilities.GetImage(Project, folderExpandedView: true);
                    }
                    return _expandedIcon;
                }
                else
                {
                    if (_collapsedIcon == null)
                    {
                        _collapsedIcon = ProjectUtilities.GetImage(Project);
                    }
                    return _collapsedIcon;
                }
            }
        }

        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                    OnPropertyChanged("Icon");
                }
            }
        }

        public ICollection<ProjectNodeBase> Children
        {
            get
            {
                return _children;
            }
        }

        protected override void OnSelectedChanged()
        {
            base.OnSelectedChanged();

            if (_suppressPropagatingIsSelectedProperty)
            {
                return;
            }

            bool? isSelected = IsSelected;
            // propagate the IsSelected value down to all children, recursively
            foreach (ProjectNodeBase child in _children)
            {
                child.OnParentIsSelectedChange(isSelected);
            }
        }

        public override IEnumerable<Project> GetSelectedProjects()
        {
            return Children.SelectMany(p => p.GetSelectedProjects());
        }

        // invoked whenever one of its descendent nodes has its IsSelected property changed directly by user.
        internal void OnChildSelectedChanged()
        {
            // Here we detect the IsSelected states of all the direct children.
            // If all children are selected, mark this node as selected.
            // If all children are unselected, mark this node as unselected.
            // Otherwise, mark this node as Indeterminate state.

            bool isAllSelected = true, isAllUnselected = true;
            foreach (var child in Children)
            {
                if (child.IsSelected != true)
                {
                    isAllSelected = false;
                }
                else if (child.IsSelected != false)
                {
                    isAllUnselected = false;
                }
            }

            // don't propagate the change back to children.
            // otherwise, we'll fall into an infinite loop.
            _suppressPropagatingIsSelectedProperty = true;
            if (isAllSelected)
            {
                IsSelected = true;
            }
            else if (isAllUnselected)
            {
                IsSelected = false;
            }
            else
            {
                IsSelected = null;
            }
            _suppressPropagatingIsSelectedProperty = false;
        }

        // invoked whenever one of its descendent nodes has its IsEnabled property changed.
        internal void OnChildEnabledChanged()
        {
            // enable this node if at least one of the children node is enabled
            IsEnabled = Children.Any(c => c.IsEnabled);
        }
    }
}