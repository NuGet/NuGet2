using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Test;
using NuGetConsole.Host.PowerShell;

namespace PowerShellHost.Test {
    [TestClass]
    public class CommandParserTest {        
        [TestMethod]
        public void NullCommandThrows() {
            ExceptionAssert.ThrowsArgNull(() => CommandParser.Parse(null), "command");
        }

        [TestMethod]
        public void CommandWithArgumentSetsArgumentAndCompletionArgment() {
            // Act
            var command = CommandParser.Parse("Install-Package -Ver ");

            // Assert
            Assert.AreEqual("Install-Package", command.CommandName);
            Assert.AreEqual(1, command.Arguments.Count);
            Assert.IsTrue(command.Arguments.ContainsKey("Ver"));
            Assert.AreEqual("Ver", command.CompletionArgument);
            Assert.IsNull(command.CompletionIndex);
        }

        [TestMethod]
        public void CommandWithPositionalArgumentSetsIndex() {
            // Act
            var command = CommandParser.Parse("Install-Package ");

            // Assert
            Assert.AreEqual("Install-Package", command.CommandName);
            Assert.AreEqual(1, command.Arguments.Count);
            Assert.IsTrue(command.Arguments.ContainsKey(0));
            Assert.IsNull(command.CompletionArgument);
            Assert.AreEqual(0, command.CompletionIndex);
        }

        [TestMethod]
        public void CommandWithEmptyNamedArgument() {
            // Act
            var command = CommandParser.Parse("Install-Package -");

            // Assert
            Assert.AreEqual("Install-Package", command.CommandName);
            Assert.AreEqual(0, command.Arguments.Count);
            Assert.IsNull(command.CompletionIndex);
            Assert.IsNull(command.CompletionArgument);
        }

        [TestMethod]
        public void CommandWithNoArguments() {
            // Act
            var command = CommandParser.Parse("Install-Package");

            // Assert
            Assert.AreEqual("Install-Package", command.CommandName);
            Assert.AreEqual(0, command.Arguments.Count);
            Assert.IsNull(command.CompletionIndex);
            Assert.IsNull(command.CompletionArgument);
        }

        [TestMethod]
        public void CommandWithSingleQuotedArgument() {
            // Act
            var command = CommandParser.Parse("Install-Package 'This quoted value' -Value 'Another one' -Name 'John''s value'");

            // Assert
            Assert.AreEqual("Install-Package", command.CommandName);
            Assert.AreEqual(3, command.Arguments.Count);
            Assert.AreEqual("This quoted value", command.Arguments[0]);
            Assert.AreEqual("Another one", command.Arguments["Value"]);
            Assert.AreEqual("John's value", command.Arguments["Name"]);
            Assert.IsNull(command.CompletionIndex);
            Assert.AreEqual("Name", command.CompletionArgument);
        }

        [TestMethod]
        public void CommandWithQuotedArgument() {
            // Act
            var command = CommandParser.Parse("Install-Package \"This quoted value\" -Value \"Another `n one\" -Name \"John`\"s value\"");

            // Assert
            Assert.AreEqual("Install-Package", command.CommandName);
            Assert.AreEqual(3, command.Arguments.Count);
            Assert.AreEqual("This quoted value", command.Arguments[0]);
            Assert.AreEqual("Another `n one", command.Arguments["Value"]);
            Assert.AreEqual("John`\"s value", command.Arguments["Name"]);
            Assert.IsNull(command.CompletionIndex);
            Assert.AreEqual("Name", command.CompletionArgument);
        }

        [TestMethod]
        public void CommandWithNoArgumentValueAndValidArgumentValue() {
            // Act
            var command = CommandParser.Parse("Install-Package -Arg1 -Arg2 Value");

            // Assert
            Assert.AreEqual("Install-Package", command.CommandName);
            Assert.AreEqual(2, command.Arguments.Count);
            Assert.IsNull(command.Arguments["Arg1"]);
            Assert.AreEqual("Value", command.Arguments["Arg2"]);
            Assert.IsNull(command.CompletionIndex);
            Assert.AreEqual("Arg2", command.CompletionArgument);
        }

        [TestMethod]
        public void MultipleCommandsOnlyParsesLastCommand() {
            // Act
            var command = CommandParser.Parse("Get-Values -A 1 'B' | Install-Package -Arg1 -Arg2 Value");

            // Assert
            Assert.AreEqual("Install-Package", command.CommandName);
            Assert.AreEqual(2, command.Arguments.Count);
            Assert.IsNull(command.Arguments["Arg1"]);
            Assert.AreEqual("Value", command.Arguments["Arg2"]);
            Assert.IsNull(command.CompletionIndex);
            Assert.AreEqual("Arg2", command.CompletionArgument);
        }

        [TestMethod]
        public void AssignmentStatementWithCommandParsesCommand() {
            // Act
            var command = CommandParser.Parse("$p = Get-Project ");

            // Assert
            Assert.AreEqual("Get-Project", command.CommandName);
            Assert.AreEqual(1, command.Arguments.Count);
            Assert.AreEqual("", command.Arguments[0]);
            Assert.AreEqual(0, command.CompletionIndex);
        }
    }
}
