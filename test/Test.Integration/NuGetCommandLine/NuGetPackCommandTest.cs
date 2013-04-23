using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Configuration;

namespace NuGet.Test.Integration.NuGetCommandLine
{    
    public class NuGetPackCommandTest
    {
        // Test that when creating a package from project file, referenced projects
        // are also included in the package.
        [Fact]
        public void PackCommand_WithProjectReferences()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
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
                var r = CommandRunner.Run(
                    nugetexe, 
                    proj2Directory, 
                    "pack proj2.csproj -build -IncludeReferencedProjects",
                    waitForExit: true);                    
                Assert.Equal(0, r.Item1);

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
                Directory.Delete(workingDirectory, true);
            }
        }

        // Test that -IncludeReferencedProjects and -Symbols cannot be used together.
        [Fact]
        public void PackCommand_WithProjectReferences_Symbols()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Util.CreateDirectory(workingDirectory);

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
                var r = CommandRunner.Run(
                    nugetexe, 
                    workingDirectory,
                    "pack proj2.csproj -build -symbols -IncludeReferencedProjects",
                    waitForExit: true);    
                
                // Assert
                Assert.NotEqual(0, r.Item1);
                Assert.True(r.Item2.Contains("not supported"));
            }
            finally
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        // Test that when creating a package from project file, a referenced project that
        // has a nuspec file is added as dependency.
        [Fact]
        public void PackCommand_ReferencedProjectWithNuspecFile()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
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
                var r = CommandRunner.Run(
                    nugetexe, 
                    proj1Directory, 
                    "pack proj1.csproj -build -IncludeReferencedProjects",
                    waitForExit: true);                    
                Assert.Equal(0, r.Item1);

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
                Directory.Delete(workingDirectory, true);
            }
        }

        // Test that recognized tokens such as $id$ in the nuspec file of the 
        // referenced project are replaced.
        [Fact]
        public void PackCommand_NuspecFileWithTokens()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                // Arrange
                Util.CreateDirectory(workingDirectory);

                CreateTestProject(workingDirectory, "proj1",
                    new string[] { 
                        @"..\proj2\proj2.csproj"
                    });
                CreateTestProject(workingDirectory, "proj2", null, "v4.0", "1.2");                
                Util.CreateFile(
                    Path.Combine(workingDirectory, "proj2"),
                    "proj2.nuspec",
@"<package xmlns='http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd'>
  <metadata>
    <id>$id$</id>
    <version>$version$</version>
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
                
                // Act
                var proj1Directory = Path.Combine(workingDirectory, "proj1");
                var r = CommandRunner.Run(
                    nugetexe,
                    proj1Directory,
                    "pack proj1.csproj -build -IncludeReferencedProjects",
                    waitForExit: true);
                Assert.Equal(0, r.Item1);

                // Assert
                var package = new OptimizedZipPackage(Path.Combine(proj1Directory, "proj1.0.0.0.0.nupkg"));
                var files = package.GetFiles().Select(f => f.Path).ToArray();
                Array.Sort(files);

                Assert.Equal(
                    files,
                    new string[] 
                    { 
                        @"lib\net40\proj1.dll"
                    });

                // proj2 is added as dependencies.
                var dependencies = package.DependencySets.First().Dependencies.OrderBy(d => d.Id);
                Assert.Equal(
                    dependencies,
                    new PackageDependency[]
                    {
                        new PackageDependency("proj2", VersionUtility.ParseVersionSpec("1.2.0.0"))
                    },
                    new PackageDepencyComparer());
            }
            finally
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        // Test that option -IncludeReferencedProjects works correctly for the case
        // where the same project is referenced by multiple projects in the 
        // reference hierarchy.
        [Fact]
        public void PackCommand_ProjectReferencedByMultipleProjects()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                // Arrange
                Util.CreateDirectory(workingDirectory);

                // create test project, with the following dependency relationships:
                // proj1 depends on proj2, proj3
                // proj2 depends on proj3
                CreateTestProject(workingDirectory, "proj1",
                    new string[] { 
                        @"..\proj2\proj2.csproj",
                        @"..\proj3\proj3.csproj"
                    });
                CreateTestProject(workingDirectory, "proj2",
                    new string[] { 
                        @"..\proj3\proj3.csproj"
                    });
                CreateTestProject(workingDirectory, "proj3", null);

                // Act
                var proj1Directory = Path.Combine(workingDirectory, "proj1");
                var r = CommandRunner.Run(
                    nugetexe,
                    proj1Directory,
                    "pack proj1.csproj -build -IncludeReferencedProjects",
                    waitForExit: true);
                Assert.Equal(0, r.Item1);

                // Assert
                var package = new OptimizedZipPackage(Path.Combine(proj1Directory, "proj1.0.0.0.0.nupkg"));
                var files = package.GetFiles().Select(f => f.Path).ToArray();
                Array.Sort(files);

                Assert.Equal(
                    files,
                    new string[] 
                    { 
                        @"lib\net40\proj1.dll", 
                        @"lib\net40\proj2.dll",
                        @"lib\net40\proj3.dll"
                    });
            }
            finally
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        // Test that when creating a package from project A, the output of a referenced project 
        // will be added to the same target framework folder as A, regardless of the target
        // framework of the referenced project.
        [Fact]
        public void PackCommand_ReferencedProjectWithDifferentTarget()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                // Arrange
                Util.CreateDirectory(workingDirectory);

                CreateTestProject(workingDirectory, "proj1",
                    new string[] { 
                        @"..\proj2\proj2.csproj"
                    });
                CreateTestProject(workingDirectory, "proj2", null, "v3.0");

                // Act
                var proj1Directory = Path.Combine(workingDirectory, "proj1");                
                var r = CommandRunner.Run(
                    nugetexe,
                    proj1Directory,
                    "pack proj1.csproj -build -IncludeReferencedProjects",
                    waitForExit: true);
                Assert.Equal(0, r.Item1);

                // Assert
                var package = new OptimizedZipPackage(Path.Combine(proj1Directory, "proj1.0.0.0.0.nupkg"));
                var files = package.GetFiles().Select(f => f.Path).ToArray();
                Array.Sort(files);

                Assert.Equal(
                    files,
                    new string[] 
                    { 
                        @"lib\net40\proj1.dll",
                        @"lib\net40\proj2.dll"
                    });
            }
            finally
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        // Test that when -IncludeReferencedProjects is not specified,
        // pack command will not try to look for the output files of the 
        // referenced projects.
        [Fact]
        public void PackCommand_IncludeReferencedProjectsOff()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
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
    <Config Condition=" + "\" '$(Config)' == ''\"" + @">Debug</Config>        
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=" + "\"'$(Config)'=='Debug'\"" + @">
    <OutputPath>debug_out</OutputPath>
  </PropertyGroup>
  <PropertyGroup Condition=" + "\"'$(Config)'=='Release'\"" + @">
    <OutputPath>release_out</OutputPath>
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
    <Config Condition=" + "\" '$(Config)' == ''\"" + @">Debug</Config>    
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=" + "\"'$(Config)'=='Debug'\"" + @">
    <OutputPath>debug_out</OutputPath>
  </PropertyGroup>
  <PropertyGroup Condition=" + "\"'$(Config)'=='Release'\"" + @">
    <OutputPath>release_out</OutputPath>
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

                var msbuild = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    @"Microsoft.NET\Framework\v4.0.30319\msbuild.exe");
                var r = CommandRunner.Run(
                    msbuild,
                    proj2Directory,
                    "proj2.csproj /p:Config=Release",
                    waitForExit: true);

                // Act 
                r = CommandRunner.Run(
                    nugetexe,
                    proj2Directory,
                    "pack proj2.csproj -p Config=Release",
                    waitForExit: true);
                Assert.Equal(0, r.Item1);

                // Assert
                var package = new OptimizedZipPackage(Path.Combine(proj2Directory, "proj2.0.0.0.0.nupkg"));
                var files = package.GetFiles().Select(f => f.Path).ToArray();
                Array.Sort(files);
                Assert.Equal(
                    files,
                    new string[] 
                    { 
                        @"lib\net40\proj2.dll"
                    });
            }
            finally
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        // Test that when -IncludeReferencedProjects is specified, the properties 
        // passed thru command line will be applied if a referenced project 
        // needs to be built.
        [Fact]
        public void PackCommand_PropertiesAppliedToReferencedProjects()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"];
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
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
    <Config Condition=" + "\" '$(Config)' == ''\"" + @">Debug</Config>        
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=" + "\"'$(Config)'=='Debug'\"" + @">
    <OutputPath>debug_out</OutputPath>
  </PropertyGroup>
  <PropertyGroup Condition=" + "\"'$(Config)'=='Release'\"" + @">
    <OutputPath>release_out</OutputPath>
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
    <Config Condition=" + "\" '$(Config)' == ''\"" + @">Debug</Config>    
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=" + "\"'$(Config)'=='Debug'\"" + @">
    <OutputPath>debug_out</OutputPath>
  </PropertyGroup>
  <PropertyGroup Condition=" + "\"'$(Config)'=='Release'\"" + @">
    <OutputPath>release_out</OutputPath>
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
                var r = CommandRunner.Run(
                    nugetexe,
                    proj2Directory,
                    "pack proj2.csproj -build -IncludeReferencedProjects -p Config=Release",
                    waitForExit: true);
                Assert.Equal(0, r.Item1);
                
                // Assert

                // Verify that proj1 was not built using the default config "Debug".
                Assert.False(Directory.Exists(Path.Combine(proj1Directory, "debug_out")));

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
        /// <param name="targetFrameworkVersion">The target framework version of the project.</param>
        /// <param name="version">The version of the assembly.</param>
        private void CreateTestProject(
            string baseDirectory, 
            string projectName, 
            string[] referencedProject,
            string targetFrameworkVersion = "v4.0",
            string version = "0.0.0.0")
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
    <TargetFrameworkVersion>{0}</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include='file1.cs' />
  </ItemGroup>
{1}  
  <Import Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets' />
</Project>", targetFrameworkVersion, reference));

            Util.CreateFile(
                projectDirectory,
                "file1.cs",
@"using System;
using System.Reflection;

[assembly: AssemblyVersion(" + "\"" + version + "\"" + @")]

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
