using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Runtime;

namespace NuGet.Test {
    [TestClass]
    public class AssemblyBindingTest {
        [TestMethod]
        public void CtorCopiesAssemblyProperties() {
            // Arrange
            var assembly = new Mock<IAssembly>();
            assembly.Setup(m => m.Name).Returns("AssemblyName");
            assembly.Setup(m => m.Culture).Returns("en-GB");
            assembly.Setup(m => m.PublicKeyToken).Returns("token");
            assembly.Setup(m => m.Version).Returns(new Version("1.0.0.0"));

            // Act
            var binding = new AssemblyBinding(assembly.Object);

            // Assert
            Assert.AreEqual("AssemblyName", binding.Name);
            Assert.AreEqual("en-GB", binding.Culture);
            Assert.AreEqual("token", binding.PublicKeyToken);
            Assert.AreEqual("1.0.0.0", binding.NewVersion);
            Assert.AreEqual("0.0.0.0-1.0.0.0", binding.OldVersion);
        }

        [TestMethod]
        public void ParseAssemblyBindingConfigSectionPreservesContent() {
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
            Assert.AreEqual("myAssembly", binding.Name);
            Assert.AreEqual("32ab4ba45e0a69a1", binding.PublicKeyToken);
            Assert.AreEqual("neutral", binding.Culture);
            Assert.AreEqual("x86", binding.ProcessorArchitecture);
            Assert.AreEqual("0.0.0.1-1.0.0.0", binding.OldVersion);
            Assert.AreEqual("2.0.0.0", binding.NewVersion);
            Assert.AreEqual("2.0.0.0", binding.CodeBaseVersion);
            Assert.AreEqual("http://www.litwareinc.com/myAssembly.dll", binding.CodeBaseHref);
            Assert.AreEqual("no", binding.PublisherPolicy);
            Assert.AreEqual(@"<dependentAssembly xmlns=""urn:schemas-microsoft-com:asm.v1"">
  <assemblyIdentity name=""myAssembly"" publicKeyToken=""32ab4ba45e0a69a1"" culture=""neutral"" processorArchitecture=""x86"" />
  <bindingRedirect oldVersion=""0.0.0.1-1.0.0.0"" newVersion=""2.0.0.0"" />
  <publisherPolicy apply=""no"" />
  <codeBase href=""http://www.litwareinc.com/myAssembly.dll"" version=""2.0.0.0"" />
</dependentAssembly>", binding.ToString());
        }
    }
}
