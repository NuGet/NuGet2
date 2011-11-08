using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Test
{
    public class CryptoHashProviderTest
    {
        [Fact]
        public void DefaultCryptoHashProviderUsesSHA512()
        {
            // Arrange
            byte[] testBytes = Encoding.UTF8.GetBytes("There is no butter knife");
            string expectedHash = "xy/brd+/mxheBbyBL7i8Oyy62P2ZRteaIkfc4yA8ncH1MYkbDo+XwBcZsOBY2YeaOucrdLJj5odPvozD430w2g==";
            IHashProvider hashProvider = new CryptoHashProvider();

            // Act
            byte[] actualHash = hashProvider.CalculateHash(testBytes);

            // Assert
            Assert.Equal(actualHash, Convert.FromBase64String(expectedHash));
        }

        [Fact]
        public void CryptoHashProviderReturnsTrueIfHashAreEqual()
        {
            // Arrange
            byte[] testBytes = Encoding.UTF8.GetBytes("There is no butter knife");
            string expectedHash = "xy/brd+/mxheBbyBL7i8Oyy62P2ZRteaIkfc4yA8ncH1MYkbDo+XwBcZsOBY2YeaOucrdLJj5odPvozD430w2g==";
            IHashProvider hashProvider = new CryptoHashProvider();

            // Act
            bool result = hashProvider.VerifyHash(testBytes, Convert.FromBase64String(expectedHash));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CryptoHashProviderReturnsFalseIfHashValuesAreUnequal()
        {
            // Arrange
            byte[] testBytes = Encoding.UTF8.GetBytes("There is no butter knife");
            byte[] badBytes = Encoding.UTF8.GetBytes("this is a bad input");
            IHashProvider hashProvider = new CryptoHashProvider();


            // Act
            byte[] testHash = hashProvider.CalculateHash(testBytes);
            byte[] badHash = hashProvider.CalculateHash(badBytes);
            bool result = hashProvider.VerifyHash(testHash, badHash);

            // Assert
            Assert.False(result);
        }

        // Ensures this issue is fixed: http://nuget.codeplex.com/workitem/1489
        [Fact]
        public void CryptoHashProviderIsThreadSafe()
        {
            // Arrange
            byte[] testBytes = Encoding.UTF8.GetBytes("There is no butter knife");
            string expectedHash = "xy/brd+/mxheBbyBL7i8Oyy62P2ZRteaIkfc4yA8ncH1MYkbDo+XwBcZsOBY2YeaOucrdLJj5odPvozD430w2g==";
            IHashProvider hashProvider = new CryptoHashProvider();

            Parallel.For(0, 10000, ignored =>
            {
                // Act
                byte[] actualHash = hashProvider.CalculateHash(testBytes);

                // Assert
                Assert.Equal(actualHash, Convert.FromBase64String(expectedHash));
            });
        }
    }
}
