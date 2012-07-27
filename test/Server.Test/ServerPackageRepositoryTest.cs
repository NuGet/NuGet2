using System;
using System.IO;
using System.Linq;
using Moq;
using NuGet.Server.Infrastructure;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test.Server.Infrastructure
{
    public class ServerPackageRepositoryTest
    {
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
