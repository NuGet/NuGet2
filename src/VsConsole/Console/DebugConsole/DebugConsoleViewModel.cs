using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NuGetConsole.DebugConsole
{
    public class DebugConsoleViewModel : INotifyPropertyChanged
    {
        private DebugConsoleLevel _activeLevel = DebugConsoleLevel.Trace;
        
        public DebugConsoleLevel ActiveLevel
        {
            get { return _activeLevel; }
            set { SetProperty(ref _activeLevel, value); }
        }

        public IEnumerable<DebugConsoleLevel> AvailableLevels
        {
            get
            {
                return Enum.GetValues(typeof(DebugConsoleLevel)).Cast<DebugConsoleLevel>();
            }
        }

        #region INotifyPropertyChanged
        // This section is ripe for plucking up into a base class!
        protected virtual void SetProperty<T>(ref T _field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(_field, newValue))
            {
                _field = newValue;
                RaisePropertyChanged(propertyName);
            }
        }

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }

    public enum DebugConsoleLevel {
        Trace = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}
