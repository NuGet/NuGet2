#if !CODE_COVERAGE
using System.Linq;
using System.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class SecurityTest {
        [TestMethod]
        public void VerifyNuGetCoreSecurityTransparent() {
            // Act
            var securityTransparentAttributes = typeof(IPackage).Assembly.GetCustomAttributes(inherit: true).OfType<SecurityTransparentAttribute>();

            // Assert
            Assert.IsTrue(securityTransparentAttributes.Any());
        }
    }
}
#endif