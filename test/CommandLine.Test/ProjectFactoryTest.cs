using System;
using System.IO;
using Microsoft.Build.Evaluation;
using Moq;
using NuGet.Commands;
using NuGet.Common;
using Xunit;

namespace NuGet.Test
{
    /// <summary>
    /// A class to test <see cref="ProjectFactory"/>.
    /// </summary>
    public class ProjectFactoryTest
    {
        [Fact]
        public void ProjectFactoryInitializesPropertiesForPreprocessor()
        {
            // arrange
            const string inputSpec = @"<?xml version=""1.0""?>
<package>
    <metadata>
        <id>$id$</id>
        <version>$version$</version>
        <description>$description$</description>
        <authors>$author$</authors>
        <copyright>$copyright$</copyright>
        <licenseUrl>http://nuget.codeplex.com/license</licenseUrl>
        <projectUrl>http://nuget.codeplex.com</projectUrl>
        <tags>nuget</tags>
    </metadata>
</package>";
            var metadata = new ManifestMetadata
            {
                Id = "ProjectFactoryTest",
                Version = "2.0.30619.9000",
                Title = "NuGet.Test",
                Description = "",
                Copyright = "\x00a9 Outercurve Foundation. All rights reserved.",
                Authors = "Outercurve Foundation",
            };
            var projectMock = new Mock<Project>();
            var factory = new ProjectFactory(projectMock.Object);

            // act
            var author = factory.InitializeProperties(metadata);
            var actual = Preprocessor.Process(inputSpec.AsStream(), factory, false);

            // assert
            Assert.Equal("Outercurve Foundation", author);
            const string expected = @"<?xml version=""1.0""?>
<package>
    <metadata>
        <id>ProjectFactoryTest</id>
        <version>2.0.30619.9000</version>
        <description></description>
        <authors>Outercurve Foundation</authors>
        <copyright>© Outercurve Foundation. All rights reserved.</copyright>
        <licenseUrl>http://nuget.codeplex.com/license</licenseUrl>
        <projectUrl>http://nuget.codeplex.com</projectUrl>
        <tags>nuget</tags>
    </metadata>
</package>";
            Assert.Equal(expected, actual);
        }
    }
}
