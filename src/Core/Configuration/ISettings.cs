using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Configuration {
    public interface ISettings {
        string GetValue(string section, string key);
        IDictionary<string, string> GetValues(string section);
        void SetValue(string section, string key, string value);
    }
}
