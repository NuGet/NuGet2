using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class ManifestFileTest {
        [TestMethod]
        public void ManifestFileReturnsNoValidationResultsIfSourceAndTargetPathAreValid() {
            // Arrange
            var manifestFile = new ManifestFile { Source = @"bin\release\MyLib.dll", Target = @"lib" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext);

            // Assert
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void ManifestFileReturnsNoValidationResultIfSourceContainsWildCardCharacters() {
            // Arrange
            var manifestFile = new ManifestFile { Source = @"b?n\**\*.dll", Target = @"lib" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext);

            // Assert
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void ManifestFileReturnsValidationResultIfSourceContainsInvalidCharacters() {
            // Arrange
            var manifestFile = new ManifestFile { Source = @"bin\\|\\*.dll", Target = @"lib" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(@"Source path 'bin\\|\\*.dll' contains invalid characters.", result.Single().ErrorMessage);
        }

        [TestMethod]
        public void ManifestFileReturnsValidationResultIfTargetContainsInvalidCharacters() {
            // Arrange
            var manifestFile = new ManifestFile { Source = @"bin\\**\\*.dll", Target = @"lib\\|\\net40" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(@"Target path 'lib\\|\\net40' contains invalid characters.", result.Single().ErrorMessage);
        }

        [TestMethod]
        public void ManifestFileReturnsValidationResultsIfSourceAndTargetContainsInvalidCharacters() {
            // Arrange
            var manifestFile = new ManifestFile { Source = @"bin|\\**\\*.dll", Target = @"lib\\|\\net40" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(@"Source path 'bin|\\**\\*.dll' contains invalid characters.", result.First().ErrorMessage);
            Assert.AreEqual(@"Target path 'lib\\|\\net40' contains invalid characters.", result.Last().ErrorMessage);
        }
    }
}
