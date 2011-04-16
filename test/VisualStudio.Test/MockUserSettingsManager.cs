using System.Collections.Generic;

namespace NuGet.VisualStudio.Test {

    using SectionValues = List<KeyValuePair<string, string>>;

    internal class MockUserSettingsManager : ISettings {

        private readonly Dictionary<string, SectionValues> _settings = new Dictionary<string, SectionValues>();

        public string GetValue(string section, string key) {
            SectionValues values;
            if (_settings.TryGetValue(section, out values)) {
                int index = values.FindIndex(p => p.Key == key);
                return index > -1 ? values[index].Value : null;
            }

            return null;
        }

        public IList<KeyValuePair<string, string>> GetValues(string section) {
            SectionValues values;
            _settings.TryGetValue(section, out values);
            return values;
        }

        public void SetValue(string section, string key, string value) {
            SectionValues values;
            if (!_settings.TryGetValue(section, out values)) {
                values = new SectionValues();
                _settings.Add(section, values);
            }

            int index = values.FindIndex(p => p.Key == key);
            if (index > -1) {
                values[index] = new KeyValuePair<string, string>(key, value);
            }
            else {
                values.Add(new KeyValuePair<string, string>(key, value));
            }
        }

        public void SetValues(string section, IList<KeyValuePair<string, string>> values) {
            foreach (var p in values) {
                SetValue(section, p.Key, p.Value);
            }
        }

        public bool DeleteValue(string section, string key) {
            SectionValues values;
            if (_settings.TryGetValue(section, out values)) {
                int index = values.FindIndex(p => p.Key == key);
                if (index > -1) {
                    values.RemoveAt(index);
                    return true;
                }
                else {
                    return false;
                }
            }

            return false;
        }

        public bool DeleteSection(string section) {
            return _settings.Remove(section);
        }
    }
}
