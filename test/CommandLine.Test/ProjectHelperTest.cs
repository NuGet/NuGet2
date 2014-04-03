using System;
using NuGet.Common;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.NuGetCommandLine
{
    public class ProjectHelperTest
    {
        [Theory]
        [InlineData(".csproj")]
        [InlineData(".vbproj")]
        [InlineData(".fsproj")]
        [InlineData(".vcxproj")]
        [InlineData(".jsproj")]
        public void ProjectHelperSupportedExtensions(string fileExtension)
        {
            Assert.True(ProjectHelper.SupportedProjectExtensions.Contains(fileExtension));
        }
    }
}
