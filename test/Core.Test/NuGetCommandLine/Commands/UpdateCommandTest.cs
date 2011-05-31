using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Test.Mocks;

namespace NuGet.Test.NuGetCommandLine.Commands {
    [TestClass]
    public class UpdateCommandTest {
        [TestMethod]
        public void SelfUpdateNoCommandLinePackageOnServerThrows() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(new MockPackageRepository());

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Console = consoleInfo.Console;

            // Act
            ExceptionAssert.Throws<CommandLineException>(() => updateCmd.SelfUpdate("c:\foo.exe", new Version("2.0")), "Unable to find 'NuGet.CommandLine' package.");
        }

        [TestMethod]
        public void SelfUpdateOlderVersionDoesNotUpdate() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            var repository = new MockPackageRepository();
            repository.Add(PackageUtility.CreatePackage("NuGet.CommandLine", "1.0"));
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(repository);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Console = consoleInfo.Console;

            // Act
            updateCmd.SelfUpdate("c:\foo.exe", new Version("2.0"));

            // Assert
            Assert.AreEqual("NuGet.exe is up to date.", consoleInfo.WrittenLines[0]);
        }

        [TestMethod]
        public void SelfUpdateNoNuGetExeInNuGetExePackageThrows() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            var repository = new MockPackageRepository();
            repository.Add(PackageUtility.CreatePackage("NuGet.CommandLine", "3.0"));
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(repository);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Console = consoleInfo.Console;
            updateCmd.Console = consoleInfo.Console;

            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => updateCmd.SelfUpdate("c:\foo.exe", new Version("2.0")), "Invalid NuGet.CommandLine package. Unable to locate NuGet.exe within the package.");
        }

        [TestMethod]
        public void SelfUpdateNewVersionDoesUpdatesExe() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            var repository = new MockPackageRepository();
            IPackage package = PackageUtility.CreatePackage("NuGet.CommandLine", "3.0", tools: new[] { "NuGet.exe" });
            repository.Add(package);
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(repository);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new MockUpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Console = consoleInfo.Console;

            // Act
            updateCmd.SelfUpdate(@"c:\NuGet.exe", new Version("2.0"));

            // Assert
            Assert.IsTrue(updateCmd.MovedFiles.ContainsKey(@"c:\NuGet.exe"));
            Assert.AreEqual(@"c:\NuGet.exe.old", updateCmd.MovedFiles[@"c:\NuGet.exe"]);
            Assert.IsTrue(updateCmd.UpdatedFiles.ContainsKey(@"c:\NuGet.exe"));
            Assert.AreEqual(@"tools\NuGet.exe", updateCmd.UpdatedFiles[@"c:\NuGet.exe"]);
        }

        [TestMethod]
        public void UpdatePackagesUpdatesPackagesInReferenceFileAndAnyAssemblyReferences() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            var mockFileSystem = new MockFileSystem();
            var referenceFile = new PackageReferenceFile(mockFileSystem, "packages.config");
            var mockProject = new Mock<MockProjectSystem>() { CallBase = true };
            var msBuildProject = mockProject.As<IMSBuildProjectSystem>();
            mockProject.Object.AddReference("A.dll");
            mockProject.Object.AddReference("B.dll");

            referenceFile.AddEntry("A", new Version("1.0"));
            referenceFile.AddEntry("B", new Version("1.0"));

            var localRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", assemblyReferences: new[] { "A.dll" }),
                PackageUtility.CreatePackage("B", assemblyReferences: new[] { "B.dll" })  
            };

            var sourceRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { "A2.dll" }),
                PackageUtility.CreatePackage("B", "2.0", assemblyReferences: new[] { "B2.dll" })
            };

            var pathResolver = new Mock<IPackagePathResolver>();
            pathResolver.Setup(m => m.GetInstallPath(It.IsAny<IPackage>())).Returns<IPackage>(p => p.Id + "." + p.Version);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Console = consoleInfo.Console;

            // Act
            updateCmd.UpdatePackages(referenceFile, msBuildProject.Object, localRepository, sourceRepository, pathResolver.Object);

            // Assert
            Assert.AreEqual(2, mockProject.Object.References.Count);
            Assert.IsTrue(mockProject.Object.ReferenceExists("A2.dll"));
            Assert.IsTrue(mockProject.Object.ReferenceExists("B2.dll"));
            Assert.IsFalse(referenceFile.EntryExists("A", new Version("1.0")));
            Assert.IsFalse(referenceFile.EntryExists("B", new Version("1.0")));
            Assert.IsTrue(referenceFile.EntryExists("A", new Version("2.0")));
            Assert.IsTrue(referenceFile.EntryExists("B", new Version("2.0")));
        }

        [TestMethod]
        public void UpdatePackagesUpdatesPackagesOnlySpecifiedPackages() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            var mockFileSystem = new MockFileSystem();
            var referenceFile = new PackageReferenceFile(mockFileSystem, "packages.config");
            var mockProject = new Mock<MockProjectSystem>() { CallBase = true };
            var msBuildProject = mockProject.As<IMSBuildProjectSystem>();
            mockProject.Object.AddReference("A.dll");
            mockProject.Object.AddReference("B.dll");

            referenceFile.AddEntry("A", new Version("1.0"));
            referenceFile.AddEntry("B", new Version("1.0"));

            var localRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", assemblyReferences: new[] { "A.dll" }),
                PackageUtility.CreatePackage("B", assemblyReferences: new[] { "B.dll" })  
            };

            var sourceRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { "A2.dll" }),
                PackageUtility.CreatePackage("B", "2.0", assemblyReferences: new[] { "B2.dll" })
            };

            var pathResolver = new Mock<IPackagePathResolver>();
            pathResolver.Setup(m => m.GetInstallPath(It.IsAny<IPackage>())).Returns<IPackage>(p => p.Id + "." + p.Version);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Id.Add("B");
            updateCmd.Console = consoleInfo.Console;

            // Act
            updateCmd.UpdatePackages(referenceFile, msBuildProject.Object, localRepository, sourceRepository, pathResolver.Object);

            // Assert
            Assert.AreEqual(2, mockProject.Object.References.Count);
            Assert.IsTrue(mockProject.Object.ReferenceExists("A.dll"));
            Assert.IsTrue(mockProject.Object.ReferenceExists("B2.dll"));
            Assert.IsTrue(referenceFile.EntryExists("A", new Version("1.0")));
            Assert.IsFalse(referenceFile.EntryExists("B", new Version("1.0")));
            Assert.IsTrue(referenceFile.EntryExists("B", new Version("2.0")));
            Assert.IsFalse(referenceFile.EntryExists("A", new Version("2.0")));
        }

        [TestMethod]
        public void UpdatePackagesSkipsPackagesThatArentInLocalRepository() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            var mockFileSystem = new MockFileSystem();
            var referenceFile = new PackageReferenceFile(mockFileSystem, "packages.config");
            var mockProject = new Mock<MockProjectSystem>() { CallBase = true };
            var msBuildProject = mockProject.As<IMSBuildProjectSystem>();
            mockProject.Object.AddReference("A.dll");
            mockProject.Object.AddReference("B.dll");

            referenceFile.AddEntry("A", new Version("1.0"));
            referenceFile.AddEntry("B", new Version("1.0"));

            var localRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", assemblyReferences: new[] { "A.dll" })
            };

            var sourceRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { "A2.dll" }),
                PackageUtility.CreatePackage("B", "2.0", assemblyReferences: new[] { "B2.dll" })
            };

            var pathResolver = new Mock<IPackagePathResolver>();
            pathResolver.Setup(m => m.GetInstallPath(It.IsAny<IPackage>())).Returns<IPackage>(p => p.Id + "." + p.Version);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Console = consoleInfo.Console;

            // Act
            updateCmd.UpdatePackages(referenceFile, msBuildProject.Object, localRepository, sourceRepository, pathResolver.Object);

            // Assert
            Assert.AreEqual(2, mockProject.Object.References.Count);
            Assert.IsTrue(mockProject.Object.ReferenceExists("A2.dll"));
            Assert.IsTrue(mockProject.Object.ReferenceExists("B.dll"));
            Assert.IsTrue(referenceFile.EntryExists("A", new Version("2.0")));
            Assert.IsFalse(referenceFile.EntryExists("A", new Version("1.0")));
            Assert.IsTrue(referenceFile.EntryExists("B", new Version("1.0")));
            Assert.IsFalse(referenceFile.EntryExists("B", new Version("2.0")));
        }

        [TestMethod]
        public void UpdatePackagesThrowsIfSpecifiedPackageNotInstalled() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            var mockFileSystem = new MockFileSystem();
            var referenceFile = new PackageReferenceFile(mockFileSystem, "packages.config");
            var mockProject = new Mock<MockProjectSystem>() { CallBase = true };
            var msBuildProject = mockProject.As<IMSBuildProjectSystem>();
            mockProject.Object.AddReference("A.dll");
            mockProject.Object.AddReference("B.dll");

            referenceFile.AddEntry("A", new Version("1.0"));
            referenceFile.AddEntry("B", new Version("1.0"));

            var localRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", assemblyReferences: new[] { "A.dll" }),
                PackageUtility.CreatePackage("B", assemblyReferences: new[] { "B.dll" })  
            };

            var sourceRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { "A2.dll" }),
                PackageUtility.CreatePackage("B", "2.0", assemblyReferences: new[] { "B2.dll" })
            };

            var pathResolver = new Mock<IPackagePathResolver>();
            pathResolver.Setup(m => m.GetInstallPath(It.IsAny<IPackage>())).Returns<IPackage>(p => p.Id + "." + p.Version);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Id.Add("C");
            updateCmd.Console = consoleInfo.Console;

            // Act
            ExceptionAssert.Throws<CommandLineException>(() => updateCmd.UpdatePackages(referenceFile, msBuildProject.Object, localRepository, sourceRepository, pathResolver.Object), "Unable to find 'C'. Make sure they are specified in packages.config.");
        }

        [TestMethod]
        public void UpdatePackagesSafe() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            var mockFileSystem = new MockFileSystem();
            var referenceFile = new PackageReferenceFile(mockFileSystem, "packages.config");
            var mockProject = new Mock<MockProjectSystem>() { CallBase = true };
            var msBuildProject = mockProject.As<IMSBuildProjectSystem>();
            mockProject.Object.AddReference("A.dll");

            referenceFile.AddEntry("A", new Version("1.0"));
            referenceFile.AddEntry("B", new Version("1.0"));

            var localRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", assemblyReferences: new[] { "A.dll" }),
            };

            var sourceRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", "1.0.1", assemblyReferences: new[] { "A1010.dll" }),
                PackageUtility.CreatePackage("A", "1.0.20", assemblyReferences: new[] { "A1020.dll" }),
                PackageUtility.CreatePackage("A", "1.5", assemblyReferences: new[] { "A15.dll" }),
                PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { "A2.dll" })
            };

            var pathResolver = new Mock<IPackagePathResolver>();
            pathResolver.Setup(m => m.GetInstallPath(It.IsAny<IPackage>())).Returns<IPackage>(p => p.Id + "." + p.Version);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Safe = true;
            updateCmd.Console = consoleInfo.Console;

            // Act
            updateCmd.UpdatePackages(referenceFile, msBuildProject.Object, localRepository, sourceRepository, pathResolver.Object);

            // Assert
            Assert.AreEqual(1, mockProject.Object.References.Count);
            Assert.IsTrue(mockProject.Object.ReferenceExists("A1020.dll"));
            Assert.IsTrue(referenceFile.EntryExists("A", new Version("1.0.20")));
            Assert.IsTrue(referenceFile.EntryExists("B", new Version("1.0")));
        }

        [TestMethod]
        public void UpdatePackagesSkipsPackagesThatAreNotAssemblyOnly() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            var mockFileSystem = new MockFileSystem();
            var referenceFile = new PackageReferenceFile(mockFileSystem, "packages.config");
            var mockProject = new Mock<MockProjectSystem>() { CallBase = true };
            var msBuildProject = mockProject.As<IMSBuildProjectSystem>();
            mockProject.Object.AddReference("A.dll");

            referenceFile.AddEntry("A", new Version("1.0"));
            referenceFile.AddEntry("B", new Version("1.0"));

            var localRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", assemblyReferences: new[] { "A.dll" }),
                PackageUtility.CreatePackage("B", content: new[] { "B.txt" })  
            };

            var sourceRepository = new MockPackageRepository { 
                PackageUtility.CreatePackage("A", "2.0", assemblyReferences: new[] { "A2.dll" }),
                PackageUtility.CreatePackage("B", "2.0", content: new[] { "B2.txt" })
            };

            var pathResolver = new Mock<IPackagePathResolver>();
            pathResolver.Setup(m => m.GetInstallPath(It.IsAny<IPackage>())).Returns<IPackage>(p => p.Id + "." + p.Version);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Console = consoleInfo.Console;

            // Act
            updateCmd.UpdatePackages(referenceFile, msBuildProject.Object, localRepository, sourceRepository, pathResolver.Object);

            // Assert
            Assert.AreEqual(1, mockProject.Object.References.Count);
            Assert.IsTrue(mockProject.Object.ReferenceExists("A2.dll"));
            Assert.IsFalse(referenceFile.EntryExists("A", new Version("1.0")));
            Assert.IsTrue(referenceFile.EntryExists("B", new Version("1.0")));
            Assert.IsTrue(referenceFile.EntryExists("A", new Version("2.0")));
            Assert.IsFalse(referenceFile.EntryExists("B", new Version("2.0")));
        }

        private ConsoleInfo GetConsoleInfo() {
            var lines = new List<string>();
            var console = new Mock<IConsole>();
            console.Setup(m => m.WriteLine(It.IsAny<string>())).Callback<string>(lines.Add);
            console.Setup(m => m.WriteWarning(It.IsAny<string>())).Callback<string>(lines.Add);            
            return new ConsoleInfo(console.Object, lines);
        }

        // Using Tuple.ItemN is makes the code harder to read
        private class ConsoleInfo {
            public ConsoleInfo(IConsole console, IList<string> lines) {
                Console = console;
                WrittenLines = lines;
            }
            public IConsole Console { get; private set; }
            public IList<string> WrittenLines { get; private set; }
        }

        private class MockUpdateCommand : UpdateCommand {
            public Dictionary<string, string> MovedFiles = new Dictionary<string, string>();
            public Dictionary<string, string> UpdatedFiles = new Dictionary<string, string>();

            public MockUpdateCommand(IPackageRepositoryFactory factory, IPackageSourceProvider sourceProvider)
                : base(factory, sourceProvider) {
            }

            protected override void Move(string oldPath, string newPath) {
                MovedFiles[oldPath] = newPath;
            }

            protected override void UpdateFile(string exePath, IPackageFile file) {
                UpdatedFiles[exePath] = file.Path;
            }
        }
    }
}
