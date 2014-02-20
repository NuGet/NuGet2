using System.IO;
using Moq;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class XdtTransformTest
    {
        [Fact]
        public void AddPackageWithXdtTransformFile()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            mockProjectSystem.AddFile("web.config",
@"<configuration>
    <system.web>
        <compilation debug=""true"" />
    </system.web>
</configuration>
".AsStream());
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());

            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(m => m.Listed).Returns(true);
            
            var file = new Mock<IPackageFile>();
            file.Setup(m => m.Path).Returns(@"content\web.config.install.xdt");
            file.Setup(m => m.EffectivePath).Returns("web.config.install.xdt");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration xmlns:xdt=""http://schemas.microsoft.com/XML-Document-Transform"">
    <system.web>
        <compilation xdt:Locator=""Condition('@debug=true')"" debug=""false"" xdt:Transform=""Replace"" />
    </system.web>
</configuration>".AsStream());

            var file2 = new Mock<IPackageFile>();
            file2.Setup(m => m.Path).Returns(@"content\web.config.uninstall.xdt");
            file2.Setup(m => m.EffectivePath).Returns("web.config.uninstall.xdt");
            file2.Setup(m => m.GetStream()).Returns(() =>
@"<configuration xmlns:xdt=""http://schemas.microsoft.com/XML-Document-Transform"">
    <system.web>
        <compilation xdt:Locator=""Match(debug)"" debug=""false"" xdt:Transform=""Remove"" />
    </system.web>
</configuration>".AsStream());

            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object, file2.Object });
            mockRepository.AddPackage(package.Object);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(mockProjectSystem.FileExists("web.config.install.xdt"));
            Assert.False(mockProjectSystem.FileExists("web.config.uninstall.xdt"));
            Assert.True(mockProjectSystem.FileExists("web.config"));
            Assert.Equal(
@"<configuration>
    <system.web>
        <compilation debug=""false""/>
    </system.web>
</configuration>
", mockProjectSystem.OpenFile("web.config").ReadToEnd());
        }

        // Regression test for the bug that XDT won't work when xml nodes have
        // attributes.
        [Fact]
        public void XdtTransformOnXmlNodeWithAttributes()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            mockProjectSystem.AddFile("test.xml",
@"<a attrib=""b""/>".AsStream());
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());

            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(m => m.Listed).Returns(true);

            var file = new Mock<IPackageFile>();
            file.Setup(m => m.Path).Returns(@"content\test.xml.install.xdt");
            file.Setup(m => m.EffectivePath).Returns("test.xml.install.xdt");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<a xmlns:xdt=""http://schemas.microsoft.com/XML-Document-Transform""><test xdt:Transform=""InsertIfMissing""/></a>".AsStream());            

            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object });
            mockRepository.AddPackage(package.Object);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(mockProjectSystem.FileExists("test.xml"));
            var actual = mockProjectSystem.OpenFile("test.xml").ReadToEnd();
            Assert.Equal("<a attrib=\"b\">\t<test/></a>", actual);
        }

        [Fact]
        public void ReThrowWithMeaningfulErrorMessageWhenXdtFileHasSyntaxError()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            mockProjectSystem.AddFile("web.config",
@"<configuration>
    <system.web>
        <compilation debug=""true"" />
    </system.web>
</configuration>
".AsStream());
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());

            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(m => m.Listed).Returns(true);

            var file = new Mock<IPackageFile>();
            file.Setup(m => m.Path).Returns(@"content\web.config.install.xdt");
            file.Setup(m => m.EffectivePath).Returns("web.config.install.xdt");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration xmlns:xdt=""http://schemas.microsoft.com/XML-Document-Transform"">
    <system.web>
        <compilation xd:Locator=""Condition('@debug=true')"" debug=""false"" xdt:Transform=""Replace"" />
    </system.web>
</configuration>".AsStream());

            var file2 = new Mock<IPackageFile>();
            file2.Setup(m => m.Path).Returns(@"content\web.config.uninstall.xdt");
            file2.Setup(m => m.EffectivePath).Returns("web.config.uninstall.xdt");
            file2.Setup(m => m.GetStream()).Returns(() =>
@"<configuration xmlns:xdt=""http://schemas.microsoft.com/XML-Document-Transform"">
    <system.web>
        <compilation xdt:Locator=""Match(debug)"" debug=""false"" xdt:Transform=""Remove"" />
    </system.web>
</configuration>".AsStream());

            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object, file2.Object });
            mockRepository.AddPackage(package.Object);

            // Act 
            ExceptionAssert.Throws<InvalidDataException>(
                () => projectManager.AddPackageReference("A"),
                @"An error occurred while applying transformation to 'web.config' in project 'x:\MockFileSystem': 'xd' is an undeclared prefix. Line 3, position 22.");

            // Assert
            Assert.False(mockProjectSystem.FileExists("web.config.install.xdt"));
            Assert.False(mockProjectSystem.FileExists("web.config.uninstall.xdt"));
            Assert.True(mockProjectSystem.FileExists("web.config"));
            Assert.Equal(
@"<configuration>
    <system.web>
        <compilation debug=""true"" />
    </system.web>
</configuration>
", mockProjectSystem.OpenFile("web.config").ReadToEnd());
        }

        [Fact]
        public void RemovePackageWithXdtTransformFile()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            mockProjectSystem.AddFile("web.config",
@"<configuration>
    <system.web><compilation debug=""false"" /></system.web>
</configuration>
".AsStream());
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());

            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(m => m.Listed).Returns(true);

            var file = new Mock<IPackageFile>();
            file.Setup(m => m.Path).Returns(@"content\web.config.uninstall.xdt");
            file.Setup(m => m.EffectivePath).Returns("web.config.uninstall.xdt");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration xmlns:xdt=""http://schemas.microsoft.com/XML-Document-Transform"">
    <system.web>
        <compilation xdt:Locator=""Match(debug)"" debug=""false"" xdt:Transform=""Remove"" />
    </system.web>
</configuration>".AsStream());

            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object });
            mockRepository.AddPackage(package.Object);

            // Act 1
            projectManager.AddPackageReference("A");

            // Assert 1
            Assert.False(mockProjectSystem.FileExists("web.config.uninstall.xdt"));
            Assert.Equal(
@"<configuration>
    <system.web><compilation debug=""false"" /></system.web>
</configuration>
", mockProjectSystem.OpenFile("web.config").ReadToEnd());

            // Act 2
            projectManager.RemovePackageReference("A");

            // Assert 2
            Assert.False(mockProjectSystem.FileExists("web.config.uninstall.xdt"));
            Assert.True(mockProjectSystem.FileExists("web.config"));
            Assert.Equal(
@"<configuration>
    <system.web></system.web>
</configuration>
", mockProjectSystem.OpenFile("web.config").ReadToEnd());
        }
    }
}