using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValues(null));
        }

        [TestMethod]
        public void UserSettings_CallingGetValueWithNullSectionWillThrowException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValue(null, "SomeKey"));
        }

        [TestMethod]
        public void UserSettings_CallingGetValueWithNullKeyWillThrowException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.GetValue("SomeSection", null));
        }

        [TestMethod]
        public void UserSettings_CallingCtorWithMalformedConfigThrowsException() {
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<configuration><sectionName></configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());

            // Act & Assert
            ExceptionAssert.Throws<System.Xml.XmlException>(() => new UserSettings(mockFileSystem.Object));

        }
        
        [TestMethod]
        public void UserSetting_CallingGetValuesWithNonExistantSectionReturnsNull() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<configuration></configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
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
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValue("","SomeKey", "SomeValue"));
        }

        [TestMethod]
        public void UserSettings_CallingSetValueWithEmptyKeyThrowsException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValue("SomeKey", "", "SomeValue"));
        }

        [TestMethod]
        public void UserSettings_CallingSetValueWillAddSectionIfItDoesNotExist() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetValue("NewSectionName", "key", "value");

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
  <NewSectionName>
    <add key=""key"" value=""value"" />
  </NewSectionName>
</configuration>", ms.ReadToEnd());        
        }

        [TestMethod]
        public void UserSettings_CallingSetValueWillAddToSectionIfItExist() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetValue("SectionName", "keyTwo", "valueTwo");

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
    <add key=""keyTwo"" value=""valueTwo"" />
  </SectionName>
</configuration>", ms.ReadToEnd());      
        }

        [TestMethod]
        public void UserSettings_CallingSetValueWillOverrideValueIfKeyExists() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetValue("SectionName", "key", "NewValue");

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""NewValue"" />
  </SectionName>
</configuration>", ms.ReadToEnd());
        }

        [TestMethod]
        public void UserSettings_CallingSetValuesWithEmptySectionThrowsException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("key", "value") };
            var settings = new UserSettings(mockFileSystem.Object);
            
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValues("", values));
        }

        [TestMethod]
        public void UserSettings_CallingSetValuesWithNullValuesThrowsException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentNullException>(() => settings.SetValues("Section", null));
        }

        [TestMethod]
        public void UserSettings_CallingSetValuesWithEmptyKeyThrowsException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var values = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("", "value") };
            var settings = new UserSettings(mockFileSystem.Object);
            
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.SetValues("Section", values));
        }

        [TestMethod]
        public void UserSettings_CallingSetValuseWillAddSectionIfItDoesNotExist() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetValues("NewSectionName", values);

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
  </SectionName>
  <NewSectionName>
    <add key=""key"" value=""value"" />
  </NewSectionName>
</configuration>", ms.ReadToEnd());
        }

        [TestMethod]
        public void UserSettings_CallingSetValuesWillAddToSectionIfItExist() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetValues("SectionName", values);

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""value"" />
    <add key=""keyTwo"" value=""valueTwo"" />
  </SectionName>
</configuration>", ms.ReadToEnd());
        }

        [TestMethod]
        public void UserSettings_CallingSetValuesWillOverrideValueIfKeyExists() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetValues("SectionName", values);

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""NewValue"" />
  </SectionName>
</configuration>", ms.ReadToEnd());
        }

        [TestMethod]
        public void UserSettings_CallingSetValuesWilladdValuesInOrder() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            settings.SetValues("SectionName", values);

            // Assert
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value=""Value"" />
    <add key=""key1"" value=""Value1"" />
    <add key=""key2"" value=""Value2"" />
  </SectionName>
</configuration>", ms.ReadToEnd());
        }

        [TestMethod]
        public void UserSettings_CallingDeleteValueWithEmptyKeyThrowsException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.DeleteValue("SomeSection", ""));
        }

        [TestMethod]
        public void UserSettings_CallingDeleteValueWithEmptySectionThrowsException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.DeleteValue("", "SomeKey"));
        }

        [TestMethod]
        public void UserSettings_CallingDeleteValueWhenSectionNameDoesntExistReturnsFalse() {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            Assert.IsFalse(settings.DeleteValue("SectionDoesNotExists", "SomeKey"));
        }

        [TestMethod]
        public void UserSettings_CallingDeleteValueWhenKeyDoesntExistThrowsException() {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            Assert.IsFalse(settings.DeleteValue("SectionName", "KeyDoesNotExist"));
        }

        [TestMethod]
        public void UserSettings_CallingDeleteValueWithValidSectionAndKeyDeletesTheEntryAndReturnsTrue() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            

            // Act & Assert
            Assert.IsTrue(settings.DeleteValue("SectionName", "DeleteMe"));
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""keyNotToDelete"" value=""value"" />
  </SectionName>
  <SectionName2>
    <add key=""key"" value=""value"" />
  </SectionName2>
</configuration>", ms.ReadToEnd());      
        }

        [TestMethod]
        public void UserSettings_CallingDeleteSectionWithEmptySectionThrowsException() {
            // Arrange 
            var mockFileSystem = new Mock<IFileSystem>();
            var settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(() => settings.DeleteSection(""));
        }

        [TestMethod]
        public void UserSettings_CallingDeleteSectionWhenSectionNameDoesntExistReturnsFalse() {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);
            // Act & Assert
            Assert.IsFalse(settings.DeleteSection("SectionDoesNotExists"));
        }

        [TestMethod]
        public void UserSettings_CallingDeleteSectionWithValidSectionDeletesTheSectionAndReturnsTrue() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act & Assert
            Assert.IsTrue(settings.DeleteSection("SectionName"));
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName2>
    <add key=""key"" value=""value"" />
  </SectionName2>
</configuration>", ms.ReadToEnd());
        }


        /* Extension Methods for Settings Class */
        [TestMethod]
        public void UserSettingsExtentions_SetEncryptedValue() {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(@"c:\users\bob\appdata\roaming\NuGet\Nuget.Config")).Returns(false);
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile(nugetConfigPath, It.IsAny<Stream>())).Callback<string, Stream>((path, stream) => {
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
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
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
            var nugetConfigPath = "NuGet.Config";
            mockFileSystem.Setup(m => m.FileExists(nugetConfigPath)).Returns(true);
            string config = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <SectionName>
    <add key=""key"" value="""" />
  </SectionName>
</configuration>";
            mockFileSystem.Setup(m => m.OpenFile(nugetConfigPath)).Returns(config.AsStream());
            UserSettings settings = new UserSettings(mockFileSystem.Object);

            // Act
            var result = settings.GetDecryptedValue("SectionName", "NoKeyByThatName");

            // Assert
            Assert.IsNull(result);
        }

    }
}
