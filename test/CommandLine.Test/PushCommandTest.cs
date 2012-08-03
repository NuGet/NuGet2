using System;
using Moq;
using NuGet.Commands;
using NuGet.Test.Mocks;
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

        private static ISettings CreateSettings(string defaultPushSource = null)
        {
            var settings = new Mock<ISettings>();
            settings.Setup(s => s.GetValue("config", "DefaultPushSource")).Returns(defaultPushSource);
            return settings.Object;
        }

        private static IPackageSourceProvider CreateSourceProvider()
        {
            var sourceProvider = new Mock<IPackageSourceProvider>();
            return sourceProvider.Object;
        }

        [Theory]
        [InlineData(new object[] { @"X:\test\foobar.symbols.nupkg", @"http://nuget.gw.symbolsource.org/Public/NuGet" })]
        [InlineData(new object[] { @"X:\test\foobar.nupkg", @"https://www.nuget.org" })]
        [InlineData(new object[] { @"", @"https://www.nuget.org" })]
        [InlineData(new object[] { @"test.dll", @"https://www.nuget.org" })]
        public void PushCommandUsesNuGetOrgWhenNoSourceSpecified(string input, string expected)
        {
            var push = new PushCommand(CreateSourceProvider(), CreateSettings());
            Assert.Equal(expected, push.ResolveSource(input));
        }

        [Fact]
        public void PushCommandUsesSourceWhenSpecified()
        {
            const string src = "http://foo/bar";
            var push = new PushCommand(CreateSourceProvider(), CreateSettings()) {Source = src};
            Assert.Equal(src, push.ResolveSource(@"X:\test\foobar.symbols.nupkg"));
        }

        [Fact]
        public void PushCommandUsesConfFileWhenDefaultPushSourceSpecified()
        {
            const string src = "http://foo/bar/baz";
            var push = new PushCommand(CreateSourceProvider(), CreateSettings(src));
            Assert.Equal(src, push.ResolveSource(@"X:\test\foobar.symbols.nupkg"));
        }

        [Fact]
        public void PushCommandUsesSourceWhenSpecifiedEvenWhenSpecifiedAlsoInConfFile()
        {
            const string srcCmdLine = "http://foo/bar/baz1";
            const string srcConfFile = "http://foo/bar/baz2";
            var push = new PushCommand(CreateSourceProvider(), CreateSettings(srcConfFile)) {Source = srcCmdLine};
            Assert.Equal(srcCmdLine, push.ResolveSource(@"X:\test\foobar.symbols.nupkg"));
        }

    }
}
