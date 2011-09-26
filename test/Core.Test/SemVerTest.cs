using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test {
    public class SemVerTest {
        [Fact]
        public void ParseThrowsIfStringIsNullOrEmpty() {
            ExceptionAssert.ThrowsArgNullOrEmpty(() => SemVer.Parse(version: null), "version");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => SemVer.Parse(version: String.Empty), "version");
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
        public void ParseThrowsIfStringIsNotAValidSemVer(string versionString) {
            ExceptionAssert.ThrowsArgumentException(() => SemVer.Parse(versionString), 
                "version",
                String.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid version string.", versionString));
        }


        public static IEnumerable<object[]> LegacyVersionData {
            get {
                yield return new object[] { "1.022", new SemVer(new Version("1.22.0.0"), "") };
                yield return new object[] { "23.2.3", new SemVer(new Version("23.2.3.0"), "") };
                yield return new object[] { "1.3.42.10133", new SemVer(new Version("1.3.42.10133"), "") };
            }
        }

        [Theory]
        [PropertyData("LegacyVersionData")]
        public void ParseReadsLegacyStyleVersionNumbers(string versionString, SemVer expected) {
            // Act
            var actual = SemVer.Parse(versionString);

            // Assert
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.SpecialVersion, actual.SpecialVersion);
        }

        public static IEnumerable<object[]> SemVerData {
            get {
                yield return new object[] { "1.022Beta", new SemVer(new Version("1.22.0.0"), "Beta") };
                yield return new object[] { "23.2.3Alpha", new SemVer(new Version("23.2.3.0"), "Alpha") };
                yield return new object[] { "1.3.42.10133PreRelease", new SemVer(new Version("1.3.42.10133"), "PreRelease") };
                yield return new object[] { "1.3.42.200930RC-2", new SemVer(new Version("1.3.42.200930"), "RC-2") };
            }
        }

        [Theory]
        [PropertyData("SemVerData")]
        public void ParseReadsSemverAndHybridSemverVersionNumbers(string versionString, SemVer expected) {
            // Act
            var actual = SemVer.Parse(versionString);

            // Assert
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.SpecialVersion, actual.SpecialVersion);
        }

        public static IEnumerable<object[]> SemVerWithWhiteSpace {
            get {
                yield return new object[] { "  1.022Beta", new SemVer(new Version("1.22.0.0"), "Beta") };
                yield return new object[] { "23.2.3Alpha  ", new SemVer(new Version("23.2.3.0"), "Alpha") };
                yield return new object[] { "    1.3.42.10133PreRelease  ", new SemVer(new Version("1.3.42.10133"), "PreRelease") };
            }
        }

        [Theory]
        [PropertyData("SemVerWithWhiteSpace")]
        public void ParseIgnoresLeadingAndTrailingWhitespace(string versionString, SemVer expected) {
            // Act
            var actual = SemVer.Parse(versionString);

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
        public void SemVerLessThanAndGreaterThanOperatorsWorks(string versionA, string versionB) {
            // Arrange
            var itemA = new SemVer(versionA);
            var itemB = new SemVer(versionB);

            // Act and Assert
            Assert.True(itemA < itemB);
            Assert.True(itemA <= itemB);
            Assert.True(itemB > itemA);
            Assert.True(itemB >= itemA);
        }

        [Fact]
        public void SemVerThrowsIfLeftHandExpressionForCompareOperatorsIsNull() {
            // Arrange
            SemVer itemA = null;
            SemVer itemB = new SemVer("1.0");

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
        public void SemVerEqualsOperatorWorks(string versionA, string versionB) {
            // Arrange
            var itemA = new SemVer(versionA);
            var itemB = new SemVer(versionB);

            // Act and Assert
            Assert.True(itemA == itemB);
            Assert.True(itemA <= itemB);
            Assert.True(itemB == itemA);
            Assert.True(itemB >= itemA);
        }

        [Fact]
        public void SemVerEqualityComparisonsWorkForNullValues() {
            // Arrange
            SemVer itemA = null;
            SemVer itemB = null;

            // Act and Assert
            Assert.True(itemA == itemB);
            Assert.True(itemB == itemA);
            Assert.True(itemA <= itemB);
            Assert.True(itemB <= itemA);
            Assert.True(itemA >= itemB);
            Assert.True(itemB >= itemA);
        }
    }
}
