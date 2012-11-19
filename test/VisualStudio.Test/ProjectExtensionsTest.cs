using EnvDTE;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    using PackageUtility = NuGet.Test.PackageUtility;
    using Moq;

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

    }
}
