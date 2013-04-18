using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using NuGet.VisualStudio.Types;

namespace NuGet.VisualStudio
{
    public abstract class SettingsManagerBase
    {
        private readonly Lazy<ISettingsManager> _settingsManager;

        protected SettingsManagerBase(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            _settingsManager = new Lazy<ISettingsManager>
                (
                    () => (VsVersionHelper.IsVisualStudio2010 || VsVersionHelper.IsVisualStudio2012)
                            ? new SettingsManagerWrapper(serviceProvider)
                            : LoadSettingsManager(serviceProvider)
                );
        }

        private ISettingsManager LoadSettingsManager(IServiceProvider serviceProvider)
        {
            if (VsVersionHelper.IsVisualStudio2010 || VsVersionHelper.IsVisualStudio2012)
            {
                return LoadSettingsManagerForVS10And11(serviceProvider);
            }
            else
            {
                return LoadSettingsManagerForVS12(serviceProvider);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ISettingsManager LoadSettingsManagerForVS10And11(IServiceProvider serviceProvider)
        {
            return new SettingsManagerWrapper(serviceProvider);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ISettingsManager LoadSettingsManagerForVS12(IServiceProvider serviceProvider)
        {
            return new NuGet.VisualStudio12.SettingsManagerWrapper(serviceProvider);
        }

        protected bool ReadBool(string settingsRoot, string property, bool defaultValue = false)
        {
            var userSettingsStore = _settingsManager.Value.GetReadOnlySettingsStore();
            if (userSettingsStore.CollectionExists(settingsRoot))
            {
                return userSettingsStore.GetBoolean(settingsRoot, property, defaultValue);
            }
            else
            {
                return defaultValue;
            }
        }

        protected void WriteBool(string settingsRoot, string property, bool value)
        {
            IWritableSettingsStore userSettingsStore = GetWritableSettingsStore(settingsRoot);
            userSettingsStore.SetBoolean(settingsRoot, property, value);
        }

        protected int ReadInt32(string settingsRoot, string property, int defaultValue = 0)
        {
            var userSettingsStore = _settingsManager.Value.GetReadOnlySettingsStore();
            if (userSettingsStore.CollectionExists(settingsRoot))
            {
                return userSettingsStore.GetInt32(settingsRoot, property, defaultValue);
            }
            else
            {
                return defaultValue;
            }
        }

        protected void WriteInt32(string settingsRoot, string property, int value)
        {
            IWritableSettingsStore userSettingsStore = GetWritableSettingsStore(settingsRoot);
            userSettingsStore.SetInt32(settingsRoot, property, value);
        }

        protected string ReadString(string settingsRoot, string property, string defaultValue = "")
        {
            var userSettingsStore = _settingsManager.Value.GetReadOnlySettingsStore();
            if (userSettingsStore.CollectionExists(settingsRoot))
            {
                return userSettingsStore.GetString(settingsRoot, property, defaultValue);
            }
            else
            {
                return defaultValue;
            }
        }

        protected string[] ReadStrings(string settingsRoot, string[] properties, string defaultValue = "")
        {
            var userSettingsStore = _settingsManager.Value.GetReadOnlySettingsStore();
            if (userSettingsStore.CollectionExists(settingsRoot))
            {
                string[] values = new string[properties.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = userSettingsStore.GetString(settingsRoot, properties[i], defaultValue);
                }
                return values;
            }
            else
            {
                return null;
            }
        }

        protected bool DeleteProperty(string settingsRoot, string property)
        {
            IWritableSettingsStore userSettingsStore = GetWritableSettingsStore(settingsRoot);
            return userSettingsStore.DeleteProperty(settingsRoot, property);
        }

        protected void WriteStrings(string settingsRoot, string[] properties, string[] values)
        {
            Debug.Assert(properties.Length == values.Length);

            IWritableSettingsStore userSettingsStore = GetWritableSettingsStore(settingsRoot);
            for (int i = 0; i < properties.Length; i++)
            {
                userSettingsStore.SetString(settingsRoot, properties[i], values[i]);
            }
        }

        protected void WriteString(string settingsRoot, string property, string value)
        {
            IWritableSettingsStore userSettingsStore = GetWritableSettingsStore(settingsRoot);

            userSettingsStore.SetString(settingsRoot, property, value);
        }

        protected void ClearAllSettings(string settingsRoot)
        {
            IWritableSettingsStore userSettingsStore = _settingsManager.Value.GetWritableSettingsStore();
            if (userSettingsStore.CollectionExists(settingsRoot))
            {
                userSettingsStore.DeleteCollection(settingsRoot);
            }
        }

        private IWritableSettingsStore GetWritableSettingsStore(string settingsRoot)
        {
            IWritableSettingsStore userSettingsStore = _settingsManager.Value.GetWritableSettingsStore();
            if (!userSettingsStore.CollectionExists(settingsRoot))
            {
                userSettingsStore.CreateCollection(settingsRoot);
            }
            return userSettingsStore;
        }
    }
}