using Moq;
using NuGet.Commands;
using Xunit;

namespace NuGet.Test
{
    public class DeleteCommandTest
    {
        [Fact]
        public void DeleteCommandUsesUpApiKeyIfSpecifiedAsAnUnnammedArgument()
        {
            // Arrange
            var guid = "1234-5678-9801-2345";
            var deleteCommand = new DeleteCommand()
            {
                SourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict).Object,
                Settings = new Mock<ISettings>(MockBehavior.Strict).Object
            };
            deleteCommand.Arguments.AddRange(new[] { "NuGet.CommandLine", "1.0", guid });

            // Act
            var result = deleteCommand.GetApiKey("Foo");

            // Assert
            Assert.Equal(guid, result);
        }

        [Fact]
        public void DeleteCommandPrefersApiKeySpecifiedAsANamedArgument()
        {
            // Arrange
            var namedGuid = "2345-9801-5678-1234";
            var unnamedGuid = "1234-5678-9801-2345";
            var deleteCommand = new DeleteCommand() 
            {
                SourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict).Object,
                Settings = new Mock<ISettings>(MockBehavior.Strict).Object,
                ApiKey = namedGuid        
            };

            deleteCommand.Arguments.AddRange(new[] { "NuGet.CommandLine", "1.0", unnamedGuid });

            // Act
            var result = deleteCommand.GetApiKey("Foo");

            // Assert
            Assert.Equal(namedGuid, result);
        }
    }
}
