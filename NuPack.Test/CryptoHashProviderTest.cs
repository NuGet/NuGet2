using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class CryptoHashProviderTest {
        [TestMethod]
        public void DefaultCryptoHashProviderUsesSHA512() {
            // Arrange
            byte[] testBytes = Encoding.UTF8.GetBytes("There is no butter knife");
            string expectedHash = "xy/brd+/mxheBbyBL7i8Oyy62P2ZRteaIkfc4yA8ncH1MYkbDo+XwBcZsOBY2YeaOucrdLJj5odPvozD430w2g==";
            IHashProvider hashProvider = new CryptoHashProvider();

            // Act
            byte[] actualHash = hashProvider.CalculateHash(testBytes);

            // Assert
            CollectionAssert.AreEqual(actualHash, Convert.FromBase64String(expectedHash));
        }

        [TestMethod]
        public void CryptoHashProviderReturnsTrueIfHashAreEqual() {
            // Arrange
            byte[] testBytes = Encoding.UTF8.GetBytes("There is no butter knife");
            string expectedHash = "xy/brd+/mxheBbyBL7i8Oyy62P2ZRteaIkfc4yA8ncH1MYkbDo+XwBcZsOBY2YeaOucrdLJj5odPvozD430w2g==";
            IHashProvider hashProvider = new CryptoHashProvider();

            // Act
            bool result = hashProvider.VerifyHash(testBytes, Convert.FromBase64String(expectedHash));

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CryptoHashProviderReturnsFalseIfHashValuesAreUnequal() {
            // Arrange
            byte[] testBytes = Encoding.UTF8.GetBytes("There is no butter knife");
            byte[] badBytes = Encoding.UTF8.GetBytes("this is a bad input");             
            IHashProvider hashProvider = new CryptoHashProvider();


            // Act
            byte[] testHash = hashProvider.CalculateHash(testBytes);
            byte[] badHash = hashProvider.CalculateHash(badBytes);
            bool result = hashProvider.VerifyHash(testHash, badHash);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
