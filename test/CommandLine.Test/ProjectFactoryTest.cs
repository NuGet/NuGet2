using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
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
                Copyright = "\x00a9 Outercurve. All rights reserved.",
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
        <copyright>© Outercurve. All rights reserved.</copyright>
        <licenseUrl>http://nuget.codeplex.com/license</licenseUrl>
        <projectUrl>http://nuget.codeplex.com</projectUrl>
        <tags>nuget</tags>
    </metadata>
</package>";
            Assert.Equal(expected, actual);
        }
#if !MONO
        [Fact]
        public void ProjectFactoryAppliesGlobalProperties()
        {
            // Arrange
            var testAssembly = Assembly.GetExecutingAssembly();
            var projectXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <PropertyGroup>
        <ProjectGuid>{F879F274-EFA0-4157-8404-33A19B4E6AEC}</ProjectGuid>
        <OutputType>Library</OutputType>
        <RootNamespace>NuGet.Test</RootNamespace>
        <AssemblyName>" + testAssembly.GetName().Name + @"</AssemblyName>
        <TargetFrameworkProfile Condition="" '$(TargetFrameworkVersion)' == 'v4.0' "">Client</TargetFrameworkProfile>    
        <OutputPath>.</OutputPath> <!-- Force it to look for the assembly in the base path -->
    </PropertyGroup>
    
    <ItemGroup>
        <Compile Include=""..\..\Dummy.cs"">
          <Link>Dummy.cs</Link>
        </Compile>
    </ItemGroup>

    <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
    <Import Project=""C:\DoesNotExist.targets"" Condition="" '$(MyGlobalProperty)' != 'true' "" />
</Project>";

            // Set base path to the currently assembly's folder so that it will find the test assembly
            var basePath = Path.GetDirectoryName(testAssembly.CodeBase);
            var cmdLineProperties = new Dictionary<string, string>
                {
                    { "MyGlobalProperty", "true" }
                };
            var project = new Project(XmlReader.Create(new StringReader(projectXml)), cmdLineProperties, null);
            project.FullPath = Path.Combine(project.DirectoryPath, "test.csproj");
            
            // Act
            var factory = new ProjectFactory(project) { Build = false };
            factory.ProjectProperties.Add("MyGlobalProperty", "false"); // This shouldn't be applied
            factory.ProjectProperties.Add("TestProperty", "true"); // This should be applied
            var packageBuilder = factory.CreateBuilder(basePath);

            // Assert
            Assert.True(project.GetProperty("MyGlobalProperty").IsGlobalProperty);
            Assert.False(project.GetProperty("TestProperty").IsGlobalProperty);
            Assert.Equal("true", project.GetProperty("MyGlobalProperty").UnevaluatedValue, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("true", project.GetProperty("TestProperty").UnevaluatedValue, StringComparer.OrdinalIgnoreCase);
        }
#endif
    }
}
