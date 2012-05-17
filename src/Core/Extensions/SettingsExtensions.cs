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

        public static string GetConfigValue(this ISettings settings, string key)
        {
            return settings.GetValue(ConfigSection, key);
        }

        public static void SetConfigValue(this ISettings settings, string key, string value)
        {
            settings.SetValue(ConfigSection, key, value);
        }

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
