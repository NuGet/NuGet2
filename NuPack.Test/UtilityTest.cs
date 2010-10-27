using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class UtilityTest {
        [TestMethod]
        public void ParseFrameworkNameNormalizesSupportedNetFrameworkNames() {
            // Arrange
            var knownNameFormats = new[] { ".net", ".netframework", "net", "netframework" };
            Version version40 = new Version("4.0.0.0");

            // Act
            var frameworkNames = knownNameFormats.Select(fmt => Utility.ParseFrameworkName(fmt));

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
            var frameworkNames = knownNameFormats.Select(fmt => Utility.ParseFrameworkName(fmt));

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
            var frameworkName = Utility.ParseFrameworkName("NETCF20");

            // Assert
            Assert.AreEqual("NETCF", frameworkName.Identifier);
            Assert.AreEqual(version20, frameworkName.Version);
        }

        [TestMethod]
        public void ParseFrameworkNameUsesNetFrameworkIfOnlyVersionSpecified() {
            // Arrange
            Version version20 = new Version("2.0");

            // Act
            var frameworkName = Utility.ParseFrameworkName("20");

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
            var frameworkNames = versionFormats.Select(fmt => Utility.ParseFrameworkName(fmt));

            // Assert
            foreach (var frameworkName in frameworkNames) {
                Assert.AreEqual(".NETFramework", frameworkName.Identifier);
                Assert.AreEqual(version40, frameworkName.Version); 
            }
        }

        [TestMethod]
        public void ParseFrameworkNameVersionIntegerLongerThan4CharsTrimsExccess() {            
            // Act
            var frameworkName = Utility.ParseFrameworkName("NET41235");

            // Assert
            Assert.AreEqual(".NETFramework", frameworkName.Identifier);
            Assert.AreEqual(new Version("4.1.2.3"), frameworkName.Version);
        }

        [TestMethod]
        public void ParseFrameworkNameInvalidVersionFormatUsesDefaultVersion() {
            // Act
            var frameworkName = Utility.ParseFrameworkName("NET4.1.4.5.5");

            // Assert
            Assert.AreEqual(".NETFramework", frameworkName.Identifier);
            Assert.AreEqual(Utility.GetDefaultTargetFrameworkVersion(), frameworkName.Version);
        }
    }
}
