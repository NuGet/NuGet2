using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class VersionUtilityTest {
        [TestMethod]
        public void ParseFrameworkNameNormalizesSupportedNetFrameworkNames() {
            // Arrange
            var knownNameFormats = new[] { ".net", ".netframework", "net", "netframework" };
            Version version40 = new Version("4.0.0.0");

            // Act
            var frameworkNames = knownNameFormats.Select(fmt => VersionUtility.ParseFrameworkName(fmt));

            // Assert
            foreach (var frameworkName in frameworkNames) {
                Assert.AreEqual(".NETFramework", frameworkName.Identifier);
                Assert.AreEqual(version40, frameworkName.Version);
            }
        }

        [TestMethod]
        public void ParseFrameworkNameNormalizesSupportedSilverlightNames() {
            // Arrange
            var knownNameFormats = new[] { "sl", "SL", "SilVerLight", "Silverlight" };
            Version version40 = new Version("4.0.0.0");

            // Act
            var frameworkNames = knownNameFormats.Select(fmt => VersionUtility.ParseFrameworkName(fmt));

            // Assert
            foreach (var frameworkName in frameworkNames) {
                Assert.AreEqual("Silverlight", frameworkName.Identifier);
                Assert.AreEqual(version40, frameworkName.Version);
            }
        }

        [TestMethod]
        public void ParseFrameworkNameUsesFrameworkNameIfUnrecognized() {
            // Arrange
            Version version20 = new Version("2.0");

            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("NETCF20");

            // Assert
            Assert.AreEqual("NETCF", frameworkName.Identifier);
            Assert.AreEqual(version20, frameworkName.Version);
        }

        [TestMethod]
        public void ParseFrameworkNameUsesNetFrameworkIfOnlyVersionSpecified() {
            // Arrange
            Version version20 = new Version("2.0");

            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("20");

            // Assert
            Assert.AreEqual(".NETFramework", frameworkName.Identifier);
            Assert.AreEqual(version20, frameworkName.Version);
        }

        [TestMethod]
        public void ParseFrameworkNameVersionFormats() {
            // Arrange
            var versionFormats = new[] { "4.0", "40", "4" };
            Version version40 = new Version("4.0");

            // Act
            var frameworkNames = versionFormats.Select(fmt => VersionUtility.ParseFrameworkName(fmt));

            // Assert
            foreach (var frameworkName in frameworkNames) {
                Assert.AreEqual(".NETFramework", frameworkName.Identifier);
                Assert.AreEqual(version40, frameworkName.Version); 
            }
        }

        [TestMethod]
        public void ParseFrameworkNameVersionIntegerLongerThan4CharsTrimsExccess() {            
            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("NET41235");

            // Assert
            Assert.AreEqual(".NETFramework", frameworkName.Identifier);
            Assert.AreEqual(new Version("4.1.2.3"), frameworkName.Version);
        }

        [TestMethod]
        public void ParseFrameworkNameInvalidVersionFormatUsesDefaultVersion() {
            // Act
            var frameworkName = VersionUtility.ParseFrameworkName("NET4.1.4.5.5");

            // Assert
            Assert.AreEqual(".NETFramework", frameworkName.Identifier);
            Assert.AreEqual(VersionUtility.DefaultTargetFrameworkVersion, frameworkName.Version);
        }

        [TestMethod]
        public void ParseVersionSpecWithNullThrows() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => VersionUtility.ParseVersionSpec(null), "versionString");
        }

        [TestMethod]
        public void ParseVersionSpecSimpleVersionNoBrackets() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("1.2");

            // Assert
            Assert.AreEqual("1.2", versionInfo.MinVersion.ToString());
            Assert.IsTrue(versionInfo.IsMinInclusive);
            Assert.AreEqual(null, versionInfo.MaxVersion);
            Assert.IsFalse(versionInfo.IsMaxInclusive);
        }

        [TestMethod]
        public void ParseVersionSpecSimpleVersionNoBracketsExtraSpaces() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("  1  .   2  ");

            // Assert
            Assert.AreEqual("1.2", versionInfo.MinVersion.ToString());
            Assert.IsTrue(versionInfo.IsMinInclusive);
            Assert.AreEqual(null, versionInfo.MaxVersion);
            Assert.IsFalse(versionInfo.IsMaxInclusive);
        }

        [TestMethod]
        public void ParseVersionSpecMaxOnlyInclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("(,1.2]");

            // Assert
            Assert.AreEqual(null, versionInfo.MinVersion);
            Assert.IsFalse(versionInfo.IsMinInclusive);
            Assert.AreEqual("1.2", versionInfo.MaxVersion.ToString());
            Assert.IsTrue(versionInfo.IsMaxInclusive);
        }

        [TestMethod]
        public void ParseVersionSpecMaxOnlyExclusive() {
            var versionInfo = VersionUtility.ParseVersionSpec("(,1.2)");
            Assert.AreEqual(null, versionInfo.MinVersion);
            Assert.IsFalse(versionInfo.IsMinInclusive);
            Assert.AreEqual("1.2", versionInfo.MaxVersion.ToString());
            Assert.IsFalse(versionInfo.IsMaxInclusive);
        }

        [TestMethod]
        public void ParseVersionSpecExactVersion() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("[1.2]");

            // Assert
            Assert.AreEqual("1.2", versionInfo.MinVersion.ToString());
            Assert.IsTrue(versionInfo.IsMinInclusive);
            Assert.AreEqual("1.2", versionInfo.MaxVersion.ToString());
            Assert.IsTrue(versionInfo.IsMaxInclusive);
        }

        [TestMethod]
        public void ParseVersionSpecMinOnlyExclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("(1.2,)");

            // Assert
            Assert.AreEqual("1.2", versionInfo.MinVersion.ToString());
            Assert.IsFalse(versionInfo.IsMinInclusive);
            Assert.AreEqual(null, versionInfo.MaxVersion);
            Assert.IsFalse(versionInfo.IsMaxInclusive);
        }

        [TestMethod]
        public void ParseVersionSpecRangeExclusiveExclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("(1.2,2.3)");

            // Assert
            Assert.AreEqual("1.2", versionInfo.MinVersion.ToString());
            Assert.IsFalse(versionInfo.IsMinInclusive);
            Assert.AreEqual("2.3", versionInfo.MaxVersion.ToString());
            Assert.IsFalse(versionInfo.IsMaxInclusive);
        }

        [TestMethod]
        public void ParseVersionSpecRangeExclusiveInclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("(1.2,2.3]");

            // Assert
            Assert.AreEqual("1.2", versionInfo.MinVersion.ToString());
            Assert.IsFalse(versionInfo.IsMinInclusive);
            Assert.AreEqual("2.3", versionInfo.MaxVersion.ToString());
            Assert.IsTrue(versionInfo.IsMaxInclusive);
        }

        [TestMethod]
        public void ParseVersionSpecRangeInclusiveExclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("[1.2,2.3)");
            Assert.AreEqual("1.2", versionInfo.MinVersion.ToString());
            Assert.IsTrue(versionInfo.IsMinInclusive);
            Assert.AreEqual("2.3", versionInfo.MaxVersion.ToString());
            Assert.IsFalse(versionInfo.IsMaxInclusive);
        }

        [TestMethod]
        public void ParseVersionSpecRangeInclusiveInclusive() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("[1.2,2.3]");

            // Assert
            Assert.AreEqual("1.2", versionInfo.MinVersion.ToString());
            Assert.IsTrue(versionInfo.IsMinInclusive);
            Assert.AreEqual("2.3", versionInfo.MaxVersion.ToString());
            Assert.IsTrue(versionInfo.IsMaxInclusive);
        }

        [TestMethod]
        public void ParseVersionSpecRangeInclusiveInclusiveExtraSpaces() {
            // Act
            var versionInfo = VersionUtility.ParseVersionSpec("   [  1 .2   , 2  .3   ]  ");

            // Assert
            Assert.AreEqual("1.2", versionInfo.MinVersion.ToString());
            Assert.IsTrue(versionInfo.IsMinInclusive);
            Assert.AreEqual("2.3", versionInfo.MaxVersion.ToString());
            Assert.IsTrue(versionInfo.IsMaxInclusive);
        }
    }
}
