using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class PathUtilityTest {
        [TestMethod]
        public void GetRelativePathAbsolutePaths() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\bar", @"c:\foo\bar\baz");

            // Assert
            Assert.AreEqual("baz", path);
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
            string path = PathUtility.GetRelativePath(@"\\baz\a\b\c", @"\\baz\");

            // Assert
            Assert.AreEqual(@"..\..\..", path);
        }

        [TestMethod]
        public void GetRelativePathUnrelatedAbsolutePaths() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo", @"d:\bar");

            // Assert
            Assert.AreEqual(@"d:\bar", path);
        }

        [TestMethod]
        public void GetRelativePathAbsoluteAndRelativePath() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo", @"bar");

            // Assert
            Assert.AreEqual(@"bar", path);
        }
    }
}
