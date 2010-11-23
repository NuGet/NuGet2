using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class PathUtilityTest {
        [TestMethod]
        public void GetRelativePathAbsolutePaths() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\bar\", @"c:\foo\bar\baz");

            // Assert
            Assert.AreEqual("baz", path);
        }

        [TestMethod]
        public void GetRelativePathDirectoryWithPeriods() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\MvcApplication1\MvcApplication1.Tests\", @"c:\foo\MvcApplication1\packages\foo.dll");

            // Assert
            Assert.AreEqual(@"..\packages\foo.dll", path);
        }

        [TestMethod]
        public void GetRelativePathAbsolutePathAndShare() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\bar", @"\\baz");

            // Assert
            Assert.AreEqual(@"\\baz", path);
        }

        [TestMethod]
        public void GetRelativePathShares() {
            // Act
            string path = PathUtility.GetRelativePath(@"\\baz\a\b\c\", @"\\baz\");

            // Assert
            Assert.AreEqual(@"..\..\..\", path);
        }

        [TestMethod]
        public void GetRelativePathFileNames() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\a\y\x.dll", @"c:\a\b.dll");

            // Assert
            Assert.AreEqual(@"..\b.dll", path);
        }

        [TestMethod]
        public void GetRelativePathUnrelatedAbsolutePaths() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo", @"d:\bar");

            // Assert
            Assert.AreEqual(@"d:\bar", path);
        }
    }
}
