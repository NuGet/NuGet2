using System;
using System.Collections.Generic;
using System.Reflection;
using Moq;
using NuGet.Common;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class SelfUpdaterTests
    {
        [Fact]
        public void SelfUpdateOlderVersionDoesNotUpdate()
        {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var repository = new MockPackageRepository();
            repository.Add(PackageUtility.CreatePackage("NuGet.CommandLine", "1.0"));
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(repository);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var selfUpdater = new SelfUpdater(factory.Object)
            {
                Console = consoleInfo.Console
            };

            // Act
            selfUpdater.SelfUpdate("c:\foo.exe", new SemanticVersion("2.0"));

            // Assert
            Assert.Equal("NuGet.exe is up to date.", consoleInfo.WrittenLines[0]);
        }

        [Fact]
        public void SelfUpdateNoNuGetExeInNuGetExePackageThrows()
        {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var repository = new MockPackageRepository();
            repository.Add(PackageUtility.CreatePackage("NuGet.CommandLine", "3.0"));
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(repository);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var selfUpdater = new SelfUpdater(factory.Object)
                            {
                                Console = consoleInfo.Console
                            };

            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => selfUpdater.SelfUpdate("c:\foo.exe", new SemanticVersion("2.0")), 
                "Invalid NuGet.CommandLine package. Unable to locate NuGet.exe within the package.");
        }

        [Fact]
        public void SelfUpdateNewVersionUpdatesExe()
        {
            // Arrange
            var factory = new Mock<IPackageRepositoryFactory>();
            var repository = new MockPackageRepository();
            IPackage package = PackageUtility.CreatePackage("NuGet.CommandLine", "3.0", tools: new[] { "NuGet.exe" });
            repository.Add(package);
            factory.Setup(m => m.CreateRepository(It.IsAny<string>())).Returns(repository);

            ConsoleInfo consoleInfo = GetConsoleInfo();
            var selfUpdater = new MockSelfUpdater(factory.Object);
            selfUpdater.Console = consoleInfo.Console;

            // Act
            selfUpdater.SelfUpdate(@"c:\NuGet.exe", new SemanticVersion("2.0"));

            // Assert
            Assert.True(selfUpdater.MovedFiles.ContainsKey(@"c:\NuGet.exe"));
            Assert.Equal(@"c:\NuGet.exe.old", selfUpdater.MovedFiles[@"c:\NuGet.exe"]);
            Assert.True(selfUpdater.UpdatedFiles.ContainsKey(@"c:\NuGet.exe"));
            Assert.Equal(@"tools\NuGet.exe", selfUpdater.UpdatedFiles[@"c:\NuGet.exe"]);
        }

        [Fact]
        public void NuGetExeAssemblyHasAssemblyInformationalVersion()
        {
            // Arrange
            var assembly = typeof(SelfUpdater).Assembly;

            // Act
            var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            SemanticVersion semanticVersion;
            var result = SemanticVersion.TryParseStrict(infoVersion.InformationalVersion, out semanticVersion);

            // Assert
            Assert.NotNull(infoVersion.InformationalVersion);
            Assert.True(result);
        }

        [Fact]
        public void GetNuGetExeVersionReturnsAssemblyInformationalVersionFromProvider()
        {
            // Arrange
            var assembly = new Mock<ICustomAttributeProvider>(MockBehavior.Strict);
            assembly.Setup(s => s.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))
                    .Returns(new[] { new AssemblyInformationalVersionAttribute("1.2.3") });

            // Act
            var version = SelfUpdater.GetNuGetVersion(assembly.Object);

            // Assert
            Assert.Equal("1.2.3", version.ToString());
        }

        [Fact]
        public void GetNuGetExeVersionReturnsNullIfGetCustomAttributesThrows()
        {
            // Arrange
            var assembly = new Mock<ICustomAttributeProvider>(MockBehavior.Strict);
            assembly.Setup(s => s.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))
                    .Throws(new Exception());

            // Act
            var version = SelfUpdater.GetNuGetVersion(assembly.Object);

            // Assert
            Assert.Null(version);
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

        private class MockSelfUpdater : SelfUpdater
        {
            public Dictionary<string, string> MovedFiles = new Dictionary<string, string>();
            public Dictionary<string, string> UpdatedFiles = new Dictionary<string, string>();

            public MockSelfUpdater(IPackageRepositoryFactory factory)
                : base(factory)
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
