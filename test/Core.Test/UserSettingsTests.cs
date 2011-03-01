using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Runtime;
using NuGet.Configuration;

namespace NuGet.Test {
    [TestClass]
    public class UserSettingsTests {

        [TestMethod]
        public void UserSettings_CallingCtroWithNullFileSystemWithThrowException() {
            // Act & Assert
            ExceptionAssert.Throws<ArgumentNullException>(() => new UserSettings(null));
        
        }

        [TestMethod]
        public void UserSettings_CallingGetValuesWithNullSectionWillThrowException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValues(null));
        }

        [TestMethod]
        public void UserSettings_CallingGetValueWithNullSectionWillThrowException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValue(null, "SomeKey"));
        }

        [TestMethod]
        public void UserSettings_CallingGetValueWithNullKeyWillThrowException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValue("SomeSection", null));
        }

        [TestMethod]
        public void UserSettings_CallingCtorWithMalformedConfigThrowsException() {
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            string config = @"<configuration><sectionName></configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());

            // Act & Assert
            ExceptionAssert.Throws<System.Xml.XmlException>(() => new UserSettings(mockFileSystem.Object));

        }

        [TestMethod]
        public void UserSetting_CallingGetValuesWithNonExistantSectionReturnsNull() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            string config = @"<configuration></configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            var result = settings.GetValues("DoesNotExisit");

            // Assert 
            Assert.IsNull(result);
        }

        [TestMethod]
        public void UserSettings_CallingGetValuesWithSectionButNoValidValuesReturnsEmptyDictionary() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <NotAdd key='key1' value='value1' />
        <Add Key='key2' Value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            var result = settings.GetValues("SectionName");

            // Assert 
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void UserSettings_CallingGetValuesWithoutSectionReturnsNull() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <Add key='key1' value='value1' />
        <Add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            var result = settings.GetValues("NotTheSectionName");

            // Arrange 
            Assert.IsNull(result);
        }

        [TestMethod]
        public void UserSettings_CallingGetValueWithoutSectionReturnsNull() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <Add key='key1' value='value1' />
        <Add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            var result = settings.GetValue("NotTheSectionName","key1");

            // Arrange 
            Assert.IsNull(result);
        }

        [TestMethod]
        public void UserSettings_CallingGetValueWithSectionButNoValidKeyReturnsNull() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <Add key='key1' value='value1' />
        <Add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            var result = settings.GetValue("SectionName", "key3");

            // Assert 
            Assert.IsNull(result);
        }

        [TestMethod]
        public void UserSettings_CallingGetValuesWithSectionReturnsDictionary() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <Add key='key1' value='value1' />
        <Add key='key2' value='value2' />
    </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            var result = settings.GetValues("SectionName");

            // Assert 
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void UserSettings_CallingGetValueWithSectionAndKeyReturnsValue() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            string config = @"
<configuration>
    <SectionName>
        <Add key='key1' value='value1' />
    </SectionName>
    <SectionNameTwo>
        <Add key='key2' value='value2' />
    </SectionNameTwo>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            var result1 = settings.GetValue("SectionName", "key1");
            var result2 = settings.GetValue("SectionNameTwo", "key2");

            // Assert 
            Assert.AreEqual("value1", result1);
            Assert.AreEqual("value2", result2);
        }

        [TestMethod]
        public void UserSettings_CallingSetValueWithEmptySectionNameThrowsException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValue("","SomeKey", "SomeValue"));
        }

        [TestMethod]
        public void UserSettings_CallingSetValueWithEmptyKeyThrowsException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValue("SomeKey", "", "SomeValue"));
        }

        [TestMethod]
        public void UserSettings_CallingSetValueWillAddSectionIfItDoesNotExist() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <Add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetValue("NewSectionName", "key", "value");

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <Add key=""key"" value=""value"" />
  </SectionName>
  <NewSectionName>
    <Add key=""key"" value=""value"" />
  </NewSectionName>
</configuration>", ms.ReadToEnd());        
        }

        [TestMethod]
        public void UserSettings_CallingSetValueWillAddToSectionIfItExist() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <Add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetValue("SectionName", "keyTwo", "valueTwo");

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <Add key=""key"" value=""value"" />
    <Add key=""keyTwo"" value=""valueTwo"" />
  </SectionName>
</configuration>", ms.ReadToEnd());      
        }

        [TestMethod]
        public void UserSettings_CallingSetValueWillOverrideValueIfKeyExists() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <Add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetValue("SectionName", "key", "NewValue");

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <Add key=""key"" value=""NewValue"" />
  </SectionName>
</configuration>", ms.ReadToEnd());
        }

        [TestMethod]
        public void UserSettingsExtentions_SetEncryptedValue() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <Add key=""key"" value=""value"" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetEncryptedValue("SectionName", "key", "NewValue");

            // Assert
            Assert.IsFalse(ms.ReadToEnd().Contains("NewValue"), "Value Should Be Ecrypted and Base64 encoded.");
        }

        [TestMethod]
        public void UserSettingsExtentions_GetEncryptedValue() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(false);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });
            UserSettings settings = new UserSettings(mockFileSystem.Object);
            settings.SetEncryptedValue("SectionName", "key", "value");

            // Act
            var result = settings.GetDecryptedValue("SectionName", "key");

            // Assert
            Assert.AreEqual("value", result);
        }

        [TestMethod]
        public void UserSettingsExtentions_GetDecryptedValueWithEmptyValueReturnsEmptyString() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <Add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            var result = settings.GetDecryptedValue("SectionName", "key");

            // Assert
            Assert.AreEqual(String.Empty, result);
        }

        [TestMethod]
        public void UserSettingsExtentions_GetDecryptedValueWithNoKeyReturnsNull() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData)).Returns(@"c:\users\bob\appdata\roaming");
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(true);
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <Add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            var result = settings.GetDecryptedValue("SectionName", "NoKeyByThatName");

            // Assert
            Assert.IsNull(result);
        }


    }
}
