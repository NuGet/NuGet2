using System.ComponentModel;
using System.Windows.Input;

namespace NuGet.WebMatrix
{
    internal class ButtonViewModel : INotifyPropertyChanged
    {
        private ICommand _command;
        private object _commandParameter;
        private string _invariantName;
        private string _text;
        private bool _isCancel;

        public ICommand Command
        {
            get
            {
                return _command;
            }

            set
            {
                if (_command != value)
                {
                    _command = value;
                    this.OnPropertyChanged("Command");
                }
            }
        }

        public object CommandParameter
        {
            get
            {
                return _commandParameter;
            }

            set
            {
                if (_commandParameter != value)
                {
                    _commandParameter = value;
                    this.OnPropertyChanged("CommandParameter");
                }
            }
        }

        public string InvariantName
        {
            get
            {
                return _invariantName;
            }

            set
            {
                if (_invariantName != value)
                {
                    _invariantName = value;
                    this.OnPropertyChanged("InvariantName");
                }
            }
        }

        public string Text
        {
            get
            {
                return _text;
            }

            set
            {
                if (_text != value)
                {
                    _text = value;
                    this.OnPropertyChanged("Text");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsCancel
        {
            get
            {
                return _isCancel;
            }

            set
            {
                if(_isCancel != value)
                {
                    _isCancel = value;
                    this.OnPropertyChanged("IsCancel");
                }
            }
        }

        private void OnPropertyChanged(string name)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
