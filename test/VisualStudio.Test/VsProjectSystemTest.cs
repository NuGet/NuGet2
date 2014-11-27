using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using EnvDTE;
using Moq;
using NuGet.Test.Mocks;
using NuGet.VisualStudio.Resources;
using VSLangProj;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class VsProjectSystemTest
    {
        [Fact]
        public void GetPropertyValueUnknownPropertyReturnsNull()
        {
            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            VsProjectSystem projectSystem = new VsProjectSystem(TestUtils.GetProject("Name"), mockFileSystemProvider.Object);

            // Assert
            var value = projectSystem.GetPropertyValue("notexist");

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public void GetPropertyValueThrowsArgumentExceptionReturnsNull()
        {
            // Vs throws an argument exception when trying to index into an invalid property

            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            Project project = TestUtils.GetProject("Name",
                                                   propertyGetter: name => { throw new ArgumentException(); });
            VsProjectSystem projectSystem = new VsProjectSystem(project, mockFileSystemProvider.Object);

            // Assert
            var value = projectSystem.GetPropertyValue("notexist");

            // Assert
            Assert.Null(value);
        }

        [Theory]
        [InlineData("web.config")]
        [InlineData("web.DEBUG.CONFIG")]
        [InlineData("Web.release.Config")]
        [InlineData("Web.aaaa.config")]
        public void IsSupportedFileMethodRejectsAllVariationsOfWebConfigFile(string filePath)
        {
            // Arrange
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            Project project = TestUtils.GetProject("TestProject");
            VsProjectSystem projectSystem = new VsProjectSystem(project, mockFileSystemProvider.Object);

            // Act
            bool supported = projectSystem.IsSupportedFile(filePath);

            // Assert
            Assert.False(supported);
        }

        [Theory]
        [InlineData(".NetFramework, Version=1.0")]
        [InlineData(".NetCompact, Version=2.0, Profile=Client")]
        public void NonSilverlightProjectSupportsBindingRedirect(string targetFramework)
        {
            // Arrange
            var silverlightProject = TestUtils.GetProject(
                "Silverlight",
                propertyGetter: name => GetTargetFrameworkProperty("TargetFrameworkMoniker", targetFramework));
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            var projectSystem = new VsProjectSystem(silverlightProject, mockFileSystemProvider.Object);

            // Act
            bool bindingRedirectSupported = projectSystem.IsBindingRedirectSupported;

            // Assert
            Assert.True(bindingRedirectSupported);
        }

        [Theory]
        [InlineData("Silverlight, Version=1.0")]
        [InlineData("Silverlight, Version=2.0, Profile=Phone")]
        public void SilverlightProjectDoesNotSupportsBindingRedirect(string targetFramework)
        {
            // Arrange
            var silverlightProject = TestUtils.GetProject(
                "Silverlight",
                propertyGetter: name => GetTargetFrameworkProperty("TargetFrameworkMoniker", targetFramework));
            var mockFileSystemProvider = new Mock<IFileSystemProvider>();
            mockFileSystemProvider.Setup(fs => fs.GetFileSystem(It.IsAny<string>())).Returns(new MockFileSystem());
            var projectSystem = new VsProjectSystem(silverlightProject, mockFileSystemProvider.Object);

            // Act
            bool bindingRedirectSupported = projectSystem.IsBindingRedirectSupported;

            // Assert
            Assert.False(bindingRedirectSupported);
        }

        private Property GetTargetFrameworkProperty(string name, string targetFramework)
        {
            if (name == "TargetFrameworkMoniker")
            {
                var property = new Mock<Property>();
                property.Setup(p => p.Name).Returns(name);
                property.Setup(p => p.Value).Returns(targetFramework);
                return property.Object;
            }

            return null;
        }

        [Fact(Skip = "EnvDTE is not available.")]
        public void FSharpProjectSystemRemoveReference()
        {
            var project = new Mock<Project>();
            project.Setup(p => p.Properties.Item("FullPath").Value).Returns("x:\\");
            project.Setup(p => p.Name).Returns("Project");

            var reference = new Mock<Reference>();
            reference.Setup(r => r.Name).Returns("AbC");
            reference.Setup(r => r.Remove()).Verifiable();

            var enumerableReferences = new Reference[] { reference.Object };

            var references = new Mock<References>();
            references.Setup(s => s.Item(It.IsAny<object>())).Returns((Reference)null);
            references.Setup(s => s.GetEnumerator()).Returns(enumerableReferences.GetEnumerator());

            var fileSystem = new Mock<IFileSystem>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(f => f.GetFileSystem("x:\\")).Returns(fileSystem.Object);

            var logger = new Mock<ILogger>();

            FSharpProjectSystem projectSystem = new FSharpProjectSystem(project.Object, fileSystemProvider.Object);
            projectSystem.Logger = logger.Object;

            // Act
            projectSystem.RemoveReferenceCore("aBc", references.Object);

            // Assert
            reference.Verify();
            logger.Verify(l => l.Log(MessageLevel.Debug, VsResources.Debug_RemoveReference, "aBc", "Project"));
        }

        [Fact(Skip = "EnvDTE is not available.")]
        public void FSharpProjectSystemRemoveReferenceFailedToFindMatch()
        {
            var project = new Mock<Project>();
            project.Setup(p => p.Properties.Item("FullPath").Value).Returns("x:\\");
            project.Setup(p => p.Name).Returns("Project");

            var reference = new Mock<Reference>();
            reference.Setup(r => r.Name).Returns("AbC");

            var enumerableReferences = new Reference[] { reference.Object };

            var references = new Mock<References>();
            references.Setup(s => s.Item(It.IsAny<object>())).Returns((Reference)null);
            references.Setup(s => s.GetEnumerator()).Returns(enumerableReferences.GetEnumerator());

            var fileSystem = new Mock<IFileSystem>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(f => f.GetFileSystem("x:\\")).Returns(fileSystem.Object);

            var logger = new Mock<ILogger>();

            FSharpProjectSystem projectSystem = new FSharpProjectSystem(project.Object, fileSystemProvider.Object);
            projectSystem.Logger = logger.Object;

            // Act
            projectSystem.RemoveReferenceCore("aBcD", references.Object);

            // Assert
            reference.Verify(r => r.Remove(), Times.Never());
            logger.Verify(l => l.Log(MessageLevel.Warning, VsResources.Warning_FailedToFindMatchForRemoveReference, "aBcD"));
        }

        [Fact(Skip = "EnvDTE is not available.")]
        public void FSharpProjectSystemRemoveReferenceFailsMultipleMatches()
        {
            var project = new Mock<Project>();
            project.Setup(p => p.Properties.Item("FullPath").Value).Returns("x:\\");
            project.Setup(p => p.Name).Returns("Project");

            var reference1 = new Mock<Reference>();
            reference1.Setup(r => r.Name).Returns("AbC");

            var reference2 = new Mock<Reference>();
            reference2.Setup(r => r.Name).Returns("abc");

            var enumerableReferences = new Reference[] { reference1.Object, reference2.Object };

            var references = new Mock<References>();
            references.Setup(s => s.Item(It.IsAny<object>())).Returns((Reference)null);
            references.Setup(s => s.GetEnumerator()).Returns(enumerableReferences.GetEnumerator());

            var fileSystem = new Mock<IFileSystem>();
            var fileSystemProvider = new Mock<IFileSystemProvider>();
            fileSystemProvider.Setup(f => f.GetFileSystem("x:\\")).Returns(fileSystem.Object);

            var logger = new Mock<ILogger>();

            FSharpProjectSystem projectSystem = new FSharpProjectSystem(project.Object, fileSystemProvider.Object);
            projectSystem.Logger = logger.Object;

            // Act
            projectSystem.RemoveReferenceCore("aBc", references.Object);

            // Assert
            reference1.Verify(r => r.Remove(), Times.Never());
            reference2.Verify(r => r.Remove(), Times.Never());
            var message = String.Format(CultureInfo.CurrentCulture, VsResources.FailedToRemoveReference, "aBc");
            logger.Verify(l => l.Log(MessageLevel.Error, message));
        }
    }


    public class VcxProjectTest
    {

        public VcxProjectTest()
        {
            var resourcereader = new ResourceLoader();
            Loader.Instance = resourcereader;
        }


        [Theory]
        [InlineData("ManCpp.vcxproj", true)]
        [InlineData("ManCppClrFalse.vcxproj", false)]
        [InlineData("NativeCpp.vcxproj", false)]
        [InlineData("ManCppWithOverrideFalse.vcxproj", true)]
        [InlineData("ManCppWithOverride.vcxproj", false)]
        public void VerifyReadingManagedVcxprojFile(string name, bool expected)
        {

            var config = new Mock<Configuration>();
            config.Setup(s => s.ConfigurationName).Returns("Debug");
            config.Setup(s => s.PlatformName).Returns("Win32");


            var cut = new VcxProject(name);
            var result = cut.HasClrSupport(config.Object);

            Assert.True(result == expected);

        }
    }



    public class ResourceLoader : ILoader
    {
        public XDocument LoadXml(string resourcename)
        {
            var content = ReadResource(resourcename);
            return XDocument.Parse(content);
        }

        private string ReadResource(string resourcename)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var resource =
                assembly.GetManifestResourceNames()
                    .FirstOrDefault(res => res.ToUpper().EndsWith(resourcename.ToUpper()));

            if (resource != null) using (var stream = assembly.GetManifestResourceStream(resource))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }

            return "";
        }
    }
}