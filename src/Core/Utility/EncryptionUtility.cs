using System;
using System.Security.Cryptography;
using System.Text;

namespace NuGet
{
    internal static class EncryptionUtility
    {
        private static readonly byte[] _entropyBytes = Encoding.UTF8.GetBytes("NuGet");

        internal static string EncryptString(string value)
        {
            var decryptedByteArray = Encoding.UTF8.GetBytes(value);
            var encryptedByteArray = ProtectedData.Protect(decryptedByteArray, _entropyBytes, DataProtectionScope.CurrentUser);
            var encryptedString = Convert.ToBase64String(encryptedByteArray);
            return encryptedString;
        }

        internal static string DecryptString(string encryptedString)
        {
            var encryptedByteArray = Convert.FromBase64String(encryptedString);
            var decryptedByteArray = ProtectedData.Unprotect(encryptedByteArray, _entropyBytes, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedByteArray);
        }
    }
}
