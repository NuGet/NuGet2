using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class NuGetCommandLineTest : IDisposable, IUseFixture<NugetProgramStatic>
    {
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

        public NuGetCommandLineTest()
        {
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

        public void Dispose()
        {
            DeleteDirs();
            System.Console.SetOut(originalConsoleOutput);
            System.Console.SetError(originalErrorConsoleOutput);
            Directory.SetCurrentDirectory(startingDirectory);
        }


        [Fact]
        public void NuGetCommandLine_ShowsHelpIfThereIsNoCommand()
        {
            // Arrange 
            string[] args = new string[0];

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("usage: NuGet <command> [args] [options]"));
        }

        [Fact]
        public void PackageCommand_ThrowsWhenPassingNoArgsAndThereAreNoNuSpecFiles()
        {
            // Arrange 
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(NoSpecsfolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            Assert.Equal("Please specify a nuspec or project file to use.", consoleOutput.ToString().Trim());
        }

        [Fact]
        public void PackageCommand_ThrowsWhenPassingNoArgsAndThereIsMoreThanOneNuSpecFile()
        {
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
            Assert.Equal(1, result);
            Assert.Equal("Please specify a nuspec or project file to use.", consoleOutput.ToString().Trim());
        }

        [Fact]
        public void PackageCommand_CreatesPackageWhenPassingNoArgsAndThereOneNuSpecFile()
        {
            //Arrange
            string nuspecFile = Path.Combine(OneSpecfolder, "antlr.nuspec");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);
            File.WriteAllText(Path.Combine(OneSpecfolder, "foo.txt"), "test");
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(OneSpecfolder);

            //Act
            int result = Program.Main(args);

            //Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
        }

        [Fact]
        public void PackageCommand_CreatesPackageWhenPassingBasePath()
        {
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
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));
        }

        [Fact]
        public void PackageCommand_ShowConsistentErrorMessageWhenNuspecHasInvalidID1()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "InvalidId.nuspec");
            string expectedPackage = "InvalidId.nupkg";
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test\id</id>
    <version>1.0</version>
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
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("Attempting to build package from 'InvalidId.nuspec'.\r\nThe package ID 'test\\id' contains invalid characters. Examples of valid package IDs include 'MyPackage' and 'MyPackage.Sample'.\r\n"));
            Assert.False(File.Exists(expectedPackage));
        }

        [Fact]
        public void PackageCommand_ShowConsistentErrorMessageWhenNuspecHasInvalidID2()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "InvalidId.nuspec");
            string expectedPackage = "InvalidId.nupkg";
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test:id</id>
    <version>1.0</version>
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
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("Attempting to build package from 'InvalidId.nuspec'.\r\nThe package ID 'test:id' contains invalid characters. Examples of valid package IDs include 'MyPackage' and 'MyPackage.Sample'.\r\n"));
            Assert.False(File.Exists(expectedPackage));
        }

        [Fact]
        public void PackageCommand_ShowConsistentErrorMessageWhenNuspecHasInvalidID3()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "InvalidId.nuspec");
            string expectedPackage = "InvalidId.nupkg";
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test|id</id>
    <version>1.0</version>
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
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("Attempting to build package from 'InvalidId.nuspec'.\r\nThe package ID 'test|id' contains invalid characters. Examples of valid package IDs include 'MyPackage' and 'MyPackage.Sample'.\r\n"));
            Assert.False(File.Exists(expectedPackage));
        }

        [Fact]
        public void PackageCommand_ShowConsistentErrorMessageWhenNuspecHasInvalidID4()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "InvalidId.nuspec");
            string expectedPackage = "InvalidId.nupkg";
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test/id</id>
    <version>1.0</version>
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
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("Attempting to build package from 'InvalidId.nuspec'.\r\nThe package ID 'test/id' contains invalid characters. Examples of valid package IDs include 'MyPackage' and 'MyPackage.Sample'.\r\n"));
            Assert.False(File.Exists(expectedPackage));
        }

        [Fact]
        public void PackageCommand_ShowConsistentErrorMessageWhenNuspecHasIDExceedingMaxLength()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "InvalidId.nuspec");
            string expectedPackage = "InvalidId.nupkg";
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa9</id>
    <version>1.0</version>
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
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("Attempting to build package from 'InvalidId.nuspec'.\r\nId must not exceed 100 characters.\r\n"));
            Assert.False(File.Exists(expectedPackage));
        }

        [Fact]
        public void PackageCommand_ShowConsistentErrorMessageWhenNuspecHasVersionExceedingMaxLength()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "InvalidId.nuspec");
            string expectedPackage = "InvalidId.nupkg";
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>two</id>
    <version>1.0-wwwwwwwwwwwwwwwwwwww0</version>
    <authors>Terence Parr</authors>
    <description>ANother Tool for Language Recognition, is a language tool that provides a framework for constructing recognizers, interpreters, compilers, and translators from grammatical descriptions containing actions in a variety of target languages.</description>
    <language>en-US</language>
    <dependencies>
       <dependency id=""X"" />
    </dependencies>
  </metadata>

</package>");

            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("Attempting to build package from 'InvalidId.nuspec'.\r\nThe special version part cannot exceed 20 characters.\r\n"));
            Assert.False(File.Exists(expectedPackage));
        }

        [Fact]
        public void PackageCommand_SpecifyingFilesInNuspecOnlyPackagesSpecifiedFiles()
        {
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
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt" });
        }

        [Fact]
        public void PackageCommand_SpecifyingEmptyFilesElementInNuspecPackagesNoFiles()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "SpecWithFiles.nuspec");
            string expectedPackage = "empty.2.2.2.nupkg";
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file1.txt"), "file 1");
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file2.txt"), "file 2");
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file3.txt"), "file 3");
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>empty</id>
    <version>2.2.2</version>
    <authors>Terence Parr</authors>
    <description>ANother Tool for Language Recognition, is a language tool that provides a framework for constructing recognizers, interpreters, compilers, and translators from grammatical descriptions containing actions in a variety of target languages.</description>
    <language>en-US</language>
    <dependencies>
      <dependency id=""aaa"" />
    </dependencies>
  </metadata>
  <files />
</package>");
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            VerifyPackageContents(expectedPackage, new string[0]);
        }

        [Fact]
        public void PackageCommand_AcceptEmptyDependenciesElement()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "SpecWithFiles.nuspec");
            string expectedPackage = "dep.2.2.2.nupkg";
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file1.txt"), "file 1");
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>dep</id>
    <version>2.2.2</version>
    <authors>Terence Parr</authors>
    <description>ANother Tool for Language Recognition, is a language tool that provides a framework for constructing recognizers, interpreters, compilers, and translators from grammatical descriptions containing actions in a variety of target languages.</description>
    <language>en-US</language>
    <dependencies>
    </dependencies>
  </metadata>
</package>");
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            ZipPackage package = VerifyPackageContents(expectedPackage, new[] { @"file1.txt" });

            Assert.False(package.Dependencies.Any());
        }

        [Fact]
        public void PackageCommand_AcceptEmptyFrameworkAssemblyElement()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "SpecWithFiles.nuspec");
            string expectedPackage = "framework.2.2.2.nupkg";
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "file1.txt"), "file 1");
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>framework</id>
    <version>2.2.2</version>
    <authors>Terence Parr</authors>
    <description>ANother Tool for Language Recognition, is a language tool that provides a framework for constructing recognizers, interpreters, compilers, and translators from grammatical descriptions containing actions in a variety of target languages.</description>
    <language>en-US</language>
    <frameworkAssemblies>
    </frameworkAssemblies>
  </metadata>
</package>");
            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            ZipPackage package = VerifyPackageContents(expectedPackage, new[] { @"file1.txt" });

            Assert.False(package.FrameworkAssemblies.Any());
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileCreatesPackageAndSymbolsPackge()
        {
            // Arrange            
            string expectedPackage = "FakeProject.1.2.0.0.nupkg";
            string expectedSymbolsPackage = "FakeProject.1.2.0.0.symbols.nupkg";

            WriteProjectFile("Runner.cs", @"using System;
public class Runner { 
    public static void Run() { 
        Console.WriteLine(""Hello World"");
    }
}");
            WriteProjectFile(@"..\Foo.cs", @"using System;
public class Foo { 
    public static void Run() { 
        Console.WriteLine(""Hello World"");
    }
}");
            WriteProjectFile(@"Bar.cs", @"using System;
public class Bar { 
    public static void Run() { 
        Console.WriteLine(""Hello World"");
    }
}");
            WriteProjectFile(@"..\Baz.cs", @"using System;
public class Baz { 
    public static void Run() { 
        Console.WriteLine(""Hello World"");
    }
}");
            WriteAssemblyInfo("FakeProject",
                               "1.2.0.0",
                               "David Inc",
                               "This is a test. Ignore me");

            CreateProject("FakeProject",
                          compile: new[] { "Runner.cs", @"..\Foo.cs", @"..\projects\Bar.cs" },
                          links: new[] { Tuple.Create(@"..\Baz.cs", @"Folder\Baz.cs") });

            string[] args = new string[] { "pack", "-Symbols", "-Build" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));
            var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\FakeProject.dll" });
            Assert.Equal("FakeProject", package.Id);
            Assert.Equal(new SemanticVersion("1.2"), package.Version);
            Assert.Equal("David Inc", package.Authors.First());
            Assert.Equal("This is a test. Ignore me", package.Description);
            Assert.True(File.Exists(expectedSymbolsPackage));
            VerifyPackageContents(expectedSymbolsPackage, new[] { @"src\Foo.cs",
                                                                  @"src\Runner.cs",
                                                                  @"src\Folder\Baz.cs",
                                                                  @"src\Bar.cs",
                                                                  @"src\Properties\AssemblyInfo.cs",
                                                                  @"lib\net40\FakeProject.dll",
                                                                  @"lib\net40\FakeProject.pdb" });
        }


        [Fact]
        public void PackageCommand_SpecifyingProjectFilePacksContentAndOutput()
        {
            // Arrange                        
            string expectedPackage = "ProjectWithCotent.1.5.0.0.nupkg";
            var contentFiles = new[] { "Foo.xml", "Bar.txt" };
            var sourceFiles = new[] { "A.cs", "B.cs" };

            foreach (var contentFile in contentFiles)
            {
                WriteProjectFile(contentFile, contentFile);
            }

            int index = 0;
            foreach (var sourceFile in sourceFiles)
            {
                WriteProjectFile(sourceFile, String.Format(@"using System;
public class Cl_{0} {{
    public void Foo() {{ }}
}}
", index++));
            }

            WriteAssemblyInfo("ProjectWithCotent",
                               "1.5.0.0",
                               "David",
                               "Project with content");

            CreateProject("ProjectWithCotent", content: contentFiles, compile: sourceFiles);

            string[] args = new string[] { "pack", "-Build" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\ProjectWithCotent.dll",
                                                                         @"content\Foo.xml",
                                                                         @"content\Bar.txt" });
            Assert.Equal("ProjectWithCotent", package.Id);
            Assert.Equal(new SemanticVersion("1.5"), package.Version);
            Assert.Equal("David", package.Authors.First());
            Assert.Equal("Project with content", package.Description);
        }

        [Fact]
        public void PackageCommand_TestDefaultPackageIssueRules()
        {
            //Arrange
            string nuspecFile = Path.Combine(OneSpecfolder, "beta.nuspec");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);

            // violated rule: Invalid Framework Folder
            Directory.CreateDirectory(Path.Combine(OneSpecfolder, "lib"));
            Directory.CreateDirectory(Path.Combine(OneSpecfolder, "lib", "unknown"));
            File.WriteAllText(Path.Combine(OneSpecfolder, "lib\\unknown\\abc.dll"), "assembly");

            // violated rule: Assembly placed directly under lib
            File.WriteAllText(Path.Combine(OneSpecfolder, "lib\\def.dll"), "assembly");

            // violated rule: Assembly placed directly under lib
            Directory.CreateDirectory(Path.Combine(OneSpecfolder, "content"));
            File.WriteAllText(Path.Combine(OneSpecfolder, "content\\hello.dll"), "assembly");

            // violated rule: Script file placed outside tools
            File.WriteAllText(Path.Combine(OneSpecfolder, "install.ps1"), "script");

            // violated rule: Unrecognized script file
            Directory.CreateDirectory(Path.Combine(OneSpecfolder, "tools"));
            File.WriteAllText(Path.Combine(OneSpecfolder, "tools\\myscript.ps1"), "script");

            // violated rule: transform file outside content folder
            File.WriteAllText(Path.Combine(OneSpecfolder, "tools\\web.config.transform"), "transform");

            // violated rule: non-assembly inside lib
            File.WriteAllText(Path.Combine(OneSpecfolder, "lib\\mylibrary.xml"), "xml");

            string[] args = new string[] { "pack" };
            Directory.SetCurrentDirectory(OneSpecfolder);

            //Act
            int result = Program.Main(args);

            //Assert
            Assert.Equal(0, result);
            string output = consoleOutput.ToString();
            Assert.True(output.Contains("Successfully created package"));

            // Asserts for package issues
            Assert.True(output.Contains("7 issue(s) found with package 'Antlr'."));
            Assert.True(output.Contains("Incompatible files in lib folder"));
            Assert.True(output.Contains("Invalid framework folder"));
            Assert.True(output.Contains("Assembly not inside a framework folder"));
            Assert.True(output.Contains("Assembly outside lib folder"));
            Assert.True(output.Contains("PowerScript file outside tools folder"));
            Assert.True(output.Contains("Unrecognized PowerScript file"));
            Assert.True(output.Contains("Incompatible files in lib folder"));
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileWithNuSpecWithTokensSubstitutesMetadataFromProject()
        {
            // Arrange
            string expectedPackage = "ProjectWithNuSpec.1.2.0.0.nupkg";
            WriteAssemblyInfo("ProjectWithNuSpec",
                               "1.2.0.0",
                               "David",
                               "Project with content",
                               "Title of Package");

            WriteProjectFile("foo.cs", "public class Foo { }");
            WriteProjectFile("package.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>$id$</id>
    <title>$title$</title>
    <version>$version$</version>
    <authors>$author$</authors>
    <description>Description from nuspec</description>
    <language>fr-FR</language>
    <tags>t1 t2</tags>
    <dependencies>
        <dependency id=""elmah"" version=""1.5"" />
    </dependencies>
    <frameworkAssemblies>
        <frameworkAssembly assemblyName=""System.Web"" />
    </frameworkAssemblies>
  </metadata>
</package>");

            CreateProject("ProjectWithNuSpec", content: new[] { "package.nuspec" },
                                               compile: new[] { "foo.cs" });

            string[] args = new string[] { "pack", "ProjectWithNuSpec.csproj", "-Build" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\ProjectWithNuSpec.dll" });
            Assert.Equal("ProjectWithNuSpec", package.Id);
            Assert.Equal(new SemanticVersion("1.2"), package.Version);
            Assert.Equal("Title of Package", package.Title);
            Assert.Equal("David", package.Authors.First());
            Assert.Equal("Description from nuspec", package.Description);
            var dependencies = package.Dependencies.ToList();
            Assert.Equal(1, dependencies.Count);
            Assert.Equal("elmah", dependencies[0].Id);
            var frameworkAssemblies = package.FrameworkAssemblies.ToList();
            Assert.Equal("System.Web", frameworkAssemblies[0].AssemblyName);
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileWithNuSpecWithEmptyFilesElementDoNotIncludeContentFiles()
        {
            // Arrange
            string expectedPackage = "ProjectWithNuSpecEmptyFiles.1.0.0.0.nupkg";
            WriteAssemblyInfo("ProjectWithNuSpecEmptyFiles",
                               "1.0.0.0",
                               "Luan",
                               "Project with content",
                               "Title of Package");

            WriteProjectFile("foo.cs", "public class Foo { }");
            WriteProjectFile("package.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>$id$</id>
    <title>$title$</title>
    <version>$version$</version>
    <authors>$author$</authors>
    <description>Description from nuspec</description>
    <tags>t1 t2</tags>
    <dependencies>
        <dependency id=""elmah"" version=""1.5"" />
    </dependencies>
  </metadata>
  <files />
</package>");
            WriteProjectFile("readme.txt", "This is so fun.");
            CreateProject("ProjectWithNuSpecEmptyFiles", content: new[] { "package.nuspec", "readme.txt" },
                                               compile: new[] { "foo.cs" });

            string[] args = new string[] { "pack", "ProjectWithNuSpecEmptyFiles.csproj", "-Build" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\ProjectWithNuSpecEmptyFiles.dll" });
            Assert.False(package.GetFiles("content").Any());
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileWithNuSpecNamedAfterProjectUsesNuSpecForMetadata()
        {
            // Arrange                        
            string expectedPackage = "Test.1.2.nupkg";
            WriteAssemblyInfo("FooProject",
                               "1.5.0.0",
                               "David",
                               "Project with content");

            WriteProjectFile("foo.cs", "public class Foo { }");
            WriteProjectFile("FooProject.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>Test</id>
    <version>1.2</version>
    <description>Description from nuspec</description>    
    <authors>John</authors>
  </metadata>
</package>");

            CreateProject("FooProject", compile: new[] { "foo.cs" });

            string[] args = new string[] { "pack", "FooProject.csproj", "-Build" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\FooProject.dll" });
            Assert.Equal("Test", package.Id);
            Assert.Equal(new SemanticVersion("1.2"), package.Version);
            Assert.Equal("Description from nuspec", package.Description);
            Assert.Equal("John", package.Authors.First());
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileWithNoBuildThrowsIfProjectNotBuilt()
        {
            // Arrange                        
            WriteAssemblyInfo("ProjectNoBuild",
                               "1.5.0.0",
                               "David",
                               "Project with content");

            WriteProjectFile("foo.cs", "public class Foo { }");
            CreateProject("ProjectNoBuild", compile: new[] { "foo.cs" });

            string[] args = new string[] { "pack", "ProjectNoBuild.csproj" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("Make sure the project has been built."));
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileWithNuSpecWithUnsupportedTokensThrows()
        {
            // Arrange                        
            string expectedPackage = "ProjectWithBrokenNuSpec.1.2.nupkg";
            WriteAssemblyInfo("ProjectWithBrokenNuSpec",
                               "1.2.0.0",
                               "David",
                               "Project with content");

            WriteProjectFile("foo.cs", "public class Foo { }");
            WriteProjectFile("package.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>$id2$</id>
    <version>$version$</version>
    <authors>$author$</authors>
    <description>Description from nuspec</description>
  </metadata>
</package>");

            CreateProject("ProjectWithBrokenNuSpec", content: new[] { "package.nuspec" },
                                               compile: new[] { "foo.cs" });

            string[] args = new string[] { "pack", "ProjectWithBrokenNuSpec.csproj", "-Build" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("The replacement token 'id2' has no value."));
            Assert.False(File.Exists(expectedPackage));
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileAndNuSpecWithFilesMergesFiles()
        {
            // Arrange                        
            string expectedPackage = "ProjectWithNuSpecAndFiles.1.3.0.0.nupkg";
            WriteAssemblyInfo("ProjectWithNuSpecAndFiles",
                               "1.3.0.0",
                               "David2",
                               "Project with nuspec that has files");

            WriteProjectFile("foo.cs", "public class Foo { }");
            WriteProjectFile("package.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>$AssemblyName$</id>   
    <version>$version$</version>
    <description>$description$</description>
    <authors>$author$</authors>
  </metadata>
  <files>
    <file src=""bin\Debug\*.dll"" target=""lib\net40"" />
    <file src=""bin\Debug\*.pdb"" target=""lib\net40"" />
    <file src=""bin\Debug\*.xml"" target=""lib\net40"" />
  </files>
</package>");

            CreateProject("ProjectWithNuSpecAndFiles", content: new[] { "package.nuspec" },
                                                       compile: new[] { "foo.cs" });

            string[] args = new string[] { "pack", "ProjectWithNuSpecAndFiles.csproj", "-Build" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\ProjectWithNuSpecAndFiles.dll", 
                                                                         @"lib\net40\ProjectWithNuSpecAndFiles.pdb" });
            Assert.Equal("ProjectWithNuSpecAndFiles", package.Id);
            Assert.Equal(new SemanticVersion("1.3"), package.Version);
            Assert.Equal("David2", package.Authors.First());
            Assert.Equal("Project with nuspec that has files", package.Description);
        }

        [Fact]
        public void PackageCommand_PrefersProjectFileIfNuSpecAndProjectFileAreInTheSameDirectory()
        {
            // Arrange                        
            string expectedPackage = "ProjectWithNuSpecProjectWins.1.2.0.0.nupkg";
            WriteAssemblyInfo("ProjectWithNuSpecProjectWins",
                               "1.2.0.0",
                               "David2",
                               "Project with nuspec");

            WriteProjectFile("foo.cs", "public class Foo { }");
            WriteProjectFile("package.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>$AssemblyName$</id>   
    <version>$version$</version>
    <description>$description$</description>
    <authors>$author$</authors>
  </metadata>
</package>");

            CreateProject("ProjectWithNuSpecProjectWins", content: new[] { "package.nuspec" },
                                                       compile: new[] { "foo.cs" });

            string[] args = new string[] { "pack", "-Build" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\ProjectWithNuSpecProjectWins.dll" });
            Assert.Equal("ProjectWithNuSpecProjectWins", package.Id);
            Assert.Equal(new SemanticVersion("1.2"), package.Version);
            Assert.Equal("David2", package.Authors.First());
            Assert.Equal("Project with nuspec", package.Description);
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectOnlyPacksAssemblyThatProjectProduced()
        {
            // Arrange                        
            string expectedPackage = "ProjectWithAssembliesInOutputPath.1.3.0.0.nupkg";
            WriteAssemblyInfo("ProjectWithAssembliesInOutputPath",
                               "1.3.0.0",
                               "David2",
                               "Project with nuspec that has files");

            WriteProjectFile("foo.cs", "public class Foo { }");
            WriteProjectFile(@"bin\Debug\Fake.dll", "Some fakedll");
            WriteProjectFile(@"bin\Debug\ProjectWithAssembliesInOutputPath.Fake.dll", "Some fakedll");

            CreateProject("ProjectWithAssembliesInOutputPath", compile: new[] { "foo.cs" });

            string[] args = new string[] { "pack", "ProjectWithAssembliesInOutputPath.csproj", "-Build" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\ProjectWithAssembliesInOutputPath.dll" });
            Assert.Equal("ProjectWithAssembliesInOutputPath", package.Id);
            Assert.Equal(new SemanticVersion("1.3"), package.Version);
            Assert.Equal("David2", package.Authors.First());
            Assert.Equal("Project with nuspec that has files", package.Description);
        }

        [Fact]
        public void PackageCommand_WhenErrorIsThrownPackageFileIsDeleted()
        {
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
            Assert.Equal(1, result);
            Assert.False(File.Exists(expectedPackage));
        }


        [Fact]
        public void PackCommandAllowsPassingPropertiesFromCommandLine()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "SpecWithProperties.nuspec");
            string expectedPackage = "foo.1.1.nupkg";
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "foo.txt"), "test");
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>$id$</id>   
    <version>$version$</version>
    <description>Desc</description>
    <authors>Auth</authors>
  </metadata>
</package>");
            string[] args = new string[] { "pack", "/p", "id=foo;version=1.1" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(File.Exists(expectedPackage));
            var package = VerifyPackageContents(expectedPackage, new[] { @"foo.txt" });
            Assert.Equal("foo", package.Id);
            Assert.Equal(new SemanticVersion("1.1"), package.Version);
            Assert.Equal("Auth", package.Authors.First());
            Assert.Equal("Desc", package.Description);
        }

        [Fact]
        public void PackCommandAllowsPassingVersionSetsVersionProperty()
        {
            // Arrange            
            string nuspecFile = Path.Combine(SpecificFilesFolder, "SpecWithProperties.nuspec");
            string expectedPackage = "MyPackage.2.5.nupkg";
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "foo.txt"), "test");
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>MyPackage</id>   
    <version>$version$</version>
    <description>Desc</description>
    <authors>Auth</authors>
  </metadata>
</package>");
            string[] args = new string[] { "pack", "/version", "2.5" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(File.Exists(expectedPackage));
            var package = VerifyPackageContents(expectedPackage, new[] { @"foo.txt" });
            Assert.Equal("MyPackage", package.Id);
            Assert.Equal(new SemanticVersion("2.5"), package.Version);
            Assert.Equal("Auth", package.Authors.First());
            Assert.Equal("Desc", package.Description);
        }

        [Fact]
        public void UpdateCommandThrowsWithNoArguments()
        {
            // Arrange            
            var args = new string[] { "update" };

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("No packages.config or solution file specified. Use the -self switch to update NuGet.exe."));
        }

        [Fact]
        public void UpdateCommandWithInvalidFileThrows()
        {
            // Arrange            
            var args = new string[] { "update", "lolz.txt" };

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("No packages.config or solution file specified."));
        }

        private ZipPackage VerifyPackageContents(string packageFile, IEnumerable<string> expectedFiles)
        {
            var package = new ZipPackage(packageFile);
            var files = package.GetFiles().Select(f => f.Path).OrderBy(f => f).ToArray();
            Assert.Equal(expectedFiles.OrderBy(f => f).ToArray(), files);
            return package;
        }

        private void CreateProject(string projectName, IEnumerable<string> content = null, IEnumerable<string> compile = null, IEnumerable<Tuple<string, string>> links = null)
        {
            string projectFile = Path.Combine(ProjectFilesFolder, projectName + ".csproj");
            File.WriteAllText(projectFile, GetProjectContent(projectName, compile: compile, content: content, links: links));
        }

        private static string GetProjectContent(string projectName,
                                                string targetFrameworkVersion = "4.0",
                                                IEnumerable<string> compile = null,
                                                IEnumerable<string> content = null,
                                                IEnumerable<Tuple<string, string>> links = null)
        {
            compile = compile ?? Enumerable.Empty<string>();
            content = content ?? Enumerable.Empty<string>();
            links = links ?? Enumerable.Empty<Tuple<string, string>>();
            string compileItemGroup = String.Join(Environment.NewLine, compile.Select(path => String.Format(@"<Compile Include=""{0}"" />", path)));
            string contentItemGroup = String.Join(Environment.NewLine, content.Select(path => String.Format(@"<Content Include=""{0}"" />", path)));
            string linkItemGroup = String.Join(Environment.NewLine, links.Select(link => String.Format(@"<Compile Include=""{0}""><Link>{1}</Link></Compile>", link.Item1, link.Item2)));
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
  <ItemGroup>
    {4}
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>
", projectName, targetFrameworkVersion, contentItemGroup, compileItemGroup, linkItemGroup);
        }

        private static void WriteProjectFile(string path, string contents)
        {
            string fullPath = Path.Combine(ProjectFilesFolder, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, contents);
        }

        private static void WriteAssemblyInfo(string assemblyName, string version, string author, string description)
        {
            WriteAssemblyInfo(assemblyName, version, author, description, null);
        }

        private static void WriteAssemblyInfo(string assemblyName, string version, string author, string description, string title)
        {
            WriteProjectFile(@"Properties\AssemblyInfo.cs", GetAssemblyInfoContent(assemblyName, version, author, description, title));
        }

        private static string GetAssemblyInfoContent(string assemblyName, string version, string author, string description, string title)
        {
            return String.Format(@"using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle(""{4}"")]
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
", assemblyName, version, author, description, title);
        }

        private static void DeleteDirs()
        {
            DeleteDir(NoSpecsfolder);
            DeleteDir(OneSpecfolder);
            DeleteDir(TwoSpecsFolder);
            DeleteDir(SpecificFilesFolder);
            DeleteDir(OutputFolder);
            DeleteDir(ProjectFilesFolder);
        }

        private static void DeleteDir(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    foreach (var file in Directory.GetFiles(directory))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (FileNotFoundException)
                        {

                        }
                    }
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch (DirectoryNotFoundException)
            {

            }
        }

        public void SetFixture(NugetProgramStatic data)
        {
            //use fixture sets up / tears down the static (awesome idea! <sarcasm/>) use extensions.
        }
    }
}