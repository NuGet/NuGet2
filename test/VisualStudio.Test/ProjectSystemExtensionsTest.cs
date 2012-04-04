using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class ProjectSystemExtensionsTest
    {
        [Fact]
        public void CreateRefreshFileAddsRefreshFileUnderBinDirectory()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(VersionUtility.DefaultTargetFramework, @"x:\test\site\");
            var assemblyPath = @"x:\test\packages\Foo.1.0\lib\net40\Foo.dll";

            // Act
            projectSystem.CreateRefreshFile(assemblyPath);

            // Assert
            Assert.Equal(@"..\packages\Foo.1.0\lib\net40\Foo.dll", projectSystem.ReadAllText(@"bin\Foo.dll.refresh"));
        }

        [Fact]
        public void CreateRefreshFileUsesAbsolutePathIfRelativePathsCannotBeFormed()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(VersionUtility.DefaultTargetFramework, @"z:\test\site\");
            var assemblyPath = @"x:\test\packages\Foo.1.0\lib\net40\Bar.net40.dll";

            // Act
            projectSystem.CreateRefreshFile(assemblyPath);

            // Assert
            Assert.Equal(@"x:\test\packages\Foo.1.0\lib\net40\Bar.net40.dll", projectSystem.ReadAllText(@"bin\Bar.net40.dll.refresh"));
        }
    }
}
