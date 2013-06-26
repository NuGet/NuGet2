using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Settings;

namespace NuGet.VisualStudio
{
    internal class WritableSettingsStoreWrapper : SettingsStoreWrapper, IWritableSettingsStore
    {
        private readonly WritableSettingsStore _store;

        public WritableSettingsStoreWrapper(WritableSettingsStore store)
            : base(store)
        {
            _store = store;
        }

        public void DeleteCollection(string collection)
        {
            _store.DeleteCollection(collection);
        }

        public void CreateCollection(string collection)
        {
            _store.CreateCollection(collection);
        }

        public bool DeleteProperty(string collection, string propertyName)
        {
            return _store.DeleteProperty(collection, propertyName);
        }

        public void SetBoolean(string collection, string propertyName, bool value)
        {
            _store.SetBoolean(collection, propertyName, value);
        }

        public void SetInt32(string collection, string propertyName, int value)
        {
            _store.SetInt32(collection, propertyName, value);
        }

        public void SetString(string collection, string propertyName, string value)
        {
            _store.SetString(collection, propertyName, value);
        }
    }
}
