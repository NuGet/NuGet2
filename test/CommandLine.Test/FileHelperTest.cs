using System.IO;
using Xunit;

namespace NuGet.Test.NuGetCommandLine
{
    public class FileHelperTest
    {
        [Fact]
        public void SameBinaryFileStreamShouldBeEqual()
        {
            // Arrange
            using (var file1 = File.OpenRead("./Moq.dll"))
            using (var file2 = File.OpenRead("./Moq.dll"))
            {
                // Act
                var result = FileHelper.AreFilesEqual(file1, file2);
                // Assert
                Assert.True(result);
            }
        }

        [Fact]
        public void DifferentBinaryFileStreamsShouldNotBeEqual()
        {
            // Arrange
            using (var file1 = File.OpenRead("./Moq.dll"))
            using (var file2 = File.OpenRead("./Ninject.dll"))
            {
                // Act
                var result = FileHelper.AreFilesEqual(file1, file2);

                // Assert
                Assert.False(result);
            }
        }

        [Fact]
        public void SameTextFileStreamShouldBeEqual()
        {
            // Arrange
            using (var file1 = File.OpenRead("./Moq.xml"))
            using (var file2 = File.OpenRead("./Moq.xml"))
            {
                // Act
                var result = FileHelper.AreFilesEqual(file1, file2);
                // Assert
                Assert.True(result);
            }
        }

        [Fact]
        public void DifferentTextFileStreamsShouldNotBeEqual()
        {
            // Arrange
            using (var file1 = File.OpenRead("./Moq.xml"))
            using (var file2 = File.OpenRead("./Ninject.xml"))
            {
                // Act
                var result = FileHelper.AreFilesEqual(file1, file2);

                // Assert
                Assert.False(result);
            }
        }

        [Fact]
        public void SameBinaryFilePathShouldBeEqual()
        {
            // Act
            var result = FileHelper.AreFilesEqual("./Moq.dll", "./Moq.dll");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DifferentBinaryFilePathsShouldNotBeEqual()
        {
            // Act
            var result = FileHelper.AreFilesEqual("./Moq.dll", "./Ninject.dll");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SameTextFilePathsShouldBeEqual()
        {
            // Act
            var result = FileHelper.AreFilesEqual("./Moq.xml", "./Moq.xml");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DifferentTextFilePathsShouldNotBeEqual()
        {
            // Act
            var result = FileHelper.AreFilesEqual("./Moq.xml", "./Ninject.xml");

            // Assert
            Assert.False(result);
        }
    }
}