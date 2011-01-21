using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class ProjectExtensionsTest {
        [TestMethod]
        public void GetOutputPathForWebSite() {
            // Arrange
            Project project = TestUtils.GetProject("WebProject", VsConstants.WebSiteProjectTypeGuid);

            // Act
            string path = project.GetOutputPath();

            // Assert
            Assert.AreEqual(@"WebProject\Bin", path);
        }
    }
}
