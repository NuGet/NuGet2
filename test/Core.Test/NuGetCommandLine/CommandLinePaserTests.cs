using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Commands;

namespace NuGet.Test.NuGetCommandLine {
    [TestClass]
    public class CommandLinePaserTests {

        [TestMethod]
        public void GetNextCommandLineItem_ReturnsNullWithNullInput() {
            // Act
            string actualItem = CommandLineParser.GetNextCommandLineItem(null);
            // Assert
            Assert.IsNull(actualItem);
        }

        [TestMethod]
        public void GetNextCommandLineItem_ReturnsNullWithEmptyInput() {
            // Arrange
            var argsEnumerator = new List<string>().GetEnumerator();
            // Act
            string actualItem = CommandLineParser.GetNextCommandLineItem(argsEnumerator);
            // Assert
            Assert.IsNull(actualItem);
        }
        
        [TestMethod]
        public void ParseCommandLine_ThrowsCommandLineExpectionWhenUnknownCommand() {
            // Arrange 
            var cmdMgr = new Mock<ICommandManager>();
            cmdMgr.Setup(cm => cm.GetCommand(It.IsAny<string>())).Returns<ICommand>(null);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            List<string> input = new List<string>() { "SomeUnknownCommand", "SomeArgs" };
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
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            var argsEnumerator = new List<string>().GetEnumerator();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, argsEnumerator);
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
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            var argsEnumerator = new List<string>() { "optionOne", "optionTwo" }.GetEnumerator();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, argsEnumerator );
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
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            string expectedErrorMessage = "Unknown option: '/NotAnOption'";
            var argsEnumerator = new List<string>() {"/NotAnOption"}.GetEnumerator();
            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(ExpectedCommand, argsEnumerator), expectedErrorMessage);
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
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            var argsEnumerator = new List<string>() { "/Message", "foo bar" }.GetEnumerator();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, argsEnumerator);
            // Assert
            Assert.AreEqual("foo bar", ((MockCommand)actualCommand).Message);
        }

        [TestMethod]
        public void ExtractOptions_UsesShortenedForm() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            var argsEnumerator = new List<string>() { "/Mess", "foo bar" }.GetEnumerator();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, argsEnumerator);
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
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            var argsEnumerator = new List<string>() { "-Message", "foo bar" }.GetEnumerator();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, argsEnumerator);
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
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            string expectedErrorMessage = "Missing option value for: '/Message'";
            var argsEnumerator = new List<string>() { "/Message" }.GetEnumerator();

            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(ExpectedCommand, argsEnumerator), expectedErrorMessage);
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
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            var argsEnumerator = new List<string>() { "-IsWorking" }.GetEnumerator();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, argsEnumerator);
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
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            var argsEnumerator = new List<string>() { "-IsWorking-" }.GetEnumerator();
            // Act
            ICommand actualCommand = parser.ExtractOptions(ExpectedCommand, argsEnumerator);
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
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            string expectedErrorMessage = "Invalid option value: '/Count null'";
            var argsEnumerator = new List<string>() { "/Count", "null" }.GetEnumerator();
            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(ExpectedCommand, argsEnumerator), expectedErrorMessage);
        }

        [TestMethod]
        public void ExtractOptions_ThrowsIfCommandHasNoProperties() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();

            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            string expectedErrorMessage = "Unknown option: '/Count'";
            var argsEnumerator = new List<string>() { "/Count", "null" }.GetEnumerator();
            
            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(ExpectedCommand, argsEnumerator), expectedErrorMessage);
        }

        [TestMethod]
        public void ExtractOptions_ThrowsIfCommandOptionIsAmbigious() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();

            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option 1");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Count");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            mockOptionAttribute = new OptionAttribute("Mock Option 2");
            mockPropertyInfo = typeof(MockCommand).GetProperty("Counter");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);

            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            string expectedErrorMessage = "Ambiguous option 'Co'. Possible values: Count Counter.";
            var argsEnumerator = new List<string>() { "/Co", "null" }.GetEnumerator();

            // Act & Assert

            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(ExpectedCommand, argsEnumerator), expectedErrorMessage);
        }

        [TestMethod]
        public void ExtractOptions_ThrowsIfCommandOptionDoesNotExist() {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();

            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option 1");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Count");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            mockOptionAttribute = new OptionAttribute("Mock Option 2");
            mockPropertyInfo = typeof(MockCommand).GetProperty("Counter");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);

            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var ExpectedCommand = new MockCommand();
            string expectedErrorMessage = "Unknown option: '/37264752DOESNOTEXIST!!'";
            var argsEnumerator = new List<string>() { "/37264752DOESNOTEXIST!!", "false" }.GetEnumerator();

            // Act & Assert

            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(ExpectedCommand, argsEnumerator), expectedErrorMessage);
        }

        [TestMethod]
        public void ExtractOptionAddsValuesToListCommand() {
            // Arrange
            var cmdMgr = new CommandManager();
            var command = new MockCommandWithMultiple();
            var arguments = "/ListProperty Val1 /RegularProp RegularPropValue /ListProperty Val2  /ListProperty Val3";
            var parser = new CommandLineParser(cmdMgr);

            // Act
            parser.ExtractOptions(command, arguments.Split().AsEnumerable().GetEnumerator());
            
            // Assert
            Assert.AreEqual(command.RegularProp, "RegularPropValue");
            Assert.AreEqual(command.ListProperty.Count, 3);
            Assert.AreEqual(command.ListProperty[0], "Val1");
            Assert.AreEqual(command.ListProperty[1], "Val2");
            Assert.AreEqual(command.ListProperty[2], "Val3");
        }

        [TestMethod]
        public void ExtractOptionsSplitsValueBySemiColorForCollectionOption() {
            // Arrange
            var cmdMgr = new CommandManager();
            var command = new MockCommandWithMultiple();
            var arguments = "/ListProperty Val1 /RegularProp RegularPropValue /ListProperty Val2;Val3;Val4  /ListProperty Val5";
            var parser = new CommandLineParser(cmdMgr);

            // Act
            parser.ExtractOptions(command, arguments.Split().AsEnumerable().GetEnumerator());

            // Assert
            Assert.AreEqual(command.RegularProp, "RegularPropValue");
            Assert.AreEqual(command.ListProperty.Count, 5);
            Assert.AreEqual(command.ListProperty[0], "Val1");
            Assert.AreEqual(command.ListProperty[1], "Val2");
            Assert.AreEqual(command.ListProperty[2], "Val3");
            Assert.AreEqual(command.ListProperty[3], "Val4");
            Assert.AreEqual(command.ListProperty[4], "Val5");
        }


        private class MockCommand : ICommand {

            public List<string> Arguments { get; set; }

            public string Message { get; set; }

            public bool IsWorking { get; set; }

            public int Count { get; set; }

            public int Counter { get; set; }

            public void Execute() {
                throw new NotImplementedException();
            }

            public CommandAttribute CommandAttribute {
                get { return new CommandAttribute("Mock", "Mock Command Desc"); }
            }

            public IEnumerable<CommandAttribute> GetCommandAttribute() {
                return new[] { CommandAttribute };
            }
        }


        private class MockCommandWithMultiple : Command {
            private readonly List<string> _listProperty = new List<string>();

            [Option("Regular Option")]
            public string RegularProp { get; set; }

            [Option("List property")]
            public List<string> ListProperty { 
                get { return _listProperty; } 
            }

            public override void ExecuteCommand() {
                throw new NotImplementedException();
            }
        }
    }
}
