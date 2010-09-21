using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Data;

namespace NuPack.Dialog.PackageManagerUI {
    internal class SizeInBytesConverter : IValueConverter {
        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern long StrFormatByteSize(long fileSize, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer, int bufferSize);

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            const int bufferSize = 30;
            StringBuilder buffer = new StringBuilder(bufferSize);

            //Call the native API method which does this formatting for us.
            StrFormatByteSize(System.Convert.ToInt64(value), buffer, bufferSize);
            return buffer.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            //We don't need to convert back from the UI, so don't implement this method.
            throw new NotImplementedException();
        }

        #endregion
    }
}
