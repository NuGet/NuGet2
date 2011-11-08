using System;
using System.Security.Cryptography;

namespace NuGet
{
    public class CryptoHashProvider : IHashProvider
    {
        /// <remarks>
        /// SHA512CNG is much faster in Windows Vista and higher.
        /// </remarks>
        private static readonly bool _useSHA512Cng = Environment.OSVersion.Version >= new Version(6, 0);

        public byte[] CalculateHash(byte[] data)
        {
            using (var hashAlgorithm = GetHashAlgorithm())
            {
                return hashAlgorithm.ComputeHash(data);
            }
        }

        public bool VerifyHash(byte[] data, byte[] hash)
        {
            byte[] dataHash = CalculateHash(data);

            if (dataHash.Length != hash.Length)
            {
                return false;
            }

            for (int i = 0; i < dataHash.Length; i++)
            {
                if (dataHash[i] != hash[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static HashAlgorithm GetHashAlgorithm()
        {
            return _useSHA512Cng ? SHA512Cng.Create() : SHA512CryptoServiceProvider.Create();
        }
    }
}
