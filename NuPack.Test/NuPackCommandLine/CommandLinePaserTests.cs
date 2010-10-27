using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace NuGet.Test.NuGetCommandLine {
    [TestClass]
    public class CommandLinePaserTests {
        [TestMethod]
        public void GetNextCommandLineItem_BreaksOnSpacesOutSideDoubleQuotes() {
            // Arrange
            string input = "foo bar";
            // Act
            string actualItem = CommandLineParser.GetNextCommandLineItem(ref input);
            // Assert
            Assert.AreEqual("foo", actualItem);
            Assert.AreEqual("bar", input);
        }

        [TestMethod]
        public void GetNextCommandLineItem_DeosNotBreakOnSpacesinSideDoubleQuotes() {
            // Arrange
            string input = "\"foo bar\"";
            // Act
            string actualItem = CommandLineParser.GetNextCommandLineItem(ref input);
            // Assert
            Assert.AreEqual("foo bar", actualItem);
            Assert.AreEqual("", input);
        }

        [TestMethod]
        public void GetNextCommandLineItem_DoesNotBreakOnSpacesWithOutClosingQuote() {
            // Arrange
            string input = "\"foo bar";
            // Act
            string actualItem = CommandLineParser.GetNextCommandLineItem(ref input);
            // Assert
            Assert.AreEqual("foo bar", actualItem);
            Assert.AreEqual("", input);
        }

        [TestMethod]
        public void GetNextCommandLineItem_ReturnsEmptyStringWithEmptyInput() {
            // Arrange
            string input = "";
            // Act
            string actualItem = CommandLineParser.GetNextCommandLineItem(ref input);
            // Assert
            Assert.AreEqual("", actualItem);
            Assert.AreEqual("", input);
        }

        [TestMethod]
        public void GetNextCommandLineItem_BreaksOnSpacesWithASingleQuote() {
            // Arrange
            string input = "'foo bar'";
            // Act
            string actualItem = CommandLineParser.GetNextCommandLineItem(ref input);
            // Assert
            Assert.AreEqual("'foo", actualItem);
            Assert.AreEqual("bar'", input);
        }

        [TestMethod]
        public void ArgCountTooHigh_ReturnsTrueIfThereAreToManyArgs() {
            // Arrage
            var cmdMgr = new Mock<ICommandManager>();
            ICommand cmd = new MockCommand();
            cmd.Arguments = new List<string> { "foo", "bar" };
            CommandAttribute mockCommandAttribute = new CommandAttribute("foo", "bar") { MaxArgs = 1 };
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(mockCommandAttribute);
            CommandLineParser paser = new CommandLineParser(cmdMgr.Object);
            // Act
            bool actual = paser.ArgCountTooHigh(cmd);
            // Assert
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ArgCountTooHigh_ReturnsFalseIfThereIsEqualArgs() {
            // Arrage 
            var cmdMgr = new Mock<ICommandManager>();
            ICommand cmd = new MockCommand();
            cmd.Arguments = new List<string> { "foo", "bar" };
            CommandAttribute mockCommandAttribute = new CommandAttribute("foo", "bar") { MaxArgs = 2 };
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(mockCommandAttribute);
            CommandLineParser paser = new CommandLineParser(cmdMgr.Object);
            // Act
            bool actual = paser.ArgCountTooHigh(cmd);
            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ArgCountTooLow_ReturnsTrueIfThereAreToFewArgs() {
            // Arrage 
            var cmdMgr = new Mock<ICommandManager>();
            ICommand cmd = new MockCommand();
            cmd.Arguments = new List<string> { "foo", "bar" };
            CommandAttribute mockCommandAttribute = new CommandAttribute("foo", "bar") { MinArgs = 4 };
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(mockCommandAttribute);
            CommandLineParser paser = new CommandLineParser(cmdMgr.Object);
            // Act
            bool actual = paser.ArgCountTooLow(cmd);
            // Assert
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void ArgCountTooLow_ReturnsFalseIfThereIsEqualArgs() {
            // Arrage 
            var cmdMgr = new Mock<ICommandManager>();
            ICommand cmd = new MockCommand();
            cmd.Arguments = new List<string> { "foo", "bar" };
            CommandAttribute mockCommandAttribute = new CommandAttribute("foo", "bar") { MinArgs = 2 };
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(mockCommandAttribute);
            CommandLineParser paser = new CommandLineParser(cmdMgr.Object);
            // Act
            bool actual = paser.ArgCountTooLow(cmd);
            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ParseCommandLine_ReturnsNullIfNextItemInCommandLineIsEmptyString() {
            CommandLineParser parser = new CommandLineParser(new Mock<ICommandManager>().Object);
            string input = "NuGet";
            // Act
            ICommand actualCommand = parser.ParseCommandLine(input);
            // Assert
            Assert.IsNull(actualCommand);
        }

        [TestMethod]
        public void ParseCommandLine_ThrowsCommandLineExpectionWhenUnknownCommand() {
            // Arrange 
            var cmdMgr = new Mock<ICommandManager>();
            cmdMgr.Setup(cm => cm.GetCommand(It.IsAny<string>())).Returns<ICommand>(null);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            string input = "NuGet SomeUnknownCommand SomeArgs";
            string expectedExceptionMessage = "Unknown command: 'SomeUnknownCommand'";
            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ParseCommandLine(input), expectedExceptionMessage);
        }

        [TestMethod]
        public void ExtractOptions_ReturnsEmptyCommandWhenCommandLineIsEmpty() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(new CommandAttribute("Mock", "Mock Command Attirbute"));
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, "");
            // Assert
            Assert.AreEqual(0, actualCommand.Arguments.Count);
            Assert.IsNull(((MockCommand)actualCommand).Message);
        }

        [TestMethod]
        public void ExtractOptions_AddsArgumentsWhenItemsDoNotStartWithSlashOrDash() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(new CommandAttribute("Mock", "Mock Command Attirbute"));
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, "optionOne optionTwo");
            // Assert
            Assert.AreEqual(2, actualCommand.Arguments.Count);
            Assert.AreEqual("optionOne", actualCommand.Arguments[0]);
            Assert.AreEqual("optionTwo", actualCommand.Arguments[1]);
            Assert.IsNull(((MockCommand)actualCommand).Message);
        }

        [TestMethod]
        public void ExtractOptions_ThrowsCommandLineExpectionWhenOptionUnknow() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(new CommandAttribute("Mock", "Mock Command Attirbute"));
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            string expectedErrorMessage = "Unknown option: '/NotAnOption'";
            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(ExpectedCommand, "/NotAnOption"), expectedErrorMessage);
        }

        [TestMethod]
        public void ExtractOptions_ParsesOptionsThatStartWithSlash() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(new CommandAttribute("Mock", "Mock Command Attirbute"));
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, "/Message \"foo bar\"");
            // Assert
            Assert.AreEqual("foo bar", ((MockCommand)actualCommand).Message);
        }

        [TestMethod]
        public void ExtractOptions_ParsesOptionsThatStartWithDash() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(new CommandAttribute("Mock", "Mock Command Attirbute"));
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, "-Message \"foo bar\"");
            // Assert
            Assert.AreEqual("foo bar", ((MockCommand)actualCommand).Message);
        }

        [TestMethod]
        public void ExtractOptions_ThrowsWhenOptionHasNoValue() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(new CommandAttribute("Mock", "Mock Command Attirbute"));
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            string expectedErrorMessage = "Missing option value for: '/Message'";
            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(ExpectedCommand, "/Message"), expectedErrorMessage);
        }

        [TestMethod]
        public void ExtractOptions_ParsesBoolOptionsAsTrueIfPresent() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("IsWorking");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(new CommandAttribute("Mock", "Mock Command Attirbute"));
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, "-IsWorking");
            // Assert
            Assert.IsTrue(((MockCommand)actualCommand).IsWorking);
        }

        [TestMethod]
        public void ExtractOptions_ParsesBoolOptionsAsFalseIfFollowedByDash() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("IsWorking");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(new CommandAttribute("Mock", "Mock Command Attirbute"));
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, "-IsWorking-");
            // Assert
            Assert.IsFalse(((MockCommand)actualCommand).IsWorking);
        }

        [TestMethod]
        public void ExtractOptions_ThrowsIfUnableToConvertType() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();

            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Count");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);

            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            cmdMgr.Setup(cm => cm.GetCommandAttribute(It.IsAny<ICommand>())).Returns(new CommandAttribute("Mock", "Mock Command Attirbute"));
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            string expectedErrorMessage = "Invalid option value: '/Count null'";
            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(ExpectedCommand, "/Count null"), expectedErrorMessage);
        }


        private class MockCommand : ICommand {

            public List<string> Arguments { get; set; }

            public string Message { get; set; }

            public bool IsWorking { get; set; }

            public int Count { get; set; }

            public void Execute() {
                throw new NotImplementedException();
            }
        }

    }
}
