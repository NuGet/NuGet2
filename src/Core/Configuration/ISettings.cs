using System.Collections.Generic;

namespace NuGet {
    public interface ISettings {
        string GetValue(string section, string key);
        ICollection<KeyValuePair<string, string>> GetValues(string section);
        void SetValue(string section, string key, string value);
        void SetValues(string section, ICollection<KeyValuePair<string, string>> values);
        bool DeleteValue(string section, string key);
        bool DeleteSection(string section);
    }
}
