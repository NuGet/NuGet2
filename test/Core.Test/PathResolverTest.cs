using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace NuGet.Test
{

    public class PathResolverTest
    {
        [Fact]
        public void GetPathToEnumerateReturnsSearchPathIfSearchPathIsNotUnderBasePath()
        {
            // Arrange
            var basePath = @"c:\packages\bin";
            var searchPath = @"d:\work\projects\project1\bin\*\*.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.Equal(@"d:\work\projects\project1\bin", result);
        }

        [Fact]
        public void GetPathToEnumerateReturnsSearchPathIfSearchPathIsUnderBasePath()
        {
            // Arrange
            var basePath = @"d:\work";
            var searchPath = @"d:\work\projects\project1\bin\*\*.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.Equal(@"d:\work\projects\project1\bin", result);
        }

        [Fact]
        public void GetPathToEnumerateReturnsSearchPathIfItIsANetworkPath()
        {
            // Arrange
            var basePath = @"c:\work";
            var searchPath = @"\\build-vm\shared\drops\nuget.*";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.Equal(@"\\build-vm\shared\drops", result);
        }

        [Fact]
        public void GetPathToEnumerateReturnsSearchPathIfItIsRootedPath()
        {
            // Arrange
            var basePath = @"c:\work";
            var searchPath = @"\Volumes\Storage\users\test\repos\nuget\packages\test.*";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.Equal(@"\Volumes\Storage\users\test\repos\nuget\packages", result);
        }

        [Fact]
        public void GetPathToEnumerateReturnsCombinedPathFromBaseForSearchWithWildcardFileName()
        {
            // Arrange
            var basePath = @"c:\work\projects\my-project";
            var searchPath = @"bin\debug\*.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.Equal(@"c:\work\projects\my-project\bin\debug", result);
        }

        [Fact]
        public void GetPathToEnumerateReturnsCombinedPathFromBaseForSearchWithWildcardInPath()
        {
            // Arrange
            var basePath = @"c:\work\projects\my-project";
            var searchPath = @"output\*\binaries\NuGet.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.Equal(@"c:\work\projects\my-project\output", result);
        }

        [Fact]
        public void GetPathToEnumerateReturnsBasePathIfSearchPathStartsWithRecursiveWildcard()
        {
            // Arrange
            var basePath = @"c:\work\projects\my-project";
            var searchPath = @"**\*.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.Equal(@"c:\work\projects\my-project", result);
        }

        [Fact]
        public void GetPathToEnumerateReturnsBasePathIfSearchPathStartsWithWildcard()
        {
            // Arrange
            var basePath = @"c:\work\projects\my-project\bin";
            var searchPath = @"*.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.Equal(@"c:\work\projects\my-project\bin", result);
        }

        [Fact]
        public void GetPathToEnumerateReturnsFullPathIfSearchPathDoesNotContainWildCards()
        {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPath = @"bin\release\SuperBin.dll";

            // Act
            var result = PathResolver.GetPathToEnumerateFrom(basePath, searchPath);

            // Assert
            Assert.Equal(@"c:\work\projects\my-project\bin\release", result);
        }

        [Fact]
        public void ResolvePackagePathPreservesPortionOfWildCardInPackagePath()
        {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPattern = @"bin\release\**\*.dll";
            var fullPath = @"c:\work\projects\my-project\bin\release\net40\foo.dll";
            var targetPath = @"lib";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.Equal(@"lib\net40\foo.dll", result);
        }

        [Fact]
        public void ResolvePackagePathAppendsFullTargetPathToPortionOfWildCardInPackagePath()
        {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPattern = @"bin\release\**\*.dll";
            var fullPath = @"c:\work\projects\my-project\bin\release\net40\foo.dll";
            var targetPath = @"lib\assemblies\";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.Equal(@"lib\assemblies\net40\foo.dll", result);
        }

        [Fact]
        public void ResolvePackagePathAppendsFileNameToTargetForWildCardInExtension()
        {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPattern = @"bin\release\NuGet.*";
            var fullPath = @"c:\work\projects\my-project\bin\release\NuGet.pdb";
            var targetPath = @"lib\";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.Equal(@"lib\NuGet.pdb", result);
        }

        [Fact]
        public void ResolvePackagePathAppendsFileNameToTargetForWildCardInFileName()
        {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPattern = @"bin\release\*.dll";
            var fullPath = @"c:\work\projects\my-project\bin\release\NuGet.dll";
            var targetPath = @"lib\net40";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.Equal(@"lib\net40\NuGet.dll", result);
        }

        [Fact]
        public void ResolvePackagePathAppendsFileNameToTargetForWildCardInPath()
        {
            // Arrange
            var basePath = @"c:\work\projects\my-project\";
            var searchPattern = @"out\*\NuGet*.dll";
            var fullPath = @"c:\work\projects\my-project\out\NuGet.Core\NuGet.dll";
            var targetPath = @"lib\net35";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.Equal(@"lib\net35\NuGet.dll", result);
        }

        [Fact]
        public void ResolvePackagePathAppendsFileNameToTargetForMultipleWildCardInPath()
        {
            // Arrange
            var basePath = @"c:\work\";
            var searchPattern = @"src\Nu*\bin\*\NuGet*.dll";
            var fullPath = @"c:\My Documents\Temporary Internet Files\NuGet\src\NuGet.Core\bin\release\NuGet.dll";
            var targetPath = @"lib";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.Equal(@"lib\NuGet.dll", result);
        }


        [Fact]
        public void ResolvePackagePathReturnsTargetPathIfNoWildCardIsPresentInSearchPatternAndTargetPathHasSameExtension()
        {
            // Arrange
            var basePath = @"c:\work\";
            var searchPattern = @"site\css\main.css";
            var fullPath = @"c:\work\site\css\main.css";
            var targetPath = @"content\css\site.css";

            // Act
            var basePathToEnumerate = PathResolver.GetPathToEnumerateFrom(basePath, searchPattern);
            var result = PathResolver.ResolvePackagePath(basePathToEnumerate, searchPattern, fullPath, targetPath);

            // Assert
            Assert.Equal(@"content\css\site.css", result);
        }

        [Fact]
        public void GetMatchesFiltersByWildCards()
        {
            // Arrange
            var files = new[] { 
                new PhysicalPackageFile { SourcePath = @"content\1.txt" }, 
                new PhysicalPackageFile { SourcePath = @"content\foo\bar.txt" },
                new PhysicalPackageFile { SourcePath = @"baz.pdb" },
            };

            // Act
            var matches = PathResolver.GetMatches(files, f => f.SourcePath, new[] { @"content\*.txt", "*.pdb" });

            // Assert
            Assert.Equal(2, matches.Count());
            Assert.Equal(@"content\1.txt", matches.ElementAt(0).SourcePath);
            Assert.Equal(@"baz.pdb", matches.ElementAt(1).SourcePath);
        }

        [Fact]
        public void GetMatchesAllowsRecursiveWildcardMatches()
        {
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
            Assert.Equal(3, matches.Count());
            Assert.Equal(@"content\1.txt", matches.ElementAt(0).SourcePath);
            Assert.Equal(@"content\foo\bar.txt", matches.ElementAt(1).SourcePath);
            Assert.Equal(@"lib\baz.pdb", matches.ElementAt(2).SourcePath);
        }

        [Fact]
        public void GetMatchesAgainstUnixStylePaths()
        {
            // Arrange
            var files = new[] { 
                new PhysicalPackageFile { SourcePath = @"content\1.txt" }, 
                new PhysicalPackageFile { SourcePath = @"content\foo\bar.txt" },
                new PhysicalPackageFile { SourcePath = @"lib\baz.pdb" },
                new PhysicalPackageFile { SourcePath = @"baz.dll" },
            };

            // Act
            var matches = PathResolver.GetMatches(files, f => f.SourcePath, new[] { @"content/**/.txt", "**.pdb" });

            // Assert
            Assert.Equal(3, matches.Count());
            Assert.Equal(@"content\1.txt", matches.ElementAt(0).SourcePath);
            Assert.Equal(@"content\foo\bar.txt", matches.ElementAt(1).SourcePath);
            Assert.Equal(@"lib\baz.pdb", matches.ElementAt(2).SourcePath);
        }

        [Fact]
        public void GetMatchesPerformsRecursiveWildcardSearch()
        {
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
            Assert.Equal(3, matches.Count());
            Assert.Equal(@"content\1.txt", matches.ElementAt(0).SourcePath);
            Assert.Equal(@"content\foo\bar.txt", matches.ElementAt(1).SourcePath);
            Assert.Equal(@"lib\baz.pdb", matches.ElementAt(2).SourcePath);
        }

        [Fact]
        public void GetMatchesPerformsExactMatches()
        {
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
            Assert.Equal(2, matches.Count());
            Assert.Equal(@"foo.dll", matches.ElementAt(0).SourcePath);
            Assert.Equal(@"bin\debug\baz.dll", matches.ElementAt(1).SourcePath);
        }

        [Fact]
        public void FilterPathRemovesItemsThatMatchWildcard()
        {
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
            Assert.Equal(2, files.Count());
            Assert.Equal(@"bin\debug\baz.dll", files[0].Path);
            Assert.Equal(@"bin\debug\notbaz.dll", files[1].Path);
        }

        [Fact]
        public void NormalizeExcludeWildcardIgnoresBasePathForWildcardMatchingAnySubdirectory()
        {
            // Arrange
            var wildcard = @"**\exclude.me";
            var basePath = @"c:\base\path";

            // Act
            var result = PathResolver.NormalizeWildcardForExcludedFiles(basePath, wildcard);

            // Assert
            Assert.Equal(wildcard, result);
        }
    }
}
