using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NuGet.Common;
using NuGet.ServerExtensions;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test.ServerExtensions
{
    public class MirrorCommandTest
    {
        [Fact]
        public void MirrorCommandMirrorsPackageIfArgumentIsNotPackageReferenceFile()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand("Foo");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertSinglePackage(mirrorCommand, "Foo", "1.0");
        }

        [Fact]
        public void MirrorCommandUsesMirrorOperation()
        {
            // Arrange
            var mockRepo = new MockPackageRepository() { PackageUtility.CreatePackage("Foo") };
            var mockFactory = new Mock<IPackageRepositoryFactory>();
            mockFactory.Setup(r => r.CreateRepository(It.IsAny<string>())).Returns(mockRepo);
            var mirrorCommand = new TestMirrorCommand("Foo", mockFactory.Object);

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            Assert.Equal(RepositoryOperationNames.Mirror, mockRepo.LastOperation);
        }

        [Fact]
        public void MirrorCommandForPackageReferenceFileWarnsIfThereIsNoPackageToMirror()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
</packages>".AsStream());

            MessageLevel? level = null;
            string message = null;
            var console = new Mock<IConsole>();
            console.Setup(c => c.Log(It.IsAny<MessageLevel>(), It.IsAny<string>(), It.IsAny<object[]>())).Callback((MessageLevel a, string b, object[] c) =>
            {
                if (a == MessageLevel.Warning)
                {
                    level = a;
                    message = b;
                }
            });

            var mirrorCommand = new TestMirrorCommand(@"x:\test\packages.config", fileSystem: fileSystem) { Console = console.Object };

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            Assert.Equal(level, MessageLevel.Warning);
            Assert.Equal(message, "No packages found to check for mirroring.");
        }

        [Fact]
        public void MirrorCommandMirrorsPackageSuccessfullyIfCacheRepositoryIsNotSet()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand("Foo");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertSinglePackage(mirrorCommand, "Foo", "1.0");
        }

        [Fact]
        public void MirrorCommandResolvesSourceName()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand("Foo");

            mirrorCommand.Source.Add("Some source name");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertSinglePackage(mirrorCommand, "Foo", "1.0");
        }

        [Fact]
        public void MirrorCommandLogsWarningsForFailingRepositoriesIfNoSourcesAreSpecified()
        {
            // Arrange
            MessageLevel? level = null;
            string message = null;
            var repositoryA = new MockPackageRepository();
            repositoryA.AddPackage(PackageUtility.CreatePackage("Foo"));
            var repositoryB = new Mock<IPackageRepository>();
            repositoryB.Setup(c => c.GetPackages()).Returns(GetPackagesWithException().AsQueryable());
            var console = new Mock<IConsole>();
            console.Setup(c => c.Log(It.IsAny<MessageLevel>(), It.IsAny<string>(), It.IsAny<object[]>())).Callback((MessageLevel a, string b, object[] c) =>
            {
                if (a == MessageLevel.Warning)
                {
                    level = a;
                    message = b;
                }
            });

            var sourceProvider = GetSourceProvider(new[] { new PackageSource("A"), new PackageSource("B") });
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository("A")).Returns(repositoryA);
            factory.Setup(c => c.CreateRepository("B")).Returns(repositoryB.Object);
            var mirrorCommand = new TestMirrorCommand("Foo", factory.Object, sourceProvider)
            {
                Console = console.Object
            };

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            Assert.Equal("Boom", message);
            Assert.Equal(MessageLevel.Warning, level.Value);
        }

        [Fact]
        public void MirrorCommandThrowsIfConfigFileDoesNotExist()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand(@"x:\test\packages.config");

            // Act and Assert
            ExceptionAssert.Throws<FileNotFoundException>(() => mirrorCommand.ExecuteCommand(), @"x:\test\packages.config not found.");
        }

        [Fact]
        public void MirrorCommandThrowsIfVersionAndPackageConfigBothSpecified()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
</packages>".AsStream());
            var mirrorCommand = new TestMirrorCommand(@"x:\test\packages.config", fileSystem: fileSystem) { Version = "1.0" };

            // Act and Assert
            ExceptionAssert.Throws<ArgumentException>(() => mirrorCommand.ExecuteCommand(), "Version should be specified in packages.config file instead.");
        }

        [Fact]
        public void MirrorCommandUsesMultipleSourcesIfSpecified()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand("Baz");

            mirrorCommand.Source.Add("Some Source name");
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertSinglePackage(mirrorCommand, "Baz", "0.7");
        }

        [Fact]
        public void MirrorCommandUsesLocalCacheIfNoCacheIsFalse()
        {
            // Arrange
            var localCache = new Mock<IPackageRepository>(MockBehavior.Strict);
            localCache.Setup(c => c.GetPackages()).Returns(new[] { PackageUtility.CreatePackage("Gamma") }.AsQueryable()).Verifiable();
            var mirrorCommand = new TestMirrorCommand("Gamma", machineCacheRepository: localCache.Object)
            {
                NoCache = false
            };

            mirrorCommand.Source.Add("Some Source name");
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert            
            AssertSinglePackage(mirrorCommand, "Gamma", "1.0");
            localCache.Verify();
        }

        [Fact]
        public void MirrorCommandDoesNotUseLocalCacheIfNoCacheIsTrue()
        {
            // Arrange
            var localCache = new Mock<IPackageRepository>(MockBehavior.Strict);
            var mirrorCommand = new TestMirrorCommand("Baz", machineCacheRepository: localCache.Object)
            {
                NoCache = true
            };

            mirrorCommand.Source.Add("Some Source name");
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert            
            AssertSinglePackage(mirrorCommand, "Baz", "0.7");
            localCache.Verify(c => c.GetPackages(), Times.Never());
        }

        [Fact]
        public void MirrorCommandMirrorsPrereleasePackageIfFlagIsSpecified()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand("Baz") { Prerelease = true };

            mirrorCommand.Source.Add("Some Source name");
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertSinglePackage(mirrorCommand, "Baz", "0.8.1-alpha");
        }

        [Fact]
        public void MirrorCommandMirrorsPackagesConfigWithVersion()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
<package id=""Foo"" />
<package id=""Baz"" version=""0.4"" />
</packages>".AsStream());
            var mirrorCommand = new TestMirrorCommand(@"x:\test\packages.config", fileSystem: fileSystem);
            mirrorCommand.Source.Add("Some Source name");
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertTwoPackages(mirrorCommand, "Foo", "1.0", "Baz", "0.4");
        }

        [Fact]
        public void MirrorCommandMirrorsPackagesConfigWithoutVersion()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
<package id=""Foo"" />
<package id=""Baz"" />
</packages>".AsStream());
            var mirrorCommand = new TestMirrorCommand(@"x:\test\packages.config", fileSystem: fileSystem);
            mirrorCommand.Source.Add("Some Source name");
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertTwoPackages(mirrorCommand, "Foo", "1.0", "Baz", "0.7");
        }

        [Fact]
        public void MirrorCommandMirrorsPackagesConfigWithoutVersionPrerelease()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
<package id=""Foo"" />
<package id=""Baz"" />
</packages>".AsStream());
            var mirrorCommand = new TestMirrorCommand(@"x:\test\packages.config", fileSystem: fileSystem) { Prerelease = true };

            mirrorCommand.Source.Add("Some Source name");
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertTwoPackages(mirrorCommand, "Foo", "1.0", "Baz", "0.8.1-alpha");
        }

        private static IPackageRepositoryFactory GetFactory()
        {
            var repositoryA = new MockPackageRepository { PackageUtility.CreatePackage("Foo"), PackageUtility.CreatePackage("Baz", "0.4"), PackageUtility.CreatePackage("Baz", "0.7") };
            var repositoryB = new MockPackageRepository { PackageUtility.CreatePackage("Bar", "0.5"), PackageUtility.CreatePackage("Baz", "0.8.1-alpha") };

            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Some source")))).Returns(repositoryA);
            factory.Setup(c => c.CreateRepository(It.Is<string>(f => f.Equals("Some other source")))).Returns(repositoryB);

            return factory.Object;
        }

        private static IPackageSourceProvider GetSourceProvider(IEnumerable<PackageSource> sources = null)
        {
            var sourceProvider = new Mock<IPackageSourceProvider>();
            sources = sources ?? new[] { new PackageSource("Some source", "Some source name"), new PackageSource("Some other source") };
            sourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);

            return sourceProvider.Object;
        }

        private static IEnumerable<IPackage> GetPackagesWithException()
        {
            yield return PackageUtility.CreatePackage("Baz");
            throw new InvalidOperationException("Boom");
        }

        private sealed class TestMirrorCommand : MirrorCommand
        {
            private readonly IFileSystem _fileSystem;
            private readonly IPackageRepository _destinationRepository;
            private readonly IPackageRepository _machineCacheRepository;

            public TestMirrorCommand(
                string packageId,
                IPackageRepositoryFactory factory = null,
                IPackageSourceProvider sourceProvider = null,
                IFileSystem fileSystem = null,
                IPackageRepository machineCacheRepository = null
                )
                : base(
                    sourceProvider ?? GetSourceProvider(),
                    CreateSettings(),
                    factory ?? GetFactory()
                )
            {
                Arguments.Add(packageId);
                Arguments.Add("destinationurlpull");
                Arguments.Add("destinationurlpush");
                _fileSystem = fileSystem ?? new MockFileSystem();
                _destinationRepository = new MockPackageRepository("destinationurlpull");
                _machineCacheRepository = machineCacheRepository ?? new MockPackageRepository();
            }

            protected override IPackageRepository CacheRepository
            {
                get { return _machineCacheRepository; }
            }

            private static ISettings CreateSettings()
            {
                var settings = new Mock<ISettings>();
                return settings.Object;
            }

            protected override IFileSystem CreateFileSystem()
            {
                return _fileSystem;
            }

            protected override IPackageRepository GetTargetRepository(string pullUrl, string pushUrl)
            {
                return _destinationRepository;
            }

            public IPackageRepository DestinationRepository
            {
                get { return _destinationRepository; }
            }
        }

        private static void AssertSinglePackage(TestMirrorCommand mirrorCommand, string id, string version)
        {
            var pack = mirrorCommand.DestinationRepository.GetPackages().Single();
            Assert.Equal(id, pack.Id);
            Assert.Equal(version, pack.Version.ToString());
        }

        private static void AssertTwoPackages(TestMirrorCommand mirrorCommand, string id, string version,
            string id2, string version2)
        {
            var pack = mirrorCommand.DestinationRepository.GetPackages();
            Assert.Equal(2, pack.Count());
            var first = pack.First();
            var last = pack.Last();
            Assert.Equal(id, first.Id);
            Assert.Equal(version, first.Version.ToString());
            Assert.Equal(id2, last.Id);
            Assert.Equal(version2, last.Version.ToString());
        }
    }
}
