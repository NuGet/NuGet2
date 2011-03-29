using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace NuGet.Test.Integration.NuGetCommandLine {
    [TestClass]
    public class NuGetCommandLineTest {
        private const string NoSpecsfolder = @".\nospecs\";
        private const string OneSpecfolder = @".\onespec\";
        private const string TwoSpecsFolder = @".\twospecs\";
        private const string OutputFolder = @".\output\";
        private const string SpecificFilesFolder = @".\specific_files\";
        private const string ProjectFilesFolder = @".\projects\";
        private const string NugetExePath = @".\NuGet.exe";

        private StringWriter consoleOutput;
        private TextWriter originalConsoleOutput;
        private TextWriter originalErrorConsoleOutput;
        private string startingDirectory;


        [TestInitialize]
        public void Initialize() {
            DeleteDirs();

            Directory.CreateDirectory(NoSpecsfolder);
            Directory.CreateDirectory(OneSpecfolder);
            Directory.CreateDirectory(TwoSpecsFolder);
            Directory.CreateDirectory(SpecificFilesFolder);
            Directory.CreateDirectory(OutputFolder);
            Directory.CreateDirectory(ProjectFilesFolder);

            originalConsoleOutput = System.Console.Out;
            originalErrorConsoleOutput = System.Console.Error;
            consoleOutput = new StringWriter();
            System.Console.SetOut(consoleOutput);
            System.Console.SetError(consoleOutput);
            startingDirectory = Directory.GetCurrentDirectory();
        }

        [TestCleanup]
        public void Cleanup() {
            DeleteDirs();
            System.Console.SetOut(originalConsoleOutput);
            System.Console.SetError(originalErrorConsoleOutput);
            Directory.SetCurrentDirectory(startingDirectory);
        }


        [TestMethod]
        public void NuGetCommandLine_ShowsHelpIfThereIsNoCommand() {
            // Arrange 
            string[] args = new string[0];

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(consoleOutput.ToString().Contains("usage: NuGet <command> [args] [options]"));
        }

        [TestMethod]
        public void PackageCommand_ThrowsWhenPassingNoArgsAndThereAreNoNuSpecFiles() {
            // Arrange 
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(NoSpecsfolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(1, result);
            Assert.AreEqual("Please specify a nuspec or project file to use.", consoleOutput.ToString().Trim());
        }

        [TestMethod]
        public void PackageCommand_ThrowsWhenPassingNoArgsAndThereIsMoreThanOneNuSpecFile() {
            // Arrange
            string nuspecFile = Path.Combine(TwoSpecsFolder, "antlr.nuspec");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);
            string nuspecFile2 = Path.Combine(TwoSpecsFolder, "antlr2.nuspec");
            File.WriteAllText(nuspecFile2, NuSpecFileContext.FileContents);
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(TwoSpecsFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(1, result);
            Assert.AreEqual("Please specify a nuspec or project file to use.", consoleOutput.ToString().Trim());
        }

        [TestMethod]
        public void PackageCommand_CreatesPackageWhenPassingNoArgsAndThereOneNuSpecFile() {
            //Arrange
            string nuspecFile = Path.Combine(OneSpecfolder, "antlr.nuspec");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);
            File.WriteAllText(Path.Combine(OneSpecfolder, "foo.txt"), "test");
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(OneSpecfolder);

            //Act
            int result = Program.Main(args);

            //Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(consoleOutput.ToString().Contains("Successfully created package"));
        }

        [TestMethod]
        public void PackageCommand_CreatesPackageWhenPassingBasePath() {
            //Arrange
            string nuspecFile = Path.Combine(OneSpecfolder, "Antlr.nuspec");
            string expectedPackage = Path.Combine("..\\output\\", "Antlr.3.1.1.nupkg");
            File.WriteAllText(Path.Combine(OneSpecfolder, "foo.txt"), "test");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);
            string[] args = new string[] { "pack", "-o", "..\\output\\" };
            Directory.SetCurrentDirectory(OneSpecfolder);

            //Act
            int result = Program.Main(args);

            //Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.IsTrue(File.Exists(expectedPackage));
        }

        [TestMethod]
        public void PackageCommand_SpecifyingFilesInNuspecOnlyPackagesSpecifiedFiles() {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "SpecWithFiles.nuspec");
            string expectedPackage = "test.1.1.1.nupkg";
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file1.txt"), "file 1");
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file2.txt"), "file 2");
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file3.txt"), "file 3");
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Terence Parr</authors>
    <description>ANother Tool for Language Recognition, is a language tool that provides a framework for constructing recognizers, interpreters, compilers, and translators from grammatical descriptions containing actions in a variety of target languages.</description>
    <language>en-US</language>
  </metadata>
  <files>
    <file src=""file1.txt"" target=""content"" />
  </files>
</package>");
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.IsTrue(File.Exists(expectedPackage));

            VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt" });
        }

        [TestMethod]
        public void PackageCommand_SpecifyingProjectFileCreatesPackageAndSymbolsPackge() {
            // Arrange            
            string expectedPackage = "FakeProject.1.2.0.0.nupkg";
            string expectedSymbolsPackage = "FakeProject.1.2.0.0.symbols.nupkg";
            File.WriteAllText(Path.Combine(ProjectFilesFolder, "Runner.cs"), @"using System;
public class Runner { 
    public static void Run() { 
        Console.WriteLine(""Hello World"");
    }
}");
            CreateAssemblyInfo("FakeProject",
                               "1.2.0.0",
                               "David Inc",
                               "This is a test. Ignore me");

            CreateProject("FakeProject", compile: new[] { "Runner.cs" });

            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.IsTrue(File.Exists(expectedPackage));
            Assert.IsTrue(File.Exists(expectedSymbolsPackage));

            var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\FakeProject.dll" });
            Assert.AreEqual("FakeProject", package.Id);
            Assert.AreEqual(new Version("1.2.0.0"), package.Version);
            Assert.AreEqual("David Inc", package.Authors.First());
            Assert.AreEqual("This is a test. Ignore me", package.Description);
            VerifyPackageContents(expectedSymbolsPackage, new[] { @"src\Runner.cs", 
                                                                  @"src\Properties\AssemblyInfo.cs",
                                                                  @"lib\net40\FakeProject.dll",
                                                                  @"lib\net40\FakeProject.pdb" });
        }

        [TestMethod]
        public void PackageCommand_SpecifyingProjectFilePacksContentAndOutput() {
            // Arrange                        
            string expectedPackage = "ProjectWithCotent.1.5.0.0.nupkg";
            var contentFiles = new[] { "Foo.xml", "Bar.txt" };
            var sourceFiles = new[] { "A.cs", "B.cs" };

            foreach (var contentFile in contentFiles) {
                File.WriteAllText(Path.Combine(ProjectFilesFolder, contentFile), contentFile);
            }

            int index = 0;
            foreach (var sourceFile in sourceFiles) {
                string path = Path.Combine(ProjectFilesFolder, sourceFile);
                File.WriteAllText(path, String.Format(@"using System;
public class Cl_{0} {{
    public void Foo() {{ }}
}}
", index++));
            }

            CreateAssemblyInfo("ProjectWithCotent",
                               "1.5.0.0",
                               "David",
                               "Project with content");

            CreateProject("ProjectWithCotent", content: contentFiles, compile: sourceFiles);

            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(0, result);
            Assert.IsTrue(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.IsTrue(File.Exists(expectedPackage));

            var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\ProjectWithCotent.dll",
                                                                         @"content\Foo.xml",
                                                                         @"content\Bar.txt" });
            Assert.AreEqual("ProjectWithCotent", package.Id);
            Assert.AreEqual(new Version("1.5.0.0"), package.Version);
            Assert.AreEqual("David", package.Authors.First());
            Assert.AreEqual("Project with content", package.Description);
        }

        [TestMethod]
        public void PackageCommand_WhenErrorIsThrownPackageFileIsDeleted() {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "SpecWithErrors.nuspec");
            string expectedPackage = "hello world.1.1.1.nupkg";
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>hello world</id>
    <version>1.1.1</version>
    <authors>Bar</authors>
    <description>Foo</description>
    <language>en-US</language>
  </metadata>
</package>");
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.AreEqual(1, result);
            Assert.IsFalse(File.Exists(expectedPackage));
        }

        private ZipPackage VerifyPackageContents(string packageFile, IEnumerable<string> expectedFiles) {
            var package = new ZipPackage(packageFile);
            var files = package.GetFiles().Select(f => f.Path).OrderBy(f => f).ToList();
            CollectionAssert.AreEqual(expectedFiles.OrderBy(f => f).ToList(), files);
            return package;
        }

        private void CreateProject(string projectName, IEnumerable<string> content = null, IEnumerable<string> compile = null) {
            string projectFile = Path.Combine(ProjectFilesFolder, projectName + ".csproj");
            File.WriteAllText(projectFile, GetProjectContent(projectName, compile: compile, content: content));
        }

        private static string GetProjectContent(string projectName, string targetFrameworkVersion = "4.0", IEnumerable<string> compile = null, IEnumerable<string> content = null) {
            compile = compile ?? Enumerable.Empty<string>();
            content = content ?? Enumerable.Empty<string>();
            string compileItemGroup = String.Join(Environment.NewLine, compile.Select(path => String.Format(@"<Compile Include=""{0}"" />", path)));
            string contentItemGroup = String.Join(Environment.NewLine, content.Select(path => String.Format(@"<Content Include=""{0}"" />", path)));
            return String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{{572A487C-B388-4490-B7E8-0382ABDAF729}}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>{0}</RootNamespace>
    <AssemblyName>{0}</AssemblyName>
    <TargetFrameworkVersion>v{1}</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Data.DataSetExtensions"" />
    <Reference Include=""Microsoft.CSharp"" />
    <Reference Include=""System.Data"" />
    <Reference Include=""System.Xml"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""Properties\AssemblyInfo.cs"" />
  </ItemGroup>
  <ItemGroup>
    {2}
  </ItemGroup>
  <ItemGroup>
    {3}
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>
", projectName, targetFrameworkVersion, contentItemGroup, compileItemGroup);
        }

        private static void CreateAssemblyInfo(string assemblyName, string version, string author, string description) {
            var propertiesDir = Path.Combine(ProjectFilesFolder, "Properties");
            Directory.CreateDirectory(propertiesDir);
            string assemblyInfo = Path.Combine(propertiesDir, "AssemblyInfo.cs");
            File.WriteAllText(assemblyInfo,
                              GetAssemblyInfoContent(assemblyName, version, author, description));
        }

        private static string GetAssemblyInfoContent(string assemblyName, string version, string author, string description) {
            return String.Format(@"using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle(""{0}"")]
[assembly: AssemblyDescription(""{3}"")]
[assembly: AssemblyConfiguration("""")]
[assembly: AssemblyCompany(""{2}"")]
[assembly: AssemblyProduct(""{0}"")]
[assembly: AssemblyCopyright(""Copyright © NuGet"")]
[assembly: AssemblyTrademark("""")]
[assembly: AssemblyCulture("""")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion(""{1}"")]
[assembly: AssemblyFileVersion(""{1}"")]
", assemblyName, version, author, description);
        }

        private static void DeleteDirs() {
            DeleteDir(NoSpecsfolder);
            DeleteDir(OneSpecfolder);
            DeleteDir(TwoSpecsFolder);
            DeleteDir(SpecificFilesFolder);
            DeleteDir(OutputFolder);
            DeleteDir(ProjectFilesFolder);
        }

        private static void DeleteDir(string directory) {
            try {
                if (Directory.Exists(directory)) {
                    foreach (var file in Directory.GetFiles(directory)) {
                        try {
                            File.Delete(file);
                        }
                        catch (FileNotFoundException) {

                        }
                    }
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch (DirectoryNotFoundException) {

            }
        }
    }
}