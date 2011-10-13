using System;
using System.Linq;
using System.Xml.Linq;
using Moq;
using NuGet.Runtime;
using Xunit;

namespace NuGet.Test
{

    public class AssemblyBindingTest
    {
        [Fact]
        public void CtorCopiesAssemblyProperties()
        {
            // Arrange
            var assembly = new Mock<IAssembly>();
            assembly.Setup(m => m.Name).Returns("AssemblyName");
            assembly.Setup(m => m.Culture).Returns("en-GB");
            assembly.Setup(m => m.PublicKeyToken).Returns("token");
            assembly.Setup(m => m.Version).Returns(new Version("1.0.0.0"));

            // Act
            var binding = new AssemblyBinding(assembly.Object);

            // Assert
            Assert.Equal("AssemblyName", binding.Name);
            Assert.Equal("en-GB", binding.Culture);
            Assert.Equal("token", binding.PublicKeyToken);
            Assert.Equal("1.0.0.0", binding.NewVersion);
            Assert.Equal("0.0.0.0-1.0.0.0", binding.OldVersion);
        }

        [Fact]
        public void ParseAssemblyBindingConfigSectionPreservesContent()
        {
            // Arrange
            string assemblyBindingXml = @"<assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
         <dependentAssembly>
            <assemblyIdentity name=""myAssembly""
                              publicKeyToken=""32ab4ba45e0a69a1""
                              culture=""neutral""
                              processorArchitecture=""x86"" />
            <bindingRedirect oldVersion=""0.0.0.1-1.0.0.0""
                             newVersion=""2.0.0.0""/>
            <codeBase version=""2.0.0.0""
                      href=""http://www.litwareinc.com/myAssembly.dll""/>
            <publisherPolicy apply=""no""/>
         </dependentAssembly>
      </assemblyBinding>";
            var dependentAssembly = XElement.Parse(assemblyBindingXml).Elements().First();

            // Act
            var binding = AssemblyBinding.Parse(dependentAssembly);

            // Assert
            Assert.Equal("myAssembly", binding.Name);
            Assert.Equal("32ab4ba45e0a69a1", binding.PublicKeyToken);
            Assert.Equal("neutral", binding.Culture);
            Assert.Equal("x86", binding.ProcessorArchitecture);
            Assert.Equal("0.0.0.1-1.0.0.0", binding.OldVersion);
            Assert.Equal("2.0.0.0", binding.NewVersion);
            Assert.Equal("2.0.0.0", binding.CodeBaseVersion);
            Assert.Equal("http://www.litwareinc.com/myAssembly.dll", binding.CodeBaseHref);
            Assert.Equal("no", binding.PublisherPolicy);
            Assert.Equal(@"<dependentAssembly xmlns=""urn:schemas-microsoft-com:asm.v1"">
  <assemblyIdentity name=""myAssembly"" publicKeyToken=""32ab4ba45e0a69a1"" culture=""neutral"" processorArchitecture=""x86"" />
  <bindingRedirect oldVersion=""0.0.0.1-1.0.0.0"" newVersion=""2.0.0.0"" />
  <publisherPolicy apply=""no"" />
  <codeBase href=""http://www.litwareinc.com/myAssembly.dll"" version=""2.0.0.0"" />
</dependentAssembly>", binding.ToString());
        }
    }
}
