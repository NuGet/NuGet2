using System;
using Moq;
using NuGet.Runtime;
using NuGet.Test.Mocks;
using Xunit;

namespace NuGet.Test
{

    public class BindingRedirectManagerTest
    {
        [Fact]
        public void AddingBindingRedirectToEmptyConfig()
        {
            // Arrange            
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("config", @"<configuration></configuration>");

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "3.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            string outputContent = mockFileSystem.OpenFile("config").ReadToEnd();
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
</configuration>", outputContent);
        }

        [Fact]
        public void AddingBindingRedirectWithMultipleAssemblyBindingSectionsAddsToFirstOne()
        {
            // Arrange            
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("config", @"
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
</configuration>");

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "3.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            string outputContent = mockFileSystem.OpenFile("config").ReadToEnd();
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
</configuration>", outputContent);
        }

        [Fact]
        public void AddingBindingRedirectsDoesNotAddDuplicates()
        {
            // Arrange            
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-3.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>");

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "3.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            string outputContent = mockFileSystem.OpenFile("config").ReadToEnd();
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
</configuration>", outputContent);
        }

        [Fact]
        public void AddingBindingRedirectsOverwritesAssemblyBindingIfBindingForAssemblyExists()
        {
            // Arrange            
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-3.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>");
            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "5.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            string outputContent = mockFileSystem.OpenFile("config").ReadToEnd();
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
</configuration>", outputContent);
        }

        [Fact]
        public void AddingBindingRedirectsFileWithDuplicateAssemblyIdentitiesOverwritesAssemblyBindingIfBindingForAssemblyExists()
        {
            // Arrange            
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("config", @"<?xml version=""1.0"" encoding=""utf-8""?>
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
</configuration>");

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "5.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            string outputContent = mockFileSystem.OpenFile("config").ReadToEnd();
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
</configuration>", outputContent);
        }

        [Fact]
        public void RemoveBindingRedirectsRemovesParentNodeIfLastElement()
        {
            // Arrange            
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("config", @"<?xml version=""1.0"" encoding=""utf-8""?>
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
</configuration>");

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "3.0.0.0");

            // Act
            bindingRedirectManager.RemoveBindingRedirects(new[] { assemblyBinding });

            // Assert
            string outputContent = mockFileSystem.OpenFile("config").ReadToEnd();
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
    </assemblyBinding>
  </runtime>
</configuration>", outputContent);
        }

        [Fact]
        public void RemoveBindingRedirectsDoesNotRemoveParentNodeIfNotLastElement()
        {
            // Arrange            
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("config", @"<?xml version=""1.0"" encoding=""utf-8""?>
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
</configuration>");

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "3.0.0.0");

            // Act
            bindingRedirectManager.RemoveBindingRedirects(new[] { assemblyBinding });

            // Assert
            string outputContent = mockFileSystem.OpenFile("config").ReadToEnd();
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
</configuration>", outputContent);
        }

        [Fact]
        public void AddBindingRedirectUpdatesElementsPreservingCommentsAndWhitespace()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile("config", @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly><assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <!-- This is a comment that should not be removed -->
        <bindingRedirect oldVersion=""2.0.0.0"" newVersion=""3.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>");

            var bindingRedirectManager = new BindingRedirectManager(mockFileSystem, "config");
            AssemblyBinding assemblyBinding = GetAssemblyBinding("AssemblyName", "token", "4.0.0.0");

            // Act
            bindingRedirectManager.AddBindingRedirects(new[] { assemblyBinding });

            // Assert
            string outputContent = mockFileSystem.OpenFile("config").ReadToEnd();

            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly><assemblyIdentity name=""AssemblyName"" publicKeyToken=""token"" culture=""neutral"" />
        <!-- This is a comment that should not be removed -->
        <bindingRedirect oldVersion=""0.0.0.0-4.0.0.0"" newVersion=""4.0.0.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>", outputContent);
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
