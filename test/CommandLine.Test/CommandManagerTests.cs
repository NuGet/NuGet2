using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace NuGet.Test.NuGetCommandLine
{
    public class CommandManagerTests
    {
        [Fact]
        public void RegisterCommand_AddsCommandToDictionary()
        {
            // Arrange
            CommandManager cm = new CommandManager();
            ICommand mockCommand = new MockCommand();
            // Act
            cm.RegisterCommand(mockCommand);
            // Assert
            Assert.Equal(1, cm.GetCommands().Count());
        }

        [Fact]
        public void GetCommand_ThrowsIfNoCommandFound()
        {
            // Arrange
            CommandManager cm = new CommandManager();

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(() => cm.GetCommand("NoCommandByThisName"), "Unknown command: 'NoCommandByThisName'");
        }

        [Fact]
        public void GetCommand_ReturnsCorrectCommand()
        {
            // Arrange
            CommandManager cm = new CommandManager();
            ICommand expectedCommand = new MockCommand();
            cm.RegisterCommand(expectedCommand);
            // Act
            ICommand actualCommand = cm.GetCommand("MockCommand");
            // Assert
            Assert.Equal(expectedCommand, actualCommand);
        }

        [Fact]
        public void GetCommandOptions_ThrowsWhenOptionHasNoSetter()
        {
            // Arrange 
            CommandManager cm = new CommandManager();
            ICommand cmd = new MockCommandBadOption();
            cm.RegisterCommand(cmd);
            string expectedErrorText = "[option] on 'NuGet.Test.NuGetCommandLine.CommandManagerTests+MockCommandBadOption.Message' is invalid without a setter.";
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cm.GetCommandOptions(cmd), expectedErrorText);
        }

        [Fact]
        public void GetCommandOptions_ReturnsCorrectOpionAttributeAndPropertyInfo()
        {
            // Arrange 
            CommandManager cm = new CommandManager();
            ICommand cmd = new MockCommand();
            cm.RegisterCommand(cmd);
            Dictionary<OptionAttribute, PropertyInfo> expected = new Dictionary<OptionAttribute, PropertyInfo>();
            var expectedOptionAttributeOne = new OptionAttribute("A Option");
            var expectedPropertyInfoOne = typeof(MockCommand).GetProperty("Message");
            expected.Add(expectedOptionAttributeOne, expectedPropertyInfoOne);
            var expectedOptionAttributeTwo = new OptionAttribute("A Option Two");
            var expectedPropertyInfoTwo = typeof(MockCommand).GetProperty("MessageTwo");
            expected.Add(expectedOptionAttributeTwo, expectedPropertyInfoTwo);
            // Act
            IDictionary<OptionAttribute, PropertyInfo> actual = cm.GetCommandOptions(cmd);
            // Assert
            Assert.Equal(2, actual.Count);
            Assert.Equal(expectedOptionAttributeOne, actual.Keys.First());
            Assert.Equal(expectedPropertyInfoOne, actual[expectedOptionAttributeOne]);
            Assert.Equal(expectedOptionAttributeTwo, actual.Keys.Last());
            Assert.Equal(expectedPropertyInfoTwo, actual[expectedOptionAttributeTwo]);

        }

        [Fact]
        public void RegisterCommand_DoesNotRegisterCommandIfNoCommandAttributesArePresent()
        {
            // Arrange 
            CommandManager cm = new CommandManager();
            ICommand cmd = new MockCommandEmptyAttributes();
            cm.RegisterCommand(cmd);

            // Act
            var registeredCommands = cm.GetCommands();

            // Assert
            Assert.False(registeredCommands.Any());
        }

        [Fact]
        public void RegisterCommand_ReturnsExactMatchesEvenIfAmbigious()
        {
            // Arrange 
            CommandManager cm = new CommandManager();

            cm.RegisterCommand(new MockCommand(new CommandAttribute("Foo", "desc")));
            cm.RegisterCommand(new MockCommand(new CommandAttribute("FooBar", "desc")));

            // Act
            var result = cm.GetCommand("Foo");

            // Assert
            // If we get this far, we've found 'foo'
            Assert.NotNull(result);
            Assert.Equal(result.CommandAttribute.CommandName, "Foo");
        }

        [Fact]
        public void RegisterCommand_ThrowsIfCommandNamesAreAmbigious()
        {
            // Arrange 
            CommandManager cm = new CommandManager();

            cm.RegisterCommand(new MockCommand(new CommandAttribute("Foo", "desc")));
            cm.RegisterCommand(new MockCommand(new CommandAttribute("FooBar", "desc")));

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(() => cm.GetCommand("f"), "Ambiguous command 'f'. Possible values: Foo FooBar.");
        }

        private class MockCommand : ICommand
        {
            private readonly List<string> _arguments = new List<string>();
            private readonly CommandAttribute _attribute;

            public IList<string> Arguments
            {
                get { return _arguments; }
            }

            [Option("A Option")]
            public string Message { get; set; }
            [Option("A Option Two")]
            public string MessageTwo { get; set; }
            public void Execute() { }

            public MockCommand()
                : this(new CommandAttribute("MockCommand", "This is a Mock Command"))
            {

            }

            public MockCommand(CommandAttribute attribute)
            {
                _attribute = attribute;
            }

            public CommandAttribute CommandAttribute
            {
                get
                {
                    return _attribute;
                }
            }

            public IEnumerable<CommandAttribute> GetCommandAttribute()
            {
                return new[] { CommandAttribute };
            }

            public bool IncludedInHelp(string optionName)
            {
                return true;
            }
        }

        private class MockCommandBadOption : ICommand
        {
            private readonly List<string> _arguments = new List<string>();

            public IList<string> Arguments
            {
                get { return _arguments; }
            }

            [Option("A Option")]
            public string Message { get { return "Bad"; } }
            public void Execute() { }


            public CommandAttribute CommandAttribute
            {
                get
                {
                    return new CommandAttribute("MockCommandBadOption", "This is a Mock Command With A Option Without a Setter");
                }
            }

            public IEnumerable<CommandAttribute> GetCommandAttribute()
            {
                return new[] { CommandAttribute };
            }

            public bool IncludedInHelp(string optionName)
            {
                return true;
            }
        }

        private class MockCommandEmptyAttributes : ICommand
        {
            [Option("A Option")]
            public string Message { get { return "Bad"; } }
            public void Execute() { }

            public IList<string> Arguments { get; set; }

            public CommandAttribute CommandAttribute
            {
                get
                {
                    return null;
                }
            }

            public IEnumerable<CommandAttribute> GetCommandAttribute()
            {
                return Enumerable.Empty<CommandAttribute>();
            }

            public bool IncludedInHelp(string optionName)
            {
                return true;
            }
        }
    }
}
