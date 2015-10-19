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
            settings.Setup(s => s.GetValue("config", "DefaultPushSource", false)).Returns(defaultPushSource);
            return settings.Object;
        }

        private static IPackageSourceProvider CreateSourceProvider()
        {
            var sourceProvider = new Mock<IPackageSourceProvider>();
            return sourceProvider.Object;
        }

        [Theory]
        [InlineData(new object[] { @"X:\test\foobar.symbols.nupkg", @"https://nuget.smbsrc.net/" })]
        [InlineData(new object[] { @"X:\test\foobar.nupkg", @"https://www.nuget.org" })]
        [InlineData(new object[] { @"", @"https://www.nuget.org" })]
        [InlineData(new object[] { @"test.dll", @"https://www.nuget.org" })]
        public void PushCommandUsesNuGetOrgWhenNoSourceSpecified(string input, string expected)
        {
            var push = new PushCommand();
            push.SourceProvider = CreateSourceProvider();
            push.Settings = CreateSettings();
            Assert.Equal(expected, push.ResolveSource(input));
        }

        [Fact]
        public void PushCommandUsesSourceWhenSpecified()
        {
            const string src = "http://foo/bar";
            var push = new PushCommand();
            push.SourceProvider = CreateSourceProvider();
            push.Settings = CreateSettings();
            push.Source = src;
            Assert.Equal(src, push.ResolveSource(@"X:\test\foobar.symbols.nupkg"));
        }

        [Fact]
        public void PushCommandUsesConfFileWhenDefaultPushSourceSpecified()
        {
            const string src = "http://foo/bar/baz";
            var push = new PushCommand();
            push.SourceProvider = CreateSourceProvider();
            push.Settings = CreateSettings(src);
            Assert.Equal(src, push.ResolveSource(@"X:\test\foobar.symbols.nupkg"));
        }

        [Fact]
        public void PushCommandUsesSourceWhenSpecifiedEvenWhenSpecifiedAlsoInConfFile()
        {
            const string srcCmdLine = "http://foo/bar/baz1";
            const string srcConfFile = "http://foo/bar/baz2";
            var push = new PushCommand();
            push.SourceProvider = CreateSourceProvider();
            push.Settings = CreateSettings(srcConfFile);
            push.Source = srcCmdLine;
            Assert.Equal(srcCmdLine, push.ResolveSource(@"X:\test\foobar.symbols.nupkg"));
        }

        [Fact]
        public void PushCommandUsesSourceFromConfigurationDefaultsWhenDefaultPushSourceNotSpecifiedByUserOrInConfigFile()
        {
            // Arrange
            var push = new PushCommand();
            push.SourceProvider = CreateSourceProvider();
            push.Settings = CreateSettings();

            // Set Configuration Defaults
            var mockFileSystem = new MockFileSystem();
            var configurationDefaultsPath = "NuGetDefaults.config";
            mockFileSystem.AddFile(configurationDefaultsPath, @"
<configuration>
     <config>
        <add key='DefaultPushSource' value='http://contoso.com/packages/' />
    </config>
</configuration>");

            ConfigurationDefaults configurationDefaults = new ConfigurationDefaults(mockFileSystem, configurationDefaultsPath);

            // Act & Assert
            Assert.Equal(push.ResolveSource(@"X:\test\foobar.symbols.nupkg", configurationDefaults.DefaultPushSource), "http://contoso.com/packages/");
        }

        [Fact]
        public void PushCommandThrowsAnExceptionWhenPackageFileDoesntExist()
        {
            // Arrange            
            var packageFilename = "non.existant.file.nupkg";

            var push = new PushCommand();
            push.Arguments.Add(packageFilename);
            push.ApiKey = "apikey";
            var expectedErrorMessage = String.Format("File does not exist ({0}).", packageFilename);

            // Act & Assert            
            ExceptionAssert.Throws<CommandLineException>(() => push.Execute(), expectedErrorMessage);
        }

    }
}
