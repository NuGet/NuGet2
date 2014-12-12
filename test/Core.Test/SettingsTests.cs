using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using Xunit;
using NuGet.Test.Mocks;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class SettingsTests
    {
        [Fact]
        public void CallingCtorWithNullFileSystemWithThrowException()
        {
            // Act & Assert
            ExceptionAssert.Throws<ArgumentNullException>(() => new Settings(null));
        }

        [Fact]
        public void WillGetConfigurationFromSpecifiedPath()
        {
            // Arrange 
            const string configFile = "NuGet.Config";
            var mockFileSystem = new Mock<IFileSystem>();
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
        <add Key='key2' Value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(configFile)).Returns(config.AsStream());
            mockFileSystem.Setup(m => m.FileExists(configFile)).Returns(true);
            mockFileSystem.Setup(m => m.GetFullPath(configFile)).Returns(configFile);

            // Act
            new Settings(mockFileSystem.Object);

            // Assert 
            mockFileSystem.Verify(x => x.OpenFile(configFile), Times.Once(), "File was not read");
        }

        [Fact]
        public void CallingGetValuesWithNullSectionWillThrowException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValues(null));
        }

        [Fact]
        public void CallingGetValueWithNullSectionWillThrowException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValue(null, "SomeKey"));
        }

        [Fact]
        public void CallingGetValueWithNullKeyWillThrowException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValue("SomeSection", null));
        }

        [Fact]
        public void CallingCtorWithMalformedConfigThrowsException()
        {
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<configuration><sectionName></configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);

            // Act & Assert
            ExceptionAssert.Throws<System.Xml.XmlException>(() => new Settings(mockFileSystem));

        }

        [Fact]
        public void UserSetting_CallingGetValuesWithNonExistantSectionReturnsEmpty()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            
            string config = @"<configuration></configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result = settings.GetValues("DoesNotExisit");

            // Assert 
            Assert.Empty(result);
        }

        [Fact]
        public void CallingGetValuesWithSectionWithInvalidAddItemsThrows()
        {
            // Arrange
            var config = @"
<configuration>
    <SectionName>
        <add Key='key2' Value='value2' />
    </SectionName>
</configuration>";
            var nugetConfigPath = "NuGet.Config";
            var mockFileSystem = new MockFileSystem(@"x:\test");
            mockFileSystem.AddFile(nugetConfigPath, config.AsStream());
            Settings settings = new Settings(mockFileSystem);

            // Act and Assert
            ExceptionAssert.Throws<InvalidDataException>(() => settings.GetValues("SectionName"), @"Unable to parse config file 'x:\test\NuGet.Config'.");
        }

        [Fact]
        public void GetValuesThrowsIfSettingsIsMissingKeys()
        {
            var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
<packageSources>
<add key="""" value=""C:\Temp\Nuget"" />
</packageSources>
<activePackageSource>
<add key=""test2"" value=""C:\Temp\Nuget"" />
</activePackageSource>
</configuration>";
            var nugetConfigPath = "NuGet.Config";
            var mockFileSystem = new MockFileSystem(@"x:\test");
            mockFileSystem.AddFile(nugetConfigPath, config.AsStream());
            Settings settings = new Settings(mockFileSystem);

            // Act and Assert
            ExceptionAssert.Throws<InvalidDataException>(() => settings.GetValues("packageSources"), @"Unable to parse config file 'x:\test\NuGet.Config'.");
        }

        [Fact]
        public void CallingGetValuesWithoutSectionReturnsEmptyList()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
        <add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result = settings.GetValues("NotTheSectionName");

            // Arrange 
            Assert.Empty(result);
        }

        [Fact]
        public void CallingGetValueWithoutSectionReturnsNull()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
        <add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result = settings.GetValue("NotTheSectionName", "key1");

            // Arrange 
            Assert.Null(result);
        }

        [Fact]
        public void CallingGetValueWithSectionButNoValidKeyReturnsNull()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
        <add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result = settings.GetValue("SectionName", "key3");

            // Assert 
            Assert.Null(result);
        }

        [Fact]
        public void CallingGetValuesWithSectionReturnsDictionary()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
        <add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result = settings.GetValues("SectionName");

            // Assert 
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void CallingGetValueWithSectionAndKeyReturnsValue()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
    </SectionName>
    <SectionNameTwo>
        <add key='key2' value='value2' />
    </SectionNameTwo>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result1 = settings.GetValue("SectionName", "key1");
            var result2 = settings.GetValue("SectionNameTwo", "key2");

            // Assert 
            Assert.Equal("value1", result1);
            Assert.Equal("value2", result2);
        }

        [Fact]
        public void CallingSetValueWithEmptySectionNameThrowsException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValue("", "SomeKey", "SomeValue"));
        }

        [Fact]
        public void CallingSetValueWithEmptyKeyThrowsException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValue("SomeKey", "", "SomeValue"));
        }

        [Fact]
        public void CallingSetValueWillAddSectionIfItDoesNotExist()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetValue("NewSectionName", "key", "value");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
  <NewSectionName>
    <add key=""key"" value=""value"" />
  </NewSectionName>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingSetValueWillAddToSectionIfItExist()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetValue("SectionName", "keyTwo", "valueTwo");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
    <add key=""keyTwo"" value=""valueTwo"" />
  </SectionName>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingSetValueWillOverrideValueIfKeyExists()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetValue("SectionName", "key", "NewValue");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""NewValue"" />
  </SectionName>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingSetValuesWithEmptySectionThrowsException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("key", "value") };
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValues("", values));
        }

        [Fact]
        public void CallingSetValuesWithNullValuesThrowsException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentNullException>(() => settings.SetValues("Section", null));
        }

        [Fact]
        public void CallingSetValuesWithEmptyKeyThrowsException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("", "value") };
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValues("Section", values));
        }

        [Fact]
        public void CallingSetValuseWillAddSectionIfItDoesNotExist()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("key", "value") };
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetValues("NewSectionName", values);

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
  <NewSectionName>
    <add key=""key"" value=""value"" />
  </NewSectionName>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingSetValuesWillAddToSectionIfItExist()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("keyTwo", "valueTwo") };
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetValues("SectionName", values);

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
    <add key=""keyTwo"" value=""valueTwo"" />
  </SectionName>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingSetValuesWillOverrideValueIfKeyExists()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("key", "NewValue") };
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetValues("SectionName", values);

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""NewValue"" />
  </SectionName>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingSetValuesWilladdValuesInOrder()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""Value"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("key1", "Value1"), 
                                                                    new KeyValuePair<string, string>("key2", "Value2") };
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetValues("SectionName", values);

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""Value"" />
    <add key=""key1"" value=""Value1"" />
    <add key=""key2"" value=""Value2"" />
  </SectionName>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingSetNestedValuesAddsItemsInNestedElement()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            var values = new [] { new KeyValuePair<string, string>("key1", "Value1"), 
                                  new KeyValuePair<string, string>("key2", "Value2") };
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetNestedValues("SectionName", "MyKey", values);

            // Assert
            Assert.Equal(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <MyKey>
      <add key=""key1"" value=""Value1"" />
      <add key=""key2"" value=""Value2"" />
    </MyKey>
  </SectionName>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingSetNestedValuesPreservesOtherKeys()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <MyKey>
      <add key=""key1"" value=""Value1"" />
      <add key=""key2"" value=""Value2"" />
    </MyKey>
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            var values = new[] { new KeyValuePair<string, string>("key3", "Value3"), 
                                  new KeyValuePair<string, string>("key4", "Value4") };
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetNestedValues("SectionName", "MyKey2", values);

            // Assert
            Assert.Equal(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <MyKey>
      <add key=""key1"" value=""Value1"" />
      <add key=""key2"" value=""Value2"" />
    </MyKey>
    <MyKey2>
      <add key=""key3"" value=""Value3"" />
      <add key=""key4"" value=""Value4"" />
    </MyKey2>
  </SectionName>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingSetNestedAppendsValuesToExistingKeys()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <MyKey>
      <add key=""key1"" value=""Value1"" />
      <add key=""key2"" value=""Value2"" />
    </MyKey>
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            var values = new[] { new KeyValuePair<string, string>("key3", "Value3"), 
                                  new KeyValuePair<string, string>("key4", "Value4") };
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetNestedValues("SectionName", "MyKey", values);

            // Assert
            Assert.Equal(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <MyKey>
      <add key=""key1"" value=""Value1"" />
      <add key=""key2"" value=""Value2"" />
      <add key=""key3"" value=""Value3"" />
      <add key=""key4"" value=""Value4"" />
    </MyKey>
  </SectionName>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingDeleteValueWithEmptyKeyThrowsException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.DeleteValue("SomeSection", ""));
        }

        [Fact]
        public void CallingDeleteValueWithEmptySectionThrowsException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.DeleteValue("", "SomeKey"));
        }

        [Fact]
        public void CallingDeleteValueWhenSectionNameDoesntExistReturnsFalse()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);
            
            // Act & Assert
            Assert.False(settings.DeleteValue("SectionDoesNotExists", "SomeKey"));
        }

        [Fact]
        public void CallingDeleteValueWhenKeyDoesntExistThrowsException()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act & Assert
            Assert.False(settings.DeleteValue("SectionName", "KeyDoesNotExist"));
        }

        [Fact]
        public void CallingDeleteValueWithValidSectionAndKeyDeletesTheEntryAndReturnsTrue()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""DeleteMe"" value=""value"" />
    <add key=""keyNotToDelete"" value=""value"" />
  </SectionName>
  <SectionName2>
    <add key=""key"" value=""value"" />
  </SectionName2>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act & Assert
            Assert.True(settings.DeleteValue("SectionName", "DeleteMe"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""keyNotToDelete"" value=""value"" />
  </SectionName>
  <SectionName2>
    <add key=""key"" value=""value"" />
  </SectionName2>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }

        [Fact]
        public void CallingDeleteSectionWithEmptySectionThrowsException()
        {
            // Arrange 
            var mockFileSystem = new MockFileSystem();
            var settings = new Settings(mockFileSystem);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.DeleteSection(""));
        }

        [Fact]
        public void CallingDeleteSectionWhenSectionNameDoesntExistReturnsFalse()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act & Assert
            Assert.False(settings.DeleteSection("SectionDoesNotExists"));
        }

        [Fact]
        public void CallingDeleteSectionWithValidSectionDeletesTheSectionAndReturnsTrue()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""DeleteMe"" value=""value"" />
    <add key=""keyNotToDelete"" value=""value"" />
  </SectionName>
  <SectionName2>
    <add key=""key"" value=""value"" />
  </SectionName2>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act & Assert
            Assert.True(settings.DeleteSection("SectionName"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName2>
    <add key=""key"" value=""value"" />
  </SectionName2>
</configuration>", mockFileSystem.ReadAllText(nugetConfigPath));
        }


        /* Extension Methods for Settings Class */
        [Fact]
        public void UserSettingsExtentions_SetEncryptedValue()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            settings.SetEncryptedValue("SectionName", "key", "NewValue");

            // Assert
            var content = mockFileSystem.ReadAllText(nugetConfigPath);
            Assert.False(content.Contains("NewValue"));
        }

        [Fact]
        public void UserSettingsExtentions_GetEncryptedValue()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            Settings settings = new Settings(mockFileSystem);
            settings.SetEncryptedValue("SectionName", "key", "value");

            // Act
            var result = settings.GetDecryptedValue("SectionName", "key");

            // Assert
            Assert.Equal("value", result);
        }

        [Fact]
        public void UserSettingsExtentions_GetDecryptedValueWithEmptyValueReturnsEmptyString()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result = settings.GetDecryptedValue("SectionName", "key");

            // Assert
            Assert.Equal(String.Empty, result);
        }

        [Fact]
        public void UserSettingsExtentions_GetDecryptedValueWithNoKeyReturnsNull()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result = settings.GetDecryptedValue("SectionName", "NoKeyByThatName");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetConfigSettingReadsValueFromConfigSection()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetValue("config", "foo", false))
                    .Returns("bar")
                    .Verifiable();

            // Act
            var result = settings.Object.GetConfigValue("foo");

            // Assert
            Assert.Equal("bar", result);
            settings.Verify();
        }

        [Fact]
        public void GetConfigSettingReadsEncryptedValueFromConfigSection()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            var value = EncryptionUtility.EncryptString("hello world");
            settings.Setup(s => s.GetValue("config", "foo", false))
                    .Returns(value)
                    .Verifiable();

            // Act
            var result = settings.Object.GetConfigValue("foo", decrypt: true);

            // Assert
            Assert.Equal("hello world", result);
            settings.Verify();
        }

        [Fact]
        public void SetConfigSettingWritesValueToConfigSection()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.SetValue("config", "foo", "bar"))
                    .Verifiable();

            // Act
            settings.Object.SetConfigValue("foo", "bar");

            // Assert
            settings.Verify();
        }

        [Fact]
        public void SetConfigSettingWritesEncryptedValueToConfigSection()
        {
            // Arrange
            string value = null;
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.SetValue("config", "foo", It.IsAny<string>()))
                    .Callback<string, string, string>((_, __, v) => value = v)
                    .Verifiable();

            // Act
            settings.Object.SetConfigValue("foo", "bar", encrypt: true);

            // Assert
            settings.Verify();
            Assert.Equal("bar", EncryptionUtility.DecryptString(value));
        }

        [Fact]
        public void DeleteConfigSettingDeletesValueFromConfigSection()
        {
            // Arrange
            var settings = new Mock<ISettings>(MockBehavior.Strict);
            settings.Setup(s => s.DeleteValue("config", "foo"))
                    .Returns(true)
                    .Verifiable();

            // Act
            bool result = settings.Object.DeleteConfigValue("foo");

            // Assert
            Assert.True(result);
            settings.Verify();
        }

        [Fact]
        public void GetValueIgnoresClearedValues()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""foo"" />
    <clear />
    <add key=""key2"" value=""bar"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result1 = settings.GetValue("SectionName", "Key1");
            var result2 = settings.GetValue("SectionName", "Key2");

            // Assert
            Assert.Null(result1);
            Assert.Equal("bar", result2);
        }

        private void AssertEqualCollections(IList<KeyValuePair<string, string>> actual, string[] expected)
        {
            Assert.Equal(actual.Count, expected.Length/2);
            for (int i=0;i<actual.Count;++i)
            {
                Assert.Equal(expected[2 * i], actual[i].Key);
                Assert.Equal(expected[2 * i + 1], actual[i].Value);
            }
        }

        [Fact]
        public void GetValuesIgnoresClearedValues()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />
    <add key=""key2"" value=""value2"" />
    <clear />
    <add key=""key3"" value=""value3"" />
    <add key=""key4"" value=""value4"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result = settings.GetValues("SectionName");
            

            // Assert
            AssertEqualCollections(result, new [] { "key3", "value3", "key4", "value4"});            
        }

        [Fact]
        public void GetValuesWithIsPathTrue()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"c:\root");
            var nugetConfigPath = "NuGet.Config";
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <!-- values that are relative paths -->
    <add key=""key1"" value=""..\value1"" />
    <add key=""key2"" value=""a\b\c"" />
    <add key=""key3"" value="".\a\b\c"" />

    <!-- values that are not relative paths -->
    <add key=""key4"" value=""c:\value2"" />
    <add key=""key5"" value=""http://value3"" />    
    <add key=""key6"" value=""\\a\b\c"" />
    <add key=""key7"" value=""\a\b\c"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(nugetConfigPath, config);
            Settings settings = new Settings(mockFileSystem);

            // Act
            var result = settings.GetSettingValues("SectionName", isPath: true)
                .Select(v => new KeyValuePair<string, string>(v.Key, v.Value))
                .ToList();

            // Assert
            AssertEqualCollections(
                result, 
                new[] { 
                    "key1", @"c:\root\..\value1",
                    "key2", @"c:\root\a\b\c",
                    "key3", @"c:\root\.\a\b\c",

                    "key4", @"c:\value2",
                    "key5", @"http://value3",
                    "key6", @"\\a\b\c",
                    "key7", @"\a\b\c"
                });
        }


        [Fact]
        public void GetValuesMultipleConfFilesAdditive()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"C:\mockfilesystem\dir1\dir2");
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key3"" value=""value3"" />
    <add key=""key4"" value=""value4"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile("NuGet.Config", config);
            config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />
    <add key=""key2"" value=""value2"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(@"C:\mockfilesystem\dir1\NuGet.Config", config);

            var settings = Settings.LoadDefaultSettings(mockFileSystem, null, null);

            // Act
            var result = settings.GetValues("SectionName");

            // Assert
            AssertEqualCollections(result, new[] {"key1", "value1", "key2", "value2" , "key3", "value3", "key4", "value4" });
        }

        [Fact]
        public void GetValuesMultipleConfFilesClear()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"C:\mockfilesystem\dir1\dir2");
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <clear /> <!-- i.e. ignore values from prior conf files -->
    <add key=""key3"" value=""value3"" />
    <add key=""key4"" value=""value4"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile("NuGet.Config", config);
            config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />
    <add key=""key2"" value=""value2"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(@"C:\mockfilesystem\dir1\NuGet.Config", config);

            var settings = Settings.LoadDefaultSettings(mockFileSystem, null, null);

            // Act
            var result = settings.GetValues("SectionName");
            
            // Assert
            AssertEqualCollections(result, new[] { "key3", "value3", "key4", "value4" });
        }

        [Fact]
        public void GetSettingValuesMultipleConfFilesClear()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"C:\mockfilesystem\dir1\dir2");
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <clear /> <!-- i.e. ignore values from prior conf files -->
    <add key=""key3"" value=""value3"" />
    <add key=""key4"" value=""value4"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile("NuGet.Config", config);
            config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />
    <add key=""key2"" value=""value2"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(@"C:\mockfilesystem\dir1\NuGet.Config", config);

            var settings = Settings.LoadDefaultSettings(mockFileSystem, null, null);

            // Act
            var result = settings.GetSettingValues("SectionName", isPath: false);

            // Assert
            Assert.Equal<SettingValue>(
                new [] {
                    new SettingValue("key3", "value3", isMachineWide: false, priority: 0),
                    new SettingValue("key4", "value4", isMachineWide: false, priority: 0)
                },
                result);
        }

        [Fact]
        public void GetSingleValuesMultipleConfFiles()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"C:\mockfilesystem\dir1\dir2");
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key3"" value=""value3"" />
    <add key=""key4"" value=""value4"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile("NuGet.Config", config);
            config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />
    <add key=""key2"" value=""value2"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(@"C:\mockfilesystem\dir1\NuGet.Config", config);

            var settings = Settings.LoadDefaultSettings(mockFileSystem, null, null);

            // Assert
            Assert.Equal("value4", settings.GetValue("SectionName", "key4"));
            Assert.Equal("value3", settings.GetValue("SectionName", "key3"));
            Assert.Equal("value2", settings.GetValue("SectionName", "key2"));
            Assert.Equal("value1", settings.GetValue("SectionName", "key1"));
        }

        [Fact]
        public void GetSingleValuesMultipleConfFilesWithDupes()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"C:\mockfilesystem\dir1\dir2");
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""LastOneWins1"" />
    <add key=""key2"" value=""LastOneWins2"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(@"NuGet.Config", config);
            config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />
    <add key=""key2"" value=""value2"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(@"C:\mockfilesystem\dir1\NuGet.Config", config);

            var settings = Settings.LoadDefaultSettings(mockFileSystem, null, null);

            // Assert
            Assert.Equal("LastOneWins2", settings.GetValue("SectionName", "key2"));
            Assert.Equal("LastOneWins1", settings.GetValue("SectionName", "key1"));
        }

        [Fact]
        public void GetSingleValuesMultipleConfFilesClear()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"C:\mockfilesystem\dir1\dir2");
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <clear /> <!-- i.e. ignore values from prior conf files -->
    <add key=""key2"" value=""value2"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile("NuGet.Config", config);
            config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />    
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(@"C:\mockfilesystem\dir1\NuGet.Config", config);

            var settings = Settings.LoadDefaultSettings(mockFileSystem, null, null);

            // Assert
            Assert.Equal("value2", settings.GetValue("SectionName", "key2"));
            Assert.Equal(null, settings.GetValue("SectionName", "key1"));
        }        

        [Fact]
        public void GetValueReturnsPathRelativeToConfigWhenPathIsNotRooted()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"x:\mock-directory\");
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""path-key"" value=""foo\bar"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile("nuget.config", config);
            var settings = new Settings(mockFileSystem, "nuget.config");

            // Act
            string result = settings.GetValue("SectionName", "path-key", isPath: true);

            // Assert
            Assert.Equal(@"x:\mock-directory\foo\bar", result);
        }

        [Fact]
        public void GetValuesWithUserSpecifiedDefaultConfigFile()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"C:\mockfilesystem\dir1\dir2");
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key3"" value=""value3"" />
    <add key=""key4"" value=""value4"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile(@"C:\mockfilesystem\dir1\NuGet.Config", config);

            config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />
    <add key=""key2"" value=""value2"" />
  </SectionName>
</configuration>";
            mockFileSystem.AddFile("UserDefinedConfigFile.confg", config);

            var settings = Settings.LoadDefaultSettings(
                mockFileSystem, 
                "UserDefinedConfigFile.confg",
                null);

            // Act
            var result = settings.GetValues("SectionName");

            // Assert
            AssertEqualCollections(result, new[] { "key1", "value1", "key2", "value2", "key3", "value3", "key4", "value4" });
        }

        [Theory]
        [InlineData(@"z:\foo")]
        [InlineData(@"x:\foo\bar\qux")]
        [InlineData(@"\\share\folder\subfolder")]
        public void GetValueReturnsPathWhenPathIsRooted(string value)
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"x:\mock-directory\");
            string config = String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""path-key"" value=""{0}"" />
  </SectionName>
</configuration>", value);
            mockFileSystem.AddFile("nuget.config", config);
            var settings = new Settings(mockFileSystem, "nuget.config");

            // Act
            string result = settings.GetValue("SectionName", "path-key", isPath: true);

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void GetValueReturnsPathRelativeToRootOfConfig()
        {
            // Arrange
            var mockFileSystem = new Mock<MockFileSystem>(@"x:\mock-directory\") { CallBase = true };
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""path-key"" value=""\Blah"" />
  </SectionName>
</configuration>";
            mockFileSystem.Object.AddFile("nuget.config", config);
            var settings = new Settings(mockFileSystem.Object, "nuget.config");

            // Act
            string result = settings.GetValue("SectionName", "path-key", isPath: true);

            // Assert
            Assert.Equal(@"x:\Blah", result);
        }

        [Fact]
        public void GetValueResolvesRelativePaths()
        {
            // Arrange
            var mockFileSystem = new Mock<MockFileSystem>(@"x:\mock-directory\") { CallBase = true };
            mockFileSystem.Setup(f => f.GetFullPath(@"x:\mock-directory\..\Blah")).Returns(@"x:\mock-directory\Qux\Blah").Verifiable();
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""path-key"" value=""..\Blah"" />
  </SectionName>
</configuration>";
            mockFileSystem.Object.AddFile("nuget.config", config);
            var settings = new Settings(mockFileSystem.Object, "nuget.config");

            // Act
            string result = settings.GetValue("SectionName", "path-key", isPath: true);

            // Assert
            mockFileSystem.Verify();
            Assert.Equal(@"x:\mock-directory\Qux\Blah", result);
        }

        // Checks that the correct files are read, in the right order, 
        // when laoding machine wide settings.
        [Fact]
        public void LoadMachineWideSettings()
        {
            // Arrange
            var fileContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";

            var mockFileSystem = new MockFileSystem(@"C:\");

            mockFileSystem.AddFile(
                @"NuGet\a1.config",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\a1.config",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\a2.config",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\a3.xconfig",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\a1.config",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\a2.config",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\a3.xconfig",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\Version\a1.config",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\Version\a2.config",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\Version\a3.xconfig",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\Version\SKU\a1.config",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\Version\SKU\a2.config",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\Version\SKU\a3.xconfig",
                fileContent);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\Version\SKU\Dir\a1.config",
                fileContent);
            
            // Act
            var settings = Settings.LoadMachineWideSettings(
                mockFileSystem, "IDE", "Version", "SKU", "TestDir");

            // Assert
            var files = settings.Select(s => s.ConfigFilePath).ToArray();
            Assert.Equal(
                files,
                new string[] {
                    @"C:\NuGet\Config\IDE\Version\SKU\a1.config",
                    @"C:\NuGet\Config\IDE\Version\SKU\a2.config",
                    @"C:\NuGet\Config\IDE\Version\a1.config",
                    @"C:\NuGet\Config\IDE\Version\a2.config",
                    @"C:\NuGet\Config\IDE\a1.config",
                    @"C:\NuGet\Config\IDE\a2.config",
                    @"C:\NuGet\Config\a1.config",
                    @"C:\NuGet\Config\a2.config"
                });            
        }

        // Tests method GetValue() with machine wide settings.
        [Fact]
        public void GetValueWithMachineWideSettings()
        {
            // Arrange            
            var mockFileSystem = new MockFileSystem(@"C:\");
            mockFileSystem.AddFile(
                @"NuGet\Config\a1.config",
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />
    <add key=""key2"" value=""value2"" />
  </SectionName>
</configuration>");
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\a1.config",
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key2"" value=""value3"" />
    <add key=""key3"" value=""value4"" />
  </SectionName>
</configuration>");
            mockFileSystem.AddFile(
               "user.config",
               @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key3"" value=""user"" />
  </SectionName>
</configuration>");

            var m = new Mock<IMachineWideSettings>();
            m.SetupGet(obj => obj.Settings).Returns(
                Settings.LoadMachineWideSettings(mockFileSystem, "IDE", "Version", "SKU"));

            // Act
            var settings = Settings.LoadDefaultSettings(
                mockFileSystem, 
                "user.config",
                m.Object);

            // Assert
            var v = settings.GetValue("SectionName", "key1");
            Assert.Equal("value1", v);

            // the value in NuGet\Config\IDE\a1.config overrides the value in
            // NuGet\Config\a1.config
            v = settings.GetValue("SectionName", "key2");
            Assert.Equal("value3", v);

            // the value in user.config overrides the value in NuGet\Config\IDE\a1.config
            v = settings.GetValue("SectionName", "key3");
            Assert.Equal("user", v);
        }

        // Tests method SetValue() with machine wide settings.
        // Verifies that the user specific config file is modified, while machine
        // wide settings files are not touched.
        [Fact]
        public void SetValueWithMachineWideSettings()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"C:\");
            var a1Config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />
  </SectionName>
</configuration>";
            var a2Config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key2"" value=""value2"" />
    <add key=""key3"" value=""value3"" />
  </SectionName>
</configuration>";

            mockFileSystem.AddFile(
                @"NuGet\Config\a1.config",
                a1Config);
            mockFileSystem.AddFile(
                @"NuGet\Config\IDE\a2.config",
                a2Config);

            mockFileSystem.AddFile(
               "user.config",
               @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key3"" value=""user"" />
  </SectionName>
</configuration>");

            var m = new Mock<IMachineWideSettings>();
            m.SetupGet(obj => obj.Settings).Returns(
                Settings.LoadMachineWideSettings(mockFileSystem, "IDE", "Version", "SKU"));

            var settings = Settings.LoadDefaultSettings(
                mockFileSystem,
                "user.config",
                m.Object);

            // Act            
            settings.SetValue("SectionName", "key1", "newValue");

            // Assert
            var text = mockFileSystem.ReadAllText("user.config");
            Assert.Equal(
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key3"" value=""user"" />
    <add key=""key1"" value=""newValue"" />
  </SectionName>
</configuration>",
                 text);

            text = mockFileSystem.ReadAllText(@"NuGet\Config\a1.config");
            Assert.Equal(a1Config, text);

            text = mockFileSystem.ReadAllText(@"NuGet\Config\IDE\a2.config");
            Assert.Equal(a2Config, text);
        }

        // Tests that when configFileName is not null, the specified
        // file must exist.
        [Fact]
        public void UserSpecifiedConfigFileMustExist()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem(@"C:\");

            // Act and assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => Settings.LoadDefaultSettings(
                    mockFileSystem,
                    configFileName: "user.config",
                    machineWideSettings: null),
                @"File 'C:\user.config' does not exist.");
        }

        // Tests the scenario where there are two user settings, both created
        // with the same machine wide settings.
        [Fact]
        public void GetValueFromTwoUserSettingsWithMachineWideSettings()
        {
            // Arrange            
            var mockFileSystem = new MockFileSystem(@"C:\");
            mockFileSystem.AddFile(
                @"NuGet\Config\a1.config",
                @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key1"" value=""value1"" />
  </SectionName>
</configuration>");
            
            mockFileSystem.AddFile(
               "user1.config",
               @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key3"" value=""user1"" />
  </SectionName>
</configuration>");
            mockFileSystem.AddFile(
               "user2.config",
               @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key3"" value=""user2"" />
  </SectionName>
</configuration>");

            var m = new Mock<IMachineWideSettings>();
            m.SetupGet(obj => obj.Settings).Returns(
                Settings.LoadMachineWideSettings(mockFileSystem, "IDE", "Version", "SKU"));

            // Act
            var settings1 = Settings.LoadDefaultSettings(
                mockFileSystem,
                "user1.config",
                m.Object);
            var settings2 = Settings.LoadDefaultSettings(
                mockFileSystem,
                "user2.config",
                m.Object);

            // Assert
            var v = settings1.GetValue("SectionName", "key3");
            Assert.Equal("user1", v);
            v = settings1.GetValue("SectionName", "key1");
            Assert.Equal("value1", v);

            v = settings2.GetValue("SectionName", "key3");
            Assert.Equal("user2", v);
            v = settings2.GetValue("SectionName", "key1");
            Assert.Equal("value1", v);
        }
    }
}
