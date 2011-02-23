using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Commands;

namespace NuGet.Test.NuGetCommandLine {
    [TestClass]
    public class CommandTest {
        [TestMethod]
        public void GetCommandAttributes_ReturnsEmptyIfNoCommandAttributes() {
            // Arrange
            var command = new CommandWithBadName();

            // Act and Assert
            Assert.IsNull(command.CommandAttribute);
        }

        [TestMethod]
        public void GetCommandAttributes_UsesCommandNameAndDefaultDescriptionIfNoCommandAttributesPresent() {
            // Arrange
            var command = new MockWithoutCommandAttributesCommand();

            // Act and Assert
            Assert.AreEqual(command.CommandAttribute.CommandName, "MockWithoutCommandAttributes");
            Assert.AreEqual(command.CommandAttribute.Description, "No description was provided for this command.");
        }

        [TestMethod]
        public void GetCommandAttributes_UsesCommandAttributesIfAvailable() {
            // Arrange
            var command = new MockCommandWithCommandAttributes();
            
            // Act and Assert
            Assert.AreEqual(command.CommandAttribute.CommandName, "NameFromAttribute");
            Assert.AreEqual(command.CommandAttribute.Description, "DescFromAttribute");
        }

        private class CommandWithBadName : Command {
            public override void ExecuteCommand() {

            }
        }

        private class MockWithoutCommandAttributesCommand : Command {
            public override void ExecuteCommand() {

            }
        }

        [CommandAttribute(commandName: "NameFromAttribute", description: "DescFromAttribute")]
        public class MockCommandWithCommandAttributes : Command {
            public override void ExecuteCommand() {

            }
        }
    }
}
