using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test.NuGetCommandLine {
    [TestClass]
    public class CommandManagerTests {
        [TestMethod]
        public void RegisterCommand_AddsCommandToDictionary() {
            // Arrage
            CommandManager cm = new CommandManager();
            ICommand mockCommand = new MockCommand();
            // Act
            cm.RegisterCommand(mockCommand);
            // Assert
            Assert.AreEqual(1, cm.GetCommands().Count);
        }

        [TestMethod]
        public void GetCommand_ReturnsNullIfNoCommandFound() {
            // Arrange
            CommandManager cm = new CommandManager();
            // Act
            ICommand cmd = cm.GetCommand("NoCommandByThisName");
            // Assert
            Assert.IsNull(cmd);
        }

        [TestMethod]
        public void GetCommand_ReturnsCorrectCommand() {
            // Arrange
            CommandManager cm = new CommandManager();
            ICommand expectedCommand = new MockCommand();
            cm.RegisterCommand(expectedCommand);
            // Act
            ICommand actualCommand = cm.GetCommand("MockCommand");
            // Assert
            Assert.AreEqual(expectedCommand, actualCommand);
        }

        [TestMethod]
        public void GetCommandAttribute_ReturnsNullIfNoCommandFound() {
            // Arrange
            CommandManager cm = new CommandManager();
            // Act
            CommandAttribute commandAttribute = cm.GetCommandAttribute(new MockCommand());
            // Assert
            Assert.IsNull(commandAttribute);
        }

        [TestMethod]
        public void GetCommandAttribute_ReturnsCorrectCommandAttribute() {
            // Arrange
            CommandManager cm = new CommandManager();
            ICommand cmd = new MockCommand();
            CommandAttribute expectedCommandAttribute = ((CommandAttribute)cmd.GetType().GetCustomAttributes(typeof(CommandAttribute), true)[0]);
            cm.RegisterCommand(cmd);
            // Act 
            CommandAttribute actualCommandAttribute = cm.GetCommandAttribute(cmd);
            // Assert
            Assert.AreEqual(expectedCommandAttribute, actualCommandAttribute);
        }

        [TestMethod]
        public void GetCommandOptions_ThrowsWhenOptionHasNoSetter() {
            // Arrange 
            CommandManager cm = new CommandManager();
            ICommand cmd = new MockCommandBadOption();
            cm.RegisterCommand(cmd);
            string expectedErrorText = "[option] on 'NuGet.Test.NuGetCommandLine.CommandManagerTests+MockCommandBadOption.Message' is invalid without a setter.";
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => cm.GetCommandOptions(cmd), expectedErrorText);
        }

        [TestMethod]
        public void GetCommandOptions_ReturnsCorrectOpionAttributeAndPropertyInfo() {
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
            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual(expectedOptionAttributeOne, actual.Keys.First());
            Assert.AreEqual(expectedPropertyInfoOne, actual[expectedOptionAttributeOne]);
            Assert.AreEqual(expectedOptionAttributeTwo, actual.Keys.Last());
            Assert.AreEqual(expectedPropertyInfoTwo, actual[expectedOptionAttributeTwo]);

        }

        [Command("MockCommand", "This is a Mock Command")]
        private class MockCommand : ICommand {
            public System.Collections.Generic.List<string> Arguments { get; set; }
            [Option("A Option")]
            public string Message { get; set; }
            [Option("A Option Two")]
            public string MessageTwo { get; set; }
            public void Execute() { }
        }

        [Command("MockCommandBabOption", "This is a Mock Command With A Option Without a Setter")]
        private class MockCommandBadOption : ICommand {
            public System.Collections.Generic.List<string> Arguments { get; set; }
            [Option("A Option")]
            public string Message { get { return "Bad"; } }
            public void Execute() { }
        }
    }
}
