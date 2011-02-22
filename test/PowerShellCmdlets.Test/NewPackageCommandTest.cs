using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Test;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;

namespace NuGet.PowerShell.Commands.Test {
    [TestClass]
    public class NewPackageCommandTest {
        [TestMethod]
        public void NewPackageCmdletThrowsIfNoSolutionIsClosed() {
            // Arrange
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var cmdlet = new NewPackageCommand(TestUtils.GetSolutionManager(isSolutionOpen: false, defaultProjectName: null), packageManagerFactory.Object, null);

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(), "The current environment doesn't have a solution open.");
        }

        [TestMethod]
        public void NewPackageCmdletThrowsIfProjectSpecifiedDoesNotExist() {
            // Arrange
            var project = "does-not-exist";
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var solutionManager = TestUtils.GetSolutionManager(defaultProjectName: "test", projects: new[] { TestUtils.GetProject("test") });
            var cmdlet = new NewPackageCommand(solutionManager, packageManagerFactory.Object, null);
            cmdlet.ProjectName = project;

            // Act and Assert            
            ExceptionAssert.Throws<ItemNotFoundException>(() => cmdlet.GetResults(),
                String.Format("Project '{0}' is not found.", project));
        }

        [TestMethod]
        public void NewPackageCmdletThrowsIfSpecFileDoesNotExistAndSpecParameterDoesNotExist() {
            // Arrange
            var projectName = "test";
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var project = TestUtils.GetProject(projectName, projectFiles: new[] { "test.cs", "assembly.info", "foo.dll" });
            var solutionManager = TestUtils.GetSolutionManager(projects: new[] { project });
            var cmdlet = new NewPackageCommand(solutionManager, packageManagerFactory.Object, null);
            cmdlet.ProjectName = projectName;

            // Act and Assert
            ExceptionAssert.Throws<ItemNotFoundException>(() => cmdlet.GetResults(),
                "Unable to locate a .nuspec file in the specified project.");
        }

        [TestMethod]
        public void NewPackageCmdletThrowsIfMultipleSpecFilesExistAndSpecParameterDoesNotExist() {
            // Arrange
            var projectName = "test";
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Returns((IVsPackageManager)null);
            var project = TestUtils.GetProject(projectName, projectFiles: new[] { "foo.nuspec", "bar.nuspec", "foo.dll" });
            var solutionManager = TestUtils.GetSolutionManager(projects: new[] { project });
            var cmdlet = new NewPackageCommand(solutionManager, packageManagerFactory.Object, null);
            cmdlet.ProjectName = projectName;

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cmdlet.GetResults(),
                "More than one .nuspec files were found.");
        }

        [TestMethod]
        public void RemoveExcludedFilesRemovesManifestAndOtherNuGetageFiles() {
            // Arrange
            var packageBuilder = new PackageBuilder();
            var files = new[] { "somefile.nuspec", @"\foo\bar\somefile.nupkg", @"\baz\1.cs" };
            packageBuilder.Files.AddRange(from f in files  // This almost sounds like a cuss word!
                                          select new PhysicalPackageFile { TargetPath = f });

            // Act
            NewPackageCommand.RemoveExludedFiles(packageBuilder);

            // Assert
            Assert.AreEqual(@"\baz\1.cs", packageBuilder.Files.Single().Path);
        }

        [TestMethod]
        public void GetPackageFilePathAppendsProjectPathWhenPathIsNotRooted() {
            // Arrange
            var projectPath = @"X:\projects\my-project\";
            var outputFile = "mypk.out";
            var id = "id";
            var version = new Version("1.1");

            // Act
            var packagePath = NewPackageCommand.GetPackageFilePath(outputFile, projectPath, id, version);

            // Assert
            Assert.AreEqual(packagePath, Path.Combine(projectPath, outputFile));
        }

        [TestMethod]
        public void GetPackageFilePathUsesOutputFileWhenPathIsRooted() {
            // Arrange
            var projectPath = @"X:\projects\my-project\";
            var outputFile = @"X:\outputs\mypk.out";
            var id = "id";
            var version = new Version("1.1");

            // Act
            var packagePath = NewPackageCommand.GetPackageFilePath(outputFile, projectPath, id, version);

            // Assert
            Assert.AreEqual(packagePath, outputFile);
        }

        [TestMethod]
        public void GetPackageFilePathUsesIdAndVersionWhenOutputFileIsNull() {
            // Arrange
            var projectPath = @"X:\projects\my-project\";
            string outputFile = null;
            var id = "id";
            var version = new Version("1.1");

            // Act
            var packagePath = NewPackageCommand.GetPackageFilePath(outputFile, projectPath, id, version);

            // Assert
            Assert.AreEqual(packagePath, Path.Combine(projectPath, id + "." + version + ".nupkg"));
        }

    }
}
