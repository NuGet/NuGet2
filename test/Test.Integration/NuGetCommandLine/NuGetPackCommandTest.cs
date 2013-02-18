using System;
using System.Collections.Generic;
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
        [Fact]
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
                    "-build"
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
        [Fact]
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
                    "-symbols"
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
    }
}
