using System;
using Xunit;

namespace NuGet.Test
{
    public class UriHelperTest
    {
        [Fact]
        public void CreatePartUriCreatesUriFromPath()
        {
            // Act
            Uri uri = UriUtility.CreatePartUri(@"a\b\c.txt");

            // Assert
            Assert.Equal(new Uri("/a/b/c.txt", UriKind.Relative).ToString(), uri.ToString());
        }

        [Fact]
        public void CreatePartUriEncodesUri()
        {
            // Act
            Uri uri = UriUtility.CreatePartUri(@"My awesome projects\C#.NET\?123\foo.txt");

            // Assert
            Assert.Equal(@"/My%20awesome%20projects/C%23.NET/%3F123/foo.txt", uri.ToString());
        }

        [Fact]
        public void GetPathFromUri()
        {
            // Arrange
            Uri uri = new Uri("/a/b.txt", UriKind.Relative);

            // Act
            string path = UriUtility.GetPath(uri);

            // Assert
            Assert.Equal(@"a\b.txt", path);
        }

        [Fact]
        public void GetPathFromUriWithEncodedSpacesDecodesSpaces()
        {
            // Arrange
            Uri uri = new Uri("/a/b/This%20is%20a%20test/c.txt", UriKind.Relative);

            // Act
            string path = UriUtility.GetPath(uri);

            // Assert
            Assert.Equal(@"a\b\This is a test\c.txt", path);
        }
    }
}
