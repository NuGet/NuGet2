using System;
using System.ComponentModel;
using System.Globalization;

namespace NuGet {
    [TypeConverter(typeof(SemVerTypeConverter))]
    public class SemVerTypeConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType.Equals(typeof(string));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            var stringValue = value as string;
            SemVer semVer;
            if (stringValue != null && SemVer.TryParse(stringValue, out semVer)) {
                return semVer;
            }
            return null;
        }
    }
}
