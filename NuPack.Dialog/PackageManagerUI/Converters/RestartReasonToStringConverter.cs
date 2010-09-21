using System;
using System.Windows.Data;

namespace NuPack.Dialog.PackageManagerUI {
    public class RestartReasonToStringConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value == null) {
                return null;
            }

            //     if (!(value is RestartReason))
            //         throw new ArgumentException("'value' was not of type RestartReason", "value");

            string restartMessage = null;
            //    switch ((RestartReason)value)
            //    {
            //        case RestartReason.None:
            //            restartMessage = null;
            //            break;
            //        default:
            //            restartMessage = Resources.RestartDefault;
            //            break;
            //    }

            //            if (restartMessage != null)
            //          {
            //            restartMessage = string.Format(restartMessage, Utilities.ProductName);
            //      }

            return restartMessage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            //We don't need to convert back from the UI, so don't implement this method.
            throw new NotImplementedException();
        }

        #endregion
    }
}
