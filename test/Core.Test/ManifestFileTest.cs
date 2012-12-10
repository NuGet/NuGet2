using NuGet.Test.Utility;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace NuGet.Test
{

    public class ManifestFileTest
    {
        [Fact]
        public void ManifestFileReturnsNoValidationResultsIfSourceAndTargetPathAreValid()
        {
            // Arrange
            var manifestFile = new ManifestFile { Source = PathFixUtility.FixPath(@"bin\release\MyLib.dll"), Target = @"lib" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext);

            // Assert
            Assert.False(result.Any());
        }

        [Fact]
        public void ManifestFileReturnsNoValidationResultIfSourceContainsWildCardCharacters()
        {
            // Arrange
            var manifestFile = new ManifestFile { Source = @"b?n\**\*.dll", Target = @"lib" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext);

            // Assert
            Assert.False(result.Any());
        }

        [Fact]
        public void ManifestFileReturnsValidationResultIfSourceContainsInvalidCharacters()
        {
            // Arrange
            var manifestFile = new ManifestFile { Source = @"bin\\|\\*.dll", Target = @"lib" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext).ToList();

            // Assert
            Assert.Equal(1, result.Count);
            Assert.Equal(@"Source path 'bin\\|\\*.dll' contains invalid characters.", result.Single().ErrorMessage);
        }

        [Fact]
        public void ManifestFileReturnsValidationResultIfTargetContainsInvalidCharacters()
        {
            // Arrange
            var manifestFile = new ManifestFile { Source = @"bin\\**\\*.dll", Target = @"lib\\|\\net40" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext).ToList();

            // Assert
            Assert.Equal(1, result.Count);
            Assert.Equal(@"Target path 'lib\\|\\net40' contains invalid characters.", result.Single().ErrorMessage);
        }

        [Fact]
        public void ManifestFileReturnsValidationResultsIfSourceAndTargetContainsInvalidCharacters()
        {
            // Arrange
            var manifestFile = new ManifestFile { Source = @"bin|\\**\\*.dll", Target = @"lib\\|\\net40" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(@"Source path 'bin|\\**\\*.dll' contains invalid characters.", result.First().ErrorMessage);
            Assert.Equal(@"Target path 'lib\\|\\net40' contains invalid characters.", result.Last().ErrorMessage);
        }

        [Fact]
        public void ManifestFileReturnsValidationResultsIfTargetPathContainsWildCardCharacters()
        {
            // Arrange
            var manifestFile = new ManifestFile { Source = @"bin\\**\\*.dll", Target = @"lib\\**\\net40" };
            var validationContext = new ValidationContext(new object(), null, null);

            // Act
            var result = manifestFile.Validate(validationContext).ToList();

            // Assert
            Assert.Equal(1, result.Count);
            Assert.Equal(@"Target path 'lib\\**\\net40' contains invalid characters.", result.Single().ErrorMessage);
        }
    }
}
