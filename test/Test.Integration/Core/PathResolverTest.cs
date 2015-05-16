using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Xunit;

namespace NuGet.Test.Integration.PathResolver
{
    /// <summary>
    /// Tests based on scenarios specified in http://nuget.codeplex.com/wikipage?title=File%20Element%20Specification
    /// </summary>
    public class PathResolverTest : IDisposable
    {
        private static readonly string _root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        /// <summary>
        /// Foo.dll at basePath. target="lib"
        /// Expected: \lib\foo.dll
        /// </summary>
        [Fact]
        public void SingleAssemblyTest()
        {
            // Arrange
            string search = "foo.dll";
            string target = "lib";
            string root = CreateFileSystem(new File("foo.dll"));
            Stream manifest = GetManifest(search, target);

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 1);
            Assert.Equal(packageBuilder.Files.First().Path, @"lib\foo.dll");
        }

        /// <summary>
        /// Single assembly in deep path
        /// Foo.dll in bar\baz\. target="lib"
        /// Expected \lib\foo.dll
        /// </summary>
        [Fact]
        public void SingleAssemblyInDeepPathTest()
        {
            // Arrange
            string search = @"bar\baz\foo.dll";
            string target = "lib";
            string root = CreateFileSystem(new Dir("bar",
                                            new Dir("baz",
                                                new File("foo.dll"))));
            var manifest = GetManifest(search, target);

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 1);
            Assert.Equal(packageBuilder.Files.First().Path, @"lib\foo.dll");
        }

        /// <summary>
        /// Foo.dll, bar.dll in bin\release. 
        /// Search: bin\release\*.dll target="lib"
        /// Expected \lib\foo.dll, \lib\bar.dll
        /// </summary>
        [Fact]
        public void SetOfDllsFromBinTest()
        {
            // Arrange
            string search = @"bin\release\*.dll";
            string target = "lib";
            string root = CreateFileSystem(new Dir("bin",
                                            new Dir("release",
                                                new File("foo.dll"), new File("bar.dll"))));
            Stream manifest = GetManifest(search, target);

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 2);
            Assert.Equal(packageBuilder.Files.First().Path, @"lib\bar.dll");
            Assert.Equal(packageBuilder.Files.Last().Path, @"lib\foo.dll");
        }

        /// <summary>
        /// lib\net40\foo.dll, lib\net20\foo.dll
        /// Search: lib\** target="lib"
        /// Expected \lib\net40\foo.dll, \lib\net20\foo.dll
        /// </summary>
        [Fact]
        public void DllFromDifferenFrameworkTest()
        {
            // Arrange
            string search = @"lib\**";
            string target = "lib";
            string root = CreateFileSystem(new Dir("lib",
                                            new Dir("net40", new File("foo.dll")),
                                            new Dir("net20", new File("foo.dll"))));
            Stream manifest = GetManifest(search, target);

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 2);
            Assert.Equal(packageBuilder.Files.First().Path, @"lib\net20\foo.dll");
            Assert.Equal(packageBuilder.Files.Last().Path, @"lib\net40\foo.dll");
        }

        /// <summary>
        /// Source: \css\mobile\style1.css \css\mobile\style2.css
        /// Search: css\mobile\*.css 
        /// target: content\css\mobile
        /// Expected \Content\css\mobile\style1.css \Content\css\mobile\style2.css
        /// </summary>
        [Fact]
        public void ContentFilesTest()
        {
            // Arrange
            string search = @"css\mobile\*.css";
            string target = @"content\css\mobile";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                            new Dir("mobile",
                                                new File("style1.css"), new File("style2.css"))));

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 2);
            Assert.Equal(packageBuilder.Files.First().Path, @"content\css\mobile\style1.css");
            Assert.Equal(packageBuilder.Files.Last().Path, @"content\css\mobile\style2.css");
        }

        /// <summary>
        /// Source: \css\mobile\style.css, \css\mobile\wp7\style.css, \css\browser\style.css
        /// Search: css\**\*.css
        /// Target: content\css
        /// Expected \content\css\mobile\style.css \content\css\mobile\wp7\style.css \content\css\browser\style.css
        /// </summary>
        [Fact]
        public void ContentFilesWithDirectoryStructureTest()
        {
            // Arrange
            string search = @"css\**\*.css";
            string target = @"content\css";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                            new Dir("mobile",
                                                new File("style.css"),
                                                new Dir("wp7",
                                                    new File("style.css"))),
                                            new Dir("browser",
                                                new File("style.css"))));

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 3);
            Assert.Equal(packageBuilder.Files.ElementAt(0).Path, @"content\css\browser\style.css");
            Assert.Equal(packageBuilder.Files.ElementAt(1).Path, @"content\css\mobile\style.css");
            Assert.Equal(packageBuilder.Files.ElementAt(2).Path, @"content\css\mobile\wp7\style.css");
        }

        /// <summary>
        /// Source: \css\cool\style.css
        /// Search: css\cool\style.css
        /// Target: content
        /// Expected: \Content\style.css
        /// </summary>
        [Fact]
        public void ContentFilesWithDeepPath()
        {
            // Arrange
            string search = @"css\cool\style.css";
            string target = @"content";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                            new Dir("cool",
                                                new File("style.css"))));

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 1);
            Assert.Equal(packageBuilder.Files.ElementAt(0).Path, @"content\style.css");
        }

        /// <summary>
        /// Source: images\Neatpic.png
        /// Search: images\Neatpic.png
        /// Target: Content\images\foo.bar
        /// Expected: \Content\images\foo.bar\Neatpick.png
        /// </summary>
        [Fact]
        public void ContentFilesCopiedToFolderWithDotInNameTest()
        {
            // Arrange
            string search = @"images\neatpic.png";
            string target = @"content\images\foo.bar";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("images",
                                            new File("neatpic.png")));

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 1);
            Assert.Equal(packageBuilder.Files.ElementAt(0).Path, @"content\images\foo.bar\neatpic.png");
        }

        /// <summary>
        /// Source: css\cool\style.css
        /// Search: css\cool\style.css
        /// Target: Content\css\cool
        /// Expected: Content\css\cool\style.css
        /// </summary>
        [Fact]
        public void ContentFileWithDeepPathAndDeepTargetTest()
        {
            // Arrange
            string search = @"css\cool\style.css";
            string target = @"content\css\cool";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                            new Dir("cool",
                                                new File("style.css"))));

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 1);
            Assert.Equal(packageBuilder.Files.ElementAt(0).Path, @"content\css\cool\style.css");
        }

        /// <summary>
        /// Source: css\cool\style.css
        /// Search: css\cool\style.css
        /// Target: Content\css\cool\style.css
        /// Expected: Content\css\cool\style.css
        /// </summary>
        [Fact]
        public void ContentFileWithDeepPathAndDeepTargetWithFileNameTest()
        {
            // Arrange
            string search = @"css\cool\style.css";
            string target = @"content\css\cool\style.css";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                            new Dir("cool",
                                                new File("style.css"))));

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 1);
            Assert.Equal(packageBuilder.Files.ElementAt(0).Path, @"content\css\cool\style.css");
        }

        /// <summary>
        /// Source: ie\css\style.css
        /// Search: ie\css\style.css
        /// Target: Content\css\ie.css
        /// Expected: Content\css\cool\style.css
        /// </summary>
        [Fact]
        public void ContentFileCopyAndRename()
        {
            // Arrange
            string search = @"ie\css\style.css";
            string target = @"content\css\ie.css";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("ie",
                                            new Dir("css",
                                                new File("style.css"))));

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 1);
            Assert.Equal(packageBuilder.Files.ElementAt(0).Path, @"content\css\ie.css");
        }

        /// <summary>
        /// Source: ie\css\style.css, ie\logo.jpg
        /// Search: ie\**\*.cs
        /// Target: content\style\
        /// Expected: No files to be present
        /// </summary>
        [Fact]
        public void NoFilesAreCopiedWhenFilterReturnsEmptyResults()
        {
            // Arrange
            string search = @"ie\**\*.cs";
            string target = @"content\style\";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("ie",
                                            new File("logo.jpg"),
                                            new Dir("css",
                                                new File("style.css"))));

            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 0);
        }

        /// <summary>
        /// Source: \style\css\style\style.css, \style\style.css
        /// Search: style\**\*.css
        /// Target: content\styles
        /// Expected: content\styles\style.css, content\styles\css\style\style.css 
        /// </summary>
        [Fact]
        public void DirectoryStructureWithRepeatingTerms()
        {
            // Arrange
            string search = @"style\**\*.css";
            string target = @"content\styles";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("style",
                                            new File("style.css"),
                                            new Dir("css",
                                                new Dir("style",
                                                    new File("style.css")
                                                )
                                            )));
            // Act
            var packageBuilder = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(packageBuilder.Files.Count, 2);
            Assert.Equal(packageBuilder.Files.ElementAt(0).Path, @"content\styles\style.css");
            Assert.Equal(packageBuilder.Files.ElementAt(1).Path, @"content\styles\css\style\style.css");
        }

        /// <summary>
        /// Source: \style\ie.css
        /// Search: \style\main.css
        /// Target: 
        /// Expected: Exception thrown stating file \style\main.css was not found.
        /// </summary>
        [Fact]
        public void MissingFileThrowsAnException()
        {
            // Arrange
            string search = @"style\main.css";
            string target = String.Empty;
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("style",
                                            new File("ie.css")
                                           ));
            // Act and Assert
            ExceptionAssert.Throws<FileNotFoundException>(() => CreateBuilder(manifest, root), @"File not found: 'style\main.css'.");
        }

        /// <summary>
        /// Source: main.txt
        /// Search: main.css
        /// Target: 
        /// Expected: Exception thrown stating file main.css was not found.
        /// </summary>
        [Fact]
        public void MissingFileAtRootThrowsAnException()
        {
            // Arrange
            string search = @"main.css";
            string target = String.Empty;
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new File("main.txt"));

            // Act and Assert
            ExceptionAssert.Throws<FileNotFoundException>(() => CreateBuilder(manifest, root), @"File not found: 'main.css'.");
        }

        /// <summary>
        /// Source: css\main.css, css\main.txt
        /// Search: css\*.jpg
        /// Target: 
        /// </summary>
        [Fact]
        public void WildcardSearchWithNoResultingItemsDoesNotThrow()
        {
            // Arrange
            string search = @"css\*.jpg";
            string target = String.Empty;
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                                new File("main.css"),
                                                new File("main.txt")));

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.NotNull(package);
            Assert.False(package.Files.Any());
        }

        /// <summary>
        /// Source: project-files\static\css\main.css
        /// Search: ..\static\css\main.css
        /// Target: content\css
        /// Expected: \content\css\main.css
        /// </summary>
        [Fact]
        public void RelativePathsWithNoWildCards()
        {
            // Arrange
            string search = @"..\static\css\main.css";
            string target = @"content\css";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("project-files",
                                                new Dir("static",
                                                    new Dir("css",
                                                        new File("main.css")))));

            // Act
            var package = CreateBuilder(manifest, Path.Combine(root, "project-files", "nuget"));

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(package.Files.First().Path, @"content\css\main.css");
        }

        /// <summary>
        /// Source: project-files\static\css\main.css
        /// Search: ..\..\static\css\main.css
        /// Target: content\css\style.css
        /// Expected: \content\css\style.css
        /// </summary>
        [Fact]
        public void RelativePathsWithCopyRename()
        {
            // Arrange
            string search = @"..\..\static\css\main.css";
            string target = @"content\css\style.css";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("project-files",
                                                new Dir("static",
                                                    new Dir("css",
                                                        new File("main.css")))));

            // Act
            var package = CreateBuilder(manifest, Path.Combine(root, "project-files", "nuget-files", "manifest"));

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(package.Files.First().Path, @"content\css\style.css");
        }

        /// <summary>
        /// Source: src\awesomeproj\bin\release\awesomeproj.core.dll, src\awesomeproj\bin\release\awesomeproj.aux.dll
        /// Search: ..\..\src\awesomeproj\bin\release\*.dll
        /// Target: lib\net40
        /// Expected: lib\net40\awesomeproj.core.dll, lib\net40\awesomeproj.aux.dll
        /// </summary>
        [Fact]
        public void RelativePathWithWildCards()
        {
            // Arrange
            string search = @"..\..\src\awesomeproj\bin\release\*.dll";
            string target = @"lib\net40";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("src",
                                                new Dir("awesomeproj",
                                                    new Dir("bin",
                                                        new Dir("release",
                                                            new File("awesomeproj.core.dll"), new File("awesomeproj.aux.dll"))))));

            // Act
            var package = CreateBuilder(manifest, Path.Combine(root, "build", "nuget"));

            // Assert
            Assert.Equal(2, package.Files.Count);
            Assert.Equal(package.Files.First().Path, @"lib\net40\awesomeproj.aux.dll");
            Assert.Equal(package.Files.Last().Path, @"lib\net40\awesomeproj.core.dll");
        }

        /// <summary>
        /// Source: [TestDir]\bin\release\foo.dll
        /// Search: [TestDir]\bin\release\foo.dll
        /// Target: lib
        /// Expected: lib\foo.dll
        /// </summary>
        [Fact]
        public void AbsolutePathWithNoWildCards()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                    new File("foo.dll"))));
            string search = Path.Combine(root, @"bin\release\foo.dll");
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, @"x:\nuget-files\some-dir"); //This basePath would never be used, so we're ok.

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(package.Files.First().Path, @"lib\foo.dll");
        }

        /// <summary>
        /// Source: [TestDir]\bin\release\foo.dll
        /// Search: [TestDir]\bin\release\foo.dll
        /// Target: lib\bar.dll
        /// Expected: lib\bar.dll
        /// </summary>
        [Fact]
        public void AbsolutePathWithFileRename()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                    new File("foo.dll"))));
            string search = Path.Combine(root, @"bin\release\foo.dll");
            string target = @"lib\bar.dll";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, @"x:\nuget-files\some-dir"); //This basePath would never be used, so we're ok.

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(package.Files.First().Path, @"lib\bar.dll");
        }

        /// <summary>
        /// Source: [TestDir]\bin\release\net40\foo.dll, [TestDir]\bin\release\net35\foo.dll
        /// Search: [TestDir]\bin\release\**\*.dll
        /// Target: lib
        /// Expected: lib\foo.dll
        /// </summary>
        [Fact]
        public void AbsolutePathWithWildcard()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                    new File("foo.dll"))));

            string search = Path.Combine(root, @"bin\release\*.dll");
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, @"x:\nuget-files\some-dir"); //This basePath would never be used, so we're ok.

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(package.Files.First().Path, @"lib\foo.dll");
        }

        /// <summary>
        /// Source: bin\release\foo.dll;bin\release\bar.dll;sample\test.dll
        /// Search: bin\release\*.dll;sample\test.dll
        /// Target: lib
        /// Expected: lib\foo.dll, lib\bar.dll, lib\test.dll
        /// </summary>
        [Fact]
        public void MultipleFileSourcesCanBeSpecifiedUsingSemiColonSeparator()
        {
            // Arrange
            string root = CreateFileSystem(
                                            new Dir("sample", new File("test.dll")),
                                            new Dir("bin",
                                                new Dir("release",
                                                    new File("foo.dll"),
                                                    new File("bar.dll")
                                                    )));

            string search = @"bin\release\*.dll;sample\test.dll";
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(3, package.Files.Count);
            Assert.Equal(package.Files.ElementAt(0).Path, @"lib\bar.dll");
            Assert.Equal(package.Files.ElementAt(1).Path, @"lib\foo.dll");
            Assert.Equal(package.Files.ElementAt(2).Path, @"lib\test.dll");
        }

        /// <summary>
        /// Source: sample\test.dll
        /// Search: ;;
        /// Target: lib
        /// </summary>
        [Fact]
        public void ManifestThrowsIfFirstFileSourceValuesInSemiColonSeparatedListsAreEmpty()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("sample", new File("test.dll")));

            string search = @";;";
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act and Assert
            ExceptionAssert.Throws<ValidationException>(() => CreateBuilder(manifest, root), "Source is required.");
        }

        /// <summary>
        /// Source: sample\test.dll
        /// Search: ;;
        /// Target: lib
        /// </summary>
        [Fact]
        public void ManifestIgnoresEmptyItemsInSemiColonSeparatedList()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("sample", new File("test.dll")));

            string search = @";sample\test.dll;";
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(package.Files.ElementAt(0).Path, @"lib\test.dll");
        }

        /// <summary>
        /// Source: bin\release\foo.dll
        /// Search: bin\release\foo.dll;bin\release\bar.dll
        /// Target: lib
        /// Expected: Exception stating File not found: bin\release\bar.dll
        /// </summary>
        [Fact]
        public void PackageBuilderThrowsIfAnyOneItemOfSemiColonSeparatedListIsNotFound()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                    new File("foo.dll")
                                                    )));

            string search = @"bin\release\foo.dll;bin\release\bar.dll";
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act and Assert
            ExceptionAssert.Throws<FileNotFoundException>(() => CreateBuilder(manifest, root), @"File not found: 'bin\release\bar.dll'.");
        }

        /// <summary>
        /// Source: [TestDir]\bin\release\net40\foo.dll, [TestDir]\bin\release\net35\foo.dll
        /// Search: [TestDir]\bin\release\**\*.dll
        /// Target: lib
        /// Expected: lib\net40\foo.dll, lib\net35\foo.dll
        /// </summary>
        [Fact]
        public void AbsolutePathWithRecursiveWildcard()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                    new Dir("net40",
                                                        new File("foo.dll")),
                                                    new Dir("net35",
                                                        new File("foo.dll")))));

            string search = Path.Combine(root, @"bin\release\**\*.dll");
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, @"x:\nuget-files\some-dir"); //This basePath would never be used, so we're ok.

            // Assert
            Assert.Equal(2, package.Files.Count);
            Assert.Equal(package.Files.First().Path, @"lib\net35\foo.dll");
            Assert.Equal(package.Files.Last().Path, @"lib\net40\foo.dll");
        }

        /// <summary>
        /// Source: bin\release\baz.dll
        /// Search: bin\*\*.dll
        /// Target: lib
        /// Expected: lib\release\baz.dll
        /// </summary>
        [Fact]
        public void PathWithWildCardInDirectoryNameAndFileName()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                        new File("baz.dll"))));
            string search = @"bin\*\*.dll";
            string target = "lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(@"lib\baz.dll", package.Files.First().Path);
        }

        /// <summary>
        /// Source: bin\release\baz.dll
        /// Search: bin\*\*.dll
        /// Target: lib
        /// Expected: lib\release\baz.dll
        /// </summary>
        [Fact]
        public void PathWithWildCardInDirectoryNameAndFileExtension()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                        new File("baz.dll"))));
            string search = @"bin\*\*.*";
            string target = "lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(@"lib\baz.dll", package.Files.First().Path);
        }

        /// <summary>
        /// Source: bin\release\baz.dll
        /// Search: bin\*\*.dll
        /// Target: lib
        /// Expected: lib\release\baz.dll
        /// </summary>
        [Fact]
        public void SourceStructureIsPreservedWithRecursiveWildCard()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                        new File("baz.dll"))));
            string search = @"bin\**\*.*";
            string target = "lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(@"lib\release\baz.dll", package.Files.First().Path);
        }

        /// <summary>
        /// Source: bin\release\baz.dll
        /// Search: bin\*\*.dll
        /// Target: lib\foo.dll
        /// Expected: lib\release\baz.dll
        /// In the case of a path without wildcard characters, we rename the source if the source and target extensions are identical.
        /// </summary>
        [Fact]
        public void SourceWithWildcardIgnoresExtensionBasedRenameRule()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                        new File("baz.dll"))));
            string search = @"bin\*\*.dll";
            string target = @"lib\foo.dll";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(@"lib\foo.dll\baz.dll", package.Files.First().Path);
        }

        /// <summary>
        /// Source: bin\release\baz.dll
        /// Search: bin\*\*.dll
        /// Target: lib\foo.dll
        /// Expected: lib\release\baz.dll
        /// In the case of a path without wildcard characters, we rename the source if the source and target extensions are identical.
        /// </summary>
        [Fact]
        public void WildCardInMiddleOfPath()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("Output",
                                            new Dir("NuGet",
                                                new Dir("NuGet.Server",
                                                    new File("NuGet.Server.dll"),
                                                    new File("NuGet.Core.dll")),
                                                new Dir("NuGet.Core",
                                                    new File("NuGet.Core.dll")))));
            string search = @"Output\*\NuGet.Core\*.Core.dll";
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(@"lib\NuGet.Core.dll", package.Files.First().Path);
        }

        /// <summary>
        /// Source: bin\release\baz.dll
        /// Search: bin\*\*.dll
        /// Target: lib\foo.dll
        /// Expected: lib\release\baz.dll
        /// In the case of a path without wildcard characters, we rename the source if the source and target extensions are identical.
        /// </summary>
        [Fact]
        public void WildCardInMiddleOfPathAndExtension()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("Output",
                                            new Dir("NuGet",
                                                new Dir("NuGet.Server",
                                                    new File("NuGet.Server.dll"),
                                                    new File("NuGet.Core.dll")),
                                                new Dir("NuGet.Core",
                                                    new File("NuGet.Core.dll")))));
            string search = @"Output\*\NuGet.Server\*.dll";
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(2, package.Files.Count);
            Assert.Equal(@"lib\NuGet.Core.dll", package.Files.First().Path);
            Assert.Equal(@"lib\NuGet.Server.dll", package.Files.Last().Path);
        }

        /// <summary>
        /// Source: bin\release\baz.dll
        /// Search: bin\*\*.dll
        /// Target: lib\foo.dll
        /// Expected: lib\release\baz.dll
        /// In the case of a path without wildcard characters, we rename the source if the source and target extensions are identical.
        /// </summary>
        [Fact]
        public void RecursiveSearchForPathNotInBaseDirectory()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("Packages", new Dir("bin")),
                                           new Dir("Output",
                                                new Dir("NuGet",
                                                    new Dir("NuGet.Server",
                                                        new File("NuGet.Server.dll"),
                                                        new File("NuGet.Core.dll")))));

            string search = Path.Combine(root, @"output\**\*.dll");
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            string builderBase = Path.Combine(@"packages\bin");
            var package = CreateBuilder(manifest, builderBase);

            // Assert
            Assert.Equal(2, package.Files.Count);
            Assert.Equal(@"lib\NuGet\NuGet.Server\NuGet.Core.dll", package.Files.First().Path);
            Assert.Equal(@"lib\NuGet\NuGet.Server\NuGet.Server.dll", package.Files.Last().Path);
        }

        /// <summary>
        /// Source: bin\release\baz.dll
        /// Search: ..\bin\release\*.dll
        /// Target: lib
        /// Expected: lib\release\baz.dll
        /// </summary>
        [Fact]
        public void RelativePathWithWildCard()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("properties"),
                                           new Dir("bin",
                                               new Dir("release", new File("baz.dll"))));

            string searchPath = @"..\bin\release\*.dll";
            string target = @"lib";
            Stream manifest = GetManifest(searchPath, target);

            // Act
            string builderBase = Path.Combine(root, "properties");
            var package = CreateBuilder(manifest, builderBase);

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(@"lib\baz.dll", package.Files.Single().Path);
        }

        /// <summary>
        /// Source: bin\release\baz.dll
        /// Search: ..\..\bin\*\*.dll
        /// Target: lib
        /// Expected: lib\release\baz.dll
        /// </summary>
        [Fact]
        public void DeepRelativePathWithWildCard()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("tools",
                                                new Dir("nuspec")),
                                           new Dir("bin",
                                               new Dir("release", new File("baz.dll"))));

            string searchPath = @"..\..\bin\release\*.dll";
            string target = @"lib";
            Stream manifest = GetManifest(searchPath, target);

            // Act
            string builderBase = Path.Combine(root, @"tools\nuspec");
            var package = CreateBuilder(manifest, builderBase);

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(@"lib\baz.dll", package.Files.Single().Path);
        }

        /// <summary>
        /// Source: bin\release\baz.dll
        /// Search: ..\..\bin\*\*.dll
        /// Target: lib
        /// Expected: lib\release\baz.dll
        /// </summary>
        [Fact]
        public void RelativePathWithRecursiveWildCard()
        {
            // Arrange
            string root = CreateFileSystem(new Dir("tools",
                                                new Dir("nuspec")),
                                           new Dir("bin",
                                               new File("Output.dll"),
                                               new Dir("release",
                                                   new File("baz.dll"))));

            string searchPath = @"..\..\bin\**\*.dll";
            string target = @"lib";
            Stream manifest = GetManifest(searchPath, target);

            // Act
            string builderBase = Path.Combine(root, @"tools\nuspec");
            var package = CreateBuilder(manifest, builderBase);

            // Assert
            Assert.Equal(2, package.Files.Count);
            Assert.Equal(@"lib\Output.dll", package.Files.First().Path);
            Assert.Equal(@"lib\release\baz.dll", package.Files.Last().Path);
        }

        /// <summary>
        /// Source: output\foo.dll, output\output\foo.dll, output\output\output\foo.dll, output\foo\output.dll
        /// Search: output\output\foo.dll
        /// Target: lib\foo.dll
        /// Expected: lib\release\baz.dll
        /// In the case of a path without wildcard characters, we rename the source if the source and target extensions are identical.
        /// </summary>
        [Fact]
        public void WildCardInPathDoesNotPickUpFileInNestedDirectories()
        {
            // Arrange
            string expectedContent = "this is the right foo";
            string root = CreateFileSystem(new Dir("Output",
                                               new Dir("foo",
                                                    new File("output.dll")),
                                               new File("foo.dll"),
                                                new Dir("output",
                                                    new File("foo.dll", expectedContent),
                                                        new Dir("output",
                                                            new File("foo.dll")))));

            string search = Path.Combine(root, @"output\*\foo.dll");
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(@"lib\foo.dll", package.Files.First().Path);

            // Verify that we picked up the right file
            var actualContents = package.Files.First().GetStream().ReadToEnd();
            Assert.Equal(expectedContent, actualContents);
        }

        [Fact]
        public void PerformWildcardSearchWithUnixStyleDirectory()
        {
            // Arrange
            var testDirectory = Path.Combine(Environment.CurrentDirectory, "testdir");
            var testFile = Path.Combine(testDirectory, "test.nupkg");
            Directory.CreateDirectory(testDirectory);
            using (StreamWriter writer = System.IO.File.CreateText(testFile))
            {
                writer.Write("test");
            }

            try
            {
                // Act. Test that a unix style directory, "testdir/" can be searched correctly.
                var result = NuGet.PathResolver.PerformWildcardSearch(Environment.CurrentDirectory, @"testdir/");
                Assert.Equal(testFile, result.First());
            }
            finally
            {
                // Cleanup
                Directory.Delete(testDirectory, true);
            }
        }

        [Fact]
        public void PerformWildcardSearchWithUnixStylePath()
        {
            // Arrange
            var testDirectory = Path.Combine(Environment.CurrentDirectory, "testdir");
            var testFile = Path.Combine(testDirectory, "test.nupkg");
            Directory.CreateDirectory(testDirectory);
            using (StreamWriter writer = System.IO.File.CreateText(testFile))
            {
                writer.Write("test");
            }

            try
            {
                // Act. Test that a unix style path, "testdir/test.nupkg" can be searched correctly.
                var result = NuGet.PathResolver.PerformWildcardSearch(
                    Environment.CurrentDirectory, @"testdir/test.nupkg");
                Assert.Equal(testFile, result.First());
            }
            finally
            {
                // Cleanup
                Directory.Delete(testDirectory, true);
            }
        }


        [Fact]
        public void ExclusionWithSimpleExtensions()
        {
            // Arrange
            var root = CreateExclusionProject();
            var manifest = GetExclusionManifest(@"**\*.*", "", @"**\*.pdb");

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(6, package.Files.Count);
            Assert.Equal("Main.cs", package.Files[0].Path);
            Assert.Equal("MainCore.cs", package.Files[1].Path);
            Assert.Equal("MyProject.csproj", package.Files[2].Path);
            Assert.Equal(@"bin\debug\MyProject.dll", package.Files[3].Path);
            Assert.Equal(@"bin\release\MyProject.dll", package.Files[4].Path);
            Assert.Equal(@"Properties\AssemblyInfo.cs", package.Files[5].Path);
        }

        [Fact]
        public void ExclusionWithPath()
        {
            // Arrange
            var root = CreateExclusionProject();
            var manifest = GetExclusionManifest(@"**\*.*", "", @"bin\**");

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(4, package.Files.Count);
            Assert.Equal("Main.cs", package.Files[0].Path);
            Assert.Equal("MainCore.cs", package.Files[1].Path);
            Assert.Equal("MyProject.csproj", package.Files[2].Path);
            Assert.Equal(@"Properties\AssemblyInfo.cs", package.Files[3].Path);
        }

        [Fact]
        public void MultipleExclusionsForSearchPath()
        {
            // Arrange
            var root = CreateExclusionProject();
            var manifest = GetExclusionManifest(@"**\*.*", "", @"**\*.pdb;*.csproj;Properties\*;*\Debug\*");

            // Act
            var package = CreateBuilder(manifest, root);

            // Assert
            Assert.Equal(3, package.Files.Count);
            Assert.Equal("Main.cs", package.Files[0].Path);
            Assert.Equal("MainCore.cs", package.Files[1].Path);
            Assert.Equal(@"bin\release\MyProject.dll", package.Files[2].Path);
        }

        [Fact]
        public void ExclusionsWithRelativePaths()
        {
            // Arrange
            var root = CreateExclusionProject();
            var manifest = GetExclusionManifest(@"..\bin\*\*.*", "lib", @"..\bin\debug\*");

            // Act
            var package = CreateBuilder(manifest, Path.Combine(root, "Properties"));

            // Assert
            Assert.Equal(1, package.Files.Count);
            Assert.Equal(@"lib\MyProject.dll", package.Files[0].Path);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch { }
        }

        private string CreateExclusionProject()
        {
            return CreateFileSystem(new File("MyProject.csproj"),
                                    new File("Main.cs"),
                                    new File("MainCore.cs"),
                                    new Dir("Properties",
                                        new File("AssemblyInfo.cs")),
                                    new Dir("bin",
                                        new Dir("debug",
                                            new File("MyProject.dll"),
                                            new File("MyProject.pdb")),
                                        new Dir("release",
                                            new File("MyProject.dll"))));

        }

        private static Stream GetExclusionManifest(string search, string target, string exclusion)
        {
            return String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <language>en-us</language>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata><files><file src=""{0}"" target=""{1}"" exclude=""{2}"" /></files></package>", search, target, exclusion).AsStream();
        }

        private static Stream GetManifest(string search, string target)
        {
            return String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <language>en-us</language>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata><files><file src=""{0}"" target=""{1}"" /></files></package>", search, target).AsStream();
        }

        //not as elegant as the mstest version as xUnit does not appear to give similar context about the "context" of the test being invoked
        private static string CreateFileSystem(params File[] files)
        {
            string rootDir = Path.Combine(_root, Path.GetRandomFileName());
            new Dir(rootDir, files).Create();
            return rootDir;
        }

        private static PackageBuilder CreateBuilder(Stream manifest, string root)
        {
            return new PackageBuilder(
                manifest,
                root,
                NullPropertyProvider.Instance,
                includeEmptyDirectories: false,
                packageType: PackageType.Default);
        }

        private class File
        {
            protected readonly string _name;
            private readonly string _contents;
            public File(string name, string contents = "")
            {
                _name = name;
                _contents = contents;
            }

            public File Parent { get; set; }

            public virtual void Create()
            {
                System.IO.File.AppendAllText(GetFullPath(), _contents);
            }

            public string GetFullPath()
            {
                return Parent == null ? _name : Path.Combine(Parent.GetFullPath(), _name);
            }
        }

        private class Dir : File
        {
            IEnumerable<File> _files;

            public Dir(string name, params File[] files)
                : base(name)
            {
                if (files != null)
                {
                    foreach (var f in files)
                    {
                        f.Parent = this;
                    }
                    _files = files;
                }
            }

            public override void Create()
            {
                Directory.CreateDirectory(GetFullPath());
                foreach (var f in _files)
                {
                    f.Create();
                }
            }
        }
    }
}
