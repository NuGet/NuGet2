using System.Collections.Generic;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Extensions
{
    public class VersionExtensionsTest
    {
        public static IEnumerable<object[]> LotsOfVersions
        {
            get
            {
                yield return new object[] { "1.0", "1.0", true }; // Min Version Inclusive, Equal To => Match
                yield return new object[] { "(1.0,)", "1.0", false }; // Min Version Exclusive, Equal To => No Match
                yield return new object[] { "1.0", "1.1", true }; // Min Version Inclusive, Greater Than => Match
                yield return new object[] { "(1.0,)", "1.1", true }; // Min Version Exclusive, Greater Than => Match
                yield return new object[] { "1.0", "0.1", false }; // Min Version Inclusive, Less Than => No Match
                yield return new object[] { "(1.0,)", "0.1", false }; // Min Version Exclusive, Less Than => No Match
                yield return new object[] { "[,1.0]", "1.0", true }; // Max Version Inclusive, Equal To => Match
                yield return new object[] { "(,1.0)", "1.0", false }; // Max Version Exclusive, Equal To => No Match
                yield return new object[] { "[,1.0]", "0.1", true }; // Max Version Inclusive, Less Than => Match
                yield return new object[] { "(,1.0)", "0.1", true }; // Max Version Exclusive, Less Than => Match
                yield return new object[] { "[,1.0]", "1.1", false }; // Max Version Inclusive, Greater Than => No Match
                yield return new object[] { "(,1.0)", "1.1", false }; // Max Version Exclusive, Greater Than => No Match
                yield return new object[] { "(0.5,1.0]", "0.5", false }; // Min Exclusive and Max, Equal Min => No Match
                yield return new object[] { "(0.5,1.0]", "0.7", true }; // Min Exclusive and Max, Greater Than Min, Less Than Max => Match
                yield return new object[] { "(0.5,1.0]", "0.4", false }; // Min Exclusive and Max, Less Than Min, Less Than Max => No Match
                yield return new object[] { "(0.5,1.0]", "1.4", false }; // Min Exclusive and Max, Greater Than Min, Greater Than Max => No Match
                yield return new object[] { "(0.5,1.0]", "1.0", true }; // Min Exclusive and Max, Equal Max => Match
                yield return new object[] { "[0.5,1.0)", "1.0", false }; // Max Exclusive and Min, Equal Max => No Match
                yield return new object[] { "[0.5,1.0)", "0.7", true }; // Max Exclusive and Min, Greater Than Min, Less Than Max => Match
                yield return new object[] { "[0.5,1.0)", "0.4", false }; // Max Exclusive and Min, Less Than Min, Less Than Max => No Match
                yield return new object[] { "[0.5,1.0)", "1.4", false }; // Max Exclusive and Min, Greater Than Min, Greater Than Max => No Match
                yield return new object[] { "[0.5,1.0)", "0.5", true }; // Max Exclusive and Min, Equal Min => Match
            }
        }

        [Fact]
        public void ToDelegateRequiresNonNullVersionSpec()
        {
            ExceptionAssert.ThrowsArgNull(() => VersionExtensions.ToDelegate(null), "versionInfo");
        }

        [Fact]
        public void ToDelegateWithExtractorRequiresNonNullParameters()
        {
            ExceptionAssert.ThrowsArgNull(() => VersionExtensions.ToDelegate<IPackage>(null, p => p.Version), "versionInfo");
            ExceptionAssert.ThrowsArgNull(() => VersionExtensions.ToDelegate<IPackage>(new VersionSpec(new SemanticVersion(1, 0, 0, 0)), null), "extractor");
        }

        [Theory]
        [PropertyData("LotsOfVersions")]
        public void ToDelegateOutputWorksWithPlainSemVers(string verSpec, string semVer, bool expected)
        {
            // Arrange
            IVersionSpec spec = VersionUtility.ParseVersionSpec(verSpec);
            SemanticVersion ver = new SemanticVersion(semVer);

            // Act/Assert
            Assert.Equal(expected, spec.ToDelegate<SemanticVersion>(v => v)(ver));
        }

        [Theory]
        [PropertyData("LotsOfVersions")]
        public void ToDelegateOutputWorksWithPackages(string verSpec, string semVer, bool expected)
        {
            // Arrange
            IVersionSpec spec = VersionUtility.ParseVersionSpec(verSpec);
            Mock<IPackage> mockPkg = new Mock<IPackage>();
            mockPkg.Setup(p => p.Version).Returns(new SemanticVersion(semVer));

            // Act/Assert
            Assert.Equal(expected, spec.ToDelegate()(mockPkg.Object));
        }

        [Theory]
        [PropertyData("LotsOfVersions")]
        public void SatisfiesReturnsExpectedValues(string verSpec, string semVer, bool expected)
        {
            // Arrange
            IVersionSpec spec = VersionUtility.ParseVersionSpec(verSpec);

            // Act/Assert
            Assert.Equal(expected, spec.Satisfies(new SemanticVersion(semVer)));
        }
    }
}
