using System;
using System.IO;
using Moq;
using NuGet.Runtime;
using Xunit;

namespace NuGet.Test
{

    public class BindingRedirectManagerTest
    {
        [Fact]
        public void AddingBindingRedirectToEmptyConfig()
        {
            // Arrange            
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.FileExists("config")).Returns(true);
            mockFileSystem.Setup(m => m.OpenFile("config")).Returns(@"<configuration></configuration>".AsStream());
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile("config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem.Object, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "3.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-3.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void AddingBindingRedirectWithMultipleAssemblyBindingSectionsAddsToFirstOne()
        {
            // Arrange            
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.FileExists("config")).Returns(true);
            mockFileSystem.Setup(m => m.OpenFile("config")).Returns(@"
<configuration>
    <runtime>
        <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
            <dependentAssembly>
                <assemblyIdentity name=""System.Web.Mvc"" publicKeyToken=""31bf3856ad364e35"" />
                <bindingRedirect oldVersion=""1.0.0.0"" newVersion=""2.0.0.0"" />
            </dependentAssembly>
        </assemblyBinding>
        <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
            <dependentAssembly>
                <assemblyIdentity name=""A.Library"" publicKeyToken=""a34a755ec277222f"" />
                <bindingRedirect oldVersion=""1.0.0.0-2.0.0.0"" newVersion=""2.0.0.0"" />
            </dependentAssembly>
        </assemblyBinding>
    </runtime>
</configuration>".AsStream());
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile("config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem.Object, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "3.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""System.Web.Mvc"" publicKeyToken=""31bf3856ad364e35"" />
        <bindingRedirect oldVersion=""1.0.0.0"" newVersion=""2.0.0.0"" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-3.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""A.Library"" publicKeyToken=""a34a755ec277222f"" />
        <bindingRedirect oldVersion=""1.0.0.0-2.0.0.0"" newVersion=""2.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void AddingBindingRedirectsDoesNotAddDuplicates()
        {
            // Arrange            
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.FileExists("config")).Returns(true);
            mockFileSystem.Setup(m => m.OpenFile("config")).Returns(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
    <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-3.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>".AsStream());
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile("config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem.Object, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "3.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-3.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void AddingBindingRedirectsOverwritesAssemblyBindingIfBindingForAssemblyExists()
        {
            // Arrange            
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.FileExists("config")).Returns(true);
            mockFileSystem.Setup(m => m.OpenFile("config")).Returns(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
    <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-3.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>".AsStream());
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile("config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem.Object, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "5.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-5.0.0.0"" newVersion=""5.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void AddingBindingRedirectsFileWithDuplicateAssemblyIdentitiesOverwritesAssemblyBindingIfBindingForAssemblyExists()
        {
            // Arrange            
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.FileExists("config")).Returns(true);
            mockFileSystem.Setup(m => m.OpenFile("config")).Returns(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
    <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-1.0.0.0"" newVersion=""2.0.0.0"" />
    </dependentAssembly>
    <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""1.5.0.0-3.0.0.0"" newVersion=""3.0.0.0"" />
    </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>".AsStream());
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile("config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem.Object, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "5.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-5.0.0.0"" newVersion=""5.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void RemoveBindingRedirectsRemovesParentNodeIfLastElement()
        {
            // Arrange            
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.FileExists("config")).Returns(true);
            mockFileSystem.Setup(m => m.OpenFile("config")).Returns(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
    </assemblyBinding>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
    <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-3.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>".AsStream());
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile("config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem.Object, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "3.0.0.0");

            // Act
            bindingRedirectManager.RemoveBindingRedirects(new[] { assemblyBinding });

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1""></assemblyBinding>
  </runtime>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void RemoveBindingRedirectsDoesNotRemoveParentNodeIfNotLastElement()
        {
            // Arrange            
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.FileExists("config")).Returns(true);
            mockFileSystem.Setup(m => m.OpenFile("config")).Returns(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
    <dependentAssembly>
        <assemblyIdentity name=""System.Web.Mvc"" publicKeyToken=""31bf3856ad364e35"" />
        <bindingRedirect oldVersion=""1.0.0.0"" newVersion=""2.0.0.0"" />
      </dependentAssembly>
    <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-3.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>".AsStream());
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile("config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem.Object, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "3.0.0.0");

            // Act
            bindingRedirectManager.RemoveBindingRedirects(new[] { assemblyBinding });

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""System.Web.Mvc"" publicKeyToken=""31bf3856ad364e35"" />
        <bindingRedirect oldVersion=""1.0.0.0"" newVersion=""2.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>", ms.ReadToEnd());
        }

        [Fact]
        public void AddBindingRedirectUpdatesElementsPreservingComments()
        {
            // Arrange            
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(m => m.FileExists("config")).Returns(true);
            mockFileSystem.Setup(m => m.OpenFile("config")).Returns(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <!-- This is a comment that should not be removed -->
        <bindingRedirect oldVersion=""2.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>".AsStream());
            var ms = new MemoryStream();
            mockFileSystem.Setup(m => m.AddFile("config", It.IsAny<Stream>())).Callback<string, Stream>((path, stream) =>
            {
                stream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
            });

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem.Object, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "4.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <!-- This is a comment that should not be removed -->
        <bindingRedirect oldVersion=""0.0.0.0-4.0.0.0"" newVersion=""4.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>", ms.ReadToEnd());
        }

        private static AssemblyBinding GetAssemblyBinding(string name, string publicKey, string version, string culture = "neutral")
        {
            var assembly = new Mock<IAssembly>(MockBehavior.Strict);
            assembly.Setup(m => m.Name).Returns(name);
            assembly.Setup(m => m.PublicKeyToken).Returns(publicKey);
            assembly.Setup(m => m.Version).Returns(new Version(version));
            assembly.Setup(m => m.Culture).Returns(culture);
            return new AssemblyBinding(assembly.Object);
        }
    }
}
