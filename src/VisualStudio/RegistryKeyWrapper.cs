using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace NuGet.VisualStudio
{
    internal class RegistryKeyWrapper : IRegistryKey
    {
        private RegistryKey _registryKey;

        public RegistryKeyWrapper(RegistryKey registryKey)
        {
            _registryKey = registryKey;
        }

        public IRegistryKey OpenSubKey(string name)
        {
            var key = _registryKey.OpenSubKey(name);

            if (key != null)
            {
                return new RegistryKeyWrapper(key);
            }

            return null;
        }

        public object GetValue(string name)
        {
            return _registryKey.GetValue(name);
        }

        public void Close()
        {
            _registryKey.Close();
        }
    }
}
