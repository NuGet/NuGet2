using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using NuGet.Commands;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.NuGetCommandLine
{
    public class CommandLinePaserTests
    {
        [Fact]
        public void GetNextCommandLineItem_ReturnsNullWithNullInput()
        {
            // Act
            string actualItem = CommandLineParser.GetNextCommandLineItem(null);
            // Assert
            Assert.Null(actualItem);
        }

        [Fact]
        public void GetNextCommandLineItem_ReturnsNullWithEmptyInput()
        {
            // Arrange
            var argsEnumerator = new List<string>().GetEnumerator();
            // Act
            string actualItem = CommandLineParser.GetNextCommandLineItem(argsEnumerator);
            // Assert
            Assert.Null(actualItem);
        }

        [Fact]
        public void ParseCommandLine_ThrowsCommandLineExpectionWhenUnknownCommand()
        {
            // Arrange 
            var cmdMgr = new Mock<ICommandManager>();
            cmdMgr.Setup(cm => cm.GetCommand(It.IsAny<string>())).Returns<ICommand>(null);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            List<string> input = new List<string>() { "SomeUnknownCommand", "SomeArgs" };
            string expectedExceptionMessage = "Unknown command: 'SomeUnknownCommand'";
            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ParseCommandLine(input), expectedExceptionMessage);
        }

        [Fact]
        public void ExtractOptions_ReturnsEmptyCommandWhenCommandLineIsEmpty()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var command = new MockCommand();
            var argsEnumerator = new List<string>().GetEnumerator();
            
            // Act
            parser.ExtractOptions(command, argsEnumerator);
            
            // Assert
            Assert.Equal(0, command.Arguments.Count);
            Assert.Null(command.Message);
        }

        [Fact]
        public void ExtractOptions_AddsArgumentsWhenItemsDoNotStartWithSlashOrDash()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var command = new MockCommand();
            var argsEnumerator = new List<string>() { "optionOne", "optionTwo" }.GetEnumerator();
            
            // Act
            parser.ExtractOptions(command, argsEnumerator);

            // Assert
            Assert.Equal(2, command.Arguments.Count);
            Assert.Equal("optionOne", command.Arguments[0]);
            Assert.Equal("optionTwo", command.Arguments[1]);
            Assert.Null(command.Message);
        }

        [Fact]
        public void ExtractOptions_ThrowsCommandLineExpectionWhenOptionUnknow()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var command = new MockCommand();
            string expectedErrorMessage = "Unknown option: '/NotAnOption'";
            var argsEnumerator = new List<string>() { "/NotAnOption" }.GetEnumerator();
            
            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(command, argsEnumerator), expectedErrorMessage);
        }

        [Fact]
        public void ExtractOptions_ParsesOptionsThatStartWithSlash()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var cmd = new MockCommand();
            var argsEnumerator = new List<string>() { "/Message", "foo bar" }.GetEnumerator();
            
            // Act
            parser.ExtractOptions(cmd, argsEnumerator);
            
            // Assert
            Assert.Equal("foo bar", cmd.Message);
        }

        [Fact]
        public void ExtractOptions_UsesShortenedForm()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var command = new MockCommand();
            var argsEnumerator = new List<string>() { "/Mess", "foo bar" }.GetEnumerator();
            
            // Act
            parser.ExtractOptions(command, argsEnumerator);
            
            // Assert
            Assert.Equal("foo bar", command.Message);
        }

        [Fact]
        public void ExtractOptions_ParsesOptionsThatStartWithDash()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var command = new MockCommand();
            var argsEnumerator = new List<string>() { "-Message", "foo bar" }.GetEnumerator();
            
            // Act
            parser.ExtractOptions(command, argsEnumerator);
            
            // Assert
            Assert.Equal("foo bar", command.Message);
        }

        [Fact]
        public void ExtractOptions_ThrowsWhenOptionHasNoValue()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Message");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var command = new MockCommand();
            string expectedErrorMessage = "Missing option value for: '/Message'";
            var argsEnumerator = new List<string>() { "/Message" }.GetEnumerator();

            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(command, argsEnumerator), expectedErrorMessage);
        }

        [Fact]
        public void ExtractOptions_ParsesBoolOptionsAsTrueIfPresent()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("IsWorking");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var command = new MockCommand();
            var argsEnumerator = new List<string>() { "-IsWorking" }.GetEnumerator();
            
            // Act
            parser.ExtractOptions(command, argsEnumerator);
            
            // Assert
            Assert.True(command.IsWorking);
        }

        [Fact]
        public void ExtractOptions_ParsesBoolOptionsAsFalseIfFollowedByDash()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();
            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("IsWorking");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var command = new MockCommand();
            var argsEnumerator = new List<string>() { "-IsWorking-" }.GetEnumerator();
            
            // Act
            parser.ExtractOptions(command, argsEnumerator);
            
            // Assert
            Assert.False(command.IsWorking);
        }

        [Fact]
        public void ExtractOptions_ThrowsIfUnableToConvertType()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();

            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            var mockOptionAttribute = new OptionAttribute("Mock Option");
            var mockPropertyInfo = typeof(MockCommand).GetProperty("Count");
            MockCommandOptions.Add(mockOptionAttribute, mockPropertyInfo);

            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var command = new MockCommand();
            string expectedErrorMessage = "Invalid option value: '/Count null'";
            var argsEnumerator = new List<string>() { "/Count", "null" }.GetEnumerator();
            
            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(command, argsEnumerator), expectedErrorMessage);
        }

        [Fact]
        public void ExtractOptions_ThrowsIfCommandHasNoProperties()
        {
            // Arrange
            var cmdMgr = new Mock<ICommandManager>();

            var MockCommandOptions = new Dictionary<OptionAttribute, PropertyInfo>();
            cmdMgr.Setup(cm => cm.GetCommandOptions(It.IsAny<ICommand>())).Returns(MockCommandOptions);
            CommandLineParser parser = new CommandLineParser(cmdMgr.Object);
            var command = new MockCommand();
            string expectedErrorMessage = "Unknown option: '/Count'";
            var argsEnumerator = new List<string>() { "/Count", "null" }.GetEnumerator();

            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(command, argsEnumerator), expectedErrorMessage);
        }

        [Fact]
        public void ExtractOptions_ThrowsIfCommandOptionIsAmbigious()
        {
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
            var command = new MockCommand();
            string expectedErrorMessage = "Ambiguous option 'Co'. Possible values: Count Counter.";
            var argsEnumerator = new List<string>() { "/Co", "null" }.GetEnumerator();

            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(command, argsEnumerator), expectedErrorMessage);
        }

        [Fact]
        public void ExtractOptions_ThrowsIfCommandOptionDoesNotExist()
        {
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
            var command = new MockCommand();
            string expectedErrorMessage = "Unknown option: '/37264752DOESNOTEXIST!!'";
            var argsEnumerator = new List<string>() { "/37264752DOESNOTEXIST!!", "false" }.GetEnumerator();

            // Act & Assert
            ExceptionAssert.Throws<CommandLineException>(() => parser.ExtractOptions(command, argsEnumerator), expectedErrorMessage);
        }

        [Fact]
        public void ExtractOptionAddsValuesToListCommand()
        {
            // Arrange
            var cmdMgr = new CommandManager();
            var command = new MockCommandWithMultiple();
            var arguments = "/ListProperty Val1 /RegularProp RegularPropValue /ListProperty Val2  /ListProperty Val3";
            var parser = new CommandLineParser(cmdMgr);

            // Act
            parser.ExtractOptions(command, arguments.Split().AsEnumerable().GetEnumerator());

            // Assert
            Assert.Equal(command.RegularProp, "RegularPropValue");
            Assert.Equal(command.ListProperty.Count, 3);
            Assert.Equal(command.ListProperty[0], "Val1");
            Assert.Equal(command.ListProperty[1], "Val2");
            Assert.Equal(command.ListProperty[2], "Val3");
        }

        [Fact]
        public void ExtractOptionsSplitsValueBySemiColorForCollectionOption()
        {
            // Arrange
            var cmdMgr = new CommandManager();
            var command = new MockCommandWithMultiple();
            var arguments = "/ListProperty Val1 /RegularProp RegularPropValue /ListProperty Val2;Val3;Val4  /ListProperty Val5";
            var parser = new CommandLineParser(cmdMgr);

            // Act
            parser.ExtractOptions(command, arguments.Split().AsEnumerable().GetEnumerator());

            // Assert
            Assert.Equal(command.RegularProp, "RegularPropValue");
            Assert.Equal(command.ListProperty.Count, 5);
            Assert.Equal(command.ListProperty[0], "Val1");
            Assert.Equal(command.ListProperty[1], "Val2");
            Assert.Equal(command.ListProperty[2], "Val3");
            Assert.Equal(command.ListProperty[3], "Val4");
            Assert.Equal(command.ListProperty[4], "Val5");
        }

        [Fact]
        public void AssignValueAssignsPrimitiveValues()
        {
            // Arrange
            var cmd = new AssignValueType();
            var property = cmd.GetType().GetProperty("PrimitiveProperty");
            var value = "Qux";

            // Act
            CommandLineParser.AssignValue(cmd, property, "PrimitiveProperty", value);

            // Assert
            Assert.Equal("Qux", cmd.PrimitiveProperty);
        }

        [Fact]
        public void AssignValueAddsToCollection()
        {
            // Arrange
            var cmd = new AssignValueType();
            var property = cmd.GetType().GetProperty("DictionaryProperty");
            var value = "MyKey=MyTestValue";

            // Act
            CommandLineParser.AssignValue(cmd, property, "DictionaryProperty", value);

            // Assert
            Assert.Equal("MyTestValue", cmd.DictionaryProperty["MyKey"]);
        }

        [Theory]
        [InlineData("Quiet", TestEnum.Quiet)]
        [InlineData("di", TestEnum.Diagnostic)]
        [InlineData("Norm", TestEnum.Normal)]
        public void AssignValueSetsEnumProperty(string value, TestEnum expectedValue)
        {
            // Arrange
            var cmd = new AssignValueType();
            var property = cmd.GetType().GetProperty("TestEnum");

            // Act
            CommandLineParser.AssignValue(cmd, property, "TestEnum", value);

            // Assert
            Assert.Equal(expectedValue, cmd.TestEnum);
        }

        [Fact]
        public void AssignValueThrowsIfPartialEnumValueIsAmbigious()
        {
            // Arrange
            var cmd = new AssignValueType();
            var property = cmd.GetType().GetProperty("TestEnum");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(() => CommandLineParser.AssignValue(cmd, property, "TestEnum", "d"),
                "Ambiguous option 'd'. Possible values: Diagnostic Detailed.");
        }

        private class MockCommand : ICommand
        {
            private readonly List<string> _arguments = new List<string>();

            public IList<string> Arguments
            {
                get
                {
                    return _arguments;
                }
            }

            public string Message { get; set; }

            public bool IsWorking { get; set; }

            public int Count { get; set; }

            public int Counter { get; set; }

            public void Execute()
            {
                throw new NotImplementedException();
            }

            public CommandAttribute CommandAttribute
            {
                get { return new CommandAttribute("Mock", "Mock Command Desc"); }
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


        private class MockCommandWithMultiple : Command
        {
            private readonly List<string> _listProperty = new List<string>();

            [Option("Regular Option")]
            public string RegularProp { get; set; }

            [Option("List property")]
            public List<string> ListProperty
            {
                get { return _listProperty; }
            }

            public override void ExecuteCommand()
            {
                throw new NotImplementedException();
            }
        }

        public class AssignValueType
        {
            private readonly Dictionary<string, string> _dict = new Dictionary<string, string>();

            public string PrimitiveProperty { get; set; }

            public Dictionary<string, string> DictionaryProperty
            {
                get { return _dict; }
            }

            public TestEnum TestEnum { get; set; }
        }

        public enum TestEnum 
        {
            Normal,
            Diagnostic,
            Detailed,
            Quiet
        }
    }
}
