using System.Collections.Generic;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test.NuGetCommandLine.Commands
{

    public class UpdateCommandTest
    {
        [Fact]
        public void SelfUpdateNoCommandLinePackageOnServerThrows()
        {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var sourceProvider = new Mock<IPackageSourceProvider>();
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(new MockPackageRepository());

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var updateCmd = new UpdateCommand(factory.Object, sourceProvider.Object);
            updateCmd.Console = consoleInfo.Console;

            // Act
            ExceptionAssert.Throws<CommandLineException>(() => updateCmd.SelfUpdate("c:\foo.exe", new SemanticVersion("2.0")), "Unable to find 'NuGet.CommandLine' package.");
        }

        [Fact]
        public void SelfUpdateOlderVersionDoesNotUpdate()
        {
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
            updateCmd.SelfUpdate("c:\foo.exe", new SemanticVersion("2.0"));

            // Assert
            Assert.Equal("NuGet.exe is up to date.", consoleInfo.WrittenLines[0]);
        }

        [Fact]
        public void SelfUpdateNoNuGetExeInNuGetExePackageThrows()
        {
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
            ExceptionAssert.Throws<CommandLineException>(() => updateCmd.SelfUpdate("c:\foo.exe", new SemanticVersion("2.0")), "Invalid NuGet.CommandLine package. Unable to locate NuGet.exe within the package.");
        }

        [Fact]
        public void SelfUpdateNewVersionDoesUpdatesExe()
        {
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
            updateCmd.SelfUpdate(@"c:\NuGet.exe", new SemanticVersion("2.0"));

            // Assert
            Assert.True(updateCmd.MovedFiles.ContainsKey(@"c:\NuGet.exe"));
            Assert.Equal(@"c:\NuGet.exe.old", updateCmd.MovedFiles[@"c:\NuGet.exe"]);
            Assert.True(updateCmd.UpdatedFiles.ContainsKey(@"c:\NuGet.exe"));
            Assert.Equal(@"tools\NuGet.exe", updateCmd.UpdatedFiles[@"c:\NuGet.exe"]);
        }


        private ConsoleInfo GetConsoleInfo()
        {
            var lines = new List<string>();
            var console = new Mock<IConsole>();
            console.Setup(m => m.WriteLine(It.IsAny<string>())).Callback<string>(lines.Add);
            console.Setup(m => m.WriteWarning(It.IsAny<string>())).Callback<string>(lines.Add);
            return new ConsoleInfo(console.Object, lines);
        }

        // Using Tuple.ItemN is makes the code harder to read
        private class ConsoleInfo
        {
            public ConsoleInfo(IConsole console, IList<string> lines)
            {
                Console = console;
                WrittenLines = lines;
            }
            public IConsole Console { get; private set; }
            public IList<string> WrittenLines { get; private set; }
        }

        private class MockUpdateCommand : UpdateCommand
        {
            public Dictionary<string, string> MovedFiles = new Dictionary<string, string>();
            public Dictionary<string, string> UpdatedFiles = new Dictionary<string, string>();

            public MockUpdateCommand(IPackageRepositoryFactory factory, IPackageSourceProvider sourceProvider)
                : base(factory, sourceProvider)
            {
            }

            protected override void Move(string oldPath, string newPath)
            {
                MovedFiles[oldPath] = newPath;
            }

            protected override void UpdateFile(string exePath, IPackageFile file)
            {
                UpdatedFiles[exePath] = file.Path;
            }
        }
    }
}
