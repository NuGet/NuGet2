using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NuGet.Test.Integration.NuGetCommandLine
{    
    public class NuGetPackCommandTest
    {
        // Test that when creating a package from project file, referenced projects
        // are also included in the package.
        [Fact(Skip="Has problem running on CI server")]
        public void PackCommand_WithProjectReferences()
        {
            var oldCurrentDirectory = Directory.GetCurrentDirectory();
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            try
            {
                Util.CreateDirectory(workingDirectory);
                var proj1Directory = Path.Combine(workingDirectory, "proj1");
                var proj2Directory = Path.Combine(workingDirectory, "proj2");
                Util.CreateDirectory(proj1Directory);
                Util.CreateDirectory(proj2Directory);

                // create project 1
                Util.CreateFile(
                    proj1Directory, 
                    "proj1.csproj",
@"<Project ToolsVersion='4.0' DefaultTargets='Build' 
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <OutputPath>out</OutputPath>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include='proj1_file1.cs' />
  </ItemGroup>
  <ItemGroup>
    <Content Include='proj1_file2.txt' />
  </ItemGroup>
  <Import Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets' />
</Project>");
                Util.CreateFile(
                    proj1Directory,
                    "proj1_file1.cs",
@"using System;

namespace Proj1
{
    public class Class1
    {
        public int A { get; set; }
    }
}");
                Util.CreateFile(
                    proj1Directory,
                    "proj1_file2.txt",
                    "file2");

                // Create project 2, which references project 1
                Util.CreateFile(
                    proj2Directory,
                    "proj2.csproj",
@"<Project ToolsVersion='4.0' DefaultTargets='Build' 
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <OutputPath>out</OutputPath>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include='..\proj1\proj1.csproj' />
  </ItemGroup>
  <ItemGroup>
    <Compile Include='proj2_file1.cs' />
  </ItemGroup>
  <Import Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets' />
</Project>");
                Util.CreateFile(
                    proj2Directory,
                    "proj2_file1.cs",
@"using System;

namespace Proj2
{
    public class Class1
    {
        public int A { get; set; }
    }
}");
                
                // Act
                Directory.SetCurrentDirectory(proj2Directory);
                string[] args = new string[] {
                    "pack",
                    "proj2.csproj",
                    "-build",
                    "-IncludeReferencedProjects"
                };
                int r = Program.Main(args);
                Assert.Equal(0, r);

                // Assert
                var package = new OptimizedZipPackage(Path.Combine(proj2Directory, "proj2.0.0.0.0.nupkg"));
                var files = package.GetFiles().Select(f => f.Path).ToArray();
                Array.Sort(files);
                Assert.Equal(
                    files,
                    new string[] 
                    { 
                        @"content\proj1_file2.txt",
                        @"lib\net40\proj1.dll", 
                        @"lib\net40\proj2.dll" 
                    });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        // Test that when creating a symbols package from project file, referenced projects
        // are also included in the package.
        [Fact(Skip = "Has problem running on CI server")]
        public void PackCommand_WithProjectReferences_Symbols()
        {
            var oldCurrentDirectory = Directory.GetCurrentDirectory();
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);

                // create project 1
                Util.CreateFile(
                    workingDirectory,
                    "proj1.csproj",
@"<Project ToolsVersion='4.0' DefaultTargets='Build' 
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <OutputPath>out1</OutputPath>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include='proj1_file1.cs' />
  </ItemGroup>
  <ItemGroup>
    <Content Include='proj1_file2.txt' />
  </ItemGroup>
  <Import Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets' />
</Project>");
                Util.CreateFile(
                    workingDirectory,
                    "proj1_file1.cs",
@"using System;

namespace Proj1
{
    public class Class1
    {
        public int A { get; set; }
    }
}");
                Util.CreateFile(
                    workingDirectory,
                    "proj1_file2.txt",
                    "file2");

                // Create project 2, which references project 1
                Util.CreateFile(
                    workingDirectory,
                    "proj2.csproj",
@"<Project ToolsVersion='4.0' DefaultTargets='Build' 
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <OutputPath>out2</OutputPath>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include='proj1.csproj' />
  </ItemGroup>
  <ItemGroup>
    <Compile Include='proj2_file1.cs' />
  </ItemGroup>
  <Import Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets' />
</Project>");
                Util.CreateFile(
                    workingDirectory,
                    "proj2_file1.cs",
@"using System;

namespace Proj2
{
    public class Class1
    {
        public int A { get; set; }
    }
}");

                // Act
                string[] args = new string[] {
                    "pack",
                    "proj2.csproj",
                    "-build",
                    "-symbols",
                    "-IncludeReferencedProjects"
                };
                int r = Program.Main(args);
                Assert.Equal(0, r);

                // Assert
                var package = new OptimizedZipPackage(Path.Combine(workingDirectory, "proj2.0.0.0.0.nupkg"));
                var files = package.GetFiles().Select(f => f.Path).ToArray();
                Array.Sort(files);
                Assert.Equal(
                    files,
                    new string[] 
                    { 
                        @"content\proj1_file2.txt",
                        @"lib\net40\proj1.dll", 
                        @"lib\net40\proj2.dll" 
                    });

                var symbolPackage = new OptimizedZipPackage(
                    Path.Combine(workingDirectory, "proj2.0.0.0.0.symbols.nupkg"));
                files = symbolPackage.GetFiles().Select(f => f.Path).ToArray();
                Array.Sort(files);
                Assert.Equal(
                    files,
                    new string[] 
                    { 
                        @"content\proj1_file2.txt",
                        @"lib\net40\proj1.dll",
                        @"lib\net40\proj1.pdb", 
                        @"lib\net40\proj2.dll",
                        @"lib\net40\proj2.pdb",
                        @"src\proj1_file1.cs",
                        @"src\proj2_file1.cs"
                    });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        // Test that when creating a package from project file, a referenced project that
        // has a nuspec file is added as dependency.
        [Fact(Skip = "Has problem running on CI server")]
        public void PackCommand_ReferencedProjectWithNuspecFile()
        {
            var oldCurrentDirectory = Directory.GetCurrentDirectory();
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                // Arrange
                Util.CreateDirectory(workingDirectory);

                // create test projects. There are 7 projects, with the following 
                // dependency relationships:
                // proj1 depends on proj2 & proj3
                // proj2 depends on proj4 & proj5
                // proj3 depends on proj5 & proj7
                //
                // proj2 and proj6 have nuspec files.
                CreateTestProject(workingDirectory, "proj1", 
                    new string[] { 
                        @"..\proj2\proj2.csproj",
                        @"..\proj3\proj3.csproj"
                    });
                CreateTestProject(workingDirectory, "proj2",
                    new string[] { 
                        @"..\proj4\proj4.csproj",
                        @"..\proj5\proj5.csproj"
                    });
                CreateTestProject(workingDirectory, "proj3",
                    new string[] { 
                        @"..\proj6\proj6.csproj",
                        @"..\proj7\proj7.csproj"
                    });
                CreateTestProject(workingDirectory, "proj4", null);
                CreateTestProject(workingDirectory, "proj5", null);
                CreateTestProject(workingDirectory, "proj6", null);
                CreateTestProject(workingDirectory, "proj7", null);
                Util.CreateFile(
                    Path.Combine(workingDirectory, "proj2"),
                    "proj2.nuspec",
@"<package xmlns='http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd'>
  <metadata>
    <id>proj2</id>
    <version>1.0.0.0</version>
    <title>Proj2</title>
    <authors>test</authors>
    <owners>test</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Description</description>
    <copyright>Copyright ©  2013</copyright>
    <dependencies>
      <dependency id='p1' version='1.5.11' />
    </dependencies>
  </metadata>
</package>");
                Util.CreateFile(
                    Path.Combine(workingDirectory, "proj6"),
                    "proj6.nuspec",
@"<package xmlns='http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd'>
  <metadata>
    <id>proj6</id>
    <version>2.0.0.0</version>
    <title>Proj6</title>
    <authors>test</authors>
    <owners>test</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Description</description>
    <copyright>Copyright ©  2013</copyright>
    <dependencies>
      <dependency id='p2' version='1.5.11' />
    </dependencies>
  </metadata>
</package>");

                // Act
                var proj1Directory = Path.Combine(workingDirectory, "proj1");
                Directory.SetCurrentDirectory(proj1Directory);
                string[] args = new string[] {
                    "pack",
                    "proj1.csproj",
                    "-build",
                    "-IncludeReferencedProjects"
                };
                int r = Program.Main(args);
                Assert.Equal(0, r);

                // Assert
                var package = new OptimizedZipPackage(Path.Combine(proj1Directory, "proj1.0.0.0.0.nupkg"));
                var files = package.GetFiles().Select(f => f.Path).ToArray();
                Array.Sort(files);

                // proj3 and proj7 are included in the package.
                Assert.Equal(
                    files,
                    new string[] 
                    { 
                        @"lib\net40\proj1.dll", 
                        @"lib\net40\proj3.dll",
                        @"lib\net40\proj7.dll"
                    });

                // proj2 and proj6 are added as dependencies.
                var dependencies = package.DependencySets.First().Dependencies.OrderBy(d => d.Id);
                Assert.Equal(
                    dependencies,
                    new PackageDependency[]
                    {
                        new PackageDependency("proj2", VersionUtility.ParseVersionSpec("1.0.0.0")),
                        new PackageDependency("proj6", VersionUtility.ParseVersionSpec("2.0.0.0"))
                    },
                    new PackageDepencyComparer());
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        /// <summary>
        /// Creates a simple project.
        /// </summary>
        /// <remarks>
        /// The project is created under directory baseDirectory\projectName.
        /// The project contains just one file called file1.cs.
        /// </remarks>
        /// <param name="baseDirectory">The base directory.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="referencedProject">The list of projects referenced by this project. Can be null.</param>
        private void CreateTestProject(string baseDirectory, string projectName, string[] referencedProject)
        {
            var projectDirectory = Path.Combine(baseDirectory, projectName);
            Util.CreateDirectory(projectDirectory);

            string reference = string.Empty;
            if (referencedProject != null && referencedProject.Length > 0)
            {
                var sb = new StringBuilder();
                sb.Append("<ItemGroup>");
                foreach (var r in referencedProject)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "<ProjectReference Include='{0}' />", r);
                }
                sb.Append("</ItemGroup>");

                reference = sb.ToString();
            }

            Util.CreateFile(
                projectDirectory,
                projectName + ".csproj",
                string.Format(CultureInfo.InvariantCulture,
@"<Project ToolsVersion='4.0' DefaultTargets='Build' 
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <OutputPath>out</OutputPath>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include='file1.cs' />
  </ItemGroup>
{0}  
  <Import Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets' />
</Project>", reference));

            Util.CreateFile(
                projectDirectory,
                "file1.cs",                
@"using System;

namespace " + projectName + @" 
{
    public class Class1
    {
        public int A { get; set; }
    }
}");
        }

        private class PackageDepencyComparer : IEqualityComparer<PackageDependency>
        {
            public bool Equals(PackageDependency x, PackageDependency y)
            {
                return string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        x.VersionSpec.ToString(), 
                        y.VersionSpec.ToString(), 
                        StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(PackageDependency obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
