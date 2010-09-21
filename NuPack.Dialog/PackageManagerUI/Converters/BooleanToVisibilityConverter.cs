using System;
using System.Windows;
using System.Windows.Data;

namespace NuPack.Dialog.PackageManagerUI {
    /// <summary>
    /// This BooleanToVisibility converter allows us to override the converted value when
    /// the bound value is false.
    /// 
    /// The built-in converter in WPF restricts us to always use Collapsed when the bound 
    /// value is false.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter {
        public BooleanToVisibilityConverter() {
            //Use the same defaults as the built-in converter to avoid confusion
            WhenTrue = Visibility.Visible;
            WhenFalse = Visibility.Collapsed;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            bool? boolValue = value as bool?;
            if (boolValue == null || !boolValue.HasValue) {
                throw new ArgumentException("Value must be Boolean.", "value");
            }
            if (targetType == null || targetType != typeof(Visibility)) {
                throw new ArgumentException("Must convert to Visibility.", "targetType");
            }

            return boolValue.Value ? WhenTrue : WhenFalse;
        }

        public Visibility WhenTrue {
            get;
            set;
        }

        public Visibility WhenFalse {
            get;
            set;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
