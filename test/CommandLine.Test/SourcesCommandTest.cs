using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NuGet.Commands;
using Xunit;
using Moq;
using NuGet.Common;

namespace NuGet.Test.NuGetCommandLine.Commands {

    public class SourcesCommandTest {
        [Fact]
        public void EnableCommandWithNoNameSetThrows() {
            // Arrange
            var command = CreateCommand();
            command.Arguments.Add("Enable");

            // Act & Assert
            Exception exception = Assert.Throws<CommandLineException>(new Assert.ThrowsDelegate(command.ExecuteCommand));
            Assert.NotNull(exception);
            Assert.Equal("The name specified cannot be empty. Please provide a valid name.", exception.Message);
        }

        [Fact]
        public void DisableCommandWithNoNameSetThrows() {
            // Arrange
            var command = CreateCommand();
            command.Arguments.Add("Disable");

            // Act & Assert
            Exception exception = Assert.Throws<CommandLineException>(new Assert.ThrowsDelegate(command.ExecuteCommand));
            Assert.NotNull(exception);
            Assert.Equal("The name specified cannot be empty. Please provide a valid name.", exception.Message);
        }

        [Fact]
        public void EnableCommandThrowsWhenPackageSourceNameIsNotFound() {
            // Arrange
            var command = CreateCommand();
            command.Arguments.Add("Enable");
            command.Name = "abc";

            // Act & Assert
            Exception exception = Assert.Throws<CommandLineException>(new Assert.ThrowsDelegate(command.ExecuteCommand));
            Assert.NotNull(exception);
            Assert.Equal("Unable to find any package source(s) matching name: abc.", exception.Message);
        }

        [Fact]
        public void DisableCommandThrowsWhenPackageSourceNameIsNotFound() {
            // Arrange
            var command = CreateCommand();
            command.Arguments.Add("Disable");
            command.Name = "abc";

            // Act & Assert
            Exception exception = Assert.Throws<CommandLineException>(new Assert.ThrowsDelegate(command.ExecuteCommand));
            Assert.NotNull(exception);
            Assert.Equal("Unable to find any package source(s) matching name: abc.", exception.Message);
        }

        [Fact]
        public void EnableCommandEnableDisabledSourcesCorrectly() {
            // Arrange
            var settings = new MockUserSettingsManager();
            settings.SetValues(PackageSourceProvider.PackageSourcesSectionName,
               new[] {
                    new KeyValuePair<string, string>("one", "onesource"),       // enabled
                    new KeyValuePair<string, string>("two", "twosource"),       // disabled
                    new KeyValuePair<string, string>("three", "threesource")    // enabled
                });
            settings.SetValues(PackageSourceProvider.DisabledPackageSourcesSectionName,
                new[] {
                    new KeyValuePair<string, string>("two", "true")
                });

            var packageSourceProvider = new PackageSourceProvider(settings);
            var command = new SourcesCommand(packageSourceProvider);
            command.Arguments.Add("Enable");
            command.Name = "two";
            command.Console = new Mock<IConsole>().Object;

            // Act
            command.ExecuteCommand();

            // Assert
            var packageSources = packageSourceProvider.LoadPackageSources().ToList();
            Assert.Equal(3, packageSources.Count);
            Assert.True(packageSources[0].IsEnabled);
            Assert.True(packageSources[1].IsEnabled);
            Assert.True(packageSources[2].IsEnabled);
        }

        [Fact]
        public void EnableCommandDoNotAffectPackageSourcesThatAreAlreadyEnabled() {
            // Arrange
            var settings = new MockUserSettingsManager();
            settings.SetValues(PackageSourceProvider.PackageSourcesSectionName,
               new[] {
                    new KeyValuePair<string, string>("one", "onesource"),       // enabled
                    new KeyValuePair<string, string>("two", "twosource"),       // disabled
                    new KeyValuePair<string, string>("three", "threesource")    // enabled
                });
            settings.SetValues(PackageSourceProvider.DisabledPackageSourcesSectionName,
                new[] {
                    new KeyValuePair<string, string>("two", "true")
                });

            var packageSourceProvider = new PackageSourceProvider(settings);
            var command = new SourcesCommand(packageSourceProvider);
            command.Arguments.Add("Enable");
            command.Name = "one";
            command.Console = new Mock<IConsole>().Object;

            // Act
            command.ExecuteCommand();

            // Assert
            var packageSources = packageSourceProvider.LoadPackageSources().ToList();
            Assert.Equal(3, packageSources.Count);
            Assert.True(packageSources[0].IsEnabled);
            Assert.False(packageSources[1].IsEnabled);
            Assert.True(packageSources[2].IsEnabled);
        }

        [Fact]
        public void DisableCommandDisablePackageSourcesCorrectly() {
            // Arrange
            var settings = new MockUserSettingsManager();
            settings.SetValues(PackageSourceProvider.PackageSourcesSectionName,
               new[] {
                    new KeyValuePair<string, string>("one", "onesource"),       // enabled
                    new KeyValuePair<string, string>("two", "twosource"),       // disabled
                    new KeyValuePair<string, string>("three", "threesource")    // enabled
                });
            settings.SetValues(PackageSourceProvider.DisabledPackageSourcesSectionName,
                new[] {
                    new KeyValuePair<string, string>("two", "true")
                });

            var packageSourceProvider = new PackageSourceProvider(settings);
            var command = new SourcesCommand(packageSourceProvider);
            command.Arguments.Add("Disable");
            command.Name = "three";
            command.Console = new Mock<IConsole>().Object;

            // Act
            command.ExecuteCommand();

            // Assert
            var packageSources = packageSourceProvider.LoadPackageSources().ToList();
            Assert.Equal(3, packageSources.Count);
            Assert.True(packageSources[0].IsEnabled);
            Assert.False(packageSources[1].IsEnabled);
            Assert.False(packageSources[2].IsEnabled);
        }

        [Fact]
        public void DisableCommandDoNotAffectPackageSourcesThatAreAlreadyDisabled() {
            // Arrange
            var settings = new MockUserSettingsManager();
            settings.SetValues(PackageSourceProvider.PackageSourcesSectionName,
               new[] {
                    new KeyValuePair<string, string>("one", "onesource"),       // enabled
                    new KeyValuePair<string, string>("two", "twosource"),       // disabled
                    new KeyValuePair<string, string>("three", "threesource")    // enabled
                });
            settings.SetValues(PackageSourceProvider.DisabledPackageSourcesSectionName,
                new[] {
                    new KeyValuePair<string, string>("two", "true")
                });

            var packageSourceProvider = new PackageSourceProvider(settings);
            var command = new SourcesCommand(packageSourceProvider);
            command.Arguments.Add("Disable");
            command.Name = "two";
            command.Console = new Mock<IConsole>().Object;

            // Act
            command.ExecuteCommand();

            // Assert
            var packageSources = packageSourceProvider.LoadPackageSources().ToList();
            Assert.Equal(3, packageSources.Count);
            Assert.True(packageSources[0].IsEnabled);
            Assert.False(packageSources[1].IsEnabled);
            Assert.True(packageSources[2].IsEnabled);
        }

        private SourcesCommand CreateCommand(ISettings settings = null) {
            settings = settings ?? new MockUserSettingsManager();
            var packageSourceProvider = new PackageSourceProvider(settings);
            return new SourcesCommand(packageSourceProvider);
        }

    }
}