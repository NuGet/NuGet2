using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class NuGetCommandLineTest : IDisposable, IUseFixture<NugetProgramStatic>
    {
        private static readonly string _testRootDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        private static readonly string NoSpecsfolder = Path.Combine(_testRootDirectory, @"nospecs");
        private static readonly string OneSpecfolder = Path.Combine(_testRootDirectory, @"onespec");
        private static readonly string TwoSpecsFolder = Path.Combine(_testRootDirectory, @"twospecs");
        private static readonly string OutputFolder = Path.Combine(_testRootDirectory, @"output");
        private static readonly string SpecificFilesFolder = Path.Combine(_testRootDirectory, @"specific_files");
        private static readonly string ProjectFilesFolder = Path.Combine(_testRootDirectory, @"projects");

        private readonly StringWriter consoleOutput;
        private readonly TextWriter originalConsoleOutput;
        private readonly TextWriter originalErrorConsoleOutput;
        private readonly string startingDirectory;

        public NuGetCommandLineTest()
        {
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
            System.Console.SetOut(originalConsoleOutput);
            System.Console.SetError(originalErrorConsoleOutput);
            Directory.SetCurrentDirectory(startingDirectory);
            try
            {
                Directory.Delete(_testRootDirectory, recursive: true);
            }
            catch { }
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
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Theory]
        [InlineData("abcd")]
        [InlineData("1")]
        [InlineData("2ab")]
        [InlineData("1.2.3.4-alpha")]
        public void PackageCommand_ThrowsWhenMinClientVersionIsInvalid(string minVersionValue)
        {
            //Arrange
            string nuspecFile = Path.Combine(OneSpecfolder, "antlr.nuspec");
            File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);
            File.WriteAllText(Path.Combine(OneSpecfolder, "foo.txt"), "test");
            string[] args = new string[] { "pack", "-minClientVersion", minVersionValue };
            Directory.SetCurrentDirectory(OneSpecfolder);

            //Act
            int result = Program.Main(args);

            //Assert
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("The value of MinClientVersion argument is not a valid version."));
        }

        [Fact]
        public void PackageCommand_CreatesPackageAppliesMinClientVersionValue()
        {
            //Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine(OneSpecfolder, "antlr.nuspec");
                File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);
                File.WriteAllText(Path.Combine(OneSpecfolder, "foo.txt"), "test");
                string[] args = new string[] { "pack", "-minClientVersion", "2.4" };
                Directory.SetCurrentDirectory(OneSpecfolder);

                //Act
                int result = Program.Main(args);

                //Assert
                Assert.Equal(0, result);

                IPackage package = new OptimizedZipPackage(Path.GetFullPath("antlr.3.1.1.nupkg"));
                Assert.Equal(new Version("2.4"), package.MinClientVersion);
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void SetapikeyCommand_WithConfigOption()
        {
            var testDirectory = Path.Combine(_testRootDirectory, "testdir");
            Util.CreateDirectory(testDirectory);
            Util.CreateFile(testDirectory, "test_nuget.config", "<configuration/>");
            string[] args = new string[] 
            { 
                "setapikey", 
                "A", 
                "-ConfigFile", 
                Path.Combine(testDirectory, "test_nuget.config") 
            };
            int result = Program.Main(args);

            // Assert
            var settings = Settings.LoadDefaultSettings(
                new PhysicalFileSystem(testDirectory),
                "test_nuget.config",
                null);
            var apiKey = CommandLineUtility.GetApiKey(settings, NuGetConstants.DefaultGalleryServerUrl);
            Assert.Equal("A", apiKey);

            apiKey = CommandLineUtility.GetApiKey(settings, NuGetConstants.DefaultSymbolServerUrl);
            Assert.Equal("A", apiKey);
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
        public void PackageCommand_IncludeFileWithoutFileNameIfNoDefaultExcludeIsSet()
        {
            // Arrange
            string nuspecFile = Path.Combine(SpecificFilesFolder, "FileWithoutFileName.nuspec");
            string expectedPackage = "exon.1.0.0.nupkg";
            File.WriteAllText(Path.Combine(SpecificFilesFolder, ".htaccess"), "file 1");
            File.WriteAllText(Path.Combine(SpecificFilesFolder, ".clax"), "file 2");
            File.WriteAllText(Path.Combine(SpecificFilesFolder, "two.txt"), "file 3");
            File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>exon</id>
    <version>1.0.0</version>
    <authors>dotnetjunky</authors>
    <description>Why do you care.</description>
    <language>en-US</language>
  </metadata>
  <files>
    <file src="".htaccess"" target=""content"" />
    <file src=""*.clax"" target=""content"" />
    <file src=""two.txt"" target=""content"" />
  </files>
</package>");
            string[] args = new string[] { "pack", "-NoDefaultExcludes" };
            Directory.SetCurrentDirectory(SpecificFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists(expectedPackage));

            VerifyPackageContents(expectedPackage, new[] { @"content\.clax", @"content\.htaccess", @"content\two.txt" });
        }

        [Fact]
        public void PackageCommand_NotSpecifyingFilesElementPackagesEmptyFrameworkFolderInContent()
        {
            // Arrange         
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);

                string nuspecFile = Path.Combine("wow", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                Directory.CreateDirectory("wow\\content");
                Directory.CreateDirectory("wow\\content\\net40");
                File.WriteAllText("wow\\content\\file1.txt", "file 1");
                File.WriteAllText("wow\\content\\file2.txt", "file 2");
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
</package>");
                string[] args = new string[] { "pack" };
                Directory.SetCurrentDirectory("wow");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt", @"content\file2.txt", @"content\net40\_._" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_NotSpecifyingFilesElementPackagesEmptyFrameworkFolderInLib()
        {
            // Arrange            
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine("pta", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                Directory.CreateDirectory(Path.Combine("pta", "lib"));
                Directory.CreateDirectory(Path.Combine("pta", "lib", "net40"));
                File.WriteAllText(Path.Combine("pta", "lib\\file1.txt"), "file 1");
                File.WriteAllText(Path.Combine("pta", "lib\\file2.txt"), "file 2");
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
</package>");
                string[] args = new string[] { "pack" };
                Directory.SetCurrentDirectory("pta");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"lib\file1.txt", @"lib\file2.txt", @"lib\net40\_._" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_FileSourceEndsWithDirectoryCharPackageTheWholeDirectory()
        {
            // Arrange    
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine("hir", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                if (Directory.Exists("hir"))
                {
                    Directory.Delete("hir", recursive: true);
                }
                Directory.CreateDirectory(Path.Combine("hir", "lib"));
                Directory.CreateDirectory(Path.Combine("hir", "lib", "net40"));
                Directory.CreateDirectory(Path.Combine("hir", "lib", "net45"));
                Directory.CreateDirectory(Path.Combine("hir", "lib", "net45", "css"));
                Directory.CreateDirectory(Path.Combine("hir", "lib", "win8"));
                Directory.CreateDirectory(Path.Combine("hir", "lib", "win8", "js"));
                File.WriteAllText(Path.Combine("hir", "lib\\net45\\file1.txt"), "file 1");
                File.WriteAllText(Path.Combine("hir", "lib\\win8\\js\\file2.txt"), "file 2");
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
  <files>
    <file src=""lib\"" target=""content"" />
  </files>
</package>");
                string[] args = new string[] { "pack" };
                Directory.SetCurrentDirectory("hir");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"content\net45\file1.txt", @"content\net45\css\_._", @"content\win8\js\file2.txt", @"content\net40\_._" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Theory]
        [InlineData("lib\\net40\\")]
        [InlineData("lib\\net40")]
        public void PackageCommand_FileSourceEndsWithDirectoryCharPackageEmptyDirectory(string sourcePath)
        {
            // Arrange       
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine("tir", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                Directory.CreateDirectory(Path.Combine("tir", "lib"));
                Directory.CreateDirectory(Path.Combine("tir", "lib", "net40"));
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
  <files>
    <file src=""" + sourcePath + @""" target=""lib\net40"" />
  </files>
</package>");
                string[] args = new string[] { "pack" };
                Directory.SetCurrentDirectory("tir");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"lib\net40\_._" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_NotSpecfingFilesElementDoesNotPackageEmptyFrameworkFolderIfExcludeEmptyDirectoriesIsSet()
        {
            // Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine("bar", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                Directory.CreateDirectory(Path.Combine("bar", "content"));
                Directory.CreateDirectory(Path.Combine("bar", "content", "net40"));
                File.WriteAllText(Path.Combine("bar", "content\\file1.txt"), "file 1");
                File.WriteAllText(Path.Combine("bar", "content\\file2.txt"), "file 2");
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
</package>");
                string[] args = new string[] { "pack", "-ExcludeEmptyDirectories" };
                Directory.SetCurrentDirectory("bar");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt", @"content\file2.txt" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_FilesElementSearchIncludesEmptyFrameworkFolders()
        {
            // Arrange         
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine("cat", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                Directory.CreateDirectory(Path.Combine("cat", "content"));
                Directory.CreateDirectory(Path.Combine("cat", "content", "sl40"));
                File.WriteAllText(Path.Combine("cat", "content\\file1.txt"), "file 1");
                File.WriteAllText(Path.Combine("cat", "content\\file2.txt"), "file 2");
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
  <files>
     <file src=""**\*"" />
  </files>
</package>");
                string[] args = new string[] { "pack" };
                Directory.SetCurrentDirectory("cat");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt", @"content\file2.txt", @"content\sl40\_._" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_FilesElementSearchDoesNotIncludeEmptyFrameworkFoldersIfExcludeEmptyDirectoriesIsSet()
        {
            // Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine("ohm", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                Directory.CreateDirectory(Path.Combine("ohm", "content"));
                Directory.CreateDirectory(Path.Combine("ohm", "content", "sl40"));
                File.WriteAllText(Path.Combine("ohm", "content\\file1.txt"), "file 1");
                File.WriteAllText(Path.Combine("ohm", "content\\file2.txt"), "file 2");
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
  <files>
     <file src=""**\*"" />
  </files>
</package>");
                string[] args = new string[] { "pack", "-ExcludeEmptyDirectories" };
                Directory.SetCurrentDirectory("ohm");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt", @"content\file2.txt" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_FilesElementSearchDoesNotIncludeEmptyFrameworkFoldersIfExcluded()
        {
            // Arrange  
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine("pam", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                Directory.CreateDirectory(Path.Combine("pam", "content"));
                Directory.CreateDirectory(Path.Combine("pam", "content", "sl40"));
                File.WriteAllText(Path.Combine("pam", "content\\file1.txt"), "file 1");
                File.WriteAllText(Path.Combine("pam", "content\\file2.txt"), "file 2");
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
  <files>
     <file src=""**\*"" />
  </files>
</package>");
                string[] args = new string[] { "pack", "-Exclude", @"content\sl*" };
                Directory.SetCurrentDirectory("pam");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt", @"content\file2.txt" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_FilesElementSearchDoesNotIncludeEmptyFrameworkFoldersIfSearchPatternDoesNotMatch()
        {
            // Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine("nay", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                Directory.CreateDirectory(Path.Combine("nay", "content"));
                Directory.CreateDirectory(Path.Combine("nay", "content", "sl40"));
                File.WriteAllText(Path.Combine("nay", "content\\file1.txt"), "file 1");
                File.WriteAllText(Path.Combine("nay", "content\\file2.txt"), "file 2");
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
  <files>
     <file src=""**\*.*"" />
  </files>
</package>");
                string[] args = new string[] { "pack" };
                Directory.SetCurrentDirectory("nay");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt", @"content\file2.txt" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_FilesElementSearchDoesNotIncludeEmptyFrameworkFoldersIfSearchPatternDoesNotMatch2()
        {
            // Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine("qaw", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                Directory.CreateDirectory(Path.Combine("qaw", "content"));
                Directory.CreateDirectory(Path.Combine("qaw", "content", "winrt"));
                File.WriteAllText(Path.Combine("qaw", "content\\file1.txt"), "file 1");
                File.WriteAllText(Path.Combine("qaw", "content\\file2.txt"), "file 2");
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
  <files>
     <file src=""**\file*"" />
  </files>
</package>");
                string[] args = new string[] { "pack" };
                Directory.SetCurrentDirectory("qaw");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt", @"content\file2.txt" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_NotSpecfingFilesElementDoesNotPackageEmptyNormalFolder()
        {
            // Arrange      
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine("lil", "SpecWithFiles.nuspec");
                string expectedPackage = "test.1.1.1.nupkg";
                Directory.CreateDirectory(Path.Combine("lil", "content"));
                Directory.CreateDirectory(Path.Combine("lil", "content", "abc"));
                File.WriteAllText(Path.Combine("lil", "content\\file1.txt"), "file 1");
                File.WriteAllText(Path.Combine("lil", "content\\file2.txt"), "file 2");
                File.WriteAllText(nuspecFile, @"<?xml version=""1.0"" encoding=""utf-8""?>
<package>
  <metadata>
    <id>test</id>
    <version>1.1.1</version>
    <authors>Luan</authors>
    <description>Very cool.</description>
    <language>en-US</language>
  </metadata>
</package>");
                string[] args = new string[] { "pack" };
                Directory.SetCurrentDirectory("lil");

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                VerifyPackageContents(expectedPackage, new[] { @"content\file1.txt", @"content\file2.txt" });
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingEmptyFilesElementInNuspecPackagesNoFiles()
        {
            // Arrange 
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_AcceptEmptyDependenciesElement()
        {
            // Arrange 
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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

                Assert.False(package.DependencySets.Any());
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_AcceptEmptyFrameworkAssemblyElement()
        {
            // Arrange   
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileCreatesPackageAndSymbolsPackge()
        {
            // Arrange        
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFilePacksContentAndOutput()
        {
            // Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string expectedPackage = "ProjectWithContent.1.5.0.0.nupkg";
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

                WriteAssemblyInfo("ProjectWithContent",
                                   "1.5.0.0",
                                   "David",
                                   "Project with content");

                CreateProject("ProjectWithContent", content: contentFiles, compile: sourceFiles);

                string[] args = new string[] { "pack", "-Build" };
                Directory.SetCurrentDirectory(ProjectFilesFolder);

                // Act
                int result = Program.Main(args);

                // Assert
                Assert.Equal(0, result);
                Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                Assert.True(File.Exists(expectedPackage));

                var package = VerifyPackageContents(expectedPackage, new[] { @"lib\net40\ProjectWithContent.dll",
                                                                         @"content\Foo.xml",
                                                                         @"content\Bar.txt" });
                Assert.Equal("ProjectWithContent", package.Id);
                Assert.Equal(new SemanticVersion("1.5"), package.Version);
                Assert.Equal("David", package.Authors.First());
                Assert.Equal("Project with content", package.Description);
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_FailsIfPackageTypeIsSetToManagedAndOneOrMoreFilesDoNotConformToStrictTFMRules()
        {
            //Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine(OneSpecfolder, "beta.nuspec");
                File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);

                Directory.CreateDirectory(Path.Combine(OneSpecfolder, "lib", "unknown"));
                Directory.CreateDirectory(Path.Combine(OneSpecfolder, "content"));
                Directory.CreateDirectory(Path.Combine(OneSpecfolder, "tools"));
                File.WriteAllText(Path.Combine(OneSpecfolder, "lib\\unknown\\abc.dll"), "assembly");
                File.WriteAllText(Path.Combine(OneSpecfolder, "lib\\def.dll"), "assembly");
                File.WriteAllText(Path.Combine(OneSpecfolder, "content", "hello.txt"), "hello");
                File.WriteAllText(Path.Combine(OneSpecfolder, "tools", "install.ps1"), "script");

                string[] args = new string[] { "pack", "-type", "Managed", "-typeVersion", "2.0" };
                Directory.SetCurrentDirectory(OneSpecfolder);

                //Act
                int result = Program.Main(args);

                //Assert
                Assert.Equal(1, result);
                string output = consoleOutput.ToString();
                Assert.Contains("The following paths do not map to a well-known target framework: " +
                    @"content\hello.txt, lib\def.dll, tools\install.ps1.", output);
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_TestDefaultPackageIssueRules()
        {
            //Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
                Assert.Contains("9 issue(s) found with package 'Antlr'.", output);
                Assert.Contains("Description: The folder 'unknown' under 'lib' is not recognized as a valid framework " +
                    "name or a supported culture identifier.", output);
                Assert.Contains(@"The file 'content\hello.dll' is directly placed under the 'content' folder. " +
                    "Support for file paths that do not specify frameworks will be deprecated in the future.", output);
                Assert.Contains(@"The file 'lib\def.dll' is directly placed under the 'lib' folder. " +
                    "Support for file paths that do not specify frameworks will be deprecated in the future.", output);
                Assert.Contains(@"The file 'lib\mylibrary.xml' is directly placed under the 'lib' folder. " +
                    "Support for file paths that do not specify frameworks will be deprecated in the future.", output);
                Assert.Contains(@"The file 'tools\myscript.ps1' is directly placed under the 'tools' folder. " +
                    "Support for file paths that do not specify frameworks will be deprecated in the future.", output);
                Assert.Contains(@"The file 'tools\web.config.transform' is directly placed under the 'tools' folder. " +
                    "Support for file paths that do not specify frameworks will be deprecated in the future.", output);
                Assert.Contains(@"The script file 'install.ps1' is outside the 'tools' folder and hence will " +
                    "not be executed during installation of this package.", output);
                Assert.Contains(@"The transform file 'tools\web.config.transform' is outside the 'content' folder " +
                    "and hence will not be transformed during installation of this package.", output);
                Assert.Contains(@"The script file 'tools\myscript.ps1' is not recognized by NuGet and hence will not be executed "
                + "during installation of this package.", output);
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_TestDefaultPackageIssueRules_WithFilesInsideFrameworkAssembliesDirectory()
        {
            //Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string nuspecFile = Path.Combine(OneSpecfolder, "beta.nuspec");
                File.WriteAllText(nuspecFile, NuSpecFileContext.FileContents);

                // violated rule: Invalid Framework Folder
                Directory.CreateDirectory(Path.Combine(OneSpecfolder, "lib", "unknown"));
                Directory.CreateDirectory(Path.Combine(OneSpecfolder, "lib", "net40"));
                Directory.CreateDirectory(Path.Combine(OneSpecfolder, "content", "testfx"));
                Directory.CreateDirectory(Path.Combine(OneSpecfolder, "tools", "testfx45"));
                Directory.CreateDirectory(Path.Combine(OneSpecfolder, "tools", "net40"));
                File.WriteAllText(Path.Combine(OneSpecfolder, "lib\\unknown\\abc.dll"), "assembly");

                // violated rule: Assembly placed directly under lib

                File.WriteAllText(Path.Combine(OneSpecfolder, "lib\\net40\\def.dll"), "assembly");

                // violated rule: Assembly placed directly under lib
                File.WriteAllText(Path.Combine(OneSpecfolder, "content\\testfx\\hello.dll"), "assembly");

                // violated rule: Script file placed outside tools
                File.WriteAllText(Path.Combine(OneSpecfolder, "install.ps1"), "script");

                // violated rule: Unrecognized script file
                File.WriteAllText(Path.Combine(OneSpecfolder, "tools\\testfx45\\myscript.ps1"), "script");

                // violated rule: transform file outside content folder
                File.WriteAllText(Path.Combine(OneSpecfolder, "tools\\net40\\web.config.transform"), "transform");

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
                Assert.Contains("6 issue(s) found with package 'Antlr'.", output);
                Assert.Contains(@"The assembly 'content\testfx\hello.dll' is not inside the 'lib' folder and hence " +
                    "it won't be added as reference when the package is installed into a project.", output);
                Assert.Contains("Description: The folder 'unknown' under 'lib' is not recognized as a valid framework " +
                    "name or a supported culture identifier.", output);
                Assert.Contains(@"The file 'lib\mylibrary.xml' is directly placed under the 'lib' folder. " +
                    "Support for file paths that do not specify frameworks will be deprecated in the future.", output);
                Assert.Contains(@"The transform file 'tools\net40\web.config.transform' is outside the 'content' folder " +
                    "and hence will not be transformed during installation of this package.", output);
                Assert.Contains(@"The script file 'install.ps1' is outside the 'tools' folder and hence will " +
                    "not be executed during installation of this package.", output);
                Assert.Contains(@"The script file 'tools\testfx45\myscript.ps1' is not recognized by NuGet and hence will not be executed "
                + "during installation of this package.", output);
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileWithNuSpecWithTokensSubstitutesMetadataFromProject()
        {
            // Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
                var dependencySets = package.DependencySets.ToList();
                Assert.Equal(1, dependencySets.Count);
                var dependencies = dependencySets[0].Dependencies.ToList();
                Assert.Equal(1, dependencies.Count);
                Assert.Equal("elmah", dependencies[0].Id);
                var frameworkAssemblies = package.FrameworkAssemblies.ToList();
                Assert.Equal("System.Web", frameworkAssemblies[0].AssemblyName);
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileWithNuSpecWithEmptyFilesElementDoNotIncludeContentFiles()
        {
            // Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileWithNuSpecNamedAfterProjectUsesNuSpecForMetadata()
        {
            // Arrange       
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileWithNoBuildThrowsIfProjectNotBuilt()
        {
            // Arrange  
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileWithNuSpecWithUnsupportedTokensThrows()
        {
            // Arrange       
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileAndNuSpecWithFilesMergesFiles()
        {
            // Arrange          
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_PrefersProjectFileIfNuSpecAndProjectFileAreInTheSameDirectory()
        {
            // Arrange     
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectOnlyPacksAssemblyThatProjectProduced()
        {
            // Arrange                        
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_WhenErrorIsThrownPackageFileIsDeleted()
        {
            // Arrange  
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
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
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public void PackageCommand_SpecifyingProjectFileAndHaveDependenciesSkipContentFromDependencies()
        {
            // Arrange
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                Util.CreateDirectory(workingDirectory);
                Directory.SetCurrentDirectory(workingDirectory);
                string packagePath = SavePackage("Test.ContentPackage", "1.6.4");
                string expectedPackage = "ProjectWithDependenciesWithContent.1.2.0.0.nupkg";
                WriteAssemblyInfo("ProjectWithDependenciesWithContent",
                                  "1.2.0.0",
                                  "Thomas",
                                  "Project with content",
                                  "Title of Package");

                // add dummy .sln file to let NuGet find the /packages/ folder and dependency package  
                WriteProjectFile("ProjectWithDependenciesWithContent.sln", "");
                WriteProjectFile("foo.aspx", "");
                WriteProjectFile("foo.cs", "public class Foo { }");

                // temporarily enable package restore for the test to pass 
                string oldEnvironmentVariable = Environment.GetEnvironmentVariable("EnableNuGetPackageRestore", EnvironmentVariableTarget.Process);
                try
                {
                    Environment.SetEnvironmentVariable("EnableNuGetPackageRestore", "1", EnvironmentVariableTarget.Process);

                    // packages.config for dependencies  
                    WriteProjectFile(
                        "packages.config",
                        @"<?xml version=""1.0"" encoding=""utf-8""?>  
<packages>  
   <package id=""Test.ContentPackage"" version=""1.6.4"" />  
</packages>");

                    // added by Test.ContentPackage, but we have done local changes to this file 
                    WriteProjectFile(
                        "MyContentFile.js",
                        @"This is a file that is changed in this project. Therefore this file should be included in this package!");

                    CreateProject(
                        "ProjectWithDependenciesWithContent",
                        content: new[] { "foo.aspx", "packages.config", "MyContentFile.js", "MyContentFile2.js" },
                        compile: new[] { "foo.cs" });

                    Directory.SetCurrentDirectory(ProjectFilesFolder);

                    // Act  

                    // install packages from packages.config  
                    Program.Main(new[]
                    {
                        "install", "packages.config", "-OutputDirectory", "packages", "-source",
                        Path.GetDirectoryName(packagePath)
                    });

                    // copy content from the test package (to ensure no changes to the file)
                    File.Copy(@"packages\Test.ContentPackage.1.6.4\Content\MyContentFile2.js", @"MyContentFile2.js");

                    // execute main program (pack)
                    int result =
                        Program.Main(new[] { "pack", "ProjectWithDependenciesWithContent.csproj", "-Build", "-Verbose" });

                    // Assert  
                    Assert.Equal(0, result);
                    Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
                    Assert.True(File.Exists(expectedPackage));

                    // package should not contain content from jquery package that we have not changed
                    var package = VerifyPackageContents(
                        expectedPackage,
                        new[]
                        {
                            @"content\foo.aspx",
                            @"content\MyContentFile.js",
                            @"lib\net40\ProjectWithDependenciesWithContent.dll"
                        });
                    var dependencySets = package.DependencySets.ToList();
                    Assert.Equal(1, dependencySets.Count);
                    Assert.Equal("Test.ContentPackage", package.DependencySets.ElementAt(0).Dependencies.Single().Id);
                }
                finally
                {
                    // clean up 
                    Environment.SetEnvironmentVariable("EnableNuGetPackageRestore", oldEnvironmentVariable,
                                                       EnvironmentVariableTarget.Process);
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
                Directory.Delete(workingDirectory, true);
            }
        }

        // Tests that when error occurs accessing an URL source in install command,
        // the error message will contain the URL of the source.
        [Fact]
        public void PackageCommand_InstallCommandDisplaySourceOnError()
        {
            // Act
            Directory.SetCurrentDirectory(ProjectFilesFolder);
            int result = Program.Main(
                new[] { "install", "test_package", "-source", "http://localhost/" });

            // Assert
            Assert.NotEqual(0, result);
            var message = consoleOutput.ToString();
            Assert.True(message.Contains("http://localhost/"));
        }

        private static string SavePackage(string id, string version)
        {
            string tempPath = Path.Combine(_testRootDirectory, Path.GetRandomFileName());
            Directory.CreateDirectory(tempPath);
            var builder = new PackageBuilder();
            builder.Id = id;
            builder.Version = new SemanticVersion(version);
            builder.Description = "test desc";
            builder.Authors.Add("test");

            File.WriteAllText(Path.Combine(tempPath, "MyContentFile.js"), "My content file text");
            builder.Files.Add(new PhysicalPackageFile(useManagedCodeConventions: false)
            {
                SourcePath = Path.Combine(tempPath, "MyContentFile.js"),
                TargetPath = @"content\MyContentFile.js"
            });
            File.WriteAllText(Path.Combine(tempPath, "MyContentFile2.js"), "My content file2 text");
            builder.Files.Add(new PhysicalPackageFile(useManagedCodeConventions: false)
            {
                SourcePath = Path.Combine(tempPath, "MyContentFile2.js"),
                TargetPath = @"content\MyContentFile2.js"
            });

            string packagePath = Path.Combine(tempPath, id + "." + version + ".nupkg");
            using (var stream = File.Create(packagePath))
            {
                builder.Save(stream);
            }
            return packagePath;
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
        public void PackageCommand_WhenSpecifyingProjectFilePropertiesArePropagatedToMsBuild()
        {
            // Arrange            
            WriteProjectFile("Runner.cs", @"using System;
public class Runner { 
    public static void Run() { 
        Console.WriteLine(""Hello World"");
    }
}");
            WriteAssemblyInfo("LegacyProcessorProject",
                               "8.6.0.0",
                               "OlIsCool",
                               "This is a test. Ignore me");

            CreateProject("LegacyProcessorProject",
                          compile: new[] { "Runner.cs" });

            // re-write the project file's PropertyGroup conditions to support x86 instead of AnyCPU
            /*
             * The following two lines:
              <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
              <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
             * ...will be changed to read:
              <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|x86'">
              <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Release|x86'">
             */
            var projectFilePath = Path.Combine(ProjectFilesFolder, "LegacyProcessorProject.csproj");
            var nt = new NameTable();
            var xnm = new XmlNamespaceManager(nt);
            xnm.AddNamespace("msbuild", "http://schemas.microsoft.com/developer/msbuild/2003");
            var doc = new XmlDocument(nt);
            doc.Load(projectFilePath);
            var propertyGroupNodes = doc.SelectNodes("/msbuild:Project/msbuild:PropertyGroup[@Condition]", xnm);
            Assert.NotNull(propertyGroupNodes);
            foreach (XmlElement pgNode in propertyGroupNodes)
            {
                var condition = pgNode.Attributes["Condition"];
                condition.Value = condition.Value.Replace("AnyCPU", "x86");
            }
            doc.Save(projectFilePath);

            string[] args = new string[] { "pack", "-Build", "-Properties", "Configuration=Release;Platform=x86" };
            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            int result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(consoleOutput.ToString().Contains("Successfully created package"));
            Assert.True(File.Exists("LegacyProcessorProject.8.6.0.0.nupkg"));
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
            Assert.True(consoleOutput.ToString().Contains("No packages.config, project or solution file specified. Use the -self switch to update NuGet.exe."));
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
            Assert.True(consoleOutput.ToString().Contains("No packages.config, project or solution file specified."));
        }

        [Fact]
        public void UpdateCommand_ThrowsOnUpdateWhenMultipleProjectFilesArePresent()
        {
            // Arrange
            WriteProjectFile("foo.cs", "public class foo {}");
            WriteProjectFile("packages.config", "<?xml version=\"1.0\" encoding=\"utf-8\"?><packages><package id=\"MyPackage\" version=\"1.0.0\" targetFramework=\"net40\" /></packages>");
            CreateProject("project1", content: new[] { "packages.config" }, compile: new[] { "foo.cs" });
            CreateProject("project2", content: new[] { "packages.config" }, compile: new[] { "foo.cs" });

            var args = new[] { "update", "packages.config" };

            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(1, result);
            Assert.True(consoleOutput.ToString().Contains("Found multiple project files for "));
        }

        [Fact]
        public void UpdateCommand_ShouldAcceptProjectFileAsInput()
        {
            // Arrange
            WriteProjectFile("foo.cs", "public class foo {}");
            WriteProjectFile("packages.config", "<?xml version=\"1.0\" encoding=\"utf-8\"?><packages><package id=\"MyPackage\" version=\"1.0.0\" targetFramework=\"net40\" /></packages>");
            CreateProject("MyProject", content: new[] { "packages.config" }, compile: new[] { "foo.cs" });
            var packageSource = Path.Combine(ProjectFilesFolder, "repo");
            Util.CreateDirectory(packageSource);
            Util.CreateTestPackage("MyPackage", "1.0.0", packageSource, null);

            var repositoryPath = Path.Combine(ProjectFilesFolder, "packages");
            Util.CreateDirectory(repositoryPath);

            var args = new[] { "update", "MyProject.csproj", "-RepositoryPath", repositoryPath, "-Source", packageSource };

            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void UpdateCommand_ShouldAcceptPackagesConfigAsInput()
        {
            // Arrange
            WriteProjectFile("foo.cs", "public class foo {}");
            WriteProjectFile("packages.config", "<?xml version=\"1.0\" encoding=\"utf-8\"?><packages><package id=\"MyPackage\" version=\"1.0.0\" targetFramework=\"net40\" /></packages>");
            CreateProject("MyProject", content: new[] { "packages.config" }, compile: new[] { "foo.cs" });
            var packageSource = Path.Combine(ProjectFilesFolder, "repo");
            Util.CreateDirectory(packageSource);
            Util.CreateTestPackage("MyPackage", "1.0.0", packageSource, null);

            var repositoryPath = Path.Combine(ProjectFilesFolder, "packages");
            Util.CreateDirectory(repositoryPath);

            var args = new[] { "update", "packages.config", "-RepositoryPath", repositoryPath, "-Source", packageSource };

            Directory.SetCurrentDirectory(ProjectFilesFolder);

            // Act
            var result = Program.Main(args);

            // Assert
            Assert.Equal(0, result);
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

        public void SetFixture(NugetProgramStatic data)
        {
            //use fixture sets up / tears down the static (awesome idea! <sarcasm/>) use extensions.
        }
    }
}