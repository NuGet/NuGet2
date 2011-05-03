using System;
using System.Collections.Generic;
using System.Linq;
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
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(new MockPackageRepository());

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, GetSourceProvider());
            updateCmd.Console = consoleInfo.Console;

            // Act
            ExceptionAssert.Throws<CommandLineException>(() => updateCmd.SelfUpdate("c:\foo.exe", new Version("2.0")), "Unable to find 'NuGet.CommandLine' package.");
        }

        [TestMethod]
        public void SelfUpdateOlderVersionDoesNotUpdate() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var repository = new MockPackageRepository();
            repository.Add(PackageUtility.CreatePackage("NuGet.CommandLine", "1.0"));
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(repository);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, GetSourceProvider());
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
            var repository = new MockPackageRepository();
            repository.Add(PackageUtility.CreatePackage("NuGet.CommandLine", "3.0"));
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(repository);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, GetSourceProvider());
            updateCmd.Console = consoleInfo.Console;
            updateCmd.Console = consoleInfo.Console;

            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => updateCmd.SelfUpdate("c:\foo.exe", new Version("2.0")), "Invalid NuGet.CommandLine package. Unable to locate NuGet.exe within the package.");
        }

        [TestMethod]
        public void SelfUpdateIgnoresFailingPackageRepositories() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var repository = new MockPackageRepository();
            repository.Add(PackageUtility.CreatePackage("NuGet.CommandLine", "1.0"));
            factory.Setup(m => m.CreateRepository(It.Is<string>(c => c.Equals("bar")))).Throws(new InvalidOperationException("Can't touch this"));
            factory.Setup(m => m.CreateRepository(It.Is<string>(c => c.Equals("foo")))).Returns(repository);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, GetSourceProvider("bar", "foo"));
            updateCmd.Console = consoleInfo.Console;

            // Act
            updateCmd.SelfUpdate("c:\foo.exe", new Version("2.0"));

            // Assert
            Assert.AreEqual("NuGet.exe is up to date.", consoleInfo.WrittenLines[0]);
        }

        [TestMethod]
        public void SelfUpdateNewVersionDoesUpdatesExe() {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var repository = new MockPackageRepository();
            IPackage package = PackageUtility.CreatePackage("NuGet.CommandLine", "3.0", tools: new[] { "NuGet.exe" });
            repository.Add(package);
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(repository);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new MockUpdateCommand(factory.Object, GetSourceProvider());
            updateCmd.Console = consoleInfo.Console;

            // Act
            updateCmd.SelfUpdate(@"c:\NuGet.exe", new Version("2.0"));

            // Assert
            Assert.IsTrue(updateCmd.MovedFiles.ContainsKey(@"c:\NuGet.exe"));
            Assert.AreEqual(@"c:\NuGet.exe.old", updateCmd.MovedFiles[@"c:\NuGet.exe"]);
            Assert.IsTrue(updateCmd.UpdatedFiles.ContainsKey(@"c:\NuGet.exe"));
            Assert.AreEqual(@"tools\NuGet.exe", updateCmd.UpdatedFiles[@"c:\NuGet.exe"]);
        }

        private static ConsoleInfo GetConsoleInfo() {
            var lines = new List<string>();
            var console = new Mock<IConsole>();
            console.Setup(m => m.WriteLine(It.IsAny<string>())).Callback<string>(lines.Add);
            return new ConsoleInfo(console.Object, lines);
        }

        private static IPackageSourceProvider GetSourceProvider(params string[] sources) {
            if (!sources.Any()) {
                sources = new[] { "foo" };
            }
            var sourceProvider = new Mock<IPackageSourceProvider>();

            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources.Select(c => new PackageSource(c)));
            return sourceProvider.Object;
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

            public MockUpdateCommand(IPackageRepositoryFactory factory, IPackageSourceProvider provider)
                : base(factory, provider) {
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
