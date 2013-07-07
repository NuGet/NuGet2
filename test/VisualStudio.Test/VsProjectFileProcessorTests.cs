using Moq;
using NuGet.Test;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.VisualStudio.Test
{
    public class VsProjectFileProcessorTests
    {
        [Fact]
        public void InstallRunsProcessorForMatchedFiles()
        {
            // Arrange
            const string propertyName = "PropertyName";
            const string propertyValue = "PropertyValue";
            var processingBuilder = new ProjectFileProcessingBuilder(
                new[] {new VsProjectItemPropertySetter("*.txt", propertyName, propertyValue)}
                );

            var processingItemMock = new Mock<IProjectFileProcessingProjectItem>();
            processingItemMock.Setup(o => o.SetPropertyValue(propertyName, propertyValue));

            var sourceRepository = new MockPackageRepository();

            var package = PackageUtility
                .CreatePackage("something", "1.0", new[] {"match.txt", "nomatch.cs"});
            sourceRepository.AddPackage(package);

            var projectSystem = new MockProjectSystem(path =>
                {
                    processingItemMock.SetupGet(o=>o.Path).Returns(path);
                    return processingItemMock.Object;
                });
            var pathResolver = new DefaultPackagePathResolver(projectSystem);

            var localRepository =
                new Mock<MockPackageRepository> {CallBase = true}.As<ISharedPackageRepository>().Object;
            var projectRepository = new MockProjectPackageRepository(localRepository);

            var projectManager = new ProjectManager(
                sourceRepository,
                pathResolver,
                projectSystem,
                projectRepository,
                processingBuilder);

            // act
            projectManager.AddPackageReference(package.Id);

            // assert
            processingItemMock
                .Verify(o => o.SetPropertyValue(propertyName, propertyValue),
                        Times.Once());
        }
    }
}