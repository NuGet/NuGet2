using System;
using System.Security.Cryptography;

namespace NuGet
{
    public class CryptoHashProvider : IHashProvider
    {
        private readonly Func<HashAlgorithm> _algorithmFactory;

        public CryptoHashProvider()
            : this(SHA512.Create)
        {
        }

        public CryptoHashProvider(Func<HashAlgorithm> algorithmFactory)
        {
            _algorithmFactory = algorithmFactory;
        }

        public byte[] CalculateHash(byte[] data)
        {
            using (HashAlgorithm algorithm = _algorithmFactory())
            {
                return algorithm.ComputeHash(data);
            }
        }

        public bool VerifyHash(byte[] data, byte[] hash)
        {
            byte[] dataHash = CalculateHash(data);
            for (int i = 0; i < dataHash.Length; i++)
            {
                if (dataHash[i] != hash[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
