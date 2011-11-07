using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace NuGet.Test.NuGetCommandLine
{
    public class FileHelperTest
    {
        [Fact]
        public void SameBinaryFileStreamShouldBeEqual()
        {
            // Arrange
            using (var file1 = new FileStream("./Moq.dll", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var file2 = new FileStream("./Moq.dll", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // Act
                var result = FileHelper.IsFilesEqual(file1, file2);
                // Assert
                Assert.True(result);
            }

        }

        [Fact]
        public void DifferentBinaryFileStreamsShouldNotBeEqual()
        {
            // Arrange
            using (var file1 = new FileStream("./Moq.dll", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var file2 = new FileStream("./Ninject.dll", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //Act
                var result = FileHelper.IsFilesEqual(file1, file2);
                // Assert
                Assert.False(result);
            }

        }

        [Fact]
        public void SameTextFileStreamShouldBeEqual()
        {
            // Arrange
            using (var file1 = new FileStream("./Moq.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var file2 = new FileStream("./Moq.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // Act
                var result = FileHelper.IsFilesEqual(file1, file2);
                // Assert
                Assert.True(result);
            }

        }

        [Fact]
        public void DifferentTextFileStreamsShouldNotBeEqual()
        {
            // Arrange
            using (var file1 = new FileStream("./Moq.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var file2 = new FileStream("./Ninject.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // Act
                var result = FileHelper.IsFilesEqual(file1, file2);
                // Assert
                Assert.False(result);
            }
            
        }

        [Fact]
        public void SameBinaryFilePathShouldBeEqual()
        {
            // Arrange

            // Act
            var result = FileHelper.IsFilesEqual("./Moq.dll", "./Moq.dll");
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DifferentBinaryFilePathsShouldNotBeEqual()
        {
            // Arrange
            
            // Act
            var result = FileHelper.IsFilesEqual("./Moq.dll", "./Ninject.dll");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SameTextFilePathsShouldBeEqual()
        {
            // Arrange

            // Act
            var result = FileHelper.IsFilesEqual("./Moq.xml", "./Moq.xml");
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DifferentTextFilePathsShouldNotBeEqual()
        {
            // Arrange
            
            // Act
            var result = FileHelper.IsFilesEqual("./Moq.xml", "./Ninject.xml");
            // Assert
            Assert.False(result);
        }



    }
}
