namespace NuGet.Test {
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UriHelperTest {
        [TestMethod]
        public void CreatePartUriCreatesUriFromPath() {
            // Act
            Uri uri = UriUtility.CreatePartUri(@"a\b\c.txt");

            // Assert
            Assert.AreEqual(new Uri("/a/b/c.txt", UriKind.Relative), uri); 
        }

        [TestMethod]
        public void CreatePartUriEncodesUri() {
            // Act
            Uri uri = UriUtility.CreatePartUri(@"My awesome projects\C#.NET\?123\foo.txt");

            // Assert
            Assert.AreEqual(@"/My%20awesome%20projects/C%23.NET/%3F123/foo.txt", uri.ToString());
        }

        [TestMethod]
        public void GetPathFromUri() {
            // Arrange
            Uri uri = new Uri("/a/b.txt", UriKind.Relative);

            // Act
            string path = UriUtility.GetPath(uri);

            // Assert
            Assert.AreEqual(@"a\b.txt", path);
        }

        [TestMethod]
        public void GetPathFromUriWithEncodedSpacesDecodesSpaces() {
            // Arrange
            Uri uri = new Uri("/a/b/This%20is%20a%20test/c.txt", UriKind.Relative);

            // Act
            string path = UriUtility.GetPath(uri);

            // Assert
            Assert.AreEqual(@"a\b\This is a test\c.txt", path);
        }
    }
}
