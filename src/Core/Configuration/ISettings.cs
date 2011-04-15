using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NuGet {
    public interface ISettings {
        string GetValue(string section, string key);
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is the best fit for this internal class")]
        IList<KeyValuePair<string, string>> GetValues(string section);
        void SetValue(string section, string key, string value);
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is the best fit for this internal class")]
        void SetValues(string section, IList<KeyValuePair<string, string>> values);
        bool DeleteValue(string section, string key);
        bool DeleteSection(string section);
    }
}
