using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class ZipPackageTest {
        [TestMethod]
        public void CtorWithStreamThrowsIfNull() {
            ExceptionAssert.ThrowsArgNull(() => new ZipPackage((Stream)null), "stream");
        }

        [TestMethod]
        public void CtorWithFileNameThrowsIfNullOrEmpty() {
            ExceptionAssert.ThrowsArgNullOrEmpty(() => new ZipPackage((string)null), "fileName");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => new ZipPackage(String.Empty), "fileName");
        }

        [TestMethod]
        public void CtorWithStream() {
            // Arrange
            var builder = new PackageBuilder();
            builder.Id = "Package";
            builder.Version = new Version("1.0");
            builder.Authors.Add("David");
            builder.Description = "This is a test package";
            builder.Files.AddRange(PackageUtility.CreateFiles(new[] { @"lib\40\A.dll", @"content\foo" }));

            var ms = new MemoryStream();
            builder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            // Act
            var package = new ZipPackage(ms);

            // Assert
            Assert.AreEqual("Package", package.Id);
            Assert.AreEqual(new Version("1.0"), package.Version);
            Assert.AreEqual("David", package.Authors.First());
            var files = package.GetFiles().ToList();
            Assert.AreEqual(2, files.Count);
            Assert.AreEqual(@"content\foo", files[0].Path);
            Assert.AreEqual(@"lib\40\A.dll", files[1].Path);
            var assemblyReferences = package.AssemblyReferences.ToList();
            Assert.AreEqual(1, assemblyReferences.Count);
            Assert.AreEqual("A.dll", assemblyReferences[0].Name);
            Assert.AreEqual(new FrameworkName(".NETFramework", new Version("4.0")), assemblyReferences[0].TargetFramework);
        }
    }
}
