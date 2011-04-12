using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test.Integration.PathResolver {

    /// <summary>
    /// Tests based on scenarios specified in http://nuget.codeplex.com/wikipage?title=File%20Element%20Specification
    /// </summary>
    [TestClass]
    public class PathResolverTest {

        public TestContext TestContext { get; set; }

        /// <summary>
        /// Foo.dll at basePath. target="lib"
        /// Expected: \lib\foo.dll
        /// </summary>
        [TestMethod]
        public void SingleAssemblyTest() {
            // Arrange
            string search = "foo.dll";
            string target = "lib";
            string root = CreateFileSystem(new File("foo.dll"));
            Stream manifest = GetManifest(search, target);

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 1);
            Assert.AreEqual(packageBuilder.Files.First().Path, @"lib\foo.dll");
        }

        /// <summary>
        /// Single assembly in deep path
        /// Foo.dll in bar\baz\. target="lib"
        /// Expected \lib\foo.dll
        /// </summary>
        [TestMethod]
        public void SingleAssemblyInDeepPathTest() {
            // Arrange
            string search = @"bar\baz\foo.dll";
            string target = "lib";
            string root = CreateFileSystem(new Dir("bar",
                                            new Dir("baz",
                                                new File("foo.dll"))));
            var manifest = GetManifest(search, target);

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 1);
            Assert.AreEqual(packageBuilder.Files.First().Path, @"lib\foo.dll");
        }

        /// <summary>
        /// Foo.dll, bar.dll in bin\release. 
        /// Search: bin\release\*.dll target="lib"
        /// Expected \lib\foo.dll, \lib\bar.dll
        /// </summary>
        [TestMethod]
        public void SetOfDllsFromBinTest() {
            // Arrange
            string search = @"bin\release\*.dll";
            string target = "lib";
            string root = CreateFileSystem(new Dir("bin",
                                            new Dir("release",
                                                new File("foo.dll"), new File("bar.dll"))));
            Stream manifest = GetManifest(search, target);

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 2);
            Assert.AreEqual(packageBuilder.Files.First().Path, @"lib\bar.dll");
            Assert.AreEqual(packageBuilder.Files.Last().Path, @"lib\foo.dll");
        }

        /// <summary>
        /// lib\net40\foo.dll, lib\net20\foo.dll
        /// Search: lib\** target="lib"
        /// Expected \lib\net40\foo.dll, \lib\net20\foo.dll
        /// </summary>
        [TestMethod]
        public void DllFromDifferenFrameworkTest() {
            // Arrange
            string search = @"lib\**";
            string target = "lib";
            string root = CreateFileSystem(new Dir("lib",
                                            new Dir("net40", new File("foo.dll")),
                                            new Dir("net20", new File("foo.dll"))));
            Stream manifest = GetManifest(search, target);

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 2);
            Assert.AreEqual(packageBuilder.Files.First().Path, @"lib\net20\foo.dll");
            Assert.AreEqual(packageBuilder.Files.Last().Path, @"lib\net40\foo.dll");
        }

        /// <summary>
        /// Source: \css\mobile\style1.css \css\mobile\style2.css
        /// Search: css\mobile\*.css 
        /// target: content\css\mobile
        /// Expected \Content\css\mobile\style1.css \Content\css\mobile\style2.css
        /// </summary>
        [TestMethod]
        public void ContentFilesTest() {
            // Arrange
            string search = @"css\mobile\*.css";
            string target = @"content\css\mobile";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                            new Dir("mobile",
                                                new File("style1.css"), new File("style2.css"))));

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 2);
            Assert.AreEqual(packageBuilder.Files.First().Path, @"content\css\mobile\style1.css");
            Assert.AreEqual(packageBuilder.Files.Last().Path, @"content\css\mobile\style2.css");
        }

        /// <summary>
        /// Source: \css\mobile\style.css, \css\mobile\wp7\style.css, \css\browser\style.css
        /// Search: css\**\*.css
        /// Target: content\css
        /// Expected \content\css\mobile\style.css \content\css\mobile\wp7\style.css \content\css\browser\style.css
        /// </summary>
        [TestMethod]
        public void ContentFilesWithDirectoryStructureTest() {
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
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 3);
            Assert.AreEqual(packageBuilder.Files.ElementAt(0).Path, @"content\css\browser\style.css");
            Assert.AreEqual(packageBuilder.Files.ElementAt(1).Path, @"content\css\mobile\style.css");
            Assert.AreEqual(packageBuilder.Files.ElementAt(2).Path, @"content\css\mobile\wp7\style.css");
        }

        /// <summary>
        /// Source: \css\cool\style.css
        /// Search: css\cool\style.css
        /// Target: content
        /// Expected: \Content\style.css
        /// </summary>
        [TestMethod]
        public void ContentFilesWithDeepPath() {
            // Arrange
            string search = @"css\cool\style.css";
            string target = @"content";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                            new Dir("cool",
                                                new File("style.css"))));

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 1);
            Assert.AreEqual(packageBuilder.Files.ElementAt(0).Path, @"content\style.css");
        }

        /// <summary>
        /// Source: images\Neatpic.png
        /// Search: images\Neatpic.png
        /// Target: Content\images\foo.bar
        /// Expected: \Content\images\foo.bar\Neatpick.png
        /// </summary>
        [TestMethod]
        public void ContentFilesCopiedToFolderWithDotInNameTest() {
            // Arrange
            string search = @"images\neatpic.png";
            string target = @"content\images\foo.bar";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("images",
                                            new File("neatpic.png")));

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 1);
            Assert.AreEqual(packageBuilder.Files.ElementAt(0).Path, @"content\images\foo.bar\neatpic.png");
        }

        /// <summary>
        /// Source: css\cool\style.css
        /// Search: css\cool\style.css
        /// Target: Content\css\cool
        /// Expected: Content\css\cool\style.css
        /// </summary>
        [TestMethod]
        public void ContentFileWithDeepPathAndDeepTargetTest() {
            // Arrange
            string search = @"css\cool\style.css";
            string target = @"content\css\cool";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                            new Dir("cool",
                                                new File("style.css"))));

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 1);
            Assert.AreEqual(packageBuilder.Files.ElementAt(0).Path, @"content\css\cool\style.css");
        }

        /// <summary>
        /// Source: css\cool\style.css
        /// Search: css\cool\style.css
        /// Target: Content\css\cool\style.css
        /// Expected: Content\css\cool\style.css
        /// </summary>
        [TestMethod]
        public void ContentFileWithDeepPathAndDeepTargetWithFileNameTest() {
            // Arrange
            string search = @"css\cool\style.css";
            string target = @"content\css\cool\style.css";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                            new Dir("cool",
                                                new File("style.css"))));

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 1);
            Assert.AreEqual(packageBuilder.Files.ElementAt(0).Path, @"content\css\cool\style.css");
        }

        /// <summary>
        /// Source: ie\css\style.css
        /// Search: ie\css\style.css
        /// Target: Content\css\ie.css
        /// Expected: Content\css\cool\style.css
        /// </summary>
        [TestMethod]
        public void ContentFileCopyAndRename() {
            // Arrange
            string search = @"ie\css\style.css";
            string target = @"content\css\ie.css";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("ie",
                                            new Dir("css",
                                                new File("style.css"))));

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 1);
            Assert.AreEqual(packageBuilder.Files.ElementAt(0).Path, @"content\css\ie.css");
        }

        /// <summary>
        /// Source: ie\css\style.css, ie\logo.jpg
        /// Search: ie\**\*.cs
        /// Target: content\style\
        /// Expected: No files to be present
        /// </summary>
        [TestMethod]
        public void NoFilesAreCopiedWhenFilterReturnsEmptyResults() {
            // Arrange
            string search = @"ie\**\*.cs";
            string target = @"content\style\";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("ie",
                                            new File("logo.jpg"),
                                            new Dir("css",
                                                new File("style.css"))));

            // Act
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 0);
        }

        /// <summary>
        /// Source: \style\css\style\style.css, \style\style.css
        /// Search: style\**\*.css
        /// Target: content\styles
        /// Expected: content\styles\style.css, content\styles\css\style\style.css 
        /// </summary>
        [TestMethod]
        public void DirectoryStructureWithRepeatingTerms() {
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
            var packageBuilder = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(packageBuilder.Files.Count, 2);
            Assert.AreEqual(packageBuilder.Files.ElementAt(0).Path, @"content\styles\style.css");
            Assert.AreEqual(packageBuilder.Files.ElementAt(1).Path, @"content\styles\css\style\style.css");
        }

        /// <summary>
        /// Source: \style\ie.css
        /// Search: \style\main.css
        /// Target: 
        /// Expected: Exception thrown stating file \style\main.css was not found.
        /// </summary>
        [TestMethod]
        public void MissingFileThrowsAnException() {
            // Arrange
            string search = @"style\main.css";
            string target = String.Empty;
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("style",
                                            new File("ie.css")
                                           ));
            // Act and Assert
            ExceptionAssert.Throws<FileNotFoundException>(() => new PackageBuilder(manifest, root), @"File not found: 'style\main.css'.");
        }

        /// <summary>
        /// Source: main.txt
        /// Search: main.css
        /// Target: 
        /// Expected: Exception thrown stating file main.css was not found.
        /// </summary>
        [TestMethod]
        public void MissingFileAtRootThrowsAnException() {
            // Arrange
            string search = @"main.css";
            string target = String.Empty;
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new File("main.txt"));

            // Act and Assert
            ExceptionAssert.Throws<FileNotFoundException>(() => new PackageBuilder(manifest, root), @"File not found: 'main.css'.");
        }

        /// <summary>
        /// Source: css\main.css, css\main.txt
        /// Search: css\*.jpg
        /// Target: 
        /// </summary>
        [TestMethod]
        public void WildcardSearchWithNoResultingItemsDoesNotThrow() {
            // Arrange
            string search = @"css\*.jpg";
            string target = String.Empty;
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("css",
                                                new File("main.css"),
                                                new File("main.txt")));

            // Act
            var package = new PackageBuilder(manifest, root);

            // Assert
            Assert.IsNotNull(package);
            Assert.IsFalse(package.Files.Any());
        }

        /// <summary>
        /// Source: project-files\static\css\main.css
        /// Search: ..\static\css\main.css
        /// Target: content\css
        /// Expected: \content\css\main.css
        /// </summary>
        [TestMethod]
        public void RelativePathsWithNoWildCards() {
            // Arrange
            string search = @"..\static\css\main.css";
            string target = @"content\css";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("project-files",
                                                new Dir("static",
                                                    new Dir("css",
                                                        new File("main.css")))));

            // Act
            var package = new PackageBuilder(manifest, Path.Combine(root, "project-files", "nuget"));

            // Assert
            Assert.AreEqual(1, package.Files.Count);
            Assert.AreEqual(package.Files.First().Path, @"content\css\main.css");
        }

        /// <summary>
        /// Source: project-files\static\css\main.css
        /// Search: ..\..\static\css\main.css
        /// Target: content\css\style.css
        /// Expected: \content\css\style.css
        /// </summary>
        [TestMethod]
        public void RelativePathsWithCopyRename() {
            // Arrange
            string search = @"..\..\static\css\main.css";
            string target = @"content\css\style.css";
            Stream manifest = GetManifest(search, target);
            string root = CreateFileSystem(new Dir("project-files",
                                                new Dir("static",
                                                    new Dir("css",
                                                        new File("main.css")))));

            // Act
            var package = new PackageBuilder(manifest, Path.Combine(root, "project-files", "nuget-files", "manifest"));

            // Assert
            Assert.AreEqual(1, package.Files.Count);
            Assert.AreEqual(package.Files.First().Path, @"content\css\style.css");
        }

        /// <summary>
        /// Source: src\awesomeproj\bin\release\awesomeproj.core.dll, src\awesomeproj\bin\release\awesomeproj.aux.dll
        /// Search: ..\..\src\awesomeproj\bin\release\*.dll
        /// Target: lib\net40
        /// Expected: lib\net40\awesomeproj.core.dll, lib\net40\awesomeproj.aux.dll
        /// </summary>
        [TestMethod]
        public void RelativePathWithWildCards() {
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
            var package = new PackageBuilder(manifest, Path.Combine(root, "build", "nuget"));

            // Assert
            Assert.AreEqual(2, package.Files.Count);
            Assert.AreEqual(package.Files.First().Path, @"lib\net40\awesomeproj.aux.dll");
            Assert.AreEqual(package.Files.Last().Path, @"lib\net40\awesomeproj.core.dll");
        }

        /// <summary>
        /// Source: [TestDir]\bin\release\foo.dll
        /// Search: [TestDir]\bin\release\foo.dll
        /// Target: lib
        /// Expected: lib\foo.dll
        /// </summary>
        [TestMethod]
        public void AbsolutePathWithNoWildCards() {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                    new File("foo.dll"))));
            string search = Path.Combine(root, @"bin\release\foo.dll");
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = new PackageBuilder(manifest, @"x:\nuget-files\some-dir"); //This basePath would never be used, so we're ok.

            // Assert
            Assert.AreEqual(1, package.Files.Count);
            Assert.AreEqual(package.Files.First().Path, @"lib\foo.dll");
        }

        /// <summary>
        /// Source: [TestDir]\bin\release\foo.dll
        /// Search: [TestDir]\bin\release\foo.dll
        /// Target: lib\bar.dll
        /// Expected: lib\bar.dll
        /// </summary>
        [TestMethod]
        public void AbsolutePathWithFileRename() {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                    new File("foo.dll"))));
            string search = Path.Combine(root, @"bin\release\foo.dll");
            string target = @"lib\bar.dll";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = new PackageBuilder(manifest, @"x:\nuget-files\some-dir"); //This basePath would never be used, so we're ok.

            // Assert
            Assert.AreEqual(1, package.Files.Count);
            Assert.AreEqual(package.Files.First().Path, @"lib\bar.dll");
        }

        /// <summary>
        /// Source: [TestDir]\bin\release\net40\foo.dll, [TestDir]\bin\release\net35\foo.dll
        /// Search: [TestDir]\bin\release\**\*.dll
        /// Target: lib
        /// Expected: lib\foo.dll
        /// </summary>
        [TestMethod]
        public void AbsolutePathWithWildcard() {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                    new File("foo.dll"))));

            string search = Path.Combine(root, @"bin\release\*.dll");
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = new PackageBuilder(manifest, @"x:\nuget-files\some-dir"); //This basePath would never be used, so we're ok.

            // Assert
            Assert.AreEqual(1, package.Files.Count);
            Assert.AreEqual(package.Files.First().Path, @"lib\foo.dll");
        }

        /// <summary>
        /// Source: bin\release\foo.dll;bin\release\bar.dll;sample\test.dll
        /// Search: bin\release\*.dll;sample\test.dll
        /// Target: lib
        /// Expected: lib\foo.dll, lib\bar.dll, lib\test.dll
        /// </summary>
        [TestMethod]
        public void MultipleFileSourcesCanBeSpecifiedUsingSemiColonSeparator() {
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
            var package = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(3, package.Files.Count);
            Assert.AreEqual(package.Files.ElementAt(0).Path, @"lib\bar.dll");
            Assert.AreEqual(package.Files.ElementAt(1).Path, @"lib\foo.dll");
            Assert.AreEqual(package.Files.ElementAt(2).Path, @"lib\test.dll");
        }

        /// <summary>
        /// Source: sample\test.dll
        /// Search: ;;
        /// Target: lib
        /// Expected: lib\foo.dll, lib\bar.dll, lib\test.dll
        /// </summary>
        [TestMethod]
        public void ManifestThrowsIfFirstFileSourceValuesInSemiColonSeparatedListsAreEmpty() {
            // Arrange
            string root = CreateFileSystem(new Dir("sample", new File("test.dll")));

            string search = @";;";
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act and Assert
            ExceptionAssert.Throws<ValidationException>(() => new PackageBuilder(manifest, root), "Source is required.");
        }

        /// <summary>
        /// Source: sample\test.dll
        /// Search: ;;
        /// Target: lib
        /// Expected: lib\foo.dll, lib\bar.dll, lib\test.dll
        /// </summary>
        [TestMethod]
        public void ManifestIgnoresEmptyItemsInSemiColonSeparatedList() {
            // Arrange
            string root = CreateFileSystem(new Dir("sample", new File("test.dll")));

            string search = @";sample\test.dll;";
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act
            var package = new PackageBuilder(manifest, root);

            // Assert
            Assert.AreEqual(1, package.Files.Count);
            Assert.AreEqual(package.Files.ElementAt(0).Path, @"lib\test.dll");
        }

        /// <summary>
        /// Source: bin\release\foo.dll
        /// Search: bin\release\foo.dll;bin\release\bar.dll
        /// Target: lib
        /// Expected: Exception stating File not found: bin\release\bar.dll
        /// </summary>
        [TestMethod]
        public void PackageBuilderThrowsIfAnyOneItemOfSemiColonSeparatedListIsNotFound() {
            // Arrange
            string root = CreateFileSystem(new Dir("bin",
                                                new Dir("release",
                                                    new File("foo.dll")
                                                    )));

            string search = @"bin\release\foo.dll;bin\release\bar.dll";
            string target = @"lib";
            Stream manifest = GetManifest(search, target);

            // Act and Assert
            ExceptionAssert.Throws<FileNotFoundException>(() => new PackageBuilder(manifest, root), @"File not found: 'bin\release\bar.dll'.");
        }

        /// <summary>
        /// Source: [TestDir]\bin\release\net40\foo.dll, [TestDir]\bin\release\net35\foo.dll
        /// Search: [TestDir]\bin\release\**\*.dll
        /// Target: lib
        /// Expected: lib\net40\foo.dll, lib\net35\foo.dll
        /// </summary>
        [TestMethod]
        public void AbsolutePathWithRecursiveWildcard() {
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
            var package = new PackageBuilder(manifest, @"x:\nuget-files\some-dir"); //This basePath would never be used, so we're ok.

            // Assert
            Assert.AreEqual(2, package.Files.Count);
            Assert.AreEqual(package.Files.First().Path, @"lib\net35\foo.dll");
            Assert.AreEqual(package.Files.Last().Path, @"lib\net40\foo.dll");
        }

        private Stream GetManifest(string search, string target) {
            return String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<package><metadata>
    <id>Artem.XmlProviders</id>
    <version>2.5</version>
    <authors>Velio Ivanov</authors>
    <language>en-us</language>
    <description>Implementation of XML ASP.NET Providers (XmlRoleProvider, XmlMembershipProvider and XmlProfileProvider).</description>
  </metadata><files><file src=""{0}"" target=""{1}"" /></files></package>", search, target).AsStream();
        }

        private string CreateFileSystem(params File[] files) {
            string rootDir = Path.Combine(TestContext.TestDeploymentDir, "PathResolverIntegrationTests", TestContext.TestName);
            new Dir(rootDir, files).Create();
            return rootDir;
        }

        private class File {
            protected readonly string _name;
            public File(string name) {
                _name = name;
            }

            public File Parent { get; set; }

            public virtual void Create() {
                using (System.IO.File.Create(GetFullPath())) {
                }
            }

            public string GetFullPath() {
                return Parent == null ? _name : Path.Combine(Parent.GetFullPath(), _name);
            }
        }

        private class Dir : File {
            IEnumerable<File> _files;

            public Dir(string name, params File[] files)
                : base(name) {
                if (files != null) {
                    foreach (var f in files) {
                        f.Parent = this;
                    }
                    _files = files;
                }
            }

            public override void Create() {
                Directory.CreateDirectory(GetFullPath());
                foreach (var f in _files) {
                    f.Create();
                }
            }
        }
    }
}
