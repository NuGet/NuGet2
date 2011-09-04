#if !CODE_COVERAGE
using System.Linq;
using System.Security;
using Xunit;

namespace NuGet.Test {
    
    public class SecurityTest {
        [Fact]
        public void VerifyNuGetCoreSecurityTransparent() {
            // Act
            var securityTransparentAttributes = typeof(IPackage).Assembly.GetCustomAttributes(inherit: true).OfType<SecurityTransparentAttribute>();

            // Assert
            Assert.True(securityTransparentAttributes.Any());
        }
    }
}
#endif