using EnvDTE;
using Xunit;

namespace NuGet.VisualStudio.Test {
    
    public class ProjectExtensionsTest {
        [Fact]
        public void GetOutputPathForWebSite() {
            // Arrange
            Project project = TestUtils.GetProject("WebProject", VsConstants.WebSiteProjectTypeGuid);

            // Act
            string path = project.GetOutputPath();

            // Assert
            Assert.Equal(@"WebProject\Bin", path);
        }
    }
}
