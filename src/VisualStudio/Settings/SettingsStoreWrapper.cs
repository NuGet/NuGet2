using Microsoft.VisualStudio.Settings;

namespace NuGet.VisualStudio
{
    internal class SettingsStoreWrapper : ISettingsStore
    {
        private readonly SettingsStore _store;

        public SettingsStoreWrapper(SettingsStore store)
        {
            _store = store;
        }

        public bool CollectionExists(string collection)
        {
            return _store.CollectionExists(collection);
        }

        public bool GetBoolean(string collection, string propertyName, bool defaultValue)
        {
            return _store.GetBoolean(collection, propertyName, defaultValue);
        }

        public int GetInt32(string collection, string propertyName, int defaultValue)
        {
            return _store.GetInt32(collection, propertyName, defaultValue);
        }

        public string GetString(string collection, string propertyName, string defaultValue)
        {
            return _store.GetString(collection, propertyName, defaultValue);
        }
    }
}
