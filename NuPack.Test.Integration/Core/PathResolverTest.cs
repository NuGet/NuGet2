using System;
using System.Collections.Generic;
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

        private string CreateFileSystem(File file) {
            string rootDir = Path.Combine(TestContext.TestDir, "PathResolverIntegrationTests", TestContext.TestName);
            new Dir(rootDir, file).Create();
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
