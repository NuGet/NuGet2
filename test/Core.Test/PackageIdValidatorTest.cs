using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {    
    [TestClass]
    public class PackageIdValidatorTest {
        [TestMethod]
        public void ValidatePackageIdInvalidIdThrows() {
            // Arrange
            string packageId = "  Invalid  . Woo   .";

            // Act & Assert
            ExceptionAssert.ThrowsArgumentException(() => PackageIdValidator.ValidatePackageId(packageId), "Package id '  Invalid  . Woo   .' is invalid.");
        }

        [TestMethod]
        public void EmptyIsNotValid() {
            // Arrange
            string packageId = "";

            // Act
            bool isValid = PackageIdValidator.IsValidPackageId(packageId);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void NullThrowsException() {
            // Arrange
            string packageId = null;

            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => PackageIdValidator.IsValidPackageId(packageId), "packageId");
        }

        [TestMethod]
        public void AlphaNumericIsValid() {
            // Arrange
            string packageId = "42This1Is4You";

            // Act
            bool isValid = PackageIdValidator.IsValidPackageId(packageId);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void MultipleDotSeparatorsAllowed() {
            // Arrange
            string packageId = "I.Like.Writing.Unit.Tests";

            // Act
            bool isValid = PackageIdValidator.IsValidPackageId(packageId);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void NumbersAndWordsDotSeparatedAllowd() {
            // Arrange
            string packageId = "1.2.3.4.Uno.Dos.Tres.Cuatro";

            // Act
            bool isValid = PackageIdValidator.IsValidPackageId(packageId);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void UnderscoreDotAndDashSeparatorsAreValid() {
            // Arrange
            string packageId = "Nu_Get.Core-IsCool";

            // Act
            bool isValid = PackageIdValidator.IsValidPackageId(packageId);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void NonAlphaNumericUnderscoreDotDashIsInvalid() {
            // Arrange
            string packageId = "ILike*Asterisks";

            // Act
            bool isValid = PackageIdValidator.IsValidPackageId(packageId);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void ConsecutiveSeparatorsNotAllowed() {
            // Arrange
            string packageId = "I_.Like.-Separators";

            // Act
            bool isValid = PackageIdValidator.IsValidPackageId(packageId);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void StartingWithSeparatorsNotAllowed() {
            // Arrange
            string packageId = "-StartWithSeparator";

            // Act
            bool isValid = PackageIdValidator.IsValidPackageId(packageId);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void EndingWithSeparatorsNotAllowed() {
            // Arrange
            string packageId = "StartWithSeparator.";

            // Act
            bool isValid = PackageIdValidator.IsValidPackageId(packageId);

            // Assert
            Assert.IsFalse(isValid);
        }
    }
}
