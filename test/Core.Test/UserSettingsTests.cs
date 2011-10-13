using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using Xunit;

namespace NuGet.Test
{

    public class UserSettingsTests
    {

        [Fact]
        public void UserSettings_CallingCtroWithNullFileSystemWithThrowException()
        {
            // Act & Assert
            ExceptionAssert.Throws<ArgumentNullException>(() => new Settings(null));

        }

        [Fact]
        public void UserSettings_WillGetConfigurationFromSpecifiedPath()
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

            // Act
            Settings settings = new Settings(mockFileSystem.Object);

            // Assert 
            mockFileSystem.Verify(x => x.OpenFile(configFile), Times.Once(), "File was not read");
        }

        [Fact]
        public void UserSettings_CallingGetValuesWithNullSectionWillThrowException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValues(null));
        }

        [Fact]
        public void UserSettings_CallingGetValueWithNullSectionWillThrowException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValue(null, "SomeKey"));
        }

        [Fact]
        public void UserSettings_CallingGetValueWithNullKeyWillThrowException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValue("SomeSection", null));
        }

        [Fact]
        public void UserSettings_CallingCtorWithMalformedConfigThrowsException()
        {
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<configuration><sectionName></configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());

            // Act & Assert
            ExceptionAssert.Throws<System.Xml.XmlException>(() => new Settings(mockFileSystem.Object));

        }

        [Fact]
        public void UserSetting_CallingGetValuesWithNonExistantSectionReturnsNull()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<configuration></configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            var result = settings.GetValues("DoesNotExisit");

            // Assert 
            Assert.Null(result);
        }

        [Fact]
        public void UserSettings_CallingGetValuesWithSectionButNoValidValuesReturnsEmptyDictionary()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <Notadd key='key1' value='value1' />
        <add Key='key2' Value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            var result = settings.GetValues("SectionName");

            // Assert 
            Assert.NotNull(result);
            Assert.Equal(0, result.Count);
        }

        [Fact]
        public void UserSettings_CallingGetValuesWithoutSectionReturnsNull()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
        <add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            var result = settings.GetValues("NotTheSectionName");

            // Arrange 
            Assert.Null(result);
        }

        [Fact]
        public void UserSettings_CallingGetValueWithoutSectionReturnsNull()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
        <add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            var result = settings.GetValue("NotTheSectionName", "key1");

            // Arrange 
            Assert.Null(result);
        }

        [Fact]
        public void UserSettings_CallingGetValueWithSectionButNoValidKeyReturnsNull()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
        <add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            var result = settings.GetValue("SectionName", "key3");

            // Assert 
            Assert.Null(result);
        }

        [Fact]
        public void UserSettings_CallingGetValuesWithSectionReturnsDictionary()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
        <add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            var result = settings.GetValues("SectionName");

            // Assert 
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void UserSettings_CallingGetValueWithSectionAndKeyReturnsValue()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <add key='key1' value='value1' />
    </SectionName>
    <SectionNameTwo>
        <add key='key2' value='value2' />
    </SectionNameTwo>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            var result1 = settings.GetValue("SectionName", "key1");
            var result2 = settings.GetValue("SectionNameTwo", "key2");

            // Assert 
            Assert.Equal("value1", result1);
            Assert.Equal("value2", result2);
        }

        [Fact]
        public void UserSettings_CallingSetValueWithEmptySectionNameThrowsException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValue("", "SomeKey", "SomeValue"));
        }

        [Fact]
        public void UserSettings_CallingSetValueWithEmptyKeyThrowsException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValue("SomeKey", "", "SomeValue"));
        }

        [Fact]
        public void UserSettings_CallingSetValueWillAddSectionIfItDoesNotExist()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

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
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void UserSettings_CallingSetValueWillAddToSectionIfItExist()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            settings.SetValue("SectionName", "keyTwo", "valueTwo");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
    <add key=""keyTwo"" value=""valueTwo"" />
  </SectionName>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void UserSettings_CallingSetValueWillOverrideValueIfKeyExists()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            settings.SetValue("SectionName", "key", "NewValue");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""NewValue"" />
  </SectionName>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void UserSettings_CallingSetValuesWithEmptySectionThrowsException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("key", "value") };
            var settings = new Settings(mockFileSystem.Object);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValues("", values));
        }

        [Fact]
        public void UserSettings_CallingSetValuesWithNullValuesThrowsException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentNullException>(() => settings.SetValues("Section", null));
        }

        [Fact]
        public void UserSettings_CallingSetValuesWithEmptyKeyThrowsException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("", "value") };
            var settings = new Settings(mockFileSystem.Object);

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValues("Section", values));
        }

        [Fact]
        public void UserSettings_CallingSetValuseWillAddSectionIfItDoesNotExist()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("key", "value") };
            Settings settings = new Settings(mockFileSystem.Object);

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
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void UserSettings_CallingSetValuesWillAddToSectionIfItExist()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("keyTwo", "valueTwo") };
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            settings.SetValues("SectionName", values);

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
    <add key=""keyTwo"" value=""valueTwo"" />
  </SectionName>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void UserSettings_CallingSetValuesWillOverrideValueIfKeyExists()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("key", "NewValue") };
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            settings.SetValues("SectionName", values);

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""NewValue"" />
  </SectionName>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void UserSettings_CallingSetValuesWilladdValuesInOrder()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""Value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("key1", "Value1"), 
                                                                    new KeyValuePair<string, string>("key2", "Value2") };
            Settings settings = new Settings(mockFileSystem.Object);

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
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void UserSettings_CallingDeleteValueWithEmptyKeyThrowsException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.DeleteValue("SomeSection", ""));
        }

        [Fact]
        public void UserSettings_CallingDeleteValueWithEmptySectionThrowsException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.DeleteValue("", "SomeKey"));
        }

        [Fact]
        public void UserSettings_CallingDeleteValueWhenSectionNameDoesntExistReturnsFalse()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            Assert.False(settings.DeleteValue("SectionDoesNotExists", "SomeKey"));
        }

        [Fact]
        public void UserSettings_CallingDeleteValueWhenKeyDoesntExistThrowsException()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            Assert.False(settings.DeleteValue("SectionName", "KeyDoesNotExist"));
        }

        [Fact]
        public void UserSettings_CallingDeleteValueWithValidSectionAndKeyDeletesTheEntryAndReturnsTrue()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
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
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);



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
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void UserSettings_CallingDeleteSectionWithEmptySectionThrowsException()
        {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.DeleteSection(""));
        }

        [Fact]
        public void UserSettings_CallingDeleteSectionWhenSectionNameDoesntExistReturnsFalse()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);
            // Act & Assert
            Assert.False(settings.DeleteSection("SectionDoesNotExists"));
        }

        [Fact]
        public void UserSettings_CallingDeleteSectionWithValidSectionDeletesTheSectionAndReturnsTrue()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
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
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act & Assert
            Assert.True(settings.DeleteSection("SectionName"));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName2>
    <add key=""key"" value=""value"" />
  </SectionName2>
</configuration>", ms.ReadToEnd());
        }


        /* Extension Methods for Settings Class */
        [Fact]
        public void UserSettingsExtentions_SetEncryptedValue()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            settings.SetEncryptedValue("SectionName", "key", "NewValue");

            // Assert
            Assert.False(ms.ReadToEnd().Contains("NewValue"), "Value Should Be Ecrypted and Base64 encoded.");
        }

        [Fact]
        public void UserSettingsExtentions_GetEncryptedValue()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(false);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            Settings settings = new Settings(mockFileSystem.Object);
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
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            var result = settings.GetDecryptedValue("SectionName", "key");

            // Assert
            Assert.Equal(String.Empty, result);
        }

        [Fact]
        public void UserSettingsExtentions_GetDecryptedValueWithNoKeyReturnsNull()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            Settings settings = new Settings(mockFileSystem.Object);

            // Act
            var result = settings.GetDecryptedValue("SectionName", "NoKeyByThatName");

            // Assert
            Assert.Null(result);
        }

    }
}
