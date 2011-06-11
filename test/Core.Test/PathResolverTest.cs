using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class PathResolverTest {
        [TestMethod]
        public void GetPathToEnumerateReturnsSearchPathIfSearchPathIsNotUnderBasePath() {
            // Arrange
            var basePath = @"c:\packages\bin";
            var searchPath = @"d:\work\projects\project1\bin\*\*.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.AreEqual(@"d:\work\projects\project1\bin", result);
        }

        [TestMethod]
        public void GetPathToEnumerateReturnsSearchPathIfSearchPathIsUnderBasePath() {
            // Arrange
            var basePath = @"d:\work";
            var searchPath = @"d:\work\projects\project1\bin\*\*.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.AreEqual(@"d:\work\projects\project1\bin", result);
        }

        [TestMethod]
        public void GetPathToEnumerateReturnsSearchPathIfItIsANetworkPath() {
            // Arrange
            var basePath = @"c:\work";
            var searchPath = @"\\build-vm\shared\drops\nuget.*";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.AreEqual(@"\\build-vm\shared\drops", result);
        }

        [TestMethod]
        public void GetPathToEnumerateReturnsCombinedPathFromBaseForSearchWithWildcardFileName() {
            // Arrange
            var basePath = @"c:\work\projects\my-project";
            var searchPath = @"bin\debug\*.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.AreEqual(@"c:\work\projects\my-project\bin\debug", result);
        }

        [TestMethod]
        public void GetPathToEnumerateReturnsCombinedPathFromBaseForSearchWithWildcardInPath() {
            // Arrange
            var basePath = @"c:\work\projects\my-project";
            var searchPath = @"output\*\binaries\NuGet.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.AreEqual(@"c:\work\projects\my-project\output", result);
        }

        [TestMethod]
        public void GetPathToEnumerateReturnsBasePathIfSearchPathStartsWithRecursiveWildcard() {
            // Arrange
            var basePath = @"c:\work\projects\my-project";
            var searchPath = @"**\*.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.AreEqual(@"c:\work\projects\my-project", result);
        }

        [TestMethod]
        public void GetPathToEnumerateReturnsBasePathIfSearchPathStartsWithWildcard() {
            // Arrange
            var basePath = @"c:\work\projects\my-project\bin";
            var searchPath = @"*.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.AreEqual(@"c:\work\projects\my-project\bin", result);
        }

        [TestMethod]
        public void GetPathToEnumerateReturnsFullPathIfSearchPathDoesNotContainWildCards() {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPath = @"bin\release\SuperBin.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.AreEqual(@"c:\work\projects\my-project\bin\release", result);
        }

        [TestMethod]
        public void ResolvePackagePathPreservesPortionOfWildCardInPackagePath() {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPattern = @"bin\release\**\*.dll";
            var fullPath = @"c:\work\projects\my-project\bin\release\net40\foo.dll";
            var targetPath = @"lib";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.AreEqual(@"lib\net40\foo.dll", result);
        }

        [TestMethod]
        public void ResolvePackagePathAppendsFullTargetPathToPortionOfWildCardInPackagePath() {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPattern = @"bin\release\**\*.dll";
            var fullPath = @"c:\work\projects\my-project\bin\release\net40\foo.dll";
            var targetPath = @"lib\assemblies\";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.AreEqual(@"lib\assemblies\net40\foo.dll", result);
        }

        [TestMethod]
        public void ResolvePackagePathAppendsFileNameToTargetForWildCardInExtension() {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPattern = @"bin\release\NuGet.*";
            var fullPath = @"c:\work\projects\my-project\bin\release\NuGet.pdb";
            var targetPath = @"lib\";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.AreEqual(@"lib\NuGet.pdb", result);
        }

        [TestMethod]
        public void ResolvePackagePathAppendsFileNameToTargetForWildCardInFileName() {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPattern = @"bin\release\*.dll";
            var fullPath = @"c:\work\projects\my-project\bin\release\NuGet.dll";
            var targetPath = @"lib\net40";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.AreEqual(@"lib\net40\NuGet.dll", result);
        }

        [TestMethod]
        public void ResolvePackagePathAppendsFileNameToTargetForWildCardInPath() {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPattern = @"out\*\NuGet*.dll";
            var fullPath = @"c:\work\projects\my-project\out\NuGet.Core\NuGet.dll";
            var targetPath = @"lib\net35";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.AreEqual(@"lib\net35\NuGet.dll", result);
        }

        [TestMethod]
        public void ResolvePackagePathAppendsFileNameToTargetForMultipleWildCardInPath() {
            // Arrange
            var basePath = @"c:\work\";
            var searchPattern = @"src\Nu*\bin\*\NuGet*.dll";
            var fullPath = @"c:\My Documents\Temporary Internet Files\NuGet\src\NuGet.Core\bin\release\NuGet.dll";
            var targetPath = @"lib";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.AreEqual(@"lib\NuGet.dll", result);
        }


        [TestMethod]
        public void ResolvePackagePathReturnsTargetPathIfNoWildCardIsPresentInSearchPatternAndTargetPathHasSameExtension() {
            // Arrange
            var basePath = @"c:\work\";
            var searchPattern = @"site\css\main.css";
            var fullPath = @"c:\work\site\css\main.css";
            var targetPath = @"content\css\site.css";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.AreEqual(@"content\css\site.css", result);
        }


        [TestMethod]
        public void GetMatchesFiltersByWildCards() {
            // Arrange
            var files = new[] { 
                new PhysicalPackageFile { SourcePath = @"content\1.txt" }, 
                new PhysicalPackageFile { SourcePath = @"content\foo\bar.txt" },
                new PhysicalPackageFile { SourcePath = @"baz.pdb" },
            };

            // Act
            var matches = PathResolver.GetMatches(files, f => f.SourcePath, new[] { @"content\*.txt", "*.pdb" });

            // Assert
            Assert.AreEqual(2, matches.Count());
            Assert.AreEqual(@"content\1.txt", matches.ElementAt(0).SourcePath);
            Assert.AreEqual(@"baz.pdb", matches.ElementAt(1).SourcePath);
        }

        [TestMethod]
        public void GetMatchesAllowsRecursiveWildcardMatches() {
            // Arrange
            var files = new[] { 
                new PhysicalPackageFile { SourcePath = @"content\1.txt" }, 
                new PhysicalPackageFile { SourcePath = @"content\foo\bar.txt" },
                new PhysicalPackageFile { SourcePath = @"lib\baz.pdb" },
                new PhysicalPackageFile { SourcePath = @"baz.dll" },
            };

            // Act
            var matches = PathResolver.GetMatches(files, f => f.SourcePath, new[] { @"content\**\.txt", "**.pdb" });

            // Assert
            Assert.AreEqual(3, matches.Count());
            Assert.AreEqual(@"content\1.txt", matches.ElementAt(0).SourcePath);
            Assert.AreEqual(@"content\foo\bar.txt", matches.ElementAt(1).SourcePath);
            Assert.AreEqual(@"lib\baz.pdb", matches.ElementAt(2).SourcePath);
        }

        [TestMethod]
        public void GetMatchesPerformsRecursiveWildcardSearch() {
            // Arrange
            var files = new[] { 
                new PhysicalPackageFile { SourcePath = @"content\1.txt" }, 
                new PhysicalPackageFile { SourcePath = @"content\foo\bar.txt" },
                new PhysicalPackageFile { SourcePath = @"lib\baz.pdb" },
                new PhysicalPackageFile { SourcePath = @"baz.dll" },
            };

            // Act
            var matches = PathResolver.GetMatches(files, f => f.SourcePath, new[] { @"content\**\.txt", "**.pdb" });

            // Assert
            Assert.AreEqual(3, matches.Count());
            Assert.AreEqual(@"content\1.txt", matches.ElementAt(0).SourcePath);
            Assert.AreEqual(@"content\foo\bar.txt", matches.ElementAt(1).SourcePath);
            Assert.AreEqual(@"lib\baz.pdb", matches.ElementAt(2).SourcePath);
        }

        [TestMethod]
        public void GetMatchesPerformsExactMatches() {
            // Arrange
            var files = new[] { 
                new PhysicalPackageFile { SourcePath = @"foo.dll" }, 
                new PhysicalPackageFile { SourcePath = @"content\foo.dll" },
                new PhysicalPackageFile { SourcePath = @"bin\debug\baz.dll" },
                new PhysicalPackageFile { SourcePath = @"bin\debug\notbaz.dll" },
            };

            // Act
            var matches = PathResolver.GetMatches(files, f => f.SourcePath, new[] { @"foo.dll", @"bin\*\b*.dll" });

            // Assert
            Assert.AreEqual(2, matches.Count());
            Assert.AreEqual(@"foo.dll", matches.ElementAt(0).SourcePath);
            Assert.AreEqual(@"bin\debug\baz.dll", matches.ElementAt(1).SourcePath);
        }

        [TestMethod]
        public void FilterPathRemovesItemsThatMatchWildcard() {
            // Arrange
            var files = new List<IPackageFile>(new[] { 
                new PhysicalPackageFile { TargetPath = @"foo.dll" }, 
                new PhysicalPackageFile { TargetPath = @"content\foo.dll" },
                new PhysicalPackageFile { TargetPath = @"bin\debug\baz.dll" },
                new PhysicalPackageFile { TargetPath = @"bin\debug\notbaz.dll" },
                new PhysicalPackageFile { TargetPath = @"bin\debug\baz.pdb" },
                new PhysicalPackageFile { TargetPath = @"bin\debug\notbaz.pdb" },
            });

            // Act
            PathResolver.FilterPackageFiles(files, f => f.Path, new[] { @"**\f*.dll", @"**\*.pdb" });

            // Assert
            Assert.AreEqual(2, files.Count());
            Assert.AreEqual(@"bin\debug\baz.dll", files[0].Path);
            Assert.AreEqual(@"bin\debug\notbaz.dll", files[1].Path);
        }
    }
}
