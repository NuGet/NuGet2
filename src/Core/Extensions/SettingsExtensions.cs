using System;
using System.Security.Cryptography;
using System.Text;

namespace NuGet
{
    public static class SettingsExtensions
    {
        private const string ConfigSection = "config";
        private static readonly byte[] _entropyBytes = StringToBytes("NuGet");

        public static string GetDecryptedValue(this ISettings settings, string section, string key)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }

            var encryptedString = settings.GetValue(section, key);
            if (encryptedString == null)
            {
                return null;
            }
            if (String.IsNullOrEmpty(encryptedString))
            {
                return String.Empty;
            }
            return DecryptString(encryptedString);
        }

        public static void SetEncryptedValue(this ISettings settings, string section, string key, string value)
        {
            if (String.IsNullOrEmpty(section))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "section");
            }
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "key");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (String.IsNullOrEmpty(value))
            {
                settings.SetValue(section, key, String.Empty);
            }
            else
            {
                var encryptedString = EncryptString(value);
                settings.SetValue(section, key, encryptedString);
            }
        }

        /// <summary>
        /// Retrieves a config value for the specified key
        /// </summary>
        /// <param name="settings">The settings instance to retrieve </param>
        /// <param name="key">The key to look up</param>
        /// <param name="decrypt">Determines if the retrieved value needs to be decrypted.</param>
        /// <returns>Null if the key was not found, value from config otherwise.</returns>
        public static string GetConfigValue(this ISettings settings, string key, bool decrypt = false)
        {
            return decrypt ? settings.GetDecryptedValue(ConfigSection, key) : settings.GetValue(ConfigSection, key);
        }

        /// <summary>
        /// Sets a config value in the setting.
        /// </summary>
        /// <param name="settings">The settings instance to store the key-value in.</param>
        /// <param name="key">The key to store.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="encrypt">Determines if the value needs to be encrypted prior to storing.</param>
        public static void SetConfigValue(this ISettings settings, string key, string value, bool encrypt = false)
        {
            if (encrypt == true)
            {
                settings.SetEncryptedValue(ConfigSection, key, value);
            }
            else
            {
                settings.SetValue(ConfigSection, key, value);
            }
        }

        /// <summary>
        /// Deletes a config value from settings
        /// </summary>
        /// <param name="settings">The settings instance to delete the key from.</param>
        /// <param name="key">The key to delete.</param>
        /// <returns>True if the value was deleted, false otherwise.</returns>
        public static bool DeleteConfigValue(this ISettings settings, string key)
        {
            return settings.DeleteValue(ConfigSection, key);
        }

        internal static string EncryptString(string value)
        {
            var decryptedByteArray = StringToBytes(value);
            var encryptedByteArray = ProtectedData.Protect(decryptedByteArray, _entropyBytes, DataProtectionScope.CurrentUser);
            var encryptedString = Convert.ToBase64String(encryptedByteArray);
            return encryptedString;
        }

        internal static string DecryptString(string encryptedString)
        {
            var encryptedByteArray = Convert.FromBase64String(encryptedString);
            var decryptedByteArray = ProtectedData.Unprotect(encryptedByteArray, _entropyBytes, DataProtectionScope.CurrentUser);
            return BytesToString(decryptedByteArray);
        }

        private static byte[] StringToBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        private static string BytesToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
