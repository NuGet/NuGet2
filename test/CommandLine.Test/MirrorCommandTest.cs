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
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Foo 1.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
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
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Foo 1.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandForPackageReferenceFileReportsZeroIfThereIsNoPackageToMirror()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\packages.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
</packages>".AsStream());
            var mirrorCommand = new TestMirrorCommand(@"x:\test\packages.config", fileSystem: fileSystem);

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 0 package(s).")
                });
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
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Foo 1.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
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
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Foo 1.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandLogsWarningsForFailingRepositoriesIfNoSourcesAreSpecified()
        {
            // Arrange
            var repositoryA = new MockPackageRepository();
            repositoryA.AddPackage(PackageUtility.CreatePackage("Foo"));
            var repositoryB = new Mock<IPackageRepository>();
            repositoryB.Setup(c => c.GetPackages()).Returns(GetPackagesWithException().AsQueryable());

            var sourceProvider = GetSourceProvider(new[] { new PackageSource("A"), new PackageSource("B") });
            var factory = new Mock<IPackageRepositoryFactory>();
            factory.Setup(c => c.CreateRepository("A")).Returns(repositoryA);
            factory.Setup(c => c.CreateRepository("B")).Returns(repositoryB.Object);
            var mirrorCommand = new TestMirrorCommand("Foo", factory.Object, sourceProvider);

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Warning, "Boom"),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Foo 1.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandThrowsIfConfigFileDoesNotExist()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand(@"x:\test\packages.config");

            // Act and Assert
            ExceptionAssert.Throws<FileNotFoundException>(mirrorCommand.ExecuteCommand, @"x:\test\packages.config not found.");
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
            ExceptionAssert.Throws<ArgumentException>(mirrorCommand.ExecuteCommand, "Version should be specified in mirroring.config or packages.config file instead.");
        }

        [Fact]
        public void MirrorCommandThrowsIfVersionAndMirroringConfigBothSpecified()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\mirroring.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
</packages>".AsStream());
            var mirrorCommand = new TestMirrorCommand(@"x:\test\mirroring.config", fileSystem: fileSystem) { Version = "1.0" };

            // Act and Assert
            ExceptionAssert.Throws<ArgumentException>(mirrorCommand.ExecuteCommand, "Version should be specified in mirroring.config or packages.config file instead.");
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
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Baz 0.7' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandUsesLocalCacheIfNoCacheIsFalse()
        {
            // Arrange

            var localCache = new Mock<IPackageRepository>(MockBehavior.Strict);
            localCache.Setup(c => c.GetPackages()).Returns(new[] { PackageUtility.CreatePackage("Gamma") }.AsQueryable()).Verifiable();
            var mirrorCommand = new TestMirrorCommand("Gamma", machineCacheRepository: localCache.Object)
            {
                NoCache = false,
            };

            mirrorCommand.Source.Add("Some Source name");
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertSinglePackage(mirrorCommand, "Gamma", "1.0");
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Gamma 1.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
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
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Baz 0.7' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
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
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Baz 0.8.1-alpha' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandMirrorsMirroringConfigWithVersion()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\mirroring.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
<package id=""Foo"" />
<package id=""Baz"" version=""0.4"" />
</packages>".AsStream());
            var mirrorCommand = new TestMirrorCommand(@"x:\test\mirroring.config", fileSystem: fileSystem);
            mirrorCommand.Source.Add("Some Source name");
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertTwoPackages(mirrorCommand, "Foo", "1.0", "Baz", "0.4");
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Foo 1.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Baz 0.4' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 2 package(s).")
                });
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
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Foo 1.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Baz 0.7' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 2 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandMirrorsDependenciesByDefaultWhenUsingConfigFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\mirroring.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
<package id=""PackageWithDependencies"" />
</packages>".AsStream());
            var mirrorCommand = new TestMirrorCommand(@"x:\test\mirroring.config", fileSystem: fileSystem);
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertTwoPackages(mirrorCommand, "ChildPackage", "3.0", "PackageWithDependencies", "2.0");
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Attempting to resolve dependency 'ChildPackage (> 2.0 && < 5.0)'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'ChildPackage 3.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'PackageWithDependencies 2.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 2 package(s).")
                });
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
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Foo 1.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'Baz 0.8.1-alpha' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 2 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandMirrorsDependenciesByDefault()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand("PackageWithDependencies")
            {
                Version = "2.0"
            };
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertTwoPackages(mirrorCommand, "ChildPackage", "3.0", "PackageWithDependencies", "2.0");
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Attempting to resolve dependency 'ChildPackage (> 2.0 && < 5.0)'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'ChildPackage 3.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'PackageWithDependencies 2.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 2 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandSupportsMultipleEntriesSamePackageAndDownloadsDependents()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(@"x:\test\mirroring.config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
<package id=""PackageWithDependencies"" version=""1.0"" />
<package id=""PackageWithDependencies"" />
</packages>".AsStream());
            var mirrorCommand = new TestMirrorCommand(@"x:\test\mirroring.config", fileSystem: fileSystem);
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Attempting to resolve dependency 'ChildPackage (≥ 1.0 && < 2.0)'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'ChildPackage 1.4' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'PackageWithDependencies 1.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Attempting to resolve dependency 'ChildPackage (> 2.0 && < 5.0)'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'ChildPackage 3.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'PackageWithDependencies 2.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 4 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandSkipsAlreadyInstalledDependents()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand("PackageWithDependencies")
                {
                    Version = "2.0"
                };
            mirrorCommand.Source.Add("Some other Source");
            mirrorCommand.DestinationRepository.AddPackage(PackageUtility.CreatePackage("ChildPackage", "3.0"));

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertTwoPackages(mirrorCommand, "ChildPackage", "3.0", "PackageWithDependencies", "2.0");
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Attempting to resolve dependency 'ChildPackage (> 2.0 && < 5.0)'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'PackageWithDependencies 2.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandSkipDependentsWhenDependentsModeIsSkip()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand("PackageWithDependencies")
            {
                Version = "2.0",
                DependenciesMode = MirrorDependenciesMode.Ignore
            };
            mirrorCommand.Source.Add("Some other Source");

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertSinglePackage(mirrorCommand, "PackageWithDependencies", "2.0");
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'PackageWithDependencies 2.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
        }

        [Fact]
        public void MirrorCommandFailWhenDependentsMissingAndDependentsModeIsFail()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand("PackageWithDependencies")
            {
                Version = "2.0",
                DependenciesMode = MirrorDependenciesMode.Fail
            };
            mirrorCommand.Source.Add("Some other Source");

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(mirrorCommand.ExecuteCommand, "Unable to resolve dependency 'ChildPackage (> 2.0 && < 5.0)'.");
        }

        [Fact]
        public void MirrorCommandSucceedsWhenDependentsPresentInTargetAndDependentsModeIsFail()
        {
            // Arrange
            var mirrorCommand = new TestMirrorCommand("PackageWithDependencies")
            {
                Version = "2.0",
                DependenciesMode = MirrorDependenciesMode.Fail
            };
            mirrorCommand.Source.Add("Some other Source");
            mirrorCommand.DestinationRepository.AddPackage(PackageUtility.CreatePackage("ChildPackage", "3.0"));

            // Act
            mirrorCommand.ExecuteCommand();

            // Assert
            AssertTwoPackages(mirrorCommand, "ChildPackage", "3.0", "PackageWithDependencies", "2.0");
            AssertOutputEquals(mirrorCommand, new[]
                {
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Attempting to resolve dependency 'ChildPackage (> 2.0 && < 5.0)'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Successfully mirrored 'PackageWithDependencies 2.0' to 'destinationurlpull'."),
                    new KeyValuePair<MessageLevel, string>(MessageLevel.Info, "Mirrored 1 package(s).")
                });
        }

        private static IPackageRepositoryFactory GetFactory()
        {
            var repositoryA = new MockPackageRepository { PackageUtility.CreatePackage("Foo"), PackageUtility.CreatePackage("Baz", "0.4"), PackageUtility.CreatePackage("Baz", "0.7") };

            var dependencySets1 = new[]
                {
                    new PackageDependencySet(null, new[]
                        {
                            new PackageDependency("ChildPackage", new VersionSpec
                                {
                                    MinVersion = new SemanticVersion("1.0"),
                                    MaxVersion = new SemanticVersion("2.0"),
                                    IsMinInclusive = true
                                })
                        })
                };

            var dependencySets2 = new[]
                {
                    new PackageDependencySet(null, new[]
                        {
                            new PackageDependency("ChildPackage", new VersionSpec
                                {
                                    MinVersion = new SemanticVersion("2.0"),
                                    MaxVersion = new SemanticVersion("5.0"),
                                    IsMinInclusive = false
                                })
                        })
                };

            var repositoryB = new MockPackageRepository
                {
                    PackageUtility.CreatePackage("Bar", "0.5"),
                    PackageUtility.CreatePackage("Baz", "0.8.1-alpha"),
                    PackageUtility.CreatePackageWithDependencySets("PackageWithDependencies", "1.0", dependencySets: dependencySets1),
                    PackageUtility.CreatePackageWithDependencySets("PackageWithDependencies", "2.0", dependencySets: dependencySets2),
                    PackageUtility.CreatePackage("ChildPackage", "1.4"),
                    PackageUtility.CreatePackage("ChildPackage", "3.0")
                };

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
            private List<KeyValuePair<MessageLevel, string>> _consoleOutput = new List<KeyValuePair<MessageLevel, string>>();

            public TestMirrorCommand(
                string packageId,
                IPackageRepositoryFactory factory = null,
                IPackageSourceProvider sourceProvider = null,
                IFileSystem fileSystem = null,
                IPackageRepository machineCacheRepository = null) : 
                base(machineCacheRepository ?? new MockPackageRepository())
            {
                SourceProvider = sourceProvider ?? GetSourceProvider();
                Settings = CreateSettings();
                RepositoryFactory = factory ?? GetFactory();

                Arguments.Add(packageId);
                Arguments.Add("destinationurlpull");
                Arguments.Add("destinationurlpush");
                _fileSystem = fileSystem ?? new MockFileSystem();
                _destinationRepository = new MockPackageRepository("destinationurlpull");

                var console = new Mock<IConsole>();
                console.Setup(c => c.Log(It.IsAny<MessageLevel>(), It.IsAny<string>(), It.IsAny<object[]>())).Callback((MessageLevel a, string b, object[] c) =>
                    _consoleOutput.Add(new KeyValuePair<MessageLevel, string>(a, string.Format(b, c))));
                Console = console.Object;
            }

            public void AssertOutputContains(int line, MessageLevel level, string messageText)
            {
                Assert.True(line < _consoleOutput.Count);
                Assert.Equal(level, _consoleOutput[line].Key);
                Assert.Equal(messageText, _consoleOutput[line].Value);
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

        private static void AssertOutputEquals(TestMirrorCommand mirrorCommand, IEnumerable<KeyValuePair<MessageLevel, string>> lines)
        {
            int line = 0;
            foreach (var kvp in lines)
            {
                mirrorCommand.AssertOutputContains(line, kvp.Key, kvp.Value);
                line++;
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
