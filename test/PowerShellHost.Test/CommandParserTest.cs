using NuGet.Test;
using NuGetConsole.Host.PowerShell;
using Xunit;

namespace PowerShellHost.Test
{
    public class CommandParserTest
    {
        [Fact]
        public void NullCommandThrows()
        {
            ExceptionAssert.ThrowsArgNull(() => CommandParser.Parse(null), "command");
        }

        [Fact]
        public void CommandWithArgumentSetsArgumentAndCompletionArgment()
        {
            // Act
            var command = CommandParser.Parse("Install-Package -Ver ");

            // Assert
            Assert.Equal("Install-Package", command.CommandName);
            Assert.Equal(1, command.Arguments.Count);
            Assert.True(command.Arguments.ContainsKey("Ver"));
            Assert.Equal("Ver", command.CompletionArgument);
            Assert.Null(command.CompletionIndex);
        }

        [Fact]
        public void CommandWithPositionalArgumentSetsIndex()
        {
            // Act
            var command = CommandParser.Parse("Install-Package ");

            // Assert
            Assert.Equal("Install-Package", command.CommandName);
            Assert.Equal(1, command.Arguments.Count);
            Assert.True(command.Arguments.ContainsKey(0));
            Assert.Null(command.CompletionArgument);
            Assert.Equal(0, command.CompletionIndex);
        }

        [Fact]
        public void CommandWithEmptyNamedArgument()
        {
            // Act
            var command = CommandParser.Parse("Install-Package -");

            // Assert
            Assert.Equal("Install-Package", command.CommandName);
            Assert.Equal(0, command.Arguments.Count);
            Assert.Null(command.CompletionIndex);
            Assert.Null(command.CompletionArgument);
        }

        [Fact]
        public void CommandWithNoArguments()
        {
            // Act
            var command = CommandParser.Parse("Install-Package");

            // Assert
            Assert.Equal("Install-Package", command.CommandName);
            Assert.Equal(0, command.Arguments.Count);
            Assert.Null(command.CompletionIndex);
            Assert.Null(command.CompletionArgument);
        }

        [Fact]
        public void CommandWithSingleQuotedArgument()
        {
            // Act
            var command = CommandParser.Parse("Install-Package 'This quoted value' -Value 'Another one' -Name 'John''s value'");

            // Assert
            Assert.Equal("Install-Package", command.CommandName);
            Assert.Equal(3, command.Arguments.Count);
            Assert.Equal("This quoted value", command.Arguments[0]);
            Assert.Equal("Another one", command.Arguments["Value"]);
            Assert.Equal("John's value", command.Arguments["Name"]);
            Assert.Null(command.CompletionIndex);
            Assert.Equal("Name", command.CompletionArgument);
        }

        [Fact]
        public void CommandWithQuotedArgument()
        {
            // Act
            var command = CommandParser.Parse("Install-Package \"This quoted value\" -Value \"Another `n one\" -Name \"John`\"s value\"");

            // Assert
            Assert.Equal("Install-Package", command.CommandName);
            Assert.Equal(3, command.Arguments.Count);
            Assert.Equal("This quoted value", command.Arguments[0]);
            Assert.Equal("Another `n one", command.Arguments["Value"]);
            Assert.Equal("John`\"s value", command.Arguments["Name"]);
            Assert.Null(command.CompletionIndex);
            Assert.Equal("Name", command.CompletionArgument);
        }

        [Fact]
        public void CommandWithNoArgumentValueAndValidArgumentValue()
        {
            // Act
            var command = CommandParser.Parse("Install-Package -Arg1 -Arg2 Value");

            // Assert
            Assert.Equal("Install-Package", command.CommandName);
            Assert.Equal(2, command.Arguments.Count);
            Assert.Null(command.Arguments["Arg1"]);
            Assert.Equal("Value", command.Arguments["Arg2"]);
            Assert.Null(command.CompletionIndex);
            Assert.Equal("Arg2", command.CompletionArgument);
        }

        [Fact]
        public void MultipleCommandsOnlyParsesLastCommand()
        {
            // Act
            var command = CommandParser.Parse("Get-Values -A 1 'B' | Install-Package -Arg1 -Arg2 Value");

            // Assert
            Assert.Equal("Install-Package", command.CommandName);
            Assert.Equal(2, command.Arguments.Count);
            Assert.Null(command.Arguments["Arg1"]);
            Assert.Equal("Value", command.Arguments["Arg2"]);
            Assert.Null(command.CompletionIndex);
            Assert.Equal("Arg2", command.CompletionArgument);
        }

        [Fact]
        public void AssignmentStatementWithCommandParsesCommand()
        {
            // Act
            var command = CommandParser.Parse("$p = Get-Project ");

            // Assert
            Assert.Equal("Get-Project", command.CommandName);
            Assert.Equal(1, command.Arguments.Count);
            Assert.Equal("", command.Arguments[0]);
            Assert.Equal(0, command.CompletionIndex);
        }
    }
}
