using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Runtime;
using NuGet.Test.Mocks;

namespace NuGet.Test {
    [TestClass]
    public class BindingRedirectResolverTest {
        [TestMethod]
        public void GetBindingRedirectsTest() {
            // A, B, C2, G
            // A -> C1
            // B -> G
            // G -> C2
            var A = new MockAssembly {
                Name = "A",
                Version = new Version("1.0.0.0"),
                PublicKeyToken = "a34a755ec277222f"
            };

            var B = new MockAssembly {
                Name = "B",
                Version = new Version("1.0.0.0"),
                PublicKeyToken = "b34a755ec277222f"
            };

            var C1 = new MockAssembly {
                Name = "C",
                Version = new Version("1.0.0.0"),
                PublicKeyToken = "c34a755ec277222f"
            };

            var C2 = new MockAssembly {
                Name = "C",
                Version = new Version("2.0.0.0"),
                PublicKeyToken = "c34a755ec277222f"
            };

            var D = new MockAssembly {
                Name = "D",
                Version = new Version("1.0.0.0"),
                PublicKeyToken = "f34a755ec277222f"
            };

            var G = new MockAssembly {
                Name = "G",
                Version = new Version("1.0.0.0"),
                PublicKeyToken = "d34a755ec277222f"
            };

            var assemblies = new[] { A, B, C2, D, G };

            B.References.Add(G);
            D.References.Add(C2);
            G.References.Add(C1);
            A.References.Add(C2);

            // Act
            var redirectAssemblies = BindingRedirectResolver.GetBindingRedirects(assemblies).ToList();

            // Assert
            Assert.AreEqual(1, redirectAssemblies.Count);
            Assert.AreEqual("C", redirectAssemblies[0].Name);
            Assert.AreEqual("2.0.0.0", redirectAssemblies[0].NewVersion);
        }

        [TestMethod]
        public void GetBindingRedirectsOnlyRedirectsStrongNamedAssemblies() {
            // A, B, C2
            // A -> C2
            // B -> C1
            var A = new MockAssembly {
                Name = "A",
                Version = new Version("1.0.0.0"),
                PublicKeyToken = "a34a755ec277222f"
            };

            var B = new MockAssembly {
                Name = "B",
                Version = new Version("1.0.0.0"),
                PublicKeyToken = "b34a755ec277222f"
            };

            var C1 = new MockAssembly {
                Name = "C",
                Version = new Version("1.0.0.0")
            };

            var C2 = new MockAssembly {
                Name = "C",
                Version = new Version("2.0.0.0"),
                PublicKeyToken = "c34a755ec277222f"
            };

            var assemblies = new[] { A, B, C2 };

            A.References.Add(C2);
            B.References.Add(C1);

            // Act
            var redirectAssemblies = BindingRedirectResolver.GetBindingRedirects(assemblies).ToList();

            // Assert
            Assert.AreEqual(0, redirectAssemblies.Count);
        }
    }
}
