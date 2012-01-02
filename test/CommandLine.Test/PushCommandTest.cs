using NuGet.Commands;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PushCommandTest
    {
        [Theory]
        [InlineData(new object[] { @"X:\test\*.nupkg", @"X:\test\*.nupkg" })]
        [InlineData(new object[] { @"X:\test\**", @"X:\test\**\*.nupkg" })]
        [InlineData(new object[] { @"X:\test\*", @"X:\test\*.nupkg" })]
        [InlineData(new object[] { @"X:\test\", @"X:\test\" })]
        [InlineData(new object[] { @"X:\test\Foo.nupkg", @"X:\test\Foo.nupkg" })]
        public void EnsurePackageExtensionAppendsExtension(string input, string expected)
        {
            // Act
            var actual = PushCommand.EnsurePackageExtension(input);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
