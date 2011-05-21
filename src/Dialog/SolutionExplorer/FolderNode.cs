using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace NuGet.Dialog {
    public class FolderNode : ProjectNodeBase {
        private readonly ICollection<ProjectNodeBase> _children;
        private bool _suppressPropagatingIsSelectedProperty;
        private bool _isExpanded = true;

        public FolderNode(string name, ICollection<ProjectNodeBase> children) :
            base(name) {

            if (children == null) {
                throw new ArgumentNullException("children");
            }
            _children = children;

            foreach (var child in _children) {
                child.Parent = this;
            }
        }

        public bool IsRootFolder {
            get {
                return Parent == null;
            }
        }

        public bool IsExpanded {
            get {
                return _isExpanded;
            }
            set {
                if (_isExpanded != value) {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }
            }
        }

        public ICollection<ProjectNodeBase> Children {
            get {
                return _children;
            }
        }

        protected override void OnSelectedChanged() {
            base.OnSelectedChanged();

            if (_suppressPropagatingIsSelectedProperty) {
                return;
            }

            bool? isSelected = IsSelected;
            // propagate the IsSelected value down to all childrens, recursively
            foreach (ProjectNodeBase child in _children) {
                child.OnParentIsSelectedChange(isSelected);
            }
        }

        public override IEnumerable<Project> GetSelectedProjects() {
            return Children.SelectMany(p => p.GetSelectedProjects());
        }

        // invoked whenever one of its descendent nodes has its IsSelected property changed directly by user.
        internal void OnChildIsSelectedChanged() {
            // Here we detect the IsSelected states of all the direct children.
            // If all children are selected, mark this node as selected.
            // If all children are unselected, mark this node as unselected.
            // Otherwise, mark this node as Indeterminate state.

            bool isAllSelected = true, isAllUnselected = true;
            foreach (var child in Children) {
                if (child.IsSelected != true) {
                    isAllSelected = false;
                }
                else if (child.IsSelected != false) {
                    isAllUnselected = false;
                }
            }

            // don't propagate the change back to children.
            // otherwise, we'll fall into an infinite loop.
            _suppressPropagatingIsSelectedProperty = true;
            if (isAllSelected) {
                IsSelected = true;
            }
            else if (isAllUnselected) {
                IsSelected = false;
            }
            else {
                IsSelected = null;
            }
            _suppressPropagatingIsSelectedProperty = false;
        }
    }
}