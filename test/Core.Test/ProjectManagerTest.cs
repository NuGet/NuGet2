using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class ProjectManagerTest
    {
        [Fact]
        public void AddingPackageReferenceNullOrEmptyPackageIdThrows()
        {
            // Arrange
            ProjectManager projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.AddPackageReference((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.AddPackageReference(String.Empty), "packageId");
        }

        [Fact]
        public void AddingUnknownPackageReferenceThrows()
        {
            // Arrange
            ProjectManager projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.AddPackageReference("unknown"), "Unable to find package 'unknown'.");
        }

        [Fact]
        public void AddPackageReferenceAppliesPackageReferencesCorrectly()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A",
                "1.0",
                assemblyReferences: new[] { "lib\\a.dll", "lib\\b.dll" });
            sourceRepository.AddPackage(packageA);

            Mock<IPackage> mockPackageA = Mock.Get<IPackage>(packageA);
            mockPackageA.Setup(m => m.PackageAssemblyReferences).Returns(
                    new PackageReferenceSet[] { 
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("net40"), new [] { "a.dll" }),
                        new PackageReferenceSet(null, new [] { "b.dll" })
                    }
                );

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.True(projectSystem.ReferenceExists("a.dll"));
            Assert.False(projectSystem.ReferenceExists("b.dll"));
        }

        [Fact]
        public void AddPackageReferenceAppliesPackageReferencesCorrectly2()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A",
                "1.0",
                assemblyReferences: new[] { "lib\\net35\\a.dll", "lib\\net35\\b.dll" });
            sourceRepository.AddPackage(packageA);

            Mock<IPackage> mockPackageA = Mock.Get<IPackage>(packageA);
            mockPackageA.Setup(m => m.PackageAssemblyReferences).Returns(
                    new PackageReferenceSet[] { 
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("net40"), new [] { "a.dll" }),
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("net45"), new [] { "b.dll" })
                    }
                );

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.False(projectSystem.ReferenceExists("a.dll"));
            Assert.True(projectSystem.ReferenceExists("b.dll"));
        }

        [Fact]
        public void AddPackageReferenceAppliesPackageReferencesCorrectly3()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName("Silverlight, Version=4.5"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A",
                "1.0",
                assemblyReferences: new[] { "lib\\a.dll", "lib\\b.dll", "lib\\c.dll" });
            sourceRepository.AddPackage(packageA);

            Mock<IPackage> mockPackageA = Mock.Get<IPackage>(packageA);
            mockPackageA.Setup(m => m.PackageAssemblyReferences).Returns(
                    new PackageReferenceSet[] { 
                        new PackageReferenceSet(null, new [] { "b.dll", "a.dll", "d.dll" })
                    }
                );

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.True(projectSystem.ReferenceExists("a.dll"));
            Assert.True(projectSystem.ReferenceExists("b.dll"));
            Assert.False(projectSystem.ReferenceExists("c.dll"));
        }

        [Fact]
        public void AddPackageReferenceShouldLeaveDependencyPackageAloneIfItSatisfiesTheVersionConstraint()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA1 = PackageUtility.CreatePackage(
                "A",
                "1.1.2",
                assemblyReferences: new[] { "lib\\a1.dll" });

            IPackage packageA2 = PackageUtility.CreatePackage(
                "A",
                "1.1.9",
                assemblyReferences: new[] { "lib\\a2.dll" });

            IPackage packageB = PackageUtility.CreatePackage(
               "B",
               "1.0",
               dependencies: new [] { new PackageDependency("A", VersionUtility.ParseVersionSpec("1.1.0")) },
               assemblyReferences: new[] { "lib\\b.dll" });

            sourceRepository.AddPackage(packageA1);
            sourceRepository.AddPackage(packageA2);
            sourceRepository.AddPackage(packageB);

            // first install package A, version 1.1.2 into project
            projectManager.AddPackageReference(packageA1, ignoreDependencies: true, allowPrereleaseVersions: true);
            Assert.True(projectManager.LocalRepository.Exists("A", new SemanticVersion("1.1.2")));

            // Act
            // Now install B, which depends on A >= 1.1.0.
            projectManager.AddPackageReference("B");

            // Assert
            // NuGet should leave version 1.1.2 intact, because it already satisfies the version spec
            // It should not upgrade A to 1.1.9
            Assert.True(projectManager.LocalRepository.Exists("A", new SemanticVersion("1.1.2")));
            Assert.True(projectManager.LocalRepository.Exists("B", new SemanticVersion("1.0")));
            Assert.False(projectManager.LocalRepository.Exists("A", new SemanticVersion("1.1.9")));
        }

        [Fact]
        public void UpdatePackageReferenceShouldLeaveDependencyPackageAloneIfItSatisfiesTheVersionConstraint()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA1 = PackageUtility.CreatePackage(
                "A",
                "1.0.0",
                assemblyReferences: new[] { "lib\\a1.dll" });

            IPackage packageA2 = PackageUtility.CreatePackage(
                "A",
                "2.0.0",
                dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("1.0.0")) },
                assemblyReferences: new[] { "lib\\a2.dll" });

            IPackage packageB1 = PackageUtility.CreatePackage(
               "B",
               "1.0.0",
               assemblyReferences: new[] { "lib\\b1.dll" });

            IPackage packageB2 = PackageUtility.CreatePackage(
               "B",
               "1.0.2",
               assemblyReferences: new[] { "lib\\b2.dll" });

            sourceRepository.AddPackage(packageA1);
            sourceRepository.AddPackage(packageA2);
            sourceRepository.AddPackage(packageB1);
            sourceRepository.AddPackage(packageB2);

            projectManager.LocalRepository.AddPackage(packageA1);
            projectManager.LocalRepository.AddPackage(packageB1);

            // Act
            // Now install B, which depends on A >= 1.1.0.
            projectManager.UpdatePackageReference("A");

            // Assert
            // NuGet should leave version 1.1.2 intact, because it already satisfies the version spec
            // It should not upgrade A to 1.1.9
            Assert.True(projectManager.LocalRepository.Exists("A", new SemanticVersion("2.0.0")));
            Assert.True(projectManager.LocalRepository.Exists("B", new SemanticVersion("1.0")));
            Assert.False(projectManager.LocalRepository.Exists("B", new SemanticVersion("1.0.2")));
        }

        [Fact]
        public void UpdatePackageReferenceShouldLeaveDependencyPackageAloneIfItSatisfiesTheVersionConstraint2()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA1 = PackageUtility.CreatePackage(
                "A",
                "1.0.0",
                dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("1.0.0")) },
                assemblyReferences: new[] { "lib\\a1.dll" });

            IPackage packageA2 = PackageUtility.CreatePackage(
                "A",
                "2.0.0",
                dependencies: new[] { new PackageDependency("B", VersionUtility.ParseVersionSpec("1.0.0")) },
                assemblyReferences: new[] { "lib\\a2.dll" });

            IPackage packageB1 = PackageUtility.CreatePackage(
               "B",
               "1.0.0",
               assemblyReferences: new[] { "lib\\b1.dll" });

            IPackage packageB2 = PackageUtility.CreatePackage(
               "B",
               "1.0.2",
               assemblyReferences: new[] { "lib\\b2.dll" });

            sourceRepository.AddPackage(packageA1);
            sourceRepository.AddPackage(packageA2);
            sourceRepository.AddPackage(packageB1);
            sourceRepository.AddPackage(packageB2);

            projectManager.LocalRepository.AddPackage(packageA1);
            projectManager.LocalRepository.AddPackage(packageB1);

            // Act
            // Now install B, which depends on A >= 1.1.0.
            projectManager.UpdatePackageReference("A");

            // Assert
            // NuGet should leave version 1.1.2 intact, because it already satisfies the version spec
            // It should not upgrade A to 1.1.9
            Assert.True(projectManager.LocalRepository.Exists("A", new SemanticVersion("2.0.0")));
            Assert.True(projectManager.LocalRepository.Exists("B", new SemanticVersion("1.0")));
            Assert.False(projectManager.LocalRepository.Exists("B", new SemanticVersion("1.0.2")));
        }

        [Fact]
        public void AddPackageReferenceAppliesPackageReferencesCorrectlyWhenReferencesFilterOutAllAssemblies()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A",
                "1.0",
                assemblyReferences: new[] { "lib\\net35\\a.dll", "lib\\net35\\b.dll" });
            sourceRepository.AddPackage(packageA);

            Mock<IPackage> mockPackageA = Mock.Get<IPackage>(packageA);
            mockPackageA.Setup(m => m.PackageAssemblyReferences).Returns(
                    new PackageReferenceSet[] { 
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("net40"), new [] { "c.dll" }),
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("net45"), new [] { "d.dll" })
                    }
                );

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.False(projectSystem.ReferenceExists("a.dll"));
            Assert.False(projectSystem.ReferenceExists("b.dll"));
        }

        [Fact]
        public void AddPackageReferenceAppliesPackageReferencesCorrectlyWhenReferenceDoesNotMatch()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A",
                "1.0",
                assemblyReferences: new[] { "lib\\net35\\a.dll", "lib\\net35\\b.dll" });
            sourceRepository.AddPackage(packageA);

            Mock<IPackage> mockPackageA = Mock.Get<IPackage>(packageA);
            mockPackageA.Setup(m => m.PackageAssemblyReferences).Returns(
                    new PackageReferenceSet[] { 
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("sl50"), new [] { "a.dll" }),
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("wp8"), new [] { "b.dll" })
                    }
                );

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.True(projectSystem.ReferenceExists("a.dll"));
            Assert.True(projectSystem.ReferenceExists("b.dll"));
        }

        [Fact]
        public void AddingPackageReferenceThrowsExceptionPackageReferenceIsAdded()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            projectSystem.Setup(m => m.AddFile("file", It.IsAny<Stream>())).Throws<UnauthorizedAccessException>();
            projectSystem.Setup(m => m.Root).Returns("FakeRoot");
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "file" });
            sourceRepository.AddPackage(packageA);

            // Act
            ExceptionAssert.Throws<UnauthorizedAccessException>(() => projectManager.AddPackageReference("A"));

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA));
        }

        [Fact]
        public void AddingPackageReferenceAddsPreprocessedFileToTargetPathWithRemovedExtension()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { @"foo\bar\file.pp" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.FileExists(@"foo\bar\file.pp"));
            Assert.True(projectSystem.FileExists(@"foo\bar\file"));
        }

        [Fact]
        public void AddPackageReferenceThrowsWhenNoTargetFrameworkIsCompatibleWithPortableProject()
        {
            // Arrange
            var portableCollection = new NetPortableProfileCollection();
            portableCollection.Add(new NetPortableProfile("Profile104", new [] { VersionUtility.ParseFrameworkName("net45"), VersionUtility.ParseFrameworkName("sl5")}));

            NetPortableProfileTable.Profiles = portableCollection;

            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETPortable, Version=1.0, Profile=Profile104"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "silverlight4\\a.txt"});
            sourceRepository.AddPackage(packageA);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(
                () => projectManager.AddPackageReference("A"),
                "Could not install package 'A 1.0'. You are trying to install this package into a project that targets 'portable-net45+sl50', but the package does not contain any assembly references or content files that are compatible with that framework. For more information, contact the package author.");
        }

        [Fact]
        public void AddPackageReferenceThrowsWhenNoTargetFrameworkIsCompatibleWithNonPortableProject()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "silverlight4\\a.txt" });
            sourceRepository.AddPackage(packageA);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(
                () => projectManager.AddPackageReference("A"),
                "Could not install package 'A 1.0'. You are trying to install this package into a project that targets '.NETFramework,Version=v4.5', but the package does not contain any assembly references or content files that are compatible with that framework. For more information, contact the package author.");
        }

        [Fact]
        public void AddPackageReferencePicksPortableLibraryFilesOverFallbackFiles()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("portable-sl4+net4+wp8+windows8"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "portable-windows8+sl4+wp8+net4\\portable.txt", "me.txt" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.FileExists("me.txt"));
            Assert.True(projectSystem.FileExists("portable.txt"));
        }

        [Fact]
        public void AddPackageReferencePickPortableDependencySetOverFallbackDependencySet()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("portable-net45+wp8+windows8"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var dependencySets = new PackageDependencySet[] 
            {
                new PackageDependencySet(targetFramework: null, dependencies: new [] { PackageDependency.CreateDependency("B", "1.0.0")}),
                new PackageDependencySet(targetFramework: VersionUtility.ParseFrameworkName("net45"), dependencies: new PackageDependency[0] { }),
                new PackageDependencySet(targetFramework: VersionUtility.ParseFrameworkName("win8"), dependencies: new PackageDependency[0] { }),
                new PackageDependencySet(targetFramework: VersionUtility.ParseFrameworkName("wp8"), dependencies: new PackageDependency[0] { }),
                new PackageDependencySet(targetFramework: VersionUtility.ParseFrameworkName("portable-net45+win8+wp8"), dependencies: new PackageDependency[0] { }),
            };

            IPackage packageA = PackageUtility.CreatePackageWithDependencySets("A", "1.0", new[] { "me.txt" }, dependencySets: dependencySets);
            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", new[] { "you.txt" });

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            // Act
            projectManager.AddPackageReference("A", null, ignoreDependencies: false, allowPrereleaseVersions: true);

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.False(projectManager.LocalRepository.Exists("B"));

            Assert.True(projectSystem.FileExists("me.txt"));
            Assert.False(projectSystem.FileExists("you.txt"));
        }

        [Fact]
        public void AddPackageReferencePicksPortableLibraryAssemblyOverFallbackAssembly()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("portable-sl4+net45"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", assemblyReferences: new[] { "netcf\\you.dll", "lib\\portable-sl4+net45+wp8+windows8\\portable.dll", "lib\\me.dll" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.ReferenceExists("me.dll"));
            Assert.False(projectSystem.ReferenceExists("you.dll"));
            Assert.True(projectSystem.ReferenceExists("portable.dll"));
        }

        [Fact]
        public void AddPackageReferenceThrowsIfTheMinClientVersionIsNotSatisfied()
        {
            // Arrange            
            Version nugetVersion = typeof(IPackage).Assembly.GetName().Version;
            Version requiredVersion = new Version(nugetVersion.Major, nugetVersion.Minor, nugetVersion.Build, nugetVersion.Revision + 1);

            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("net40"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", assemblyReferences: new[] { "lib\\me.dll" }, minClientVersion: requiredVersion.ToString());
            sourceRepository.AddPackage(packageA);

            string expectedErrorMessage = 
                String.Format("The '{0}' package requires NuGet client version '{1}' or above, but the current NuGet version is '{2}'.", "A 1.0", requiredVersion.ToString(), nugetVersion.ToString());

            // Act && Assert
            ExceptionAssert.Throws<NuGetVersionNotSatisfiedException>(() => projectManager.AddPackageReference("A"), expectedErrorMessage);
        }

        [Fact]
        public void AddPackageReferenceThrowsIfTheMinClientVersionOfADependencyIsNotSatisfied()
        {
            // Arrange            
            Version nugetVersion = typeof(IPackage).Assembly.GetName().Version;
            Version requiredVersion = new Version(nugetVersion.Major, nugetVersion.Minor, nugetVersion.Build, nugetVersion.Revision + 1);

            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("net40"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A", 
                "1.0", 
                assemblyReferences: new[] { "lib\\me.dll" },
                dependencies: new PackageDependency[] { new PackageDependency("B") });
            IPackage packageB = PackageUtility.CreatePackage("B", "2.0", assemblyReferences: new[] { "lib\\you.dll" }, minClientVersion: requiredVersion.ToString());
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            string expectedErrorMessage =
                String.Format("The '{0}' package requires NuGet client version '{1}' or above, but the current NuGet version is '{2}'.", "B 2.0", requiredVersion.ToString(), nugetVersion.ToString());

            // Act && Assert
            ExceptionAssert.Throws<NuGetVersionNotSatisfiedException>(() => projectManager.AddPackageReference("A"), expectedErrorMessage);
        }

        [Fact]
        public void AddPackageReferenceDoesNotThrowIfTheMinClientVersionIsEqualNuGetVersion()
        {
            // Arrange            
            Version nugetVersion = typeof(IPackage).Assembly.GetName().Version;
            Version requiredVersion = nugetVersion;

            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("net40"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", assemblyReferences: new[] { "lib\\me.dll" }, minClientVersion: requiredVersion.ToString());
            sourceRepository.AddPackage(packageA);

            // Act && Assert
            projectManager.AddPackageReference("A");
        }

        [Fact]
        public void AddPackageReferenceDoesNotThrowIfTheMinClientVersionIsLessThanNuGetVersion()
        {
            // Arrange            
            Version nugetVersion = typeof(IPackage).Assembly.GetName().Version;
            Version requiredVersion;
            if (nugetVersion.Minor > 0)
            {
                requiredVersion = new Version(nugetVersion.Major, nugetVersion.Minor - 1, nugetVersion.Build, nugetVersion.Revision);
            }
            else
            {
                requiredVersion = new Version(nugetVersion.Major - 1, nugetVersion.Minor, nugetVersion.Build, nugetVersion.Revision);
            }

            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("net40"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", assemblyReferences: new[] { "lib\\me.dll" }, minClientVersion: requiredVersion.ToString());
            sourceRepository.AddPackage(packageA);

            // Act && Assert
            projectManager.AddPackageReference("A");
        }

        [Theory]
        [InlineData("net20\\one.txt", ".NETFramework, Version=4.0")]
        [InlineData("silverlight3\\one.txt", "Silverlight, Version=4.0")]
        [InlineData("wp7\\one.txt", "WindowsPhone, Version=8.0")]
        [InlineData("wp7\\one.txt", "Silverlight, Version=4.0, Profile=WindowsPhone71")]
        [InlineData("windows8\\one.txt", ".NETCore, Version=4.5")]
        public void AddPackageReferencePicksSpecificLibraryOverPortableOne(string pickedContentFile, string projectFramework)
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(projectFramework));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "portable40-net40+wp7+silverlight4+windows\\two.txt", pickedContentFile});
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.FileExists("two.txt"));
            Assert.True(projectSystem.FileExists("one.txt"));
        }

        [Fact]
        public void AddPackageReferencePrefersFullProfileOverClientProfileWhenInstallIntoFullProfileProject()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A",
                "1.0",
                content: new[] { "net40-client\\b.txt", "net40\\a.txt" },
                assemblyReferences: new[] { "lib\\net40\\a.dll", "lib\\net40-client\\b.dll" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.ReferenceExists("b.dll"));
            Assert.False(projectSystem.FileExists("b.txt"));

            Assert.True(projectSystem.ReferenceExists("a.dll"));
            Assert.True(projectSystem.FileExists("a.txt"));
        }

        [Fact]
        public void AddPackageReferencePrefersVersionClosenessOverClientProfileCompatibility()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5, Profile=Client"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A",
                "1.0",
                content: new[] { "net35\\a.txt", "net4.0.0.1\\b.txt", "net40-client\\c.txt", "net4.5.0.1-client\\d.txt" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.FileExists("a.txt"));
            Assert.True(projectSystem.FileExists("b.txt"));
            Assert.False(projectSystem.FileExists("c.txt"));
            Assert.False(projectSystem.FileExists("d.txt"));
        }

        [Fact]
        public void AddPackageReferencePicksMatchingProfileEvenIfItIsEmpty()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("sl4"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "sl3\\_._", "me.txt" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.FileExists("me.txt"));
            Assert.False(projectSystem.FileExists("_._"));
        }

        [Theory]
        [InlineData("portable-wp7+sl3+net40\\two.txt", "portable-net45+sl4\\one.txt")]
        [InlineData("portable-net40+sl3+wp71\\one.txt", "portable-windows8+sl2\\two.txt")]
        public void AddPackageReferencePicksThePortableLibraryWithHigherVersionOfTheMatchingFrameworks(string contentFile, string otherContentFile)
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName("Silverlight, Version=4.0"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { contentFile, otherContentFile });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectSystem.FileExists("one.txt"));
            Assert.False(projectSystem.FileExists("two.txt"));
        }

        [Fact]
        public void AddPackageReferencePicksThePortableLibraryWithMoreMatchingVersionsWhenInstalledIntoPortableProject()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("portable-net45+sl5+wp71"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A", 
                "1.0", 
                new[] { "portable-net45+sl5+wp8\\one.txt",
                        "portable-net45+sl5+wp71\\two.txt",
                        "portable-net45+sl5+wp71+win8\\three.txt",
                        "portable-net45+sl4+wp71+win8\\four.txt",
                        "portable-net40+sl4+wp71+win8\\five.txt",
                        "portable-net40+sl4+wp7+win8\\six.txt",
                        "portable-wp8+win8\\seven.txt" });
            
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.FileExists("one.txt"));
            Assert.True(projectSystem.FileExists("two.txt"));
            Assert.False(projectSystem.FileExists("three.txt"));
            Assert.False(projectSystem.FileExists("three.txt"));
            Assert.False(projectSystem.FileExists("four.txt"));
            Assert.False(projectSystem.FileExists("five.txt"));
            Assert.False(projectSystem.FileExists("six.txt"));
            Assert.False(projectSystem.FileExists("seven.txt"));
        }

        [Fact]
        public void AddPackageReferencePicksThePortableLibraryWithMoreMatchingVersionsWhenInstalledIntoPortableProject2()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("portable-net45+sl5+wp71"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A",
                "1.0",
                new[] { "portable-net45+sl5+wp8\\one.txt",
                        "portable-net45+sl5+wp8\\two.txt",
                        "portable-net45+sl5+wp71+win8\\three.txt",
                        "portable-net45+sl4+wp71+win8\\four.txt",
                        "portable-net40+sl4+wp71+win8\\five.txt",
                        "portable-net40+sl4+wp7+win8\\six.txt",
                        "portable-wp8+win8\\seven.txt" });

            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.FileExists("one.txt"));
            Assert.False(projectSystem.FileExists("two.txt"));
            Assert.True(projectSystem.FileExists("three.txt"));
            Assert.False(projectSystem.FileExists("four.txt"));
            Assert.False(projectSystem.FileExists("five.txt"));
            Assert.False(projectSystem.FileExists("six.txt"));
            Assert.False(projectSystem.FileExists("seven.txt"));
        }

        [Fact]
        public void AddPackageReferencePicksThePortableLibraryWithLeastPlatformsWhenInstalledIntoNonPortableProject()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(VersionUtility.ParseFrameworkName("netcore45"));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage(
                "A",
                "1.0",
                new[] { "portable-net45+sl4+win8+wp8\\zero.txt",
                        "portable-net45+win8+wp8\\one.txt",
                        "portable-net40+sl4+win8+wp71\\two.txt"});

            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.FileExists("zero.txt"));
            Assert.True(projectSystem.FileExists("one.txt"));
            Assert.False(projectSystem.FileExists("two.txt"));
        }

        [Fact]
        public void AddPackageReferenceWhenNewVersionOfPackageAlreadyReferencedThrows()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    new PackageDependency("B")
                                                                }, content: new[] { "foo" });
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    new PackageDependency("B")
                                                                }, content: new[] { "foo" });
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "foo" });
            projectManager.LocalRepository.AddPackage(packageA20);
            projectManager.LocalRepository.AddPackage(packageB10);

            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.AddPackageReference("A", SemanticVersion.Parse("1.0")), @"Already referencing a newer version of 'A'.");
        }

        [Fact]
        public void RemovingUnknownPackageReferenceThrows()
        {
            // Arrange
            var projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.RemovePackageReference("foo"), "Unable to find package 'foo'.");
        }

        [Fact]
        public void RemovingPackageWithModifiedContentFileWithinIgnoreMarkersSucceeds()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(
                sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var packageA = PackageUtility.CreatePackage("A", "1.0", content: new[] { "a.file" });
            sourceRepository.AddPackage(packageA);

            projectManager.AddPackageReference("A");
            Assert.True(projectManager.LocalRepository.Exists(packageA));
            Assert.True(projectSystem.FileExists("a.file"));

            string s = projectSystem.ReadAllText("a.file");

            // now modify 'a.file' to include ignore line markers
            projectSystem.AddFile("a.file", @"content\a.file
-----------------nuget: begin license text -------
dsaflkdjsal;fkjdsal;kjf
sdafkljdsal;kjfl;dkasjfl;kdas
fdsalk;fj;lkdsajfl;kdsa
    NUGET: END LICENSE TEXT --------------
");
            // Act 
            projectManager.RemovePackageReference("A");
            
            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.False(projectSystem.FileExists("a.file"));
        }

        [Fact]
        public void RemovingPackageWithModifiedContentFileWithinIgnoreMarkersSucceeds2()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(
                sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var packageA = PackageUtility.CreatePackage("A", "1.0", content: new[] { "a.file" });

            var contentFile = PackageUtility.CreateMockedPackageFile("content", "a.file", @"this is awesome.
*******NUGET: BEGIN LICENSE TEXT------------------
SDAFLKDSAJFL;KJDSAL;KFJL;DSAKJFL;KDSA
******NUGET: END LICENSE TEXT-------");

            var mockPackageA = Mock.Get<IPackage>(packageA);
            mockPackageA.Setup(p => p.GetFiles()).Returns(new[] { contentFile.Object });
            
            sourceRepository.AddPackage(packageA);

            projectManager.AddPackageReference("A");
            Assert.True(projectManager.LocalRepository.Exists(packageA));
            Assert.True(projectSystem.FileExists("a.file"));

            string s = projectSystem.ReadAllText("a.file");

            // now modify 'a.file' to include ignore line markers
            projectSystem.AddFile("a.file", @"this is awesome.");

            // Act 
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.False(projectSystem.FileExists("a.file"));
        }

        [Fact]
        public void RemovingPackageWithModifiedContentFileWithinBeginMarkersRemoveFile()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(
                sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var packageA = PackageUtility.CreatePackage("A", "1.0", content: new[] { "a.file" });
            sourceRepository.AddPackage(packageA);

            projectManager.AddPackageReference("A");
            Assert.True(projectManager.LocalRepository.Exists(packageA));
            Assert.True(projectSystem.FileExists("a.file"));

            string s = projectSystem.ReadAllText("a.file");

            // now modify 'a.file' to include ignore line markers
            projectSystem.AddFile("a.file", @"content\a.file
-----------------NUGET: BEGIN LICENSE TEXT
dsaflkdjsal;fkjdsal;kjf
sdafkljdsal;kjfl;dkasjfl;kdas
fdsalk;fj;lkdsajfl;kdsa");
            // Act 
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.False(projectSystem.FileExists("a.file"));
        }

        [Fact]
        public void RemovingPackageReferenceWithOtherProjectWithReferencesThatWereNotCopiedToProject()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var packageA = PackageUtility.CreatePackage("A", "1.0", content: new[] { "a.file" });
            var packageB = PackageUtility.CreatePackage("B", "1.0",
                                                        content: null,
                                                        assemblyReferences: new[] { PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName("SP", new Version("40.0"))) },
                                                        tools: null,
                                                        dependencies: null,
                                                        downloadCount: 0,
                                                        description: null,
                                                        summary: null,
                                                        listed: true,
                                                        tags: null);
            projectManager.LocalRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageA);
            projectManager.LocalRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageB);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
        }

        [Fact]
        public void RemovingUnknownPackageReferenceNullOrEmptyPackageIdThrows()
        {
            // Arrange
            var projectManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.RemovePackageReference((string)null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => projectManager.RemovePackageReference(String.Empty), "packageId");
        }

        [Fact]
        public void RemovingPackageReferenceWithNoDependents()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var package = PackageUtility.CreatePackage("foo", "1.2.33", content: new[] { "file1" });
            projectManager.LocalRepository.AddPackage(package);
            sourceRepository.AddPackage(package);

            // Act
            projectManager.RemovePackageReference("foo");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(package));
        }

        [Fact]
        public void AddPackageReferenceAddsContentAndReferencesProjectSystem()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "contentFile" },
                                                        new[] { "reference.dll" },
                                                        new[] { "tool" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(1, projectSystem.Paths.Count);
            Assert.Equal(1, projectSystem.References.Count);
            Assert.True(projectSystem.References.ContainsKey(@"reference.dll"));
            Assert.True(projectSystem.FileExists(@"contentFile"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceAddingPackageWithDuplicateReferenceOverwritesReference()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        assemblyReferences: new[] { "reference.dll" });
            var packageB = PackageUtility.CreatePackage("B", "1.0",
                                                        assemblyReferences: new[] { "reference.dll" });

            mockRepository.AddPackage(packageA);
            mockRepository.AddPackage(packageB);

            // Act
            projectManager.AddPackageReference("A");
            projectManager.AddPackageReference("B");

            // Assert
            Assert.Equal(0, projectSystem.Paths.Count);
            Assert.Equal(1, projectSystem.References.Count);
            Assert.True(projectSystem.References.ContainsKey(@"reference.dll"));
            Assert.True(projectSystem.References.ContainsValue(@"MockFileSystem\B.1.0\reference.dll"));
            Assert.True(localRepository.Exists("A"));
            Assert.True(localRepository.Exists("B"));
        }

        [Fact]
        public void AddPackageReferenceRaisesOnBeforeInstallAndOnAfterInstall()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "contentFile" },
                                                        new[] { "reference.dll" },
                                                        new[] { "tool" });
            projectManager.PackageReferenceAdding += (sender, e) =>
            {
                // Assert
                Assert.Equal(e.InstallPath, @"x:\MockFileSystem\A.1.0");
                Assert.Same(e.Package, packageA);
            };

            projectManager.PackageReferenceAdded += (sender, e) =>
            {
                // Assert
                Assert.Equal(e.InstallPath, @"x:\MockFileSystem\A.1.0");
                Assert.Same(e.Package, packageA);
            };

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");
        }

        [Fact]
        public void RemovePackageReferenceRaisesOnBeforeUninstallAndOnAfterUninstall()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { @"sub\file1", @"sub\file2" });
            projectManager.PackageReferenceRemoving += (sender, e) =>
            {
                // Assert
                Assert.Equal(e.InstallPath, @"x:\MockFileSystem\A.1.0");
                Assert.Same(e.Package, packageA);
            };

            projectManager.PackageReferenceRemoved += (sender, e) =>
            {
                // Assert
                Assert.Equal(e.InstallPath, @"x:\MockFileSystem\A.1.0");
                Assert.Same(e.Package, packageA);
            };

            mockRepository.AddPackage(packageA);
            projectManager.AddPackageReference("A");

            // Act
            projectManager.RemovePackageReference("A");
        }

        [Fact]
        public void RemovePackageReferenceExcludesFileIfAnotherPackageUsesThem()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { "fileA", "commonFile" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            new[] { "fileB", "commonFile" });

            mockRepository.AddPackage(packageA);
            mockRepository.AddPackage(packageB);

            projectManager.AddPackageReference("A");
            projectManager.AddPackageReference("B");

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.True(mockProjectSystem.Deleted.Contains(@"fileA"));
            Assert.True(mockProjectSystem.FileExists(@"commonFile"));
        }

        [Fact]
        public void AddPackageWithUnsupportedFilesSkipsUnsupportedFiles()
        {
            // Arrange            
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            projectSystem.Setup(m => m.IsSupportedFile("unsupported")).Returns(false);
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, localRepository);
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "a", "b", "unsupported" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(2, projectSystem.Object.Paths.Count);
            Assert.True(projectSystem.Object.FileExists("a"));
            Assert.True(projectSystem.Object.FileExists("b"));
            Assert.True(localRepository.Exists("A"));
            Assert.False(projectSystem.Object.FileExists("unsupported"));
        }

        [Fact]
        public void AddPackageWithUnsupportedTransformFileSkipsUnsupportedFile()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var localRepository = new MockPackageRepository();
            var projectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            projectSystem.Setup(m => m.IsSupportedFile("unsupported")).Returns(false);
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, localRepository);
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "a", "b", "unsupported.pp" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(2, projectSystem.Object.Paths.Count);
            Assert.True(projectSystem.Object.FileExists("a"));
            Assert.True(projectSystem.Object.FileExists("b"));
            Assert.True(localRepository.Exists("A"));
            Assert.False(projectSystem.Object.FileExists("unsupported"));
        }

        [Fact]
        public void AddPackageDoNotTransformPackagesConfigFile()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var localRepository = new MockPackageRepository();
            var projectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, localRepository);
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", 
                new[] { "a", "b", "packages.config.pp", "PACKAGES.config.transform", "packages.config.install.xdt", "packages.config.uninstall.xdt" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(6, projectSystem.Object.Paths.Count);
            Assert.True(projectSystem.Object.FileExists("a"));
            Assert.True(projectSystem.Object.FileExists("b"));
            Assert.True(projectSystem.Object.FileExists("packages.config.pp"));
            Assert.True(projectSystem.Object.FileExists("packages.config.transform"));
            Assert.True(projectSystem.Object.FileExists("packages.config.install.xdt"));
            Assert.True(projectSystem.Object.FileExists("packages.config.uninstall.xdt"));
            Assert.True(localRepository.Exists("A"));
            Assert.False(projectSystem.Object.FileExists("packages.config"));
        }

        [Fact]
        public void AddPackageDoNotTransformPackagesConfigFileInNestedFolder()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var localRepository = new MockPackageRepository();
            var projectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem.Object), projectSystem.Object, localRepository);
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "a", "b", "sub\\packages.config.pp", "local\\PACKAGES.config.transform", "fod\\packages.config.Install.XDT", "car\\Packages.Config.uninstall.Xdt" });
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(6, projectSystem.Object.Paths.Count);
            Assert.True(projectSystem.Object.FileExists("a"));
            Assert.True(projectSystem.Object.FileExists("b"));
            Assert.True(projectSystem.Object.FileExists("sub\\packages.config.pp"));
            Assert.True(projectSystem.Object.FileExists("local\\packages.config.transform"));
            Assert.True(projectSystem.Object.FileExists("fod\\packages.config.install.xdt"));
            Assert.True(projectSystem.Object.FileExists("car\\packages.config.uninstall.xdt"));
            Assert.True(localRepository.Exists("A"));
            Assert.False(projectSystem.Object.FileExists("sub\\packages.config"));
            Assert.False(projectSystem.Object.FileExists("local\\packages.config"));
        }

        [Fact]
        public void AddPackageWithTransformFile()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            mockProjectSystem.AddFile("web.config",
@"<configuration>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" />
    </system.web>
</configuration>
".AsStream());
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());
            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            package.Setup(m => m.Listed).Returns(true);
            var file = new Mock<IPackageFile>();
            file.Setup(m => m.Path).Returns(@"content\web.config.transform");
            file.Setup(m => m.EffectivePath).Returns("web.config.transform");
            // in the transform snippet below, we put the <add> tag on the same line 
            // as <configSections> tag to verify that the transform engine preserves 
            // formatting of the transform file.
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration>
    <configSections> <add a=""n"" />
    </configSections>
</configuration>
".AsStream());
            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object });
            mockRepository.AddPackage(package.Object);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            // TODO Config transformation should preserve white-space (formatting)
            // It does not at the moment, therefore <system.web> element has different indent than <configSections>.
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <configSections> <add a=""n"" />
    </configSections>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" />
    </system.web>
</configuration>
", mockProjectSystem.OpenFile("web.config").ReadToEnd());
        }

        [Fact]
        public void AddPackageAskToResolveConflictForEveryFile()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var localRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            projectSystem.AddFile("one.txt", "this is one");
            projectSystem.AddFile("two.txt", "this is two");

            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", content: new[] { "one.txt", "two.txt" });
            sourceRepository.AddPackage(packageA);

            var logger = new Mock<ILogger>();
            logger.Setup(l => l.ResolveFileConflict(It.IsAny<string>())).Returns(FileConflictResolution.OverwriteAll);
            projectManager.Logger = projectSystem.Logger = logger.Object;

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(localRepository.Exists("A"));
            Assert.Equal("content\\one.txt", projectSystem.ReadAllText("one.txt"));
            Assert.Equal("content\\two.txt", projectSystem.ReadAllText("two.txt"));

            logger.Verify(l => l.ResolveFileConflict(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void AddPackageAskToResolveConflictForEveryFileWithDependency()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var localRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            projectSystem.AddFile("one.txt", "this is one");
            projectSystem.AddFile("two.txt", "this is two");

            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            IPackage packageA = PackageUtility.CreatePackage(
                "A", "1.0", content: new[] { "one.txt" }, dependencies: new PackageDependency[] { new PackageDependency("B") });
            sourceRepository.AddPackage(packageA);

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "two.txt" });
            sourceRepository.AddPackage(packageB);

            var logger = new Mock<ILogger>();
            logger.Setup(l => l.ResolveFileConflict(It.IsAny<string>())).Returns(FileConflictResolution.OverwriteAll);
            projectManager.Logger = projectSystem.Logger = logger.Object;

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(localRepository.Exists("A"));
            Assert.Equal("content\\one.txt", projectSystem.ReadAllText("one.txt"));
            Assert.Equal("content\\two.txt", projectSystem.ReadAllText("two.txt"));

            logger.Verify(l => l.ResolveFileConflict(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void RemovePackageWithTransformFile()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            mockProjectSystem.AddFile("web.config",
@"<configuration>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" baz=""test"" />
    </system.web>
</configuration>
".AsStream());
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());
            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            var file = new Mock<IPackageFile>();
            file.Setup(m => m.Path).Returns(@"content\web.config.transform");
            file.Setup(m => m.EffectivePath).Returns("web.config.transform");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" />
    </system.web>
</configuration>
".AsStream());
            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object });
            mockRepository.AddPackage(package.Object);
            projectManager.LocalRepository.AddPackage(package.Object);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <system.web>
        <compilation baz=""test"" />
    </system.web>
</configuration>
", mockProjectSystem.OpenFile("web.config").ReadToEnd());
        }

        [Fact]
        public void RemovePackageWithTransformFileThatThrowsContinuesRemovingPackage()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            var localRepository = new MockPackageRepository();
            mockProjectSystem.AddFile("web.config", () => { throw new UnauthorizedAccessException(); });
            mockProjectSystem.AddFile("foo.txt");
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, localRepository);
            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            var file = new Mock<IPackageFile>();
            var contentFile = new Mock<IPackageFile>();
            contentFile.Setup(m => m.Path).Returns(@"content\foo.txt");
            contentFile.Setup(m => m.GetStream()).Returns(new MemoryStream());
            contentFile.Setup(m => m.EffectivePath).Returns("foo.txt");
            file.Setup(m => m.Path).Returns(@"content\web.config.transform");
            file.Setup(m => m.EffectivePath).Returns("web.config.transform");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" />
    </system.web>
</configuration>
".AsStream());
            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object, contentFile.Object });
            mockRepository.AddPackage(package.Object);
            projectManager.LocalRepository.AddPackage(package.Object);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(mockProjectSystem.FileExists("foo.txt"));
            Assert.False(localRepository.Exists(package.Object));
        }

        [Fact]
        public void RemovePackageWithUnsupportedTransformFileDoesNothing()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            mockProjectSystem.Setup(m => m.IsSupportedFile("web.config")).Returns(false);
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem.Object, new MockPackageRepository());
            var package = new Mock<IPackage>();
            package.Setup(m => m.Id).Returns("A");
            package.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            var file = new Mock<IPackageFile>();
            file.Setup(m => m.Path).Returns(@"content\web.config.transform");
            file.Setup(m => m.EffectivePath).Returns("web.config.transform");
            file.Setup(m => m.GetStream()).Returns(() =>
@"<configuration>
    <system.web>
        <compilation debug=""true"" targetFramework=""4.0"" />
    </system.web>
</configuration>
".AsStream());
            package.Setup(m => m.GetFiles()).Returns(new[] { file.Object });
            mockRepository.AddPackage(package.Object);
            projectManager.LocalRepository.AddPackage(package.Object);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(mockProjectSystem.Object.FileExists("web.config"));
        }

        [Fact]
        public void RemovePackageRemovesDirectoriesAddedByPackageFilesIfEmpty()
        {
            // Arrange
            var mockProjectSystem = new MockProjectSystem();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(new MockProjectSystem()), mockProjectSystem, new MockPackageRepository());
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                             new[] { @"sub\file1", @"sub\file2" });

            mockRepository.AddPackage(packageA);
            projectManager.AddPackageReference("A");

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.True(mockProjectSystem.Deleted.Contains(@"sub\file1"));
            Assert.True(mockProjectSystem.Deleted.Contains(@"sub\file2"));
            Assert.True(mockProjectSystem.Deleted.Contains("sub"));
        }

        [Fact]
        public void AddPackageReferenceWhenOlderVersionOfPackageInstalledDoesAnUpgrade()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", "[1.0]")
                                                                },
                                                                content: new[] { "foo" });

            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", "[2.0]")
                                                                },
                                                                content: new[] { "bar" });
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "foo" });
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "foo" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
            Assert.False(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.True(projectManager.LocalRepository.Exists(packageB20));
        }

        [Fact]
        public void UpdatePackageNullOrEmptyPackageIdThrows()
        {
            // Arrange
            ProjectManager packageManager = CreateProjectManager();

            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UpdatePackageReference(null), "packageId");
            ExceptionAssert.ThrowsArgNullOrEmpty(() => packageManager.UpdatePackageReference(String.Empty), "packageId");
        }

        [Fact]
        public void UpdatePackageReferenceWithMixedDependenciesUpdatesPackageAndDependenciesIfUnused()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0, C 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A",
                                                               "1.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B","[1.0]"),
                                                                    PackageDependency.CreateDependency("C","[1.0]")
                                                                }, content: new[] { "A.file" });

            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "B.fie" });
            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0", content: new[] { "C.file" });

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);

            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                                    PackageDependency.CreateDependency("B", "[1.0]"),
                                                                                    PackageDependency.CreateDependency("C", "[2.0]"),
                                                                                    PackageDependency.CreateDependency("D", "[1.0]")
                                                               }, content: new[] { "A.20.file" });

            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", content: new[] { "C.20" });
            IPackage packageD10 = PackageUtility.CreatePackage("D", "1.0", content: new[] { "D.20" });

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);
            sourceRepository.AddPackage(packageD10);

            // Act
            projectManager.UpdatePackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.True(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageC20));
            Assert.True(projectManager.LocalRepository.Exists(packageD10));
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
            Assert.False(projectManager.LocalRepository.Exists(packageC10));
        }

        [Fact]
        public void UpdatePackageReferenceIfPackageNotReferencedThrows()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A"), @"x:\MockFileSystem does not reference 'A'.");
        }

        [Fact]
        public void UpdatePackageReferenceToOlderVersionThrows()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0");
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0");
            IPackage packageA30 = PackageUtility.CreatePackage("A", "3.0");

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageA30);

            projectManager.LocalRepository.AddPackage(packageA20);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A", version: SemanticVersion.Parse("1.0")), @"Already referencing a newer version of 'A'.");
        }

        [Fact]
        public void UpdatePackageReferenceWithUnresolvedDependencyThrows()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                   PackageDependency.CreateDependency("B", "[1.0]"),
                                                               });

            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0");

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);

            // A 2.0 -> [B 2.0]
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", "[2.0]")
                                                            });

            sourceRepository.AddPackage(packageA20);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A"), "Unable to resolve dependency 'B (= 2.0)'.");
        }

        [Fact]
        public void UpdatePackageReferenceWithUpdateDependenciesSetToFalseIgnoresDependencies()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                               dependencies: new List<PackageDependency> { 
                                                                   PackageDependency.CreateDependency("B", "[1.0]"),
                                                               }, content: new[] { "A.cs" });


            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "B.fs.spp" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);

            // A 2.0 -> [B 2.0]
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                    PackageDependency.CreateDependency("B", "[2.0]"),
                                                                }, content: new[] { "D.a" });

            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "B.s" });

            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB20);

            // Act
            projectManager.UpdatePackageReference("A", version: null, updateDependencies: false, allowPrereleaseVersions: false);

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
            Assert.True(projectManager.LocalRepository.Exists(packageB10));
            Assert.False(projectManager.LocalRepository.Exists(packageB20));
        }

        [Fact]
        public void UpdatePackageHasNoEffectIfConstraintsDefinedDontAllowForUpdates()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var constraintProvider = new Mock<IPackageConstraintProvider>();
            constraintProvider.Setup(m => m.GetConstraint("A")).Returns(VersionUtility.ParseVersionSpec("[1.0, 2.0)"));
            constraintProvider.Setup(m => m.Source).Returns("foo");
            projectManager.ConstraintProvider = constraintProvider.Object;
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0");
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0");

            projectManager.LocalRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);

            // Act
            projectManager.UpdatePackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA10));
            Assert.False(projectManager.LocalRepository.Exists(packageA20));
        }

        [Fact]
        public void UpdateDependencyDependentsHaveSatisfiableDependencies()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [C >= 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> {                                                                         
                                                                        PackageDependency.CreateDependency("C", "1.0")
                                                                    }, content: new[] { "A" });

            // B 1.0 -> [C <= 2.0]
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0",
                                                                dependencies: new List<PackageDependency> {                                                                         
                                                                        PackageDependency.CreateDependency("C", "2.0")
                                                                    }, content: new[] { "B" });

            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0", content: new[] { "C" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);

            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", content: new[] { "C2" });

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);

            // Act
            projectManager.UpdatePackageReference("C");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA10));
            Assert.True(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageC20));
            Assert.False(projectManager.LocalRepository.Exists(packageC10));
        }

        [Fact]
        public void UpdatePackageReferenceDoesNothingIfVersionIsNotSpecifiedAndNewVersionIsLessThanOldPrereleaseVersion()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var packageA1 = PackageUtility.CreatePackage("A", "1.0", content: new string[] { "good" });
            var packageA2 = PackageUtility.CreatePackage("A", "2.0-alpha", content: new string[] { "excellent" });

            // project has A 2.0alpha installed
            projectManager.LocalRepository.AddPackage(packageA2);

            sourceRepository.AddPackage(packageA1);

            // Act
            projectManager.UpdatePackageReference("A", version: null, updateDependencies: false, allowPrereleaseVersions: false);

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A", new SemanticVersion("2.0-alpha")));
            Assert.False(projectManager.LocalRepository.Exists("A", new SemanticVersion("1.0")));
        }

        [Fact]
        public void UpdatePackageReferenceUpdateToNewerVersionIfPrereleaseFlagIsSet()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var packageA1 = PackageUtility.CreatePackage("A", "1.0", content: new string[] {"good"});
            var packageA2 = PackageUtility.CreatePackage("A", "2.0-alpha", content: new string[] {"excellent"});

            // project has A 1.0 installed
            projectManager.LocalRepository.AddPackage(packageA1);

            sourceRepository.AddPackage(packageA2);

            // Act
            projectManager.UpdatePackageReference("A", version: null, updateDependencies: false, allowPrereleaseVersions: true);

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A", new SemanticVersion("2.0-alpha")));
        }

        [Fact]
        public void UpdatePackageReferenceThrowsIfTheNewPackageHasMinClientVersionNotSatisfied()
        {
            // Arrange
            Version nugetVersion = typeof(IPackage).Assembly.GetName().Version;
            Version requiredVersion = new Version(nugetVersion.Major, nugetVersion.Minor, nugetVersion.Build, nugetVersion.Revision + 1);

            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var packageA1 = PackageUtility.CreatePackage("A", "1.0", content: new string[] { "good" });
            var packageA2 = PackageUtility.CreatePackage("A", "2.0-alpha", content: new string[] { "excellent" }, minClientVersion: requiredVersion.ToString());

            // project has A 1.0 installed
            projectManager.LocalRepository.AddPackage(packageA1);

            sourceRepository.AddPackage(packageA2);

            string expectedErrorMessage =
                String.Format("The '{0}' package requires NuGet client version '{1}' or above, but the current NuGet version is '{2}'.", "A 2.0-alpha", requiredVersion.ToString(), nugetVersion.ToString());

            // Act && Assert
            ExceptionAssert.Throws<NuGetVersionNotSatisfiedException>(
                () => projectManager.UpdatePackageReference("A", version: null, updateDependencies: false, allowPrereleaseVersions: true),
                expectedErrorMessage);
        }

        [Fact]
        public void UpdatePackageReferenceThrowsIfTheNewPackageHasDependencyMinClientVersionNotSatisfied()
        {
            // Arrange
            Version nugetVersion = typeof(IPackage).Assembly.GetName().Version;
            Version requiredVersion = new Version(nugetVersion.Major, nugetVersion.Minor, nugetVersion.Build, nugetVersion.Revision + 1);

            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var packageA1 = PackageUtility.CreatePackage("A", "1.0", content: new string[] { "good" });
            var packageA2 = PackageUtility.CreatePackage(
                "A", 
                "2.0-alpha", 
                content: new string[] { "excellent" }, 
                dependencies: new PackageDependency[] { new PackageDependency("B") });

            IPackage packageB = PackageUtility.CreatePackage("B", "2.0", assemblyReferences: new[] { "lib\\you.dll" }, minClientVersion: requiredVersion.ToString());

            // project has A 1.0 installed
            projectManager.LocalRepository.AddPackage(packageA1);

            sourceRepository.AddPackage(packageA2);
            sourceRepository.AddPackage(packageB);

            string expectedErrorMessage =
                String.Format("The '{0}' package requires NuGet client version '{1}' or above, but the current NuGet version is '{2}'.", "B 2.0", requiredVersion.ToString(), nugetVersion.ToString());

            // Act && Assert
            ExceptionAssert.Throws<NuGetVersionNotSatisfiedException>(
                () => projectManager.UpdatePackageReference("A", version: null, updateDependencies: true, allowPrereleaseVersions: true),
                expectedErrorMessage);
        }

        [Fact]
        public void UpdatePackageReferenceWithSatisfiableDependencies()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0, C 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]"),
                                                                        PackageDependency.CreateDependency("C", "[1.0]")
                                                                    }, content: new[] { "file" });

            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", new[] { "Bfile" });
            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0", new[] { "Cfile" });

            // G 1.0 -> [C (>= 1.0)]
            IPackage packageG10 = PackageUtility.CreatePackage("G", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", "1.0")
                                                                    }, content: new[] { "Gfile" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            projectManager.LocalRepository.AddPackage(packageG10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageG10);

            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]"),
                                                                        PackageDependency.CreateDependency("C", "[2.0]"),
                                                                        PackageDependency.CreateDependency("D", "[1.0]")
                                                                    }, content: new[] { "A20file" });

            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", new[] { "C20file" });
            IPackage packageD10 = PackageUtility.CreatePackage("D", "1.0", new[] { "D20file" });

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);
            sourceRepository.AddPackage(packageD10);

            // Act
            projectManager.UpdatePackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.True(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageC20));
            Assert.True(projectManager.LocalRepository.Exists(packageD10));
            Assert.True(projectManager.LocalRepository.Exists(packageG10));

            Assert.False(projectManager.LocalRepository.Exists(packageC10));
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
        }

        [Fact]
        public void UpdatePackageReferenceWithDependenciesInUseThrowsConflictError()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            // A 1.0 -> [B 1.0, C 1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]"),
                                                                        PackageDependency.CreateDependency("C", "[1.0]")
                                                                    }, content: new[] { "afile" });

            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "Bfile" });
            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0", content: new[] { "Cfile" });

            // G 1.0 -> [C 1.0]
            IPackage packageG10 = PackageUtility.CreatePackage("G", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", "[1.0]")
                                                                    }, content: new[] { "gfile" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            projectManager.LocalRepository.AddPackage(packageG10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageG10);

            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]"),
                                                                        PackageDependency.CreateDependency("C", "[2.0]"),
                                                                        PackageDependency.CreateDependency("D", "[1.0]")
                                                                    }, content: new[] { "a20file" });

            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", content: new[] { "cfile" });
            IPackage packageD10 = PackageUtility.CreatePackage("D", "1.0", content: new[] { "dfile" });

            // A 2.0 -> [B 1.0, C 2.0, D 1.0]
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);
            sourceRepository.AddPackage(packageD10);

            // Act 
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("A"), "Updating 'C 1.0' to 'C 2.0' failed. Unable to find a version of 'G' that is compatible with 'C 2.0'.");
        }

        [Fact]
        public void UpdatePackageReferenceFromRepositorySuccesfullyUpdatesDependentsIfDependentsAreResolvable()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "afile" });

            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0, 3.0]")
                                                                    }, content: new[] { "a2file" });

            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "bfile" });
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "b2file" });
            IPackage packageB30 = PackageUtility.CreatePackage("B", "3.0", content: new[] { "b3file" });
            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);
            sourceRepository.AddPackage(packageB30);

            // Act
            projectManager.UpdatePackageReference("B");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
            Assert.False(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.True(projectManager.LocalRepository.Exists(packageB30));
        }

        [Fact]
        public void UpdatePackageReferenceFromRepositoryFailsIfPackageHasUnresolvableDependents()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            // A -> B 1.0
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "afile" });
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "bfile" });
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "cfile" });
            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);

            // Act & Assert            
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("B"), "Updating 'B 1.0' to 'B 2.0' failed. Unable to find a version of 'A' that is compatible with 'B 2.0'.");
        }

        [Fact]
        public void UpdatePackageReferenceFromRepositoryFailsIfPackageHasAnyUnresolvableDependents()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            // A 1.0 -> B 1.0
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "afile" });

            // A 2.0 -> B [2.0]
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[2.0]")
                                                                    }, content: new[] { "afile" });

            // B 1.0
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "bfile" });
            // B 2.0
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0", content: new[] { "cfile" });
            // C 1.0 -> B [1.0]
            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "bfile" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);
            sourceRepository.AddPackage(packageC10);

            // Act & Assert            
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("B"), "Updating 'B 1.0' to 'B 2.0' failed. Unable to find a version of 'C' that is compatible with 'B 2.0'.");
        }

        [Fact]
        public void UpdatePackageReferenceFromRepositoryOverlappingDependencies()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            // A 1.0 -> B 1.0
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "afile" });

            // A 2.0 -> B [2.0]
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[2.0]")
                                                                    }, content: new[] { "afile" });

            // B 1.0
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b1file" });

            // B 2.0 -> C 2.0
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", "2.0")
                                                                    }, content: new[] { "afile" });

            // C 2.0
            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", content: new[] { "c2file" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);
            sourceRepository.AddPackage(packageC20);

            // Act
            projectManager.UpdatePackageReference("B");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA10));
            Assert.False(projectManager.LocalRepository.Exists(packageB10));
            Assert.True(projectManager.LocalRepository.Exists(packageA20));
            Assert.True(projectManager.LocalRepository.Exists(packageB20));
            Assert.True(projectManager.LocalRepository.Exists(packageC20));
        }
        
        [Fact]
        public void UpdatePackageReferenceFromRepositoryChainedIncompatibleDependents()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            // A 1.0 -> B [1.0]
            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0]")
                                                                    }, content: new[] { "afile" });
            // B 1.0 -> C [1.0]
            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", "[1.0]")
                                                                    }, content: new[] { "bfile" });
            // C 1.0
            IPackage packageC10 = PackageUtility.CreatePackage("C", "1.0", content: new[] { "c1file" });

            // A 2.0 -> B [1.0, 2.0)
            IPackage packageA20 = PackageUtility.CreatePackage("A", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("B", "[1.0, 2.0)")
                                                                    }, content: new[] { "afile" });

            // B 2.0 -> C [2.0]
            IPackage packageB20 = PackageUtility.CreatePackage("B", "2.0",
                                                                dependencies: new List<PackageDependency> { 
                                                                        PackageDependency.CreateDependency("C", "[2.0]")
                                                                    }, content: new[] { "cfile" });

            // C 2.0
            IPackage packageC20 = PackageUtility.CreatePackage("C", "2.0", content: new[] { "c2file" });

            projectManager.LocalRepository.AddPackage(packageA10);
            projectManager.LocalRepository.AddPackage(packageB10);
            projectManager.LocalRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA20);
            sourceRepository.AddPackage(packageB10);
            sourceRepository.AddPackage(packageB20);
            sourceRepository.AddPackage(packageC10);
            sourceRepository.AddPackage(packageC20);

            // Act & Assert            
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.UpdatePackageReference("C"), "Updating 'C 1.0' to 'C 2.0' failed. Unable to find a version of 'B' that is compatible with 'C 2.0'.");
        }

        [Fact]
        public void UpdatePackageReferenceNoVersionSpecifiedShouldUpdateToLatest()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            IPackage package10 = PackageUtility.CreatePackage("NetFramework", "1.0", content: new[] { "1.0f" });
            projectManager.LocalRepository.AddPackage(package10);
            sourceRepository.AddPackage(package10);

            IPackage package11 = PackageUtility.CreatePackage("NetFramework", "1.1", content: new[] { "1.1f" });
            sourceRepository.AddPackage(package11);

            IPackage package20 = PackageUtility.CreatePackage("NetFramework", "2.0", content: new[] { "2.0f" });
            sourceRepository.AddPackage(package20);

            IPackage package35 = PackageUtility.CreatePackage("NetFramework", "3.5", content: new[] { "3.5f" });
            sourceRepository.AddPackage(package35);

            // Act
            projectManager.UpdatePackageReference("NetFramework");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(package10));
            Assert.True(projectManager.LocalRepository.Exists(package35));
        }

        [Fact]
        public void UpdatePackageReferenceVersionSpeciedShouldUpdateToSpecifiedVersion()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
            var package10 = PackageUtility.CreatePackage("NetFramework", "1.0", new[] { "file.dll" });
            projectManager.LocalRepository.AddPackage(package10);
            sourceRepository.AddPackage(package10);

            var package11 = PackageUtility.CreatePackage("NetFramework", "1.1", new[] { "file.dll" });
            sourceRepository.AddPackage(package11);

            var package20 = PackageUtility.CreatePackage("NetFramework", "2.0", new[] { "file.dll" });
            sourceRepository.AddPackage(package20);

            // Act
            projectManager.UpdatePackageReference("NetFramework", new SemanticVersion("1.1"));

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(package10));
            Assert.True(projectManager.LocalRepository.Exists(package11));
        }

        [Fact]
        public void RemovingPackageReferenceRemovesPackageButNotDependencies()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("B")
                                                            }, content: new[] { "A" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "B" });

            projectManager.LocalRepository.AddPackage(packageA);
            projectManager.LocalRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.True(projectManager.LocalRepository.Exists(packageB));
        }

        [Fact]
        public void RemovingPackageRemoveAssembliesCorrectlyAccordingToReferences()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            projectSystem.AddReference("a.dll");
            projectSystem.AddReference("b.dll");
            projectSystem.AddReference("c.dll");

            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackage packageA = PackageUtility.CreatePackage(
                 "A",
                 "1.0",
                 assemblyReferences: new[] { "lib\\net35\\a.dll", "lib\\net35\\b.dll" });

            Mock<IPackage> mockPackageA = Mock.Get<IPackage>(packageA);
            mockPackageA.Setup(m => m.PackageAssemblyReferences).Returns(
                    new PackageReferenceSet[] { 
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("net40"), new [] { "a.dll" }),
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("wp8"), new [] { "b.dll" })
                    }
                );

            projectManager.LocalRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.False(projectSystem.ReferenceExists("a.dll"));
            Assert.True(projectSystem.ReferenceExists("b.dll"));
            Assert.True(projectSystem.ReferenceExists("c.dll"));
        }

        [Fact]
        public void RemovingPackageRemoveAssembliesCorrectlyAccordingToReferences2()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            projectSystem.AddReference("a.dll");
            projectSystem.AddReference("b.dll");
            projectSystem.AddReference("c.dll");

            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackage packageA = PackageUtility.CreatePackage(
                 "A",
                 "1.0",
                 assemblyReferences: new[] { "lib\\net35\\a.dll", "lib\\net35\\b.dll" });

            Mock<IPackage> mockPackageA = Mock.Get<IPackage>(packageA);
            mockPackageA.Setup(m => m.PackageAssemblyReferences).Returns(
                    new PackageReferenceSet[] { 
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("net50"), new [] { "a.dll" }),
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("wp8"), new [] { "b.dll" })
                    }
                );

            projectManager.LocalRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.False(projectSystem.ReferenceExists("a.dll"));
            Assert.False(projectSystem.ReferenceExists("b.dll"));
            Assert.True(projectSystem.ReferenceExists("c.dll"));
        }

        [Fact]
        public void RemovingPackageRemoveAssembliesCorrectlyAccordingToReferences3()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"));
            projectSystem.AddReference("a.dll");
            projectSystem.AddReference("b.dll");
            projectSystem.AddReference("c.dll");

            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackage packageA = PackageUtility.CreatePackage(
                 "A",
                 "1.0",
                 assemblyReferences: new[] { "lib\\net35\\a.dll", "lib\\net35\\b.dll" });

            Mock<IPackage> mockPackageA = Mock.Get<IPackage>(packageA);
            mockPackageA.Setup(m => m.PackageAssemblyReferences).Returns(
                    new PackageReferenceSet[] { 
                        new PackageReferenceSet(VersionUtility.ParseFrameworkName("net50"), new [] { "a.dll" }),
                        new PackageReferenceSet(null, new [] { "b.dll" })
                    }
                );

            projectManager.LocalRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.True(projectSystem.ReferenceExists("a.dll"));
            Assert.False(projectSystem.ReferenceExists("b.dll"));
            Assert.True(projectSystem.ReferenceExists("c.dll"));
        }

        [Fact]
        public void RemovePackageReferenceOnlyRemovedAssembliesFromTheTargetFramework()
        {
            // Arrange
            var net20 = new FrameworkName(".NETFramework", new Version("2.0"));
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(net20);
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackageAssemblyReference net20Reference = PackageUtility.CreateAssemblyReference("foo.dll", net20);
            IPackageAssemblyReference net40Reference = PackageUtility.CreateAssemblyReference("bar.dll", new FrameworkName(".NETFramework", new Version("4.0")));

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                content: null,
                assemblyReferences: new[] { net20Reference, net40Reference },
                tools: null,
                dependencies: null,
                downloadCount: 0,
                description: null,
                summary: null,
                listed: true,
                tags: null);

            projectManager.LocalRepository.AddPackage(packageA);

            sourceRepository.AddPackage(packageA);
            projectManager.AddPackageReference("A");

            // Act
            projectManager.RemovePackageReference("A");


            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.Equal(1, projectSystem.Deleted.Count);
            Assert.True(projectSystem.Deleted.Contains("foo.dll"));
        }

        [Fact]
        public void RemovingPackageRemoveImportFile()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"), "x:\\root");
            projectSystem.AddImport(@"x:\root\A.1.0\build\net35\A.props", ProjectImportLocation.Top);
            projectSystem.AddImport(@"x:\root\A.1.0\build\net35\A.targets", ProjectImportLocation.Bottom);
            
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0");

            var mockPackageA = Mock.Get(packageA);
            var files = PackageUtility.CreateFiles(new[] { "build\\net35\\A.targets", "build\\net35\\a.props" });
            mockPackageA.Setup(p => p.GetFiles()).Returns(files);

            projectManager.LocalRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.False(projectSystem.ImportExists(@"x:\root\A.1.0\content\net35\A.props"));
            Assert.False(projectSystem.ImportExists(@"x:\root\A.1.0\content\net35\A.targets"));
        }

        [Fact]
        public void RemovingPackageDoesNotRemoveImportFileIfFilePatternDoesNotMatch()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework, Version=4.5"), "x:\\root");
            projectSystem.AddImport(@"x:\root\A.1.0\content\net35\A.props", ProjectImportLocation.Top);
            projectSystem.AddImport(@"x:\root\A.1.0\content\net35\A.targets", ProjectImportLocation.Bottom);

            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackage packageA = PackageUtility.CreatePackage(
                 "A",
                 "1.0",
                 content: new[] { "net35\\A.1.0.props", "net35\\B.targets" });

            projectManager.LocalRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists(packageA));
            Assert.True(projectSystem.ImportExists(@"x:\root\A.1.0\content\net35\A.props"));
            Assert.True(projectSystem.ImportExists(@"x:\root\A.1.0\content\net35\A.targets"));
        }

        [Fact]
        public void ReAddingAPackageReferenceAfterRemovingADependencyShouldReReferenceAllDependencies()
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                dependencies: new List<PackageDependency> {
                    new PackageDependency("B")
                },
                content: new[] { "foo" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                new PackageDependency("C")
                                                            },
                                                            content: new[] { "bar" });

            var packageC = PackageUtility.CreatePackage("C", "1.0", content: new[] { "baz" });

            projectManager.LocalRepository.AddPackage(packageA);
            projectManager.LocalRepository.AddPackage(packageB);

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);

            // Act
            projectManager.AddPackageReference("A");

            // Assert            
            Assert.True(projectManager.LocalRepository.Exists(packageA));
            Assert.True(projectManager.LocalRepository.Exists(packageB));
            Assert.True(projectManager.LocalRepository.Exists(packageC));
        }

        [Fact]
        public void AddPackageReferenceWithAnyNonCompatibleReferenceThrowsAndPackageIsNotReferenced()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object, localRepository);
            mockProjectSystem.Setup(m => m.TargetFramework).Returns(new FrameworkName(".NETFramework", new Version("2.0")));
            var mockPackage = new Mock<IPackage>();
            mockPackage.Setup(m => m.Id).Returns("A");
            mockPackage.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            mockPackage.Setup(m => m.Listed).Returns(true);
            var assemblyReference = PackageUtility.CreateAssemblyReference("foo.dll", new FrameworkName(".NETFramework", new Version("5.0")));
            mockPackage.Setup(m => m.AssemblyReferences).Returns(new[] { assemblyReference });
            sourceRepository.AddPackage(mockPackage.Object);

            // Act & Assert            
            ExceptionAssert.Throws<InvalidOperationException>(
                () => projectManager.AddPackageReference("A"), 
                "Could not install package 'A 1.0'. You are trying to install this package into a project that targets '.NETFramework,Version=v2.0', but the package does not contain any assembly references or content files that are compatible with that framework. For more information, contact the package author.");
            Assert.False(localRepository.Exists(mockPackage.Object));
        }

        [Fact]
        public void AddPackageReferenceWithAnyNonCompatibleFrameworkReferenceDoesThrow()
        {
            // Arrange
            var mockProjectSystem = new Mock<MockProjectSystem>() { CallBase = true };
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(mockProjectSystem.Object), mockProjectSystem.Object, localRepository);
            mockProjectSystem.Setup(m => m.TargetFramework).Returns(VersionUtility.ParseFrameworkName("net20"));
            var mockPackage = new Mock<IPackage>();
            mockPackage.Setup(m => m.Id).Returns("A");
            mockPackage.Setup(m => m.Version).Returns(new SemanticVersion("1.0"));
            mockPackage.Setup(m => m.Listed).Returns(true);
            var frameworkReference = new FrameworkAssemblyReference("System.Web", new[] { VersionUtility.ParseFrameworkName("net50") });
            mockPackage.Setup(m => m.FrameworkAssemblies).Returns(new[] { frameworkReference });
            sourceRepository.AddPackage(mockPackage.Object);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(
                () => projectManager.AddPackageReference("A"));

            Assert.False(localRepository.Exists(mockPackage.Object));
        }

        [Fact]
        public void AddPackageReferenceImportsTargetOrPropFile()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName("Native", new Version("2.0")), "x:\\root");
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0");
            
            var mockPackageA = Mock.Get(packageA);
            var files = PackageUtility.CreateFiles(new[] { "build\\native\\A.targets", "build\\native\\a.props" });
            mockPackageA.Setup(p => p.GetFiles()).Returns(files);

            mockRepository.AddPackage(mockPackageA.Object);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(localRepository.Exists("A"));

            Assert.False(projectSystem.FileExists(@"a.targets"));
            Assert.False(projectSystem.FileExists(@"A.props"));

            Assert.True(projectSystem.ImportExists("x:\\root\\A.1.0\\build\\native\\A.props", ProjectImportLocation.Top));
            Assert.True(projectSystem.ImportExists("x:\\root\\A.1.0\\build\\native\\A.targets", ProjectImportLocation.Bottom));
        }

        [Fact]
        public void AddPackageReferenceAllowsAddingMetadataPackage()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            
            var packageA = PackageUtility.CreatePackage("A", "1.0", new [] { "hello.txt" });
            var packageB = PackageUtility.CreatePackage("B", "2.0", dependencies: new[] { new PackageDependency("A") });

            var mockPackageB = Mock.Get(packageB);
            var files = PackageUtility.CreateFiles(new[] { "readme.txt", "foo.bar" });
            mockPackageB.Setup(p => p.GetFiles()).Returns(files);
            
            mockRepository.AddPackage(packageA);
            mockRepository.AddPackage(packageB);

            // Act
            projectManager.AddPackageReference("B");

            // Assert
            Assert.True(localRepository.Exists("A"));
            Assert.True(localRepository.Exists("B"));
        }

        [Fact]
        public void AddPackageReferenceDoesNotAllowAddingDependencyPackageWhichHasToolsFiles()
        {
            // Arrange
            var projectSystem = new MockProjectSystem();
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);

            var packageA = PackageUtility.CreatePackage("A", "1.0", new[] { "hello.txt" });
            var packageB = PackageUtility.CreatePackage("B", "2.0", dependencies: new[] { new PackageDependency("A") }, tools: new [] { "aaaa.txt" });

            mockRepository.AddPackage(packageA);
            mockRepository.AddPackage(packageB);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(
                () => projectManager.AddPackageReference("B"),
                "External packages cannot depend on packages that target projects.");
        }

        [Fact]
        public void AddPackageReferenceImportsTargetOrPropFileAtContentRoot()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName("Native", new Version("2.0")), "x:\\root");
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0");

            var mockPackageA = Mock.Get(packageA);
            var files = PackageUtility.CreateFiles(new[] { "build\\A.targets", "build\\a.props", "content\\foo.css" });
            mockPackageA.Setup(p => p.GetFiles()).Returns(files);

            mockRepository.AddPackage(mockPackageA.Object);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(localRepository.Exists("A"));

            Assert.Equal(1, projectSystem.Paths.Count);
            Assert.True(projectSystem.FileExists(@"foo.css"));
            Assert.False(projectSystem.FileExists(@"a.targets"));
            Assert.False(projectSystem.FileExists(@"A.props"));

            Assert.True(projectSystem.ImportExists("x:\\root\\A.1.0\\build\\A.props", ProjectImportLocation.Top));
            Assert.True(projectSystem.ImportExists("x:\\root\\A.1.0\\build\\A.targets", ProjectImportLocation.Bottom));
        }

        [Fact]
        public void AddPackageReferenceDoesNotImportsTargetOrPropFileThatHaveNonMatchingName()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName("Native", new Version("2.0")), "x:\\root");
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0");

            var mockPackageA = Mock.Get(packageA);
            var files = PackageUtility.CreateFiles(new[] { "build\\native\\A.1.0.targets", "build\\native\\B.props", "content\\native\\foo.css" });
            mockPackageA.Setup(p => p.GetFiles()).Returns(files);

            mockRepository.AddPackage(mockPackageA.Object);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(localRepository.Exists("A"));
            Assert.False(projectSystem.ImportExists("x:\\root\\A.1.0\\build\\native\\A.1.0.targets"));
            Assert.False(projectSystem.ImportExists("x:\\root\\A.1.0\\build\\native\\b.props"));
        }

        [Fact]
        public void AddPackageReferenceDoesNotImportsTargetOrPropFileThatAreNotAtContentRoot()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName("Native", new Version("2.0")), "x:\\root");
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0");

            var mockPackageA = Mock.Get(packageA);
            var files = PackageUtility.CreateFiles(new[] { "build\\native\\foo\\A.targets", "build\\native\\bar\\B.props", "content\\native\\foo.css" });
            mockPackageA.Setup(p => p.GetFiles()).Returns(files);

            mockRepository.AddPackage(mockPackageA.Object);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(localRepository.Exists("A"));

            Assert.False(projectSystem.ImportExists("x:\\root\\A.1.0\\build\\native\\foo\\A.targets"));
            Assert.False(projectSystem.ImportExists("x:\\root\\A.1.0\\build\\native\\bar\\A.props"));
        }

        [Fact]
        public void AddPackageReferenceAddsContentAccordingToTargetFramework1()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("2.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "net20\\contentFile", "net35\\jQuery.js", "foo.css" },
                                                        new[] { "lib\\reference.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(1, projectSystem.Paths.Count);
            Assert.True(projectSystem.FileExists(@"contentFile"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceAddsContentAccordingToTargetFramework2()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("2.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "sl3\\contentFile", "winrt45\\jQuery.js", "sub\\foo.css" },
                                                        new[] { "lib\\reference.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(1, projectSystem.Paths.Count);
            Assert.True(projectSystem.FileExists(@"sub\foo.css"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceAddsContentAccordingToTargetFramework3()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("2.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "sl3\\contentFile", "winrt45\\jQuery.js" },
                                                        new[] { "lib\\reference.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(0, projectSystem.Paths.Count);
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceAddsContentAccordingToTargetFramework4()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("2.5")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "net20\\contentFile", "net35\\jQuery.js", "foo.css" },
                                                        new[] { "lib\\reference.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(1, projectSystem.Paths.Count);
            Assert.True(projectSystem.FileExists(@"contentFile"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceAddsTransformContentAccordingToTargetFramework()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "net20\\contentFile", "net35\\sub\\jQuery.js", "net35\\style.css.pp", "foo.css" },
                                                        new[] { "lib\\reference.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(2, projectSystem.Paths.Count);
            Assert.True(projectSystem.FileExists(@"sub\jQuery.js"));
            Assert.True(projectSystem.FileExists(@"style.css"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void RemovePackageReferenceRemoveContentAccordingToTargetFramework()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("2.0")));
            projectSystem.AddFile("jQuery.js", "content\\[net35]\\jQuery.js");
            projectSystem.AddFile("foo.css", "content\\foo.css");

            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "net20\\contentFile", "net35\\jQuery.js", "foo.css" },
                                                        new[] { "lib\\eference.dll" });
            mockRepository.AddPackage(packageA);
            projectManager.AddPackageReference("A");
            Assert.True(projectSystem.FileExists(@"contentFile"));

            // Act
            projectManager.RemovePackageReference("A");

            // Assert
            Assert.Equal(2, projectSystem.Paths.Count);
            Assert.True(projectSystem.FileExists(@"jQuery.js"));
            Assert.True(projectSystem.FileExists(@"foo.css"));
            Assert.False(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceThrowsIfThereIsNoCompatibleFrameworkFolderUnderContent()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        content: new[] { "silverlight\\reference.txt" });

            mockRepository.AddPackage(packageA);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.AddPackageReference("A"));

            // Assert
            Assert.False(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceThrowsIfThereIsNoCompatibleFrameworkFolderUnderContentAndLib()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        content: new[] { "silverlight\\reference.txt" },
                                                        assemblyReferences: new string[] { "lib\\windows8\\one.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            ExceptionAssert.Throws<InvalidOperationException>(() => projectManager.AddPackageReference("A"));

            // Assert
            Assert.False(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceDoesNotThrowIfThereIsCompatibleAssemblyInLibButNotInContent()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        content: new[] { "silverlight\\reference.txt" },
                                                        assemblyReferences: new string[] { "lib\\net20\\one.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(localRepository.Exists("A"));
            Assert.True(projectSystem.ReferenceExists("one.dll"));
            Assert.False(projectSystem.FileExists("reference.txt"));
        }

        [Fact]
        public void AddPackageReferenceDoesNotThrowIfThereIsCompatibleAssemblyInContentButNotInLib()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        content: new[] { "net20\\reference.txt" },
                                                        assemblyReferences: new string[] { "lib\\WindowsPhone7\\one.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(localRepository.Exists("A"));
            Assert.False(projectSystem.ReferenceExists("one.dll"));
            Assert.True(projectSystem.FileExists("reference.txt"));
        }

        [Fact]
        public void AddPackageReferenceAcceptsMetaPackage()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new [] { new PackageDependency("B") });
            var packageB = PackageUtility.CreatePackage("B", "1.0-alpha", new [] { "net\\hello.txt" });
            mockRepository.AddPackage(packageA);
            mockRepository.AddPackage(packageB);

            // Act
            projectManager.AddPackageReference("A", version: null, ignoreDependencies: false, allowPrereleaseVersions: true);

            // Assert
            Assert.True(localRepository.Exists("A"));
            Assert.True(localRepository.Exists("B"));
        }

        [Fact]
        public void AddPackageReferenceAddsAssemblyReferencesUsingNewFolderConvention()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "contentFiles" },
                                                        new[] { "lib\\net35\\reference.dll", "lib\\net45\\bar.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(1, projectSystem.References.Count);
            Assert.True(projectSystem.References.ContainsKey(@"reference.dll"));
            Assert.False(projectSystem.References.ContainsKey(@"bar.dll"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferencePicksAssemblyFromHigherVersionFolderOverFullNetProfile()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.5")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "contentFile.txt" },
                                                        new[] { "lib\\net40-client\\a4.dll", "lib\\net35-client\\a35.dll", "lib\\net20\\a20.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectSystem.ReferenceExists("a4.dll"));
            Assert.False(projectSystem.ReferenceExists("a35.dll"));
            Assert.False(projectSystem.ReferenceExists("a20.dll"));
        }

        [Fact]
        public void AddPackageReferencePicksAssemblyFromFullProfileOverClientProfile()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.5")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "contentFile.txt" },
                                                        new[] { "lib\\net45-client\\a.dll", "lib\\net45\\b.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.ReferenceExists("a.dll"));
            Assert.True(projectSystem.ReferenceExists("b.dll"));
        }

        [Fact]
        public void AddPackageReferencePicksAssemblyFromClientProfileOverFullProfile()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.5"), "Client"));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "contentFile.txt" },
                                                        new[] { "lib\\net45-client\\a.dll", "lib\\net45\\b.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectSystem.ReferenceExists("a.dll"));
            Assert.False(projectSystem.ReferenceExists("b.dll"));
        }

        [Fact]
        public void AddPackageReferenceRecognizeEmptyFrameworkFolder1()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("3.5")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "net20\\contentFile", "net35\\_._", "foo.css" },
                                                        new[] { "reference.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(0, projectSystem.Paths.Count);
            Assert.False(projectSystem.FileExists(@"_._"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceRecognizeEmptyFrameworkFolder2()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName("Silverlight", new Version("4.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        new[] { "sl20\\contentFile", "sl20\\sub\\no.txt", "sl3\\_._", "foo.css" },
                                                        new[] { "reference.dll" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.Equal(0, projectSystem.Paths.Count);
            Assert.False(projectSystem.FileExists(@"_._"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceRecognizeEmptyFrameworkFolder3()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName("Silverlight", new Version("4.0")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        assemblyReferences: new string[] { "lib\\sl3\\reference.dll", "lib\\sl35\\_._" });

            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.ReferenceExists("reference.dll"));
            Assert.False(projectSystem.ReferenceExists("_._"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceRecognizeEmptyFrameworkFolder4()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETCore", new Version("4.5")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        assemblyReferences: new string[] { 
                                                            "lib\\sl3\\reference.dll", 
                                                            "lib\\winrt\\_._"
                                                        });
            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.ReferenceExists("reference.dll"));
            Assert.False(projectSystem.ReferenceExists("_._"));
            Assert.True(localRepository.Exists("A"));
        }

        [Fact]
        public void AddPackageReferenceRecognizeEmptyFrameworkFolder5()
        {
            // Arrange
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETCore", new Version("4.5")));
            var localRepository = new MockPackageRepository();
            var mockRepository = new MockPackageRepository();
            var projectManager = new ProjectManager(mockRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, localRepository);
            var packageA = PackageUtility.CreatePackage("A", "1.0",
                                                        assemblyReferences: new string[] { 
                                                            "lib\\sl3\\reference.dll", 
                                                            "lib\\winrt\\_._",
                                                            "lib\\winrt45\\one.dll"
                                                        });
            mockRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.False(projectSystem.ReferenceExists("reference.dll"));
            Assert.False(projectSystem.ReferenceExists("_._"));
            Assert.True(projectSystem.ReferenceExists("one.dll"));
            Assert.True(localRepository.Exists("A"));
        }

        [Theory]
        [InlineData("1.0")]
        [InlineData("4.0")]
        public void AddPackageReferenceIncludeDependencyPackageCorrectly(string dependencyVersion)
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var dependency = new PackageDependency("B", null);
            var dependecySets = CreateDependencySet(".NETFramework, Version=" + dependencyVersion, dependency);

            IPackage packageA = PackageUtility.CreatePackageWithDependencySets("A", "1.0",
                                                                dependencySets: new [] { dependecySets }, 
                                                                content: new[] { "a.txt" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b.txt" });

            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");
            
            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.True(projectManager.LocalRepository.Exists("B"));
            Assert.True(projectSystem.FileExists("a.txt"));
            Assert.True(projectSystem.FileExists("b.txt"));
        }

        [Fact]
        public void AddPackageReferenceIncludeDependencyPackageCorrectly2()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("2.0")));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var dependencyB = new PackageDependency("B", null);
            var dependencySetB = CreateDependencySet(".NETFramework, Version=2.1", dependencyB);

            var dependencyC = new PackageDependency("C", null);
            var dependencySetC = CreateDependencySet(".NETFramework, Version=2.0", dependencyC);
            IPackage packageA = PackageUtility.CreatePackageWithDependencySets("A", "1.0",
                                                                dependencySets: new List<PackageDependencySet> { dependencySetB, dependencySetC },
                                                                content: new[] { "a.txt" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b.txt" });

            IPackage packageC = PackageUtility.CreatePackage("C", "2.0", content: new[] { "c.txt" });

            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.False(projectManager.LocalRepository.Exists("B"));
            Assert.True(projectManager.LocalRepository.Exists("C"));

            Assert.True(projectSystem.FileExists("a.txt"));
            Assert.False(projectSystem.FileExists("b.txt"));
            Assert.True(projectSystem.FileExists("c.txt"));
        }

        [Fact]
        public void AddPackageReferenceIncludeDependencyPackageCorrectly3()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("2.0")));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var dependencyB = new PackageDependency("B", null);
            var dependencySetB = CreateDependencySet(".NETFramework, Version=2.1", dependencyB);

            var dependencyC = new PackageDependency("C", null);
            var dependencySetC = CreateDependencySet(".NETFramework, Version=2.0", dependencyC);
            IPackage packageA = PackageUtility.CreatePackageWithDependencySets("A", "1.0",
                                                                dependencySets: new List<PackageDependencySet> { dependencySetB, dependencySetC },
                                                                content: new[] { "a.txt" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b.txt" });

            IPackage packageC = PackageUtility.CreatePackage("C", "2.0", content: new[] { "sl4\\c.txt", "net\\d.txt" });

            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.False(projectManager.LocalRepository.Exists("B"));
            Assert.True(projectManager.LocalRepository.Exists("C"));

            Assert.True(projectSystem.FileExists("a.txt"));
            Assert.False(projectSystem.FileExists("b.txt"));
            Assert.False(projectSystem.FileExists("c.txt"));
            Assert.True(projectSystem.FileExists("d.txt"));
        }

        [Theory]
        [InlineData("3.0")]
        [InlineData("4.0")]
        public void AddPackageReferenceDoNotIncludeDependencyPackageIfTargetFrameworkDoesNotMatch(string dependencyVersion)
        {
            // Arrange
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("2.0")));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var dependency = new PackageDependency("B", null);
            var dependencySet = new PackageDependencySet(
                new FrameworkName(".NETFramework", new Version(dependencyVersion)), 
                new PackageDependency[] {dependency});

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0", content: new[] { "a.txt" });
            Mock.Get(packageA).Setup(p => p.DependencySets).Returns(new [] {dependencySet});

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b.txt" });

            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.False(projectManager.LocalRepository.Exists("B"));
            Assert.True(projectSystem.FileExists("a.txt"));
            Assert.False(projectSystem.FileExists("b.txt"));
        }

        [Theory]
        [InlineData("1.0")]
        [InlineData("4.0")]
        public void RemovePackageReferenceRemoveDependencyPackageCorrectly(string dependencyVersion)
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var dependency = new PackageDependency("B", null);
            var dependencySet = new PackageDependencySet(
                new FrameworkName(".NETFramework", new Version(dependencyVersion)),
                new PackageDependency[] { dependency });
            IPackage packageA = PackageUtility.CreatePackageWithDependencySets("A", "1.0",
                                                                dependencySets: new List<PackageDependencySet> { dependencySet },
                                                                content: new[] { "a.txt" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b.txt" });

            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);

            projectManager.AddPackageReference("A");

            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.True(projectManager.LocalRepository.Exists("B"));
            Assert.True(projectSystem.FileExists("a.txt"));
            Assert.True(projectSystem.FileExists("b.txt"));

            // Act
            projectManager.RemovePackageReference("A", forceRemove: false, removeDependencies: true);

            Assert.False(projectManager.LocalRepository.Exists("A"));
            Assert.False(projectManager.LocalRepository.Exists("B"));
            Assert.False(projectSystem.FileExists("a.txt"));
            Assert.False(projectSystem.FileExists("b.txt"));
        }

        [Fact]
        public void RemovePackageReferenceRemoveDependencyPackageCorrectly2()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("2.0")));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var dependencyB = new PackageDependency("B", null);
            var dependencySetB = CreateDependencySet(".NETFramework, Version=2.1", dependencyB);

            var dependencyC = new PackageDependency("C", null);
            var dependencySetC = CreateDependencySet(".NETFramework, Version=2.0", dependencyC);

            IPackage packageA = PackageUtility.CreatePackageWithDependencySets("A", "1.0",
                                                                dependencySets: new List<PackageDependencySet> { dependencySetB, dependencySetC },
                                                                content: new[] { "a.txt" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b.txt" });

            IPackage packageC = PackageUtility.CreatePackage("C", "2.0", content: new[] { "c.txt" });

            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageC);

            projectManager.AddPackageReference("A");

            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.False(projectManager.LocalRepository.Exists("B"));
            Assert.True(projectManager.LocalRepository.Exists("C"));

            Assert.True(projectSystem.FileExists("a.txt"));
            Assert.False(projectSystem.FileExists("b.txt"));
            Assert.True(projectSystem.FileExists("c.txt"));

            // Act
            projectManager.RemovePackageReference("A", forceRemove: false, removeDependencies: true);

            Assert.False(projectManager.LocalRepository.Exists("A"));
            Assert.False(projectManager.LocalRepository.Exists("B"));
            Assert.False(projectManager.LocalRepository.Exists("C"));

            Assert.False(projectSystem.FileExists("a.txt"));
            Assert.False(projectSystem.FileExists("b.txt"));
            Assert.False(projectSystem.FileExists("c.txt"));
        }

        [Fact]
        public void UpdatePackageReferenceIncludeDependencyPackageCorrectly()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var dependency = new PackageDependency("B", null);
            var dependencySet = CreateDependencySet(".NETFramework, Version=4.5", dependency);
            IPackage packageA = PackageUtility.CreatePackageWithDependencySets("A", "1.0",
                                                             dependencySets: new List<PackageDependencySet> { dependencySet },
                                                             content: new[] { "a.txt" });

            var dependency2 = new PackageDependency("B", null);
            var dependencySet2 = CreateDependencySet(".NETFramework, Version=4.0", dependency2);
            IPackage packageA2 = PackageUtility.CreatePackageWithDependencySets("A", "2.0",
                                                                dependencySets: new List<PackageDependencySet> { dependencySet2 },
                                                                content: new[] { "a2.txt" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b.txt" });

            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageA2);

            projectManager.AddPackageReference("A", new SemanticVersion("1.0"));
            
            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.False(projectManager.LocalRepository.Exists("B"));

            // Act
            projectManager.UpdatePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists("A", new SemanticVersion("1.0")));
            Assert.True(projectManager.LocalRepository.Exists("A", new SemanticVersion("2.0")));
            Assert.True(projectManager.LocalRepository.Exists("B"));
            Assert.True(projectSystem.FileExists("a2.txt"));
            Assert.True(projectSystem.FileExists("b.txt"));
        }

        [Fact]
        public void UpdatePackageReferenceIncludeDependencyPackageCorrectly2()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));
            var projectManager = new ProjectManager(sourceRepository, new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());

            var dependency = new PackageDependency("B", null);
            var dependencySet = CreateDependencySet(".NETFramework, Version=4.0", dependency);
            IPackage packageA = PackageUtility.CreatePackageWithDependencySets("A", "1.0",
                                                             dependencySets: new List<PackageDependencySet> { dependencySet },
                                                             content: new[] { "a.txt" });

            var dependency2 = new PackageDependency("B", null);
            var dependencySet2 = CreateDependencySet(".NETFramework, Version=4.5", dependency2);
            IPackage packageA2 = PackageUtility.CreatePackageWithDependencySets("A", "2.0",
                                                                dependencySets: new List<PackageDependencySet> { dependencySet2 },
                                                                content: new[] { "a2.txt" });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0", content: new[] { "b.txt" });

            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageA2);

            projectManager.AddPackageReference("A", new SemanticVersion("1.0"));

            Assert.True(projectManager.LocalRepository.Exists("A"));
            Assert.True(projectManager.LocalRepository.Exists("B"));

            // Act
            projectManager.UpdatePackageReference("A");

            // Assert
            Assert.False(projectManager.LocalRepository.Exists("A", new SemanticVersion("1.0")));
            Assert.True(projectManager.LocalRepository.Exists("A", new SemanticVersion("2.0")));
            Assert.False(projectManager.LocalRepository.Exists("B"));
            Assert.True(projectSystem.FileExists("a2.txt"));
            Assert.False(projectSystem.FileExists("b.txt"));
        }

        [Fact]
        public void AddPackageReferencePersistTargetFarmeworkToPackagesConfigFile()
        {
            // Arrange            
            var sourceRepository = new MockPackageRepository();
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));

            var localRepository = new Mock<IPackageReferenceRepository>();
            localRepository.Setup(p => p.AddPackage("A", 
                                                    new SemanticVersion("1.0"),
                                                    new FrameworkName(".NETFramework, Version=4.0"))).Verifiable();

            localRepository.Setup(p => p.AddPackage("B",
                                                    new SemanticVersion("2.0"),
                                                    new FrameworkName(".NETFramework, Version=4.0"))).Verifiable();

            var projectManager = new ProjectManager(
                sourceRepository, 
                new DefaultPackagePathResolver(projectSystem), 
                projectSystem, 
                localRepository.Object);

            var dependency = new PackageDependency("B", null);
            var dependecySets = CreateDependencySet(".NETFramework, Version=3.0", dependency);

            IPackage packageA = PackageUtility.CreatePackageWithDependencySets("A", "1.0",
                                                                dependencySets: new[] { dependecySets },
                                                                content: new[] { "a.txt" });

            IPackage packageB = PackageUtility.CreatePackage("B", "2.0", content: new[] { "b.txt" });

            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageA);

            // Act
            projectManager.AddPackageReference("A");

            // Assert
            localRepository.VerifyAll();
        }

        [Fact]
        public void SafeUpdatingADependencyDoesNotUninstallPackage()
        {
            // Arrange
            
            var projectSystem = new MockProjectSystem(new FrameworkName(".NETFramework", new Version("4.0")));

            var packageA = PackageUtility.CreatePackage("A", "1.0", dependencies: new [] { new PackageDependency("C"), new PackageDependency("B") });
            var packageB10 = PackageUtility.CreatePackage("B", "1.0", dependencies: new[] { new PackageDependency("C") });
            var packageB101 = PackageUtility.CreatePackage("B", "1.0.1", dependencies: new[] { new PackageDependency("C") });
            var packageC = PackageUtility.CreatePackage("C", "1.0", content: new[] { "1.txt" });
            
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository { packageA, packageB10, packageB101, packageC };

            var projectManager = new ProjectManager(
                sourceRepository,
                new DefaultPackagePathResolver(projectSystem),
                projectSystem,
                localRepository);

            // Act 1 
            projectManager.AddPackageReference(packageB10, ignoreDependencies: false, allowPrereleaseVersions: false);

            // Assert 1
            Assert.Contains(packageB10, localRepository);
            Assert.Contains(packageC, localRepository);

            // Act 2
            projectManager.AddPackageReference("A");

            // Assert 2
            Assert.Contains(packageA, localRepository);
            Assert.Contains(packageB10, localRepository);
            Assert.Contains(packageC, localRepository);
        }

        private ProjectManager CreateProjectManager()
        {
            var projectSystem = new MockProjectSystem();
            return new ProjectManager(new MockPackageRepository(), new DefaultPackagePathResolver(projectSystem), projectSystem, new MockPackageRepository());
        }

        private static PackageDependencySet CreateDependencySet(
            string targetFramework, params PackageDependency[] dependencies)
        {
            return new PackageDependencySet(new FrameworkName(targetFramework), dependencies);
        }
    }
}