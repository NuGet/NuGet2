using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class SemanticVersionTest
    {
        [Theory]
        [PropertyData("ConstructorData")]
        public void StringConstructorParsesValuesCorrectly(string version, Version versionValue, string specialValue)
        {
            // Act
            SemanticVersion semanticVersion = new SemanticVersion(version);

            // Assert
            Assert.Equal(versionValue, semanticVersion.Version);
            Assert.Equal(specialValue, semanticVersion.SpecialVersion);
            Assert.Equal(version, semanticVersion.ToString());
        }

        public static IEnumerable<object[]> ConstructorData
        {
            get
            {
                yield return new object[] { "1.0.0", new Version("1.0.0.0"), "" };
                yield return new object[] { "2.3alpha", new Version("2.3.0.0"), "alpha" };
                yield return new object[] { "3.4.0.3RC-3", new Version("3.4.0.3"), "RC-3" };
            }
        }

        [Theory]
        [InlineData(20)]
        [InlineData(19)]
        [InlineData(10)]
        public void CtorDoesNotThrowIfSpecialVersionIsNotGreaterThan20(int specialVersionLength)
        {
            // Arrange
            string specialVersion = new String('d', specialVersionLength);
            string versionString = "1.0.0" + specialVersion;

            new SemanticVersion(versionString);
            new SemanticVersion(new Version("1.0.0.0"), specialVersion);
            new SemanticVersion(1, 0, 0, specialVersion);

            SemanticVersion.Parse(versionString);

            SemanticVersion sv;
            Assert.True(SemanticVersion.TryParse(versionString, out sv));
            Assert.True(SemanticVersion.TryParseStrict(versionString, out sv));
        }

        [Theory]
        [InlineData(21)]
        [InlineData(22)]
        [InlineData(100)]
        public void CtorThrowsIfSpecialVersionIsTooLong(int specialVersionLength)
        {
            // Arrange
            string specialVersion = new String('d', specialVersionLength);
            string versionString = "1.0.0" + specialVersion;

            // Act && Assert
            ExceptionAssert.ThrowsArgumentException(
                () => new SemanticVersion(versionString), 
                "version", 
                "'" + versionString + "' is not a valid version string.");

            // Act && Assert
            ExceptionAssert.ThrowsArgumentException(
                () => SemanticVersion.Parse(versionString),
                "version",
                "'" + versionString + "' is not a valid version string.");

            // Act && Assert
            SemanticVersion sv;
            Assert.False(SemanticVersion.TryParse(versionString, out sv));
            Assert.False(SemanticVersion.TryParseStrict(versionString, out sv));

            // Act && Assert
            ExceptionAssert.ThrowsArgumentException(
                () => new SemanticVersion(1, 0, 0, specialVersion),
                "specialVersion",
                "The special version part cannot exceed 20 characters.");

            // Act && Assert
            ExceptionAssert.ThrowsArgumentException(
                () => new SemanticVersion(new Version("1.0"), specialVersion),
                "specialVersion",
                "The special version part cannot exceed 20 characters.");
        }

        [Fact]
        public void ParseThrowsIfStringIsNullOrEmpty()
        {
            ExceptionAssert.ThrowsArgNullOrEmpty(() => SemanticVersion.Parse(version: null), "version");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => SemanticVersion.Parse(version: String.Empty), "version");
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1beta")]
        [InlineData("1.2Av^c")]
        [InlineData("1.2..")]
        [InlineData("1.2.3.4.5")]
        [InlineData("1.2.3.Beta")]
        [InlineData("1.2.3.4This version is full of awesomeness!!")]
        [InlineData("So.is.this")]
        [InlineData("1.34.2Release Candidate")]
        public void ParseThrowsIfStringIsNotAValidSemVer(string versionString)
        {
            ExceptionAssert.ThrowsArgumentException(() => SemanticVersion.Parse(versionString),
                "version",
                String.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid version string.", versionString));
        }

        public static IEnumerable<object[]> LegacyVersionData
        {
            get
            {
                yield return new object[] { "1.022", new SemanticVersion(new Version("1.22.0.0"), "") };
                yield return new object[] { "23.2.3", new SemanticVersion(new Version("23.2.3.0"), "") };
                yield return new object[] { "1.3.42.10133", new SemanticVersion(new Version("1.3.42.10133"), "") };
            }
        }

        [Theory]
        [PropertyData("LegacyVersionData")]
        public void ParseReadsLegacyStyleVersionNumbers(string versionString, SemanticVersion expected)
        {
            // Act
            var actual = SemanticVersion.Parse(versionString);

            // Assert
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.SpecialVersion, actual.SpecialVersion);
        }

        public static IEnumerable<object[]> SemVerData
        {
            get
            {
                yield return new object[] { "1.022Beta", new SemanticVersion(new Version("1.22.0.0"), "Beta") };
                yield return new object[] { "23.2.3Alpha", new SemanticVersion(new Version("23.2.3.0"), "Alpha") };
                yield return new object[] { "1.3.42.10133PreRelease", new SemanticVersion(new Version("1.3.42.10133"), "PreRelease") };
                yield return new object[] { "1.3.42.200930RC-2", new SemanticVersion(new Version("1.3.42.200930"), "RC-2") };
            }
        }

        [Theory]
        [PropertyData("SemVerData")]
        public void ParseReadsSemverAndHybridSemverVersionNumbers(string versionString, SemanticVersion expected)
        {
            // Act
            var actual = SemanticVersion.Parse(versionString);

            // Assert
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.SpecialVersion, actual.SpecialVersion);
        }

        public static IEnumerable<object[]> SemVerWithWhiteSpace
        {
            get
            {
                yield return new object[] { "  1.022Beta", new SemanticVersion(new Version("1.22.0.0"), "Beta") };
                yield return new object[] { "23.2.3Alpha  ", new SemanticVersion(new Version("23.2.3.0"), "Alpha") };
                yield return new object[] { "    1.3.42.10133PreRelease  ", new SemanticVersion(new Version("1.3.42.10133"), "PreRelease") };
            }
        }

        [Theory]
        [PropertyData("SemVerWithWhiteSpace")]
        public void ParseIgnoresLeadingAndTrailingWhitespace(string versionString, SemanticVersion expected)
        {
            // Act
            var actual = SemanticVersion.Parse(versionString);

            // Assert
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.SpecialVersion, actual.SpecialVersion);
        }


        [Theory]
        [InlineData("1.0", "1.0.1")]
        [InlineData("1.23", "1.231")]
        [InlineData("1.4.5.6", "1.45.6")]
        [InlineData("1.4.5.6", "1.4.5.60")]
        [InlineData("1.01", "1.10")]
        [InlineData("1.01alpha", "1.10beta")]
        [InlineData("1.01.0RC-1", "1.10.0rc-2")]
        [InlineData("1.01RC-1", "1.01")]
        [InlineData("1.01", "1.2preview")]
        public void SemVerLessThanAndGreaterThanOperatorsWorks(string versionA, string versionB)
        {
            // Arrange
            var itemA = new SemanticVersion(versionA);
            var itemB = new SemanticVersion(versionB);

            // Act and Assert
            Assert.True(itemA < itemB);
            Assert.True(itemA <= itemB);
            Assert.True(itemB > itemA);
            Assert.True(itemB >= itemA);
        }

        [Fact]
        public void SemVerThrowsIfLeftHandExpressionForCompareOperatorsIsNull()
        {
            // Arrange
            SemanticVersion itemA = null;
            SemanticVersion itemB = new SemanticVersion("1.0");

            // Act and Assert
            ExceptionAssert.ThrowsArgNull(() => { bool val = itemA < itemB; }, "version1");
            ExceptionAssert.ThrowsArgNull(() => { bool val = itemA <= itemB; }, "version1");
            ExceptionAssert.ThrowsArgNull(() => { bool val = itemA > itemB; }, "version1");
            ExceptionAssert.ThrowsArgNull(() => { bool val = itemA >= itemB; }, "version1");
        }

        [Theory]
        [InlineData("1.0", "1.0.0.0")]
        [InlineData("1.23.01", "1.23.1")]
        [InlineData("1.45.6", "1.45.6.0")]
        [InlineData("1.45.6Alpha", "1.45.6Alpha")]
        [InlineData("1.6.2BeTa", "1.6.02beta")]
        [InlineData("22.3.07     ", "22.3.07")]
        public void SemVerEqualsOperatorWorks(string versionA, string versionB)
        {
            // Arrange
            var itemA = new SemanticVersion(versionA);
            var itemB = new SemanticVersion(versionB);

            // Act and Assert
            Assert.True(itemA == itemB);
            Assert.True(itemA <= itemB);
            Assert.True(itemB == itemA);
            Assert.True(itemB >= itemA);
        }

        [Fact]
        public void SemVerEqualityComparisonsWorkForNullValues()
        {
            // Arrange
            SemanticVersion itemA = null;
            SemanticVersion itemB = null;

            // Act and Assert
            Assert.True(itemA == itemB);
            Assert.True(itemB == itemA);
            Assert.True(itemA <= itemB);
            Assert.True(itemB <= itemA);
            Assert.True(itemA >= itemB);
            Assert.True(itemB >= itemA);
        }

        [Theory]
        [InlineData("1.0")]
        [InlineData("1.0.0")]
        [InlineData("1.0.0.0")]
        [InlineData("1.0alpha")]
        [InlineData("1.0.0b")]
        [InlineData("3.0.1.2")]
        [InlineData("2.1.4.3pre-1")]
        public void ToStringReturnsOriginalValue(string version)
        {
            // Act
            SemanticVersion semVer = new SemanticVersion(version);

            // Assert
            Assert.Equal(version, semVer.ToString());
        }

        [Fact]
        public void TryParseStrictParsesStrictVersion()
        {
            // Arrange
            var versionString = "1.3.2CTP-2-Refresh-Alpha";

            // Act
            SemanticVersion version;
            bool result = SemanticVersion.TryParseStrict(versionString, out version);

            // Assert
            Assert.True(result);
            Assert.Equal(new Version("1.3.2.0"), version.Version);
            Assert.Equal("CTP-2-Refresh-Alpha", version.SpecialVersion);
        }

        [Theory]
        [InlineData("2.7")]
        [InlineData("1.3.4.5")]
        [InlineData("1.3alpha")]
        [InlineData("1.3 .4")]
        [InlineData("2.3.18.2a")]
        public void TryParseStrictReturnsFalseIfVersionIsNotStrictSemVer(string version)
        {
            // Act 
            SemanticVersion semanticVersion;
            bool result = SemanticVersion.TryParseStrict(version, out semanticVersion);

            // Assert
            Assert.False(result);
            Assert.Null(semanticVersion);
        }
    }
}
