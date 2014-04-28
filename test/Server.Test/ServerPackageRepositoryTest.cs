using Moq;
using NuGet.Server.Infrastructure;
using NuGet.Test.Mocks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Xunit;

namespace NuGet.Test.Server.Infrastructure
{
    public class ServerPackageRepositoryTest
    {

        private Dictionary<string, MemoryStream> _packageStreams;

        [Fact]
        public void ServerPackageRepositoryRemovePackage()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };

            _packageStreams = new Dictionary<string, MemoryStream>();
            AddPackage(mockProjectSystem, "test", "1.11");
            AddPackage(mockProjectSystem, "test", "1.9");
            AddPackage(mockProjectSystem, "test", "2.0-alpha");

            var serverRepository = new ServerPackageRepository(new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object);
            serverRepository.HashProvider = GetHashProvider();

            var package = CreatePackage("test", "1.11");
            var package2 = CreatePackage("test", "2.0-alpha");

            // call to cache the first time
            var packages = serverRepository.GetPackagesWithDerivedData();

            // Act
            serverRepository.RemovePackage(package);
            serverRepository.RemovePackage(package2);
            packages = serverRepository.GetPackagesWithDerivedData();

            // Assert
            Assert.Equal(1, packages.Count());
            Assert.Equal(1, packages.Where(p => p.IsLatestVersion).Count());
            Assert.Equal("1.9", packages.Where(p => p.IsLatestVersion).First().Version);

            Assert.Equal(1, packages.Where(p => p.IsAbsoluteLatestVersion).Count());
            Assert.Equal("1.9", packages.Where(p => p.IsAbsoluteLatestVersion).First().Version);
        }

        [Fact]
        public void ServerPackageRepositorySearch()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };

            _packageStreams = new Dictionary<string, MemoryStream>();
            AddPackage(mockProjectSystem, "test", "1.0");
            AddPackage(mockProjectSystem, "test2", "1.0");
            AddPackage(mockProjectSystem, "test3", "1.0-alpha");
            AddPackage(mockProjectSystem, "test4", "2.0");

            var serverRepository = new ServerPackageRepository(new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object);
            serverRepository.HashProvider = GetHashProvider();

            // Act
            var valid = serverRepository.Search("test3", true);
            var invalid = serverRepository.Search("test3", false);

            // Assert
            Assert.Equal("test3", valid.First().Id);
            Assert.Equal(0, invalid.Count());
        }

        [Fact]
        public void ServerPackageRepositoryFindPackageById()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };

            _packageStreams = new Dictionary<string, MemoryStream>();
            AddPackage(mockProjectSystem, "test", "1.0");
            AddPackage(mockProjectSystem, "test2", "1.0");
            AddPackage(mockProjectSystem, "test3", "1.0-alpha");
            AddPackage(mockProjectSystem, "test4", "2.0");

            var serverRepository = new ServerPackageRepository(new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object);
            serverRepository.HashProvider = GetHashProvider();

            // Act
            var valid = serverRepository.FindPackagesById("test");
            var invalid = serverRepository.FindPackagesById("bad");

            // Assert
            Assert.Equal("test", valid.First().Id);
            Assert.Equal(0, invalid.Count());
        }

        [Fact]
        public void ServerPackageRepositoryFindPackage()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };

            _packageStreams = new Dictionary<string, MemoryStream>();
            AddPackage(mockProjectSystem, "test", "1.0");
            AddPackage(mockProjectSystem, "test2", "1.0");
            AddPackage(mockProjectSystem, "test3", "1.0-alpha");
            AddPackage(mockProjectSystem, "test4", "2.0");

            var serverRepository = new ServerPackageRepository(new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object);
            serverRepository.HashProvider = GetHashProvider();

            // Act
            var valid = serverRepository.FindPackage("test", new SemanticVersion("1.0"));
            var invalid = serverRepository.FindPackage("bad", new SemanticVersion("1.0"));

            // Assert
            Assert.Equal("test", valid.Id);
            Assert.Null(invalid);
        }

        [Fact]
        public void ServerPackageRepositoryMultipleIds()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };

            _packageStreams = new Dictionary<string, MemoryStream>();
            AddPackage(mockProjectSystem, "test", "0.9");
            AddPackage(mockProjectSystem, "test", "1.0");
            AddPackage(mockProjectSystem, "test2", "1.0");
            AddPackage(mockProjectSystem, "test3", "1.0-alpha");
            AddPackage(mockProjectSystem, "test4", "2.0");

            var serverRepository = new ServerPackageRepository(new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object);
            serverRepository.HashProvider = GetHashProvider();

            // Act
            var packages = serverRepository.GetPackagesWithDerivedData();

            // Assert
            Assert.Equal(4, packages.Where(p => p.IsAbsoluteLatestVersion).Count());
            Assert.Equal(3, packages.Where(p => p.IsLatestVersion).Count());
            Assert.Equal(1, packages.Where(p => !p.IsAbsoluteLatestVersion).Count());
            Assert.Equal(2, packages.Where(p => !p.IsLatestVersion).Count());
        }

        [Fact]
        public void ServerPackageRepositoryIsAbsoluteLatest()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };

            _packageStreams = new Dictionary<string, MemoryStream>();
            AddPackage(mockProjectSystem, "test", "2.0-alpha");
            AddPackage(mockProjectSystem, "test", "2.1-alpha");
            AddPackage(mockProjectSystem, "test", "2.2-beta");
            AddPackage(mockProjectSystem, "test", "2.3");

            var serverRepository = new ServerPackageRepository(new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object);
            serverRepository.HashProvider = GetHashProvider();

            // Act
            var packages = serverRepository.GetPackagesWithDerivedData();

            // Assert
            Assert.Equal(1, packages.Where(p => p.IsAbsoluteLatestVersion).Count());
            Assert.Equal("2.3", packages.Where(p => p.IsAbsoluteLatestVersion).First().Version);
        }

        [Fact]
        public void ServerPackageRepositoryIsLatestOnlyPreRel()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };

            _packageStreams = new Dictionary<string, MemoryStream>();
            AddPackage(mockProjectSystem, "test", "2.0-alpha");
            AddPackage(mockProjectSystem, "test", "2.1-alpha");
            AddPackage(mockProjectSystem, "test", "2.2-beta");

            var serverRepository = new ServerPackageRepository(new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object);
            serverRepository.HashProvider = GetHashProvider();

            // Act
            var packages = serverRepository.GetPackagesWithDerivedData();

            // Assert
            Assert.Equal(0, packages.Where(p => p.IsLatestVersion).Count());
        }

        [Fact]
        public void ServerPackageRepositoryIsLatest()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };

            _packageStreams = new Dictionary<string, MemoryStream>();
            AddPackage(mockProjectSystem, "test", "1.11");
            AddPackage(mockProjectSystem, "test", "1.9");
            AddPackage(mockProjectSystem, "test", "2.0-alpha");

            var serverRepository = new ServerPackageRepository(new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object);
            serverRepository.HashProvider = GetHashProvider();

            // Act
            var packages = serverRepository.GetPackagesWithDerivedData();

            // Assert
            Assert.Equal(1, packages.Where(p => p.IsLatestVersion).Count());
            Assert.Equal("1.11", packages.Where(p => p.IsLatestVersion).First().Version);
        }

        [Fact]
        public void ServerPackageRepositoryReadsDerivedData()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            var package = new PackageBuilder() { Id = "Test", Version = new SemanticVersion("1.0"), Description = "Description" };
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(m => m.Path).Returns("foo");
            mockFile.Setup(m => m.GetStream()).Returns(new MemoryStream());
            package.Files.Add(mockFile.Object);
            package.Authors.Add("Test Author");
            var memoryStream = new MemoryStream();
            package.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            mockProjectSystem.Object.AddFile("foo.nupkg");
            mockProjectSystem.Setup(c => c.OpenFile(It.IsAny<string>())).Returns(() => new MemoryStream(memoryStream.ToArray()));
            var serverRepository = new ServerPackageRepository(new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object);
            serverRepository.HashProvider = GetHashProvider();

            // Act
            var packages = serverRepository.GetPackagesWithDerivedData();

            // Assert
            byte[] data = memoryStream.ToArray();
            Assert.Equal(data.Length, packages.Single().PackageSize);
            Assert.Equal(data.Select(Invert).ToArray(), Convert.FromBase64String(packages.Single().PackageHash).ToArray());

            //CollectionAssert.AreEquivalent(data.Select(Invert).ToArray(), Convert.FromBase64String(packages.Single().PackageHash));
            Assert.Equal(data.Length, packages.Single().PackageSize);
        }

        [Fact]
        public void ServerPackageRepositoryEmptyRepo()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };

            _packageStreams = new Dictionary<string, MemoryStream>();

            var serverRepository = new ServerPackageRepository(new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object);
            serverRepository.HashProvider = GetHashProvider();

            var package = CreatePackage("test", "1.0");

            // Act
            var findPackage = serverRepository.FindPackage("test", new SemanticVersion("1.0"));
            var findPackagesById = serverRepository.FindPackagesById("test");
            var getMetadataPackage = serverRepository.GetMetadataPackage(package);
            var getPackages = serverRepository.GetPackages().ToList();
            var getPackagesWithDerivedData = serverRepository.GetPackagesWithDerivedData().ToList();
            var getUpdates = serverRepository.GetUpdates(Enumerable.Empty<IPackageName>(), true, true, Enumerable.Empty<FrameworkName>(), Enumerable.Empty<IVersionSpec>());
            var search = serverRepository.Search("test", true).ToList();
            var source = serverRepository.Source;

            // Assert
            Assert.Null(findPackage);
            Assert.Empty(findPackagesById);
            Assert.Null(getMetadataPackage);
            Assert.Empty(getPackages);
            Assert.Null(getMetadataPackage);
            Assert.Empty(getPackagesWithDerivedData);
            Assert.Empty(getUpdates);
            Assert.Empty(search);
            Assert.NotEmpty(source);
        }

        private static IPackage CreatePackage(string id, string version)
        {
            var package = new Mock<IPackage>();
            package.Setup(p => p.Id).Returns(id);
            package.Setup(p => p.Version).Returns(new SemanticVersion(version));
            package.Setup(p => p.IsLatestVersion).Returns(true);
            package.Setup(p => p.Listed).Returns(true);

            return package.Object;
        }

        private void AddPackage(Mock<MockProjectSystem> mockProjectSystem, string id, string version)
        {
            string name = String.Format(CultureInfo.InvariantCulture, "{0}.{1}.nupkg", id, version);

            var package = new PackageBuilder() { Id = id, Version = new SemanticVersion(version), Description = "Description" };
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(m => m.Path).Returns(name);
            mockFile.Setup(m => m.GetStream()).Returns(new MemoryStream());
            package.Files.Add(mockFile.Object);
            package.Authors.Add("Test Author");
            var memoryStream = new MemoryStream();
            package.Save(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            _packageStreams.Add(name, memoryStream);

            mockProjectSystem.Object.AddFile(name);

            mockProjectSystem.Setup(c => c.OpenFile(It.IsAny<string>())).Returns<string>((s) => new MemoryStream(_packageStreams[s].ToArray()));
        }

        private static IHashProvider GetHashProvider()
        {
            var hashProvider = new Mock<IHashProvider>();
            hashProvider.Setup(c => c.CalculateHash(It.IsAny<byte[]>())).Returns((byte[] value) => value.Select(Invert).ToArray());
            hashProvider.Setup(c => c.CalculateHash(It.IsAny<Stream>())).Returns((Stream value) => value.ReadAllBytes().Select(Invert).ToArray());

            return hashProvider.Object;
        }

        private static byte Invert(byte value)
        {
            return (byte)~value;
        }
    }
}
