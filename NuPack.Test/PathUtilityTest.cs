using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class PathUtilityTest {
        [TestMethod]
        public void GetRelativePathAbsolutePaths() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\bar", @"c:\foo\bar\baz", p => true);

            // Assert
            Assert.AreEqual("baz", path);
        }

        [TestMethod]
        public void GetRelativePathDirectoryWithPeriods() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\MvcApplication1\MvcApplication1.Tests", @"c:\foo\MvcApplication1\packages\foo.dll", p => !p.EndsWith(".dll"));

            // Assert
            Assert.AreEqual(@"..\packages\foo.dll", path);
        }

        [TestMethod]
        public void GetRelativePathAbsolutePathAndShare() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo\bar", @"\\baz", p => true);

            // Assert
            Assert.AreEqual(@"\\baz", path);
        }

        [TestMethod]
        public void GetRelativePathShares() {
            // Act
            string path = PathUtility.GetRelativePath(@"\\baz\a\b\c", @"\\baz\", p => true);

            // Assert
            Assert.AreEqual(@"..\..\..", path);
        }

        [TestMethod]
        public void GetRelativePathFileNames() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\a\y\x.dll", @"c:\a\b.dll", p => !Path.HasExtension(p));

            // Assert
            Assert.AreEqual(@"..\..\b.dll", path);
        }

        [TestMethod]
        public void GetRelativePathUnrelatedAbsolutePaths() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo", @"d:\bar", p => true);

            // Assert
            Assert.AreEqual(@"d:\bar", path);
        }

        [TestMethod]
        public void GetRelativePathAbsoluteAndRelativePath() {
            // Act
            string path = PathUtility.GetRelativePath(@"c:\foo", @"bar", p => true);

            // Assert
            Assert.AreEqual(@"bar", path);
        }
    }
}
