using System;
using System.Collections.Generic;
using System.ComponentModel;
using EnvDTE;

namespace NuGet.Dialog
{
    public abstract class ProjectNodeBase : INotifyPropertyChanged
    {
        private bool _suppressNotifyParentOfIsSelectedChanged;
        private FolderNode _parent;
        private string _name;
        private bool? _isSelected = false;
        private bool _isEnabled = true;

        protected ProjectNodeBase(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            Name = name;
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "The results need to be calculated dynamically.")]
        public abstract IEnumerable<Project> GetSelectedProjects();

        public FolderNode Parent
        {
            get
            {
                return _parent;
            }
            internal set
            {
                if (_parent != value)
                {
                    _parent = value;
                    OnPropertyChanged("Parent");
                }
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnEnabledChanged();
                    OnPropertyChanged("IsEnabled");
                }
            }
        }

        public bool? IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnSelectedChanged();
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        protected virtual void OnSelectedChanged()
        {
            if (_suppressNotifyParentOfIsSelectedChanged)
            {
                return;
            }

            if (Parent != null)
            {
                Parent.OnChildSelectedChanged();
            }
        }

        private void OnEnabledChanged()
        {
            if (Parent != null)
            {
                Parent.OnChildEnabledChanged();
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void OnParentIsSelectedChange(bool? isSelected)
        {
            // When the parent folder is checked or unchecked by user, 
            // we want to apply the same state to all of its descending.
            // But we don't want to notify this back to the parent,
            // hence the suppression. Otherwise, we'll fall into an infinite loop.

            // However, only do the above if this node is enabled.
            if (IsEnabled)
            {
                _suppressNotifyParentOfIsSelectedChanged = true;
                IsSelected = isSelected;
                _suppressNotifyParentOfIsSelectedChanged = false;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}