using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test.Server.Infrastructure {
    [TestClass]
    public class HelpersTest {
        [TestMethod]
        public void GetRepositoryUrlCreatesProperUrlWithRootWebApp() {
            // Arrange
            Uri url = new Uri("http://example.com/default.aspx");
            string applicationPath = "/";

            // Act
            string repositoryUrl = Helpers.GetRepositoryUrl(url, applicationPath);

            // Assert
            Assert.AreEqual("http://example.com/nuget", repositoryUrl);
        }

        [TestMethod]
        public void GetRepositoryUrlCreatesProperUrlWithVirtualApp() {
            // Arrange
            Uri url = new Uri("http://example.com/Foo/default.aspx");
            string applicationPath = "/Foo";

            // Act
            string repositoryUrl = Helpers.GetRepositoryUrl(url, applicationPath);

            // Assert
            Assert.AreEqual("http://example.com/Foo/nuget", repositoryUrl);
        }

        [TestMethod]
        public void GetRepositoryUrlWithNonStandardPortCreatesProperUrlWithRootWebApp() {
            // Arrange
            Uri url = new Uri("http://example.com:1337/default.aspx");
            string applicationPath = "/";

            // Act
            string repositoryUrl = Helpers.GetRepositoryUrl(url, applicationPath);

            // Assert
            Assert.AreEqual("http://example.com:1337/nuget", repositoryUrl);
        }

        [TestMethod]
        public void GetRepositoryUrlWithNonStandardPortCreatesProperUrlWithVirtualApp() {
            // Arrange
            Uri url = new Uri("http://example.com:1337/Foo/default.aspx");
            string applicationPath = "/Foo";

            // Act
            string repositoryUrl = Helpers.GetRepositoryUrl(url, applicationPath);

            // Assert
            Assert.AreEqual("http://example.com:1337/Foo/nuget", repositoryUrl);
        }
    }
}
