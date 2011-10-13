using NuGet.Commands;
using Xunit;

namespace NuGet.Test.NuGetCommandLine
{
    public class CommandTest
    {
        [Fact]
        public void GetCommandAttributes_ReturnsEmptyIfNoCommandAttributes()
        {
            // Arrange
            var command = new CommandWithBadName();

            // Act and Assert
            Assert.Null(command.CommandAttribute);
        }

        [Fact]
        public void GetCommandAttributes_UsesCommandNameAndDefaultDescriptionIfNoCommandAttributesPresent()
        {
            // Arrange
            var command = new MockWithoutCommandAttributesCommand();

            // Act and Assert
            Assert.Equal(command.CommandAttribute.CommandName, "MockWithoutCommandAttributes");
            Assert.Equal(command.CommandAttribute.Description, "No description was provided for this command.");
        }

        [Fact]
        public void GetCommandAttributes_UsesCommandAttributesIfAvailable()
        {
            // Arrange
            var command = new MockCommandWithCommandAttributes();

            // Act and Assert
            Assert.Equal(command.CommandAttribute.CommandName, "NameFromAttribute");
            Assert.Equal(command.CommandAttribute.Description, "DescFromAttribute");
        }

        private class CommandWithBadName : Command
        {
            public override void ExecuteCommand()
            {

            }
        }

        private class MockWithoutCommandAttributesCommand : Command
        {
            public override void ExecuteCommand()
            {

            }
        }

        [CommandAttribute(commandName: "NameFromAttribute", description: "DescFromAttribute")]
        public class MockCommandWithCommandAttributes : Command
        {
            public override void ExecuteCommand()
            {

            }
        }
    }
}
