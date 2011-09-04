using System;
using System.Linq;
using System.Runtime.Versioning;
using Xunit;

namespace NuGet.Test {
    
    public class VersionUtilityTest {
        [Fact]
        public void ParseFrameworkNameNormalizesSupportedNetFrameworkNames() {
            // Arrange
            var knownNameFormats = new[] { ".net", ".netframework", "net", "netframework" };
            Version defaultVersion = new Version("0.0");

            // Act
            var frameworkNames = knownNameFormats.Select(fmt => VersionUtility.ParseFrameworkName(fmt));

            // Assert
            foreach (var frameworkName in frameworkNames) {
                Assert.Equal(".NETFramework", frameworkName.Identifier);
                Assert.Equal(defaultVersion, frameworkName.Version);
            }
        }

        [Fact]
        public void ParseFrameworkNameNormalizesSupportedNetMicroFrameworkNames() {
            // Arrange
            var knownNameFormats = new[] { "netmf4.1", ".NETMicroFramework4.1" };
            Version version41 = new Version("4.1");

            // Act
            var frameworkNames = knownNameFormats.Select(fmt => VersionUtility.ParseFrameworkName(fmt));

            // Assert
            foreach (var frameworkName in frameworkNames) {
                Assert.Equal(".NETMicroFramework", frameworkName.Identifier);
                Assert.Equal(version41, frameworkName.Version);
            }
        }

        [Fact]
        public void ParseFrameworkNameNormalizesSupportedSilverlightNames() {
            // Arrange
            var knownNameFormats = new[] { "sl", "SL", "SilVerLight", "Silverlight", "Silverlight " };
            Version defaultVersion = new Version("0.0");

            // Act
            var frameworkNames = knownNameFormats.Select(VersionUtility.ParseFrameworkName);

            // Assert
            foreach (var frameworkName in frameworkNames) {
                Assert.Equal("Silverlight", frameworkName.Identifier);
                Assert.Equal(defaultVersion, frameworkName.Version);
            }
        }

        [Fact]
        public void ParseFrameworkNameReturnsUnsupportedFrameworkNameIfUnrecognized() {
            // Arrange
            Version version20 = new Version("2.0");

            // Act
            var frameworkName1 = VersionUtility.ParseFrameworkName("NETCF20");
            var frameworkName2 = VersionUtility.ParseFrameworkName("NET40ClientProfile");
            var frameworkName3 = VersionUtility.ParseFrameworkName("NET40Foo");

            // Assert
            Assert.Equal("Unsupported", frameworkName1.Identifier);
            Assert.Equal("Unsupported", frameworkName2.Identifier);
            Assert.Equal("Unsupported", frameworkName3.Identifier);
        }

        [Fact]
        public void ParseFrameworkNameUsesNetFrameworkIfOnlyVersionSpecified() {
            // Arrange
            Version version20 = new Version("2.0");

            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("20");

            // Assert
            Assert.Equal(".NETFramework", frameworkName.Identifier);
            Assert.Equal(version20, frameworkName.Version);
        }

        [Fact]
        public void ParseFrameworkNameVersionFormats() {
            // Arrange
            var versionFormats = new[] { "4.0", "40", "4" };
            Version version40 = new Version("4.0");

            // Act
            var frameworkNames = versionFormats.Select(VersionUtility.ParseFrameworkName);

            // Assert
            foreach (var frameworkName in frameworkNames) {
                Assert.Equal(".NETFramework", frameworkName.Identifier);
                Assert.Equal(version40, frameworkName.Version);
            }
        }

        [Fact]
        public void ParseFrameworkNameVersionIntegerLongerThan4CharsTrimsExccess() {
            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("NET41235");

            // Assert
            Assert.Equal(".NETFramework", frameworkName.Identifier);
            Assert.Equal(new Version("4.1.2.3"), frameworkName.Version);
        }

        [Fact]
        public void ParseFrameworkNameInvalidVersionFormatUsesDefaultVersion() {
            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("NET4.1.4.5.5");

            // Assert
            Assert.Equal("Unsupported", frameworkName.Identifier);
        }

        [Fact]
        public void ParseFrameworkNameWithProfile() {
            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("net40-client");

            // Assert
            Assert.Equal(".NETFramework", frameworkName.Identifier);
            Assert.Equal(new Version("4.0"), frameworkName.Version);
            Assert.Equal("Client", frameworkName.Profile);
        }

        [Fact]
        public void ParseFrameworkNameWithUnknownProfileUsesProfileAsIs() {
            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("net40-other");

            // Assert
            Assert.Equal(".NETFramework", frameworkName.Identifier);
            Assert.Equal(new Version("4.0"), frameworkName.Version);
            Assert.Equal("other", frameworkName.Profile);
        }

        [Fact]
        public void ParseFrameworkNameWithFullProfileNoamlizesToEmptyProfile() {
            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("net40-full");

            // Assert
            Assert.Equal(".NETFramework", frameworkName.Identifier);
            Assert.Equal(new Version("4.0"), frameworkName.Version);
            Assert.Equal(String.Empty, frameworkName.Profile);
        }

        [Fact]
        public void ParseFrameworkNameWithWPProfileGetNormalizedToWindowsPhone() {
            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("sl4-wp");

            // Assert
            Assert.Equal("Silverlight", frameworkName.Identifier);
            Assert.Equal(new Version("4.0"), frameworkName.Version);
            Assert.Equal("WindowsPhone", frameworkName.Profile);
        }

        [Fact]
        public void ParseFrameworkNameWithCFProfileGetNormalizedToCompactFramework() {
            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("net20-cf");

            // Assert
            Assert.Equal(".NETFramework", frameworkName.Identifier);
            Assert.Equal(new Version("2.0"), frameworkName.Version);
            Assert.Equal("CompactFramework", frameworkName.Profile);
        }

        [Fact]
        public void ParseFrameworkNameWithEmptyProfile() {
            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("sl4-");

            // Assert
            Assert.Equal("Silverlight", frameworkName.Identifier);
            Assert.Equal(new Version("4.0"), frameworkName.Version);
            Assert.Equal(String.Empty, frameworkName.Profile);
        }

        [Fact]
        public void ParseFrameworkNameWithInvalidFrameworkNameThrows() {
            // Act
            ExceptionAssert.ThrowsArgumentException(() => VersionUtility.ParseFrameworkName("-"), "frameworkName", "Framework name is missing.");
            ExceptionAssert.ThrowsArgumentException(() => VersionUtility.ParseFrameworkName("-client"), "frameworkName", "Framework name is missing.");
            ExceptionAssert.ThrowsArgumentException(() => VersionUtility.ParseFrameworkName(""), "frameworkName", "Framework name is missing.");
            ExceptionAssert.ThrowsArgumentException(() => VersionUtility.ParseFrameworkName("---"), "frameworkName", "Invalid framework name format. Expected {framework}{version}-{profile}.");
        }

        [Fact]
        public void ParseFrameworkFolderName() {
            // foo.dll
            // sub\foo.dll -> Unsupported since we can't tell if this was meant to be a framework name or not
            // {FrameworkName}{Version}\foo.dll
            // {FrameworkName}{Version}\sub1\foo.dll
            // {FrameworkName}{Version}\sub1\sub2\foo.dll
            var f1 = VersionUtility.ParseFrameworkFolderName(@"foo.dll");
            var f2 = VersionUtility.ParseFrameworkFolderName(@"sub\foo.dll");
            var f3 = VersionUtility.ParseFrameworkFolderName(@"SL4\foo.dll");
            var f4 = VersionUtility.ParseFrameworkFolderName(@"SL3\sub1\foo.dll");
            var f5 = VersionUtility.ParseFrameworkFolderName(@"SL20\sub1\sub2\foo.dll");
            var f6 = VersionUtility.ParseFrameworkFolderName(@"net\foo.dll");

            Assert.Null(f1);
            Assert.Equal("Unsupported", f2.Identifier);
            Assert.Equal("Silverlight", f3.Identifier);
            Assert.Equal(new Version("4.0"), f3.Version);
            Assert.Equal("Silverlight", f4.Identifier);
            Assert.Equal(new Version("3.0"), f4.Version);
            Assert.Equal("Silverlight", f5.Identifier);
            Assert.Equal(new Version("2.0"), f5.Version);
            Assert.Equal(".NETFramework", f6.Identifier);
            Assert.Equal(new Version(), f6.Version);
        }

        [Fact]
        public void GetFrameworkStringFromFrameworkName() {
            // Arrange
            var net40 = new FrameworkName(".NETFramework", new Version(4, 0));
            var net40Client = new FrameworkName(".NETFramework", new Version(4, 0), "Client");
            var sl3 = new FrameworkName("Silverlight", new Version(3, 0));
            var sl4 = new FrameworkName("Silverlight", new Version(4, 0));
            var wp7 = new FrameworkName("Silverlight", new Version(4, 0), "WindowsPhone");
            var wp7Mango = new FrameworkName("Silverlight", new Version(4, 0), "WindowsPhone71");
            var netMicro41 = new FrameworkName(".NETMicroFramework", new Version(4, 1));

            // Act
            string net40Value = VersionUtility.GetFrameworkString(net40);
            string net40ClientValue = VersionUtility.GetFrameworkString(net40Client);
            string sl3Value = VersionUtility.GetFrameworkString(sl3);
            string sl4Value = VersionUtility.GetFrameworkString(sl4);
            string wp7Value = VersionUtility.GetFrameworkString(wp7);
            string wp7MangoValue = VersionUtility.GetFrameworkString(wp7Mango);
            string netMicro41Value = VersionUtility.GetFrameworkString(netMicro41);

            // Assert
            Assert.Equal(".NETFramework4.0", net40Value);
            Assert.Equal(".NETFramework4.0-Client", net40ClientValue);
            Assert.Equal("Silverlight3.0", sl3Value);
            Assert.Equal("Silverlight4.0", sl4Value);
            Assert.Equal("Silverlight4.0-WindowsPhone", wp7Value);
            Assert.Equal("Silverlight4.0-WindowsPhone71", wp7MangoValue);
            Assert.Equal(".NETMicroFramework4.1", netMicro41Value);
        }

        [Fact]
        public void ParseVersionSpecWithNullThrows() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => VersionUtility.ParseVersionSpec(null), "value");
        }

        [Fact]
        public void ParseVersionSpecSimpleVersionNoBrackets() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("1.2");

            // Assert
            Assert.Equal("1.2", versionInfo.MinVersion.ToString());
            Assert.True(versionInfo.IsMinInclusive);
            Assert.Equal(null, versionInfo.MaxVersion);
            Assert.False(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecSimpleVersionNoBracketsExtraSpaces() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("  1  .   2  ");

            // Assert
            Assert.Equal("1.2", versionInfo.MinVersion.ToString());
            Assert.True(versionInfo.IsMinInclusive);
            Assert.Equal(null, versionInfo.MaxVersion);
            Assert.False(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecMaxOnlyInclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("(,1.2]");

            // Assert
            Assert.Equal(null, versionInfo.MinVersion);
            Assert.False(versionInfo.IsMinInclusive);
            Assert.Equal("1.2", versionInfo.MaxVersion.ToString());
            Assert.True(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecMaxOnlyExclusive() {
            var versionInfo = VersionUtility.ParseVersionSpec("(,1.2)");
            Assert.Equal(null, versionInfo.MinVersion);
            Assert.False(versionInfo.IsMinInclusive);
            Assert.Equal("1.2", versionInfo.MaxVersion.ToString());
            Assert.False(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecExactVersion() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("[1.2]");

            // Assert
            Assert.Equal("1.2", versionInfo.MinVersion.ToString());
            Assert.True(versionInfo.IsMinInclusive);
            Assert.Equal("1.2", versionInfo.MaxVersion.ToString());
            Assert.True(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecMinOnlyExclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("(1.2,)");

            // Assert
            Assert.Equal("1.2", versionInfo.MinVersion.ToString());
            Assert.False(versionInfo.IsMinInclusive);
            Assert.Equal(null, versionInfo.MaxVersion);
            Assert.False(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecRangeExclusiveExclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("(1.2,2.3)");

            // Assert
            Assert.Equal("1.2", versionInfo.MinVersion.ToString());
            Assert.False(versionInfo.IsMinInclusive);
            Assert.Equal("2.3", versionInfo.MaxVersion.ToString());
            Assert.False(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecRangeExclusiveInclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("(1.2,2.3]");

            // Assert
            Assert.Equal("1.2", versionInfo.MinVersion.ToString());
            Assert.False(versionInfo.IsMinInclusive);
            Assert.Equal("2.3", versionInfo.MaxVersion.ToString());
            Assert.True(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecRangeInclusiveExclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("[1.2,2.3)");
            Assert.Equal("1.2", versionInfo.MinVersion.ToString());
            Assert.True(versionInfo.IsMinInclusive);
            Assert.Equal("2.3", versionInfo.MaxVersion.ToString());
            Assert.False(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecRangeInclusiveInclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("[1.2,2.3]");

            // Assert
            Assert.Equal("1.2", versionInfo.MinVersion.ToString());
            Assert.True(versionInfo.IsMinInclusive);
            Assert.Equal("2.3", versionInfo.MaxVersion.ToString());
            Assert.True(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecRangeInclusiveInclusiveExtraSpaces() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("   [  1 .2   , 2  .3   ]  ");

            // Assert
            Assert.Equal("1.2", versionInfo.MinVersion.ToString());
            Assert.True(versionInfo.IsMinInclusive);
            Assert.Equal("2.3", versionInfo.MaxVersion.ToString());
            Assert.True(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void NormalizeVersionFillsInZerosForUnsetVersionParts() {
            // Act
            Version version = VersionUtility.NormalizeVersion(new Version("1.5"));

            // Assert
            Assert.Equal(new Version("1.5.0.0"), version);
        }

        [Fact]
        public void ParseVersionSpecRangeIntegerRanges() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("   [1, 2]  ");

            // Assert
            Assert.Equal("1.0", versionInfo.MinVersion.ToString());
            Assert.True(versionInfo.IsMinInclusive);
            Assert.Equal("2.0", versionInfo.MaxVersion.ToString());
            Assert.True(versionInfo.IsMaxInclusive);
        }

        [Fact]
        public void ParseVersionSpecRangeNegativeIntegerRanges() {
            // Act
            IVersionSpec versionInfo;
            bool parsed = VersionUtility.TryParseVersionSpec("   [-1, 2]  ", out versionInfo);

            Assert.False(parsed);
            Assert.Null(versionInfo);
        }

        [Fact]
        public void TrimVersionTrimsRevisionIfZero() {
            // Act
            var version = VersionUtility.TrimVersion(new Version("1.2.3.0"));

            // Assert
            Assert.Equal(new Version("1.2.3"), version);
        }

        [Fact]
        public void TrimVersionTrimsRevisionAndBuildIfZero() {
            // Act
            var version = VersionUtility.TrimVersion(new Version("1.2.0.0"));

            // Assert
            Assert.Equal(new Version("1.2"), version);
        }

        [Fact]
        public void TrimVersionTrimsBuildIfRevisionIsNonZero() {
            // Act
            var version = VersionUtility.TrimVersion(new Version("1.2.0.5"));

            // Assert
            Assert.Equal(new Version("1.2.0.5"), version);
        }

        [Fact]
        public void GetAllPossibleVersionsTwoDigits() {
            // Arrange
            var expectedVersions = new[] { 
                new Version("1.0"), 
                new Version("1.0.0"),
                new Version("1.0.0.0")
            };

            // Act
            var versions = VersionUtility.GetPossibleVersions(new Version("1.0")).ToList();

            // Assert
            foreach (var v in expectedVersions) {
                Assert.True(versions.Contains(v));
            }
        }

        [Fact]
        public void GetAllPossibleVersionsThreeDigits() {
            // Arrange
            var expectedVersions = new[] { 
                new Version("1.0"), 
                new Version("1.0.0"),
                new Version("1.0.0.0")
            };

            // Act
            var versions = VersionUtility.GetPossibleVersions(new Version("1.0.0")).ToList();

            // Assert
            foreach (var v in expectedVersions) {
                Assert.True(versions.Contains(v));
            }
        }

        [Fact]
        public void GetAllPossibleVersionsFourDigits() {
            // Arrange
            var expectedVersions = new[] { 
                new Version("1.0"), 
                new Version("1.0.0"),
                new Version("1.0.0.0")
            };

            // Act
            var versions = VersionUtility.GetPossibleVersions(new Version("1.0.0.0")).ToList();

            // Assert
            foreach (var v in expectedVersions) {
                Assert.True(versions.Contains(v));
            }
        }

        [Fact]
        public void GetAllPossibleVersionsThreeDigitsWithZeroBetween() {
            // Arrange
            var expectedVersions = new[] { 
                new Version("1.0.1"), 
                new Version("1.0.1.0")
            };

            // Act
            var versions = VersionUtility.GetPossibleVersions(new Version("1.0.1")).ToList();

            // Assert
            foreach (var v in expectedVersions) {
                Assert.True(versions.Contains(v));
            }
        }

        [Fact]
        public void GetAllPossibleVersionsFourDigitsWithTrailingZeros() {
            // Arrange
            var expectedVersions = new[] { 
                new Version("1.1.0.0"), 
                new Version("1.1.0"),
                new Version("1.1")
            };

            // Act
            var versions = VersionUtility.GetPossibleVersions(new Version("1.1.0.0")).ToList();

            // Assert
            foreach (var v in expectedVersions) {
                Assert.True(versions.Contains(v));
            }
        }

        [Fact]
        public void GetSafeVersions() {
            // Act
            IVersionSpec versionSpec1 = VersionUtility.GetSafeRange(new Version("1.3"));
            IVersionSpec versionSpec2 = VersionUtility.GetSafeRange(new Version("0.9"));
            IVersionSpec versionSpec3 = VersionUtility.GetSafeRange(new Version("2.9.45.6"));

            // Assert
            AssertSafeVersion(versionSpec1, new Version("1.3"), new Version("1.4"));
            AssertSafeVersion(versionSpec2, new Version("0.9"), new Version("0.10"));
            AssertSafeVersion(versionSpec3, new Version("2.9.45.6"), new Version("2.10"));
        }

        private void AssertSafeVersion(IVersionSpec versionSpec, Version minVer, Version maxVer) {
            Assert.True(versionSpec.IsMinInclusive);
            Assert.False(versionSpec.IsMaxInclusive);
            Assert.Equal(versionSpec.MinVersion, minVer);
            Assert.Equal(versionSpec.MaxVersion, maxVer);
        }

        [Fact]
        public void TrimVersionThrowsIfVersionNull() {
            ExceptionAssert.ThrowsArgNull(() => VersionUtility.TrimVersion(null), "version");
        }

        [Fact]
        public void IsCompatibleReturnsFalseForSlAndWindowsPhoneFrameworks() {
            // Arrange
            FrameworkName sl3 = VersionUtility.ParseFrameworkName("sl3");
            FrameworkName wp7 = VersionUtility.ParseFrameworkName("sl3-wp");

            // Act
            bool wp7CompatibleWithSl = VersionUtility.IsCompatible(sl3, wp7);
            bool slCompatibleWithWp7 = VersionUtility.IsCompatible(wp7, sl3);

            // Assert
            Assert.False(slCompatibleWithWp7);
            Assert.False(wp7CompatibleWithSl);
        }

        [Fact]
        public void IsCompatibleWindowsPhoneVersions() {
            // Arrange
            FrameworkName wp7 = VersionUtility.ParseFrameworkName("sl3-wp");
            FrameworkName wp7Mango = VersionUtility.ParseFrameworkName("sl4-wp71");

            // Act
            bool wp7MangoCompatibleWithwp7 = VersionUtility.IsCompatible(wp7, wp7Mango);
            bool wp7CompatibleWithwp7Mango = VersionUtility.IsCompatible(wp7Mango, wp7);

            // Assert
            Assert.False(wp7MangoCompatibleWithwp7);
            Assert.True(wp7CompatibleWithwp7Mango);
        }

        [Fact]
        public void NetFrameworkCompatibiilityIsCompatibleReturns() {
            // Arrange
            FrameworkName net40 = VersionUtility.ParseFrameworkName("net40");
            FrameworkName net40Client = VersionUtility.ParseFrameworkName("net40-client");

            // Act
            bool netClientCompatibleWithNet = VersionUtility.IsCompatible(net40, net40Client);
            bool netCompatibleWithClient = VersionUtility.IsCompatible(net40Client, net40);

            // Assert
            Assert.True(netClientCompatibleWithNet);
            Assert.True(netCompatibleWithClient);
        }

        [Fact]
        public void LowerFrameworkVersionsAreNotCompatibleWithHigherFrameworkVersionsWithSameFrameworkName() {
            // Arrange
            FrameworkName net40 = VersionUtility.ParseFrameworkName("net40");
            FrameworkName net20 = VersionUtility.ParseFrameworkName("net20");

            // Act
            bool net40CompatibleWithNet20 = VersionUtility.IsCompatible(net20, net40);
            bool net20CompatibleWithNet40 = VersionUtility.IsCompatible(net40, net20);

            // Assert
            Assert.False(net40CompatibleWithNet20);
            Assert.True(net20CompatibleWithNet40);
        }

        [Fact]
        public void IsCompatibleReturnsTrueIfSupportedFrameworkListIsEmpty() {
            // Arrange
            FrameworkName net40Client = VersionUtility.ParseFrameworkName("net40-client");

            // Act
            var result = VersionUtility.IsCompatible(net40Client, Enumerable.Empty<FrameworkName>());

            // Assert
            Assert.True(result);
        }
    }
}