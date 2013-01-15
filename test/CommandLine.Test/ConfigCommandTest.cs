using System;
using System.Collections.Generic;
using Moq;
using NuGet.Commands;
using Xunit;

namespace NuGet.Test
{
    public class ConfigCommandTest
    {
        [Fact]
        public void ConstructorThrowsIfSettingsIsNull()
        {
            // Act and Assert
            var configCommand = new ConfigCommand();
            ExceptionAssert.Throws<InvalidOperationException>(
                () => configCommand.ExecuteCommand(),
                "Property Settings is null.");
        }

        [Fact]
        public void ExecutePrintsNothingIfNoArgumentsAndNoSetPropertiesAreSpecified()
        {
            // Arrange
            var settings = Mock.Of<ISettings>();
            var console = new MockConsole();
            var command = new ConfigCommand()
            {
                Settings = settings,
                Console = console
            };

            // Act
            command.ExecuteCommand();

            // Assert
            Assert.Empty(console.Output);
        }

        [Fact]
        public void ExecutePrintsValueOfKeyIfArgumentIsSpecified()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValue("config", "test", false)).Returns("value").Verifiable();

            var console = new MockConsole();
            var command = new ConfigCommand()
            {
                Settings = settings.Object,
                Console = console,
            };
            command.Arguments.Add("test");

            // Act
            command.ExecuteCommand();

            // Assert
            Assert.Equal("value", console.Output.Trim());
            settings.Verify();
        }

        [Fact]
        public void SpecifiedPropertiesAreSetInConfig()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.SetValue("config", "test", "value2")).Verifiable();
            settings.Setup(s => s.SetValue("config", "test2", "value1")).Verifiable();

            var console = new MockConsole();
            var command = new ConfigCommand()
            {
                Settings = settings.Object,
                Console = console,
            };
            command.Set.Add("test", "value2");
            command.Set.Add("test2", "value1");

            // Act
            command.ExecuteCommand();

            // Assert
            settings.Verify();
        }

        [Fact]
        public void SpecifiedPropertiesAreDeletedIfNoValueIsProvidedToSetProperties()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.DeleteValue("config", "test")).Returns(true).Verifiable();

            var console = new MockConsole();
            var command = new ConfigCommand()
            {
                Settings = settings.Object,
                Console = console,
            };
            command.Set.Add("test", "");

            // Act
            command.ExecuteCommand();

            // Assert
            settings.Verify();
        }

        [Fact]
        public void ExecuteThrowsIfSettingsIsNullSettings()
        {
            // Arrange
            var command = new ConfigCommand()
            {
                Settings = NullSettings.Instance
            };
            command.Set.Add("foo", "bar");

            // Act and Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => command.ExecuteCommand(),
                "\"SetValue\" cannot be called on a NullSettings. This may be caused on account of insufficient permissions to read or write to \"%AppData%\\NuGet\\NuGet.config\".");
        }
    }
}
