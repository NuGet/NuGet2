using EnvDTE;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    using PackageUtility = NuGet.Test.PackageUtility;

    public class ProjectExtensionsTest
    {
        [Fact]
        public void GetOutputPathForWebSite()
        {
            // Arrange
            Project project = TestUtils.GetProject("WebProject", VsConstants.WebSiteProjectTypeGuid);

            // Act
            string path = project.GetOutputPath();

            // Assert
            Assert.Equal(@"WebProject\Bin", path);
        }

        [Theory]
        [InlineData("", "", "Windows, Version=0.0")]
        [InlineData(null, null, "Windows, Version=0.0")]
        [InlineData("", "Windows", "Windows, Version=0.0")]
        [InlineData(null, "Windows", "Windows, Version=0.0")]
        [InlineData("8.0", "Windows", "Windows, Version=8.0")]
        [InlineData("8.1", "Windows", "Windows, Version=8.1")]
        [InlineData("", "WindowsPhoneApp", "WindowsPhoneApp, Version=0.0")]
        [InlineData("8.1", "WindowsPhoneApp", "WindowsPhoneApp, Version=8.1")]
        [InlineData("10", "vNextJSApp", "vNextJSApp, Version=10")]
        public void GetTargetFrameworkForJSProjectReturnsCorrectPlatformVersion(string platformVersion, string platformIdentifier, string exptectedTargetFramework)
        {
            // Arrange
            var project = new Mock<Project>();
            project.Setup(p => p.Kind).Returns(VsConstants.JsProjectTypeGuid);

            var verProp = new Mock<Property>();
            verProp.Setup(x => x.Value).Returns(platformVersion);

            var idProp = new Mock<Property>();
            idProp.Setup(x => x.Value).Returns(platformIdentifier);

            project.Setup(p => p.Properties.Item(It.Is<object>(v => "TargetPlatformVersion".Equals(v))))
                   .Returns(verProp.Object);

            project.Setup(p => p.Properties.Item(It.Is<object>(v => "TargetPlatformIdentifier".Equals(v))))
                   .Returns(idProp.Object);

            // Act
            string targetFramework = ProjectExtensions.GetTargetFramework(project.Object);

            // Assert
            Assert.Equal(exptectedTargetFramework, targetFramework);
        }

        [Fact]
        public void GetTargetFrameworkForXnaProjectReturnsWindowsPhoneTargetFramework()
        {
            // Arrange
            var project = new Mock<Project>();

            var xnaProperty = new Mock<Property>();
            xnaProperty.Setup(x => x.Value).Returns("Windows Phone OS 7.1");

            project.Setup(p => p.Properties.Item(It.Is<object>(v => "Microsoft.Xna.GameStudio.CodeProject.WindowsPhoneProjectPropertiesExtender.XnaRefreshLevel".Equals(v))))
                   .Returns(xnaProperty.Object);

            var fxProperty = new Mock<Property>();
            fxProperty.Setup(x => x.Value).Returns(".NETFramework,Version=v4.0");
            project.Setup(p => p.Properties.Item(It.Is<object>(v => "TargetFrameworkMoniker".Equals(v))))
                   .Returns(fxProperty.Object);

            // Act
            string targetFramework = ProjectExtensions.GetTargetFramework(project.Object);

            // Assert
            Assert.Equal("Silverlight,Version=v4.0,Profile=WindowsPhone71", targetFramework);
        }

        [Fact]
        public void GetTargetFrameworkForWrongXnaProjectDoesNotReturnWindowsPhoneTargetFramework()
        {
            // Arrange
            var project = new Mock<Project>();

            var xnaProperty = new Mock<Property>();
            xnaProperty.Setup(x => x.Value).Returns("Windows Phone OS 7.0");    // 7.0 is not recognized. Only 7.1 is.

            project.Setup(p => p.Properties.Item(It.Is<object>(v => "Microsoft.Xna.GameStudio.CodeProject.WindowsPhoneProjectPropertiesExtender.XnaRefreshLevel".Equals(v))))
                   .Returns(xnaProperty.Object);

            var fxProperty = new Mock<Property>();
            fxProperty.Setup(x => x.Value).Returns(".NETFramework,Version=v4.0");
            project.Setup(p => p.Properties.Item(It.Is<object>(v => "TargetFrameworkMoniker".Equals(v))))
                   .Returns(fxProperty.Object);

            // Act
            string targetFramework = ProjectExtensions.GetTargetFramework(project.Object);

            // Assert
            Assert.Equal(".NETFramework,Version=v4.0", targetFramework);
        }

        [Fact]
        public void GetTargetFrameworkForMissingXnaPropertyDoesNotReturnWindowsPhoneTargetFramework()
        {
            // Arrange
            var project = new Mock<Project>();

            var fxProperty = new Mock<Property>();
            fxProperty.Setup(x => x.Value).Returns(".NETFramework,Version=v4.0");
            project.Setup(p => p.Properties.Item(It.Is<object>(v => "TargetFrameworkMoniker".Equals(v))))
                   .Returns(fxProperty.Object);

            // Act
            string targetFramework = ProjectExtensions.GetTargetFramework(project.Object);

            // Assert
            Assert.Equal(".NETFramework,Version=v4.0", targetFramework);
        }

        [Theory]
        [InlineData(".NETFramework,Version=v4.5")]
        [InlineData("Silverlight,Version=v4.0")]
        [InlineData("WindowsPhone,Version=v7.0")]
        [InlineData("WindowsPhone,Version=v8.0")]
        public void GetTargetFrameworkForWrongTargetFrameowrkVersionInXnaProjectDoesNotReturnWindowsPhoneTargetFramework(string framework)
        {
            // Arrange
            var project = new Mock<Project>();

            var xnaProperty = new Mock<Property>();
            xnaProperty.Setup(x => x.Value).Returns("Windows Phone OS 7.1");

            project.Setup(p => p.Properties.Item(It.Is<object>(v => "Microsoft.Xna.GameStudio.CodeProject.WindowsPhoneProjectPropertiesExtender.XnaRefreshLevel".Equals(v))))
                   .Returns(xnaProperty.Object);

            var fxProperty = new Mock<Property>();
            fxProperty.Setup(x => x.Value).Returns(framework);
            project.Setup(p => p.Properties.Item(It.Is<object>(v => "TargetFrameworkMoniker".Equals(v))))
                   .Returns(fxProperty.Object);

            // Act
            string targetFramework = ProjectExtensions.GetTargetFramework(project.Object);

            // Assert
            Assert.Equal(framework, targetFramework);
        }

        [Fact]
        public void IsCompatibleReturnsTrueIfPackageIsCompatibleWithProject()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A", 
                "1.0", 
                content: new string[] { "net40\\a.txt", "sl3\\b.txt" },
                assemblyReferences: new string[] { "lib\\winrt45\\c.dll", "lib\\net35\\d.dll" });
            var project = new Mock<Project>();
            project.Setup(p => p.Properties.Item("TargetFrameworkMoniker").Value).Returns(".NETFramework, Version=4.5");

            // Act
            bool result = project.Object.IsCompatible(package);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCompatibleReturnsFalseIfPackageIsNotCompatibleWithProject()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                "1.0",
                content: new string[] { "net40\\a.txt", "sl3\\b.txt" },
                assemblyReferences: new string[] { "lib\\winrt45\\c.dll", "lib\\net35\\d.dll" });
            var project = new Mock<Project>();
            project.Setup(p => p.Properties.Item("TargetFrameworkMoniker").Value).Returns(".NETFramework, Version=3.0");

            // Act
            bool result = project.Object.IsCompatible(package);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCompatibleReturnsTrueIfPackageHasFallbackContentFile()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                "1.0",
                content: new string[] { "a.txt", "sl3\\b.txt" },
                assemblyReferences: new string[] { "lib\\winrt45\\c.dll", "lib\\net35\\d.dll" });
            var project = new Mock<Project>();
            project.Setup(p => p.Properties.Item("TargetFrameworkMoniker").Value).Returns(".NETFramework, Version=3.0");

            // Act
            bool result = project.Object.IsCompatible(package);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCompatibleReturnsTrueIfPackageHasFallbackAssemblyReference()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                "1.0",
                content: new string[] { "net40\\a.txt", "sl3\\b.txt" },
                assemblyReferences: new string[] { "lib\\winrt45\\c.dll", "lib\\d.dll" });
            var project = new Mock<Project>();
            project.Setup(p => p.Properties.Item("TargetFrameworkMoniker").Value).Returns(".NETFramework, Version=3.0");

            // Act
            bool result = project.Object.IsCompatible(package);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCompatibleReturnsTrueIfProjectHasNullTargetFrameworkMonikerValue()
        {
            // Arrange
            var package = PackageUtility.CreatePackage(
                "A",
                "1.0",
                content: new string[] { "net40\\a.txt", "sl3\\b.txt" },
                assemblyReferences: new string[] { "lib\\winrt45\\c.dll", "lib\\net35\\d.dll" });
            var project = new Mock<Project>();
            project.Setup(p => p.Properties.Item("TargetFrameworkMoniker").Value).Returns(null);

            // Act
            bool result = project.Object.IsCompatible(package);

            // Assert
            Assert.True(result);
        }

        // Tests that EnsureCheckedOutIfExists() calls MakeFileWritetable()
        [Fact]
        public void EnsureCheckedOutIfExistsOnReadOnlyFiles()
        {
            // Arrange
            var project = TestUtils.GetProject("Name");
            var mockFileSystem = new Mock<IFileSystem>();
            var fileName = "testFile";
            mockFileSystem.Setup(f => f.GetFullPath(fileName)).Returns(fileName);
            mockFileSystem.Setup(f => f.FileExists(fileName)).Returns(true);
            mockFileSystem.Setup(f => f.MakeFileWritable(fileName));

            // Act
            project.EnsureCheckedOutIfExists(mockFileSystem.Object, fileName);

            // Assert
            mockFileSystem.Verify(f => f.MakeFileWritable(fileName));
        }
    }
}
