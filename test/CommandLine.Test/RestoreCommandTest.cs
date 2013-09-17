using System;
using System.Collections.Generic;
using Moq;
using NuGet.Commands;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{
    public class RestoreCommandTest
    {
        /// <summary>
        /// Tests that the restore command will pick the solution file if both a solution file and
        /// a packages.config file exists.
        /// </summary>
        [Fact]
        public void RestoreCommandPreferSolutionFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("a.sln");
            fileSystem.AddFile("packages.config");

            var restoreCommand = new RestoreCommand()
            {
                FileSystem = fileSystem
            };

            // Act
            restoreCommand.DetermineRestoreMode();

            // Assert
            Assert.True(restoreCommand.RestoringForSolution);
            Assert.Equal(fileSystem.GetFullPath("a.sln"), restoreCommand.SolutionFileFullPath);
        }

        /// <summary>
        /// Tests that the restore command throws exception if there are multiple solution files.
        /// </summary>
        [Fact]
        public void RestoreCommandMultipleSolutionFiles()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("a.sln");
            fileSystem.AddFile("b.sln");

            var restoreCommand = new RestoreCommand()
            {
                FileSystem = fileSystem
            };

            // Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => restoreCommand.DetermineRestoreMode(),
                "This folder contains more than one solution file.");
        }

        /// <summary>
        /// Tests that the restore command throws exception if there are no solution 
        /// files nor packages.config file.
        /// </summary>
        [Fact]
        public void RestoreCommandNoFilesToUse()
        {
            // Arrange
            var fileSystem = new MockFileSystem();

            var restoreCommand = new RestoreCommand()
            {
                FileSystem = fileSystem
            };

            // Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => restoreCommand.DetermineRestoreMode(),
                "This folder contains no solution files, nor packages.config files.");
        }

        /// <summary>
        /// Tests that the restore command will pick the packages.config if a packages.config 
        /// file exists.
        /// </summary>
        [Fact]
        public void RestoreCommandPickPackagesConfigFile()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("packages.config");

            var restoreCommand = new RestoreCommand()
            {
                FileSystem = fileSystem
            };

            // Act
            restoreCommand.DetermineRestoreMode();

            // Assert
            Assert.False(restoreCommand.RestoringForSolution);
            Assert.Equal(fileSystem.GetFullPath("packages.config"), restoreCommand.PackagesConfigFileFullPath);
        }
    }
}
