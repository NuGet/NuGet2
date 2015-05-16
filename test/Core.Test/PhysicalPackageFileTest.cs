using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PhysicalPackageFileTest
    {
        [Theory]
        [InlineData(@"content\net45\test.txt", @"test.txt", ".NETFramework", "")]
        [InlineData(@"content\fakefx\test.txt", @"test.txt", "fakefx", "")]
        [InlineData(@"content\fakefx-someprofile\test.txt", @"test.txt", "fakefx", "someprofile")]
        public void UsingManagedCodeConventions_ResultsInStrictParsingOfTargetFramework(
            string targetPath, 
            string expectedEffectivePath, 
            string expectedIdentifier,
            string expectedProfile)
        {
            // Act
            var path = new PhysicalPackageFile(() => Stream.Null, useManagedCodeConventions: true)
            {
                TargetPath = targetPath,
            };

            // Assert
            Assert.Equal(expectedEffectivePath, path.EffectivePath);
            Assert.Equal(expectedIdentifier, path.TargetFramework.Identifier);
            Assert.Equal(expectedProfile, path.TargetFramework.Profile);
        }

        [Theory]
        [InlineData(@"content\fakefx\test.txt", @"fakefx\test.txt")]
        [InlineData(@"tools\fakefx-someprofile\test.txt", @"fakefx-someprofile\test.txt")]
        public void TargetFramework_ParsesUsingLegacyBehaviorWhenManagedCodeConventionsIsDisabled(
            string targetPath,
            string effectivePath)
        {
            // Act
            var path = new PhysicalPackageFile(() => Stream.Null, useManagedCodeConventions: false)
            {
                TargetPath = targetPath
            };

            // Assert
            Assert.Equal(effectivePath, path.EffectivePath);
        }
    }
}
