using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet {
    public interface ISettings {
        string GetValue(string section, string key);
        IDictionary<string, string> GetValues(string section);
        void SetValue(string section, string key, string value);
        void DeleteValue(string section, string key);
    }
}
