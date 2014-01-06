using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.NuGetCommandLine.Commands
{
    public class SourcesCommandTest
    {
        [Fact]
        public void ProvidingNoArgumentListsPackageSources()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(new[] { new PackageSource("FirstSource", "FirstName") });
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
            };

            var console = new MockConsole();

            string expectedText =
@"Registered Sources:
  1.  FirstName [Enabled]
      FirstSource
";

            sourceCommand.Console = console;

            // Act
            sourceCommand.ExecuteCommand();

            // Assert
            Assert.Equal(expectedText, console.Output);
        }

        [Fact]
        public void ProvidingListArgumentListsPackageSources()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(new[] { new PackageSource("FirstSource", "FirstName", isEnabled: false) });
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object
            };
            sourceCommand.Arguments.Add("list");

            var console = new MockConsole();

            string expectedText =
@"Registered Sources:
  1.  FirstName [Disabled]
      FirstSource
";

            sourceCommand.Console = console;

            // Act
            sourceCommand.ExecuteCommand();

            // Assert
            Assert.Equal(expectedText, console.Output);
        }

        [Fact]
        public void SpecifyingFormatShortSwitchesNugetSourcesListOutputToScriptParsableOutput()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(new[]
                {
                    new PackageSource("DisabledSourceUri", "FirstName", isEnabled: false),
                    new PackageSource("FirstEnabledSourceUri", "SecondName", isEnabled: true),
                    new PackageSource("SecondEnabledSourceUri", "ThirdName", isEnabled: true),
                    new PackageSource("OfficialDisabledSourceUri", "FourthName", isEnabled: false, isOfficial: true), 
                    new PackageSource("OfficialEnabledSourceUri", "FifthName", isEnabled: true, isOfficial: true), 
                });
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object
            };
            sourceCommand.Arguments.Add("list");
            sourceCommand.Format = SourcesListFormat.Short;

            var console = new MockConsole();

            string expectedText =
@"D DisabledSourceUri
E FirstEnabledSourceUri
E SecondEnabledSourceUri
DO OfficialDisabledSourceUri
EO OfficialEnabledSourceUri
";

            sourceCommand.Console = console;

            // Act
            sourceCommand.ExecuteCommand();

            // Assert
            Assert.Equal(expectedText, console.Output);
        }

        [Theory]
        [InlineData(new object[] { null })]
        [InlineData(new object[] { "" })]
        public void AddCommandThrowsIfNameIsNullOrEmpty(string name)
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = name
            };
            sourceCommand.Arguments.Add("ADD");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "The name specified cannot be empty. Please provide a valid name.");
        }

        [Fact]
        public void AddCommandThrowsIfNameIsAll()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "All"
            };
            sourceCommand.Arguments.Add("ADD");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "Package source name 'All' is a reserved name.");
        }

        [Theory]
        [InlineData(new object[] { null })]
        [InlineData(new object[] { "" })]
        public void AddCommandThrowsIfSourceIsNullOrEmpty(string source)
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "Test",
                Source = source
            };
            sourceCommand.Arguments.Add("ADD");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "The source specified cannot be empty. Please provide a valid source.");
        }

        [Theory]
        [InlineData(new object[] { @"C:\User<>\chars" })]
        [InlineData(new object[] { @"ftp:\\not-nuget.org\blah" })]
        public void AddCommandThrowsIfSourceIsInvalid(string source)
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "Test",
                Source = source
            };
            sourceCommand.Arguments.Add("ADD");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "The source specified is invalid. Please provide a valid source.");
        }

        [Fact]
        public void AddCommandThrowsIfNameAlreadyExists()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(s => s.LoadPackageSources())
                                 .Returns(new[] { new PackageSource("http://TestSource", "TestName") });
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "TestName",
                Source = "http://nuget.org"
            };
            sourceCommand.Arguments.Add("ADD");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "The name specified has already been added to the list of available package sources. Please provide a unique name.");
        }

        [Fact]
        public void AddCommandThrowsIfSourceAlreadyExists()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(s => s.LoadPackageSources())
                                 .Returns(new[] { new PackageSource("http://TestSource", "TestName") });
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "TestName1",
                Source = "http://TestSource"
            };
            sourceCommand.Arguments.Add("ADD");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "The source specified has already been added to the list of available package sources. Please provide a unique source.");
        }

        [Theory]
        [InlineData(new object[] { null, "password" })]
        [InlineData(new object[] { "", "password" })]
        [InlineData(new object[] { "user1", null })]
        [InlineData(new object[] { "user1", "" })]
        public void AddCommandThrowsIfOnlyOneOfUsernameOrPasswordIsSpecified(string userName, string password)
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "TestName",
                Source = "http://TestSource",
                UserName = userName,
                Password = password
            };
            sourceCommand.Arguments.Add("ADD");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "Both UserName and Password must be specified.");
        }

        [Fact]
        public void AddCommandAddsSourceToSourceProvider()
        {
            // Arrange
            var expectedSources = new[] { new PackageSource("http://TestSource", "TestName"), new PackageSource("http://new-source", "new-source-name") };
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(s => s.LoadPackageSources())
                                 .Returns(new[] { new PackageSource("http://TestSource", "TestName") });
            packageSourceProvider.Setup(s => s.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                .Callback((IEnumerable<PackageSource> source) => Assert.Equal(expectedSources, source));
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "new-source-name",
                Source = "http://new-source"
            };
            sourceCommand.Arguments.Add("add");
            sourceCommand.Console = new MockConsole();

            // Act 
            sourceCommand.ExecuteCommand();

            // Assert
            packageSourceProvider.Verify();
        }

        [Fact]
        public void AddCommandAddsSourceToSourceProviderWithPasswordInClearTextWhenStorePasswordInClearTextIsTrue()
        {
            // Arrange
            var expectedSources = new[] { new PackageSource("http://TestSource", "TestName"), new PackageSource("http://new-source", "new-source-name") { IsPasswordClearText = true } };
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(s => s.LoadPackageSources())
                                 .Returns(new[] { new PackageSource("http://TestSource", "TestName") });
            packageSourceProvider.Setup(s => s.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                .Callback((IEnumerable<PackageSource> source) => Assert.Equal(expectedSources, source)).Verifiable();
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "new-source-name",
                Source = "http://new-source",
                StorePasswordInClearText = true
            };
            sourceCommand.Arguments.Add("add");
            sourceCommand.Console = new MockConsole();

            // Act 
            sourceCommand.ExecuteCommand();

            // Assert
            packageSourceProvider.Verify();
        }

        [Theory]
        [InlineData(new object[] { null })]
        [InlineData(new object[] { "" })]
        public void RemoveCommandThrowsIfSourceIsNullOrEmpty(string source)
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = source,
                Source = "Source"
            };
            sourceCommand.Arguments.Add("remove");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "The name specified cannot be empty. Please provide a valid name.");
        }

        [Fact]
        public void RemoveCommandThrowsIfSourceDoesNotExist()
        {
            // Arrange
            var sources = new[] { new PackageSource("Abcd") };
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "efgh",
            };
            sourceCommand.Arguments.Add("remove");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "Unable to find any package source(s) matching name: efgh.");
        }

        [Fact]
        public void RemoveCommandRemovesMatchingSources()
        {
            // Arrange
            var sources = new[] { new PackageSource("Abcd"), new PackageSource("EFgh"), new PackageSource("pqrs") };
            var expectedSource = new[] { new PackageSource("Abcd"), new PackageSource("pqrs") };
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            packageSourceProvider.Setup(c => c.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                                 .Callback((IEnumerable<PackageSource> src) => Assert.Equal(expectedSource, src))
                                 .Verifiable();
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "efgh",
            };
            sourceCommand.Arguments.Add("remove");
            sourceCommand.Console = new MockConsole();

            // Act 
            sourceCommand.ExecuteCommand();

            // Assert
            packageSourceProvider.Verify();
        }

        [Theory]
        [InlineData(new object[] { null })]
        [InlineData(new object[] { "" })]
        public void UpdateCommandThrowsIfSourceIsNullOrEmpty(string source)
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = source,
                Source = "Source"
            };
            sourceCommand.Arguments.Add("update");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "The name specified cannot be empty. Please provide a valid name.");
        }

        [Fact]
        public void UpdateCommandThrowsIfNameDoesNotExist()
        {
            // Arrange
            var sources = new[] { new PackageSource("Abcd") };
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "efgh",
            };
            sourceCommand.Arguments.Add("update");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "Unable to find any package source(s) matching name: efgh.");
        }

        [Fact]
        public void UpdateCommandThrowsIfSourceIsInvalid()
        {
            // Arrange
            var sources = new[] { new PackageSource("Abcd"), new PackageSource("pqrs") };
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "Abcd",
                Source = "http:\\bad-url"
            };
            sourceCommand.Arguments.Add("update");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "The source specified is invalid. Please provide a valid source.");
        }

        [Fact]
        public void UpdateCommandThrowsIfSourceAlreadyExists()
        {
            // Arrange
            var sources = new[] { new PackageSource("Abcd"), new PackageSource("http://test-source", "source") };
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "Abcd",
                Source = "http://test-source"
            };
            sourceCommand.Arguments.Add("update");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand,
                "The source specified has already been added to the list of available package sources. Please provide a unique source.");
        }

        [Theory]
        [InlineData(new object[] { null, "password" })]
        [InlineData(new object[] { "", "password" })]
        [InlineData(new object[] { "user1", null })]
        [InlineData(new object[] { "user1", "" })]
        public void UpdateThrowsIfOnlyOneOfUsernameOrPasswordIsSpecified(string userName, string password)
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            packageSourceProvider.Setup(s => s.LoadPackageSources()).Returns(new[] { new PackageSource("http://testsource") });
            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "http://TestSource",
                UserName = userName,
                Password = password
            };
            sourceCommand.Arguments.Add("UPDATE");

            // Act and Assert
            ExceptionAssert.Throws<CommandLineException>(sourceCommand.ExecuteCommand, "Both UserName and Password must be specified.");
        }

        [Fact]
        public void UpdateCommandPreservesOrder()
        {
            // Arrange
            var sources = new[] { new PackageSource("First"), new PackageSource("Abcd"), new PackageSource("http://test-source", "source") };
            var expectedSources = new[] { new PackageSource("First"), new PackageSource("http://abcd-source", "Abcd"), 
                                          new PackageSource("http://test-source", "source") };
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            packageSourceProvider.Setup(c => c.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                                 .Callback((IEnumerable<PackageSource> actualSources) => Assert.Equal(expectedSources, actualSources))
                                 .Verifiable();

            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "Abcd",
                Source = "http://abcd-source"
            };
            sourceCommand.Arguments.Add("update");
            sourceCommand.Console = new MockConsole();

            // Act 
            sourceCommand.ExecuteCommand();

            // Assert
            packageSourceProvider.Verify();
        }

        [Fact]
        public void UpdateCommandStoresUsernameAndPassword()
        {
            // Arrange
            string userName = "UserName";
            string password = "test-pass";
            var sources = new[] { new PackageSource("First"), new PackageSource("Abcd"), new PackageSource("http://test-source", "source") };
            var expectedSources = new[] { new PackageSource("First"), new PackageSource("http://abcd-source", "Abcd"), 
                                          new PackageSource("http://test-source", "source") };
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            packageSourceProvider.Setup(c => c.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                                 .Callback((IEnumerable<PackageSource> actualSources) =>
                                 {
                                     Assert.Equal(expectedSources, actualSources);
                                     var s = actualSources.ElementAt(1);
                                     Assert.Equal(userName, s.UserName);
                                     Assert.Equal(password, s.Password);
                                     Assert.False(s.IsPasswordClearText);
                                 })
                                 .Verifiable();

            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "Abcd",
                Source = "http://abcd-source",
                UserName = userName,
                Password = password
            };
            sourceCommand.Arguments.Add("update");
            sourceCommand.Console = new MockConsole();

            // Act 
            sourceCommand.ExecuteCommand();

            // Assert
            packageSourceProvider.Verify();
        }

        [Fact]
        public void UpdateCommandStoresPasswordInClearTextWhenStorePasswordInClearTextIsTrue()
        {
            // Arrange
            string userName = "UserName";
            string password = "test-pass";
            var sources = new[] { new PackageSource("First") { IsPasswordClearText = true }, new PackageSource("Abcd") { IsPasswordClearText = true }, new PackageSource("http://test-source", "source") { IsPasswordClearText = true } };
            var expectedSources = new[] { new PackageSource("First") { IsPasswordClearText = true }, new PackageSource("http://abcd-source", "Abcd") { IsPasswordClearText = true }, 
                                          new PackageSource("http://test-source", "source")  { IsPasswordClearText = true }};
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(c => c.LoadPackageSources()).Returns(sources);
            packageSourceProvider.Setup(c => c.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                                 .Callback((IEnumerable<PackageSource> actualSources) =>
                                 {
                                     Assert.Equal(expectedSources, actualSources);
                                     var s = actualSources.ElementAt(1);
                                     Assert.Equal(userName, s.UserName);
                                     Assert.Equal(password, s.Password);
                                     Assert.True(s.IsPasswordClearText);
                                 })
                                 .Verifiable();

            var sourceCommand = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object,
                Name = "Abcd",
                Source = "http://abcd-source",
                UserName = userName,
                Password = password,
                StorePasswordInClearText = true
            };
            sourceCommand.Arguments.Add("update");
            sourceCommand.Console = new MockConsole();

            // Act 
            sourceCommand.ExecuteCommand();

            // Assert
            packageSourceProvider.Verify();
        }

        [Fact]
        public void EnableCommandWithNoNameSetThrows()
        {
            // Arrange
            var command = CreateCommand();
            command.Arguments.Add("Enable");

            // Act & Assert
            Exception exception = Assert.Throws<CommandLineException>(new Assert.ThrowsDelegate(command.ExecuteCommand));
            Assert.NotNull(exception);
            Assert.Equal("The name specified cannot be empty. Please provide a valid name.", exception.Message);
        }

        [Fact]
        public void DisableCommandWithNoNameSetThrows()
        {
            // Arrange
            var command = CreateCommand();
            command.Arguments.Add("Disable");

            // Act & Assert
            Exception exception = Assert.Throws<CommandLineException>(new Assert.ThrowsDelegate(command.ExecuteCommand));
            Assert.NotNull(exception);
            Assert.Equal("The name specified cannot be empty. Please provide a valid name.", exception.Message);
        }

        [Fact]
        public void EnableCommandThrowsWhenPackageSourceNameIsNotFound()
        {
            // Arrange
            var command = CreateCommand();
            command.Arguments.Add("Enable");
            command.Name = "abc";

            // Act & Assert
            Exception exception = Assert.Throws<CommandLineException>(new Assert.ThrowsDelegate(command.ExecuteCommand));
            Assert.NotNull(exception);
            Assert.Equal("Unable to find any package source(s) matching name: abc.", exception.Message);
        }

        [Fact]
        public void DisableCommandThrowsWhenPackageSourceNameIsNotFound()
        {
            // Arrange
            var command = CreateCommand();
            command.Arguments.Add("Disable");
            command.Name = "abc";

            // Act & Assert
            Exception exception = Assert.Throws<CommandLineException>(new Assert.ThrowsDelegate(command.ExecuteCommand));
            Assert.NotNull(exception);
            Assert.Equal("Unable to find any package source(s) matching name: abc.", exception.Message);
        }

        [Fact]
        public void EnableCommandEnableDisabledSourcesCorrectly()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            packageSourceProvider.Setup(s => s.LoadPackageSources()).Returns(new[] { new PackageSource("Two") { IsEnabled = false } });
            packageSourceProvider.Setup(s => s.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                                 .Callback((IEnumerable<PackageSource> sources) => Assert.Equal(new[] { new PackageSource("Two") { IsEnabled = true } }, sources))
                                 .Verifiable();

            var command = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object
            };
            command.Arguments.Add("Enable");
            command.Name = "two";
            command.Console = new Mock<IConsole>().Object;

            // Act
            command.ExecuteCommand();

            // Assert
            packageSourceProvider.Verify();
        }

        [Fact]
        public void EnableCommandDoNotAffectPackageSourcesThatAreAlreadyEnabled()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var expectedSources = new[] 
                                  { 
                                    new PackageSource("onesource", "one") { IsEnabled = true } ,
                                    new PackageSource("twosource", "two") { IsEnabled = false } ,
                                    new PackageSource("threesource", "three") { IsEnabled = true } ,
                                  };

            packageSourceProvider.Setup(s => s.LoadPackageSources()).Returns(
                new[] 
                { 
                    new PackageSource("onesource", "one") { IsEnabled = true } ,
                    new PackageSource("twosource", "two") { IsEnabled = false } ,
                    new PackageSource("threesource", "three") { IsEnabled = true } ,
                }
            );
            packageSourceProvider.Setup(s => s.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                                 .Callback((IEnumerable<PackageSource> sources) => Assert.Equal(expectedSources, sources))
                                 .Verifiable();

            var command = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object
            };
            command.Arguments.Add("Enable");
            command.Name = "one";
            command.Console = new Mock<IConsole>().Object;

            // Act
            command.ExecuteCommand();

            // Assert
            packageSourceProvider.Verify();
        }

        [Fact]
        public void DisableCommandDisablePackageSourcesCorrectly()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var expectedSources = new[] 
                                  { 
                                    new PackageSource("onesource", "one") { IsEnabled = true } ,
                                    new PackageSource("twosource", "two") { IsEnabled = false } ,
                                    new PackageSource("threesource", "three") { IsEnabled = false } ,
                                  };

            packageSourceProvider.Setup(s => s.LoadPackageSources()).Returns(
                new[] 
                { 
                    new PackageSource("onesource", "one") { IsEnabled = true } ,
                    new PackageSource("twosource", "two") { IsEnabled = false } ,
                    new PackageSource("threesource", "three") { IsEnabled = true } ,
                }
            );
            packageSourceProvider.Setup(s => s.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                                 .Callback((IEnumerable<PackageSource> sources) => Assert.Equal(expectedSources, sources))
                                 .Verifiable();

            var command = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object
            };
            command.Arguments.Add("Disable");
            command.Name = "three";
            command.Console = new Mock<IConsole>().Object;

            // Act
            command.ExecuteCommand();

            // Assert
            packageSourceProvider.Verify();
        }

        [Fact]
        public void DisableCommandDoNotAffectPackageSourcesThatAreAlreadyDisabled()
        {
            // Arrange
            var packageSourceProvider = new Mock<IPackageSourceProvider>(MockBehavior.Strict);
            var expectedSources = new[] 
                                  { 
                                    new PackageSource("onesource", "one") { IsEnabled = true } ,
                                    new PackageSource("twosource", "two") { IsEnabled = false } ,
                                    new PackageSource("threesource", "three") { IsEnabled = true } ,
                                  };

            packageSourceProvider.Setup(s => s.LoadPackageSources()).Returns(
                new[] 
                { 
                    new PackageSource("onesource", "one") { IsEnabled = true } ,
                    new PackageSource("twosource", "two") { IsEnabled = false } ,
                    new PackageSource("threesource", "three") { IsEnabled = true } ,
                }
            );
            packageSourceProvider.Setup(s => s.SavePackageSources(It.IsAny<IEnumerable<PackageSource>>()))
                                 .Callback((IEnumerable<PackageSource> sources) => Assert.Equal(expectedSources, sources))
                                 .Verifiable();

            var command = new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object
            };
            command.Arguments.Add("Disable");
            command.Name = "two";
            command.Console = new Mock<IConsole>().Object;

            // Act
            command.ExecuteCommand();

            // Assert
            packageSourceProvider.Verify();
        }

        private SourcesCommand CreateCommand()
        {
            var packageSourceProvider = new Mock<IPackageSourceProvider>();
            return new SourcesCommand()
            {
                SourceProvider = packageSourceProvider.Object
            };
        }

    }
}