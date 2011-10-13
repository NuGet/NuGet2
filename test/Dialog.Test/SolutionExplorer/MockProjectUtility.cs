using EnvDTE;
using Moq;

namespace NuGet.Dialog.Test
{
    internal static class MockProjectUtility
    {
        public static Project CreateMockProject(string name)
        {
            var project = new Mock<Project>();
            project.Setup(p => p.Name).Returns(name);
            return project.Object;
        }
    }
}
