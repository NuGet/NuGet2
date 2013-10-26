using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionManager;
using Moq;
using NuGet.Test.Mocks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class VsPackageInstallerTest
    {
        [Fact]
        public void InstallPackageConvertsVersionToSemanticVersion()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                fileSystem, 
                localRepository, 
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) 
                { 
                    CallBase = true 
                };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);
            packageRepositoryFactory.Setup(r => r.CreateRepository(@"x:\test")).Returns(new MockPackageRepository()).Verifiable();

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, packageRepositoryFactory.Object, new Mock<IOutputConsoleProvider>().Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, null);

            // Act
            installer.InstallPackage(@"x:\test", project, "foo", new Version("1.0"), ignoreDependencies: false);

            // Assert
            scriptExecutor.Verify(e => e.Execute(It.IsAny<string>(), PowerShellScripts.Init, It.IsAny<IPackage>(), null, null, It.IsAny<ILogger>()), Times.Once());
            scriptExecutor.Verify(e => e.Execute(It.IsAny<string>(), PowerShellScripts.Install, It.IsAny<IPackage>(), It.IsAny<Project>(), It.IsAny<FrameworkName>(), It.IsAny<ILogger>()), Times.Once());
            packageRepositoryFactory.Verify();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void InstallPackageTreatNullSourceAsAggregateSource1(string source)
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                fileSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null)
            {
                CallBase = true
            };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);
            packageRepositoryFactory.Setup(r => r.CreateRepository(@"(Aggregate source)")).Returns(new MockPackageRepository()).Verifiable();

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, packageRepositoryFactory.Object, new Mock<IOutputConsoleProvider>().Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, null);

            // Act
            installer.InstallPackage(source, project, "foo", new Version("1.0"), ignoreDependencies: false);

            // Assert
            Assert.True(packageManager.Object.LocalRepository.Exists("foo", new SemanticVersion("1.0")));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void InstallPackageTreatNullSourceAsAggregateSource2(string source)
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                fileSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null)
            {
                CallBase = true
            };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);
            packageRepositoryFactory.Setup(r => r.CreateRepository(@"(Aggregate source)")).Returns(new MockPackageRepository()).Verifiable();

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, packageRepositoryFactory.Object, new Mock<IOutputConsoleProvider>().Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, null);

            // Act
            installer.InstallPackage(source, project, "foo", "1.0", ignoreDependencies: false);

            // Assert
            Assert.True(packageManager.Object.LocalRepository.Exists("foo", new SemanticVersion("1.0")));
        }

        [Fact]
        public void InstallPackageThrowsIfRepositoryIsNull()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                fileSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null)
            {
                CallBase = true
            };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.Is<IPackageRepository>(p => p != null), false))
                                 .Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);
            
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, packageRepositoryFactory.Object, new Mock<IOutputConsoleProvider>().Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, null);

            // Act && Assert
            Assert.Throws<ArgumentNullException>( () => 
                installer.InstallPackage(/* repository */ null, project, "foo", "1.0", ignoreDependencies: false, skipAssemblyReferences: true)
            );
        }

        [Fact]
        public void InstallPackageRunsInitAndInstallScripts()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                fileSystem, 
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) 
                { 
                    CallBase = true 
                };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, new Mock<IPackageRepositoryFactory>().Object, new Mock<IOutputConsoleProvider>().Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, null);

            // Act
            installer.InstallPackage(sourceRepository, project, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, skipAssemblyReferences: false);

            // Assert
            scriptExecutor.Verify(e => e.Execute(It.IsAny<string>(), PowerShellScripts.Init, It.IsAny<IPackage>(), null, null, It.IsAny<ILogger>()), Times.Once());
            scriptExecutor.Verify(e => e.Execute(It.IsAny<string>(), PowerShellScripts.Install, It.IsAny<IPackage>(), It.IsAny<Project>(), It.IsAny<FrameworkName>(), It.IsAny<ILogger>()), Times.Once());
        }

        [Fact]
        public void InstallPackageTurnOffBindingRedirectIfSkipAssemblyReferences()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                fileSystem, 
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) 
                { 
                    CallBase = true 
                };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManager.As<IVsPackageManager>().SetupGet(m => m.BindingRedirectEnabled).Returns(true);

            int state = 1;
            packageManager.As<IVsPackageManager>().SetupSet(m => m.BindingRedirectEnabled = false).Callback(() => state += 1);
            packageManager.As<IVsPackageManager>().SetupSet(m => m.BindingRedirectEnabled = true).Callback(() => state *= 2);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(
                packageManagerFactory.Object, 
                scriptExecutor.Object, 
                new Mock<IPackageRepositoryFactory>().Object, 
                new Mock<IOutputConsoleProvider>().Object, 
                new Mock<IVsCommonOperations>().Object, 
                new Mock<ISolutionManager>().Object, 
                null, 
                null);

            // Act
            installer.InstallPackage(sourceRepository, project, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, skipAssemblyReferences: true);

            // Assert
            // state = 4 means that BindingRedirectEnabled is set to 'false', then to 'true', in that order.
            // no other value of 4 can result in the same value of 4.
            Assert.Equal(4, state);
            packageManager.As<IVsPackageManager>().VerifySet(m => m.BindingRedirectEnabled = false, Times.Once());
            packageManager.As<IVsPackageManager>().VerifySet(m => m.BindingRedirectEnabled = true, Times.Once());
        }

        [Fact]
        public void InstallPackageDoesNotTurnOffBindingRedirectIfNotSkipAssemblyReferences()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                fileSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null)
            {
                CallBase = true
            };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManager.As<IVsPackageManager>().SetupGet(m => m.BindingRedirectEnabled).Returns(true);

            packageManager.As<IVsPackageManager>().SetupSet(m => m.BindingRedirectEnabled = false);
            packageManager.As<IVsPackageManager>().SetupSet(m => m.BindingRedirectEnabled = true);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(
                packageManagerFactory.Object,
                scriptExecutor.Object,
                new Mock<IPackageRepositoryFactory>().Object,
                new Mock<IOutputConsoleProvider>().Object,
                new Mock<IVsCommonOperations>().Object,
                new Mock<ISolutionManager>().Object,
                null,
                null);

            // Act
            installer.InstallPackage(sourceRepository, project, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, skipAssemblyReferences: false);

            // Assert
            packageManager.As<IVsPackageManager>().VerifySet(m => m.BindingRedirectEnabled = false, Times.Never());
            packageManager.As<IVsPackageManager>().VerifySet(m => m.BindingRedirectEnabled = true, Times.Exactly(2));
        }

        [Fact]
        public void InstallPackageDoesNotUseFallbackRepository()
        {
            // Arrange
            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var scriptExecutor = new Mock<IScriptExecutor>();
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                fileSystem, 
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) 
                { 
                    CallBase = true 
                };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManagerFactory.Setup(m => m.CreatePackageManager()).Throws(new Exception("A"));
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), true)).Throws(new Exception("B"));
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);

            var package = NuGet.Test.PackageUtility.CreatePackage("foo", "1.0", new[] { "hello" }, tools: new[] { "init.ps1", "install.ps1" });
            sourceRepository.AddPackage(package);
            var installer = new VsPackageInstaller(packageManagerFactory.Object, scriptExecutor.Object, new Mock<IPackageRepositoryFactory>().Object, new Mock<IOutputConsoleProvider>().Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, null);

            // Act
            installer.InstallPackage(sourceRepository, project, "foo", new SemanticVersion("1.0"), ignoreDependencies: false, skipAssemblyReferences: false);

            // Assert
            packageManagerFactory.Verify(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false), Times.Once());
            packageManagerFactory.Verify(m => m.CreatePackageManager(), Times.Never());
            packageManagerFactory.Verify(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), true), Times.Never());
        }

        [Fact]
        public void InstallPackagesFromRegistryRepositoryThrowsRegistryKeyError()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "PreinstalledPackages";
            var hkcu_repository = new Mock<IRegistryKey>();
            var hkcu = new Mock<IRegistryKey>();
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns((IRegistryKey)null);
            var project = TestUtils.GetProject("Foo");

            var installer = new VsPackageInstaller(null, null, null, null, null, null, null, null, new[] { hkcu.Object });
            var packages = new Dictionary<string, string>();
            packages.Add("A", "1.0.0");

            // Act & Assert            
            var exception = Assert.Throws<InvalidOperationException>(() => installer.InstallPackagesFromRegistryRepository(registryKey, isPreUnzipped: false, skipAssemblyReferences: false, project: project, packageVersions: packages));
            Assert.Equal(string.Format(NuGet.VisualStudio.Resources.VsResources.PreinstalledPackages_RegistryKeyError, registryPath), exception.Message);
        }

        [Fact]
        public void InstallPackagesFromRegistryRepositoryThrowsRegistryValueError()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "PreinstalledPackages";
            var registryValue = String.Empty;
            var hkcu_repository = new Mock<IRegistryKey>();
            var hkcu = new Mock<IRegistryKey>();
            hkcu_repository.Setup(r => r.GetValue(registryKey)).Returns(registryValue);
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);
            var project = TestUtils.GetProject("Foo");            

            var installer = new VsPackageInstaller(null, null, null, null, null, null, null, null, new[] { hkcu.Object });
            var packages = new Dictionary<string, string>();
            packages.Add("A", "1.0.0");

            // Act & Assert            
            var exception = Assert.Throws<InvalidOperationException>(() => installer.InstallPackagesFromRegistryRepository(registryKey, isPreUnzipped: false, skipAssemblyReferences: false, project: project, packageVersions: packages));
            Assert.Equal(string.Format(NuGet.VisualStudio.Resources.VsResources.PreinstalledPackages_InvalidRegistryValue, registryKey, registryPath), exception.Message);
        }

        [Fact]
        public void InstallPackagesFromRegistryRepositoryThrowsWhenPackageIsMissing()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "PreinstalledPackages";
            var registryValue = @"C:\PreinstalledPackages";
            var hkcu_repository = new Mock<IRegistryKey>();
            var hkcu = new Mock<IRegistryKey>();
            hkcu_repository.Setup(k => k.GetValue(registryKey)).Returns(registryValue);
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);

            var services = new Mock<IVsPackageInstallerServices>();
            services.Setup(x => x.IsPackageInstalled(It.IsAny<Project>(), It.IsAny<string>())).Returns(false);

            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                fileSystem, 
                localRepository, 
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) { CallBase = true };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);
            packageRepositoryFactory.Setup(r => r.CreateRepository(@"x:\test")).Returns(new MockPackageRepository()).Verifiable();

            var installer = new VsPackageInstaller(packageManagerFactory.Object, null, null, null, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, services.Object, registryKeys: new[] { hkcu.Object });
            var packages = new Dictionary<string, string>();
            packages.Add("A", "1.0.0");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => installer.InstallPackagesFromRegistryRepository(registryKey, isPreUnzipped: false, skipAssemblyReferences: false, project: project, packageVersions: packages));
            Assert.True(exception.Message.Contains("A.1.0.0 : "));
        }

        [Fact]
        public void InstallPackagesFromRegistryRepositoryRaisesWarningsIfDifferentVersionInstalled()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "PreinstalledPackages";
            var registryValue = @"C:\PreinstalledPackages";
            var hkcu_repository = new Mock<IRegistryKey>();
            var hkcu = new Mock<IRegistryKey>();
            hkcu_repository.Setup(k => k.GetValue(registryKey)).Returns(registryValue);
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);

            var consoleOutput = new List<string>();
            var console = new Mock<NuGetConsole.IConsole>();
            console.Setup(c => c.WriteLine(It.IsAny<string>())).Callback<string>(consoleOutput.Add);

            var consoleProvider = new Mock<IOutputConsoleProvider>();
            consoleProvider.Setup(c => c.CreateOutputConsole(It.IsAny<bool>())).Returns(console.Object);

            var packageId = "A";
            var packageVersion = "1.0.0";

            var services = new Mock<IVsPackageInstallerServices>();
            services.Setup(x => x.IsPackageInstalled(It.IsAny<Project>(), packageId)).Returns(true);
            services.Setup(x => x.IsPackageInstalled(It.IsAny<Project>(), packageId, It.IsAny<SemanticVersion>())).Returns(false);

            var installer = new VsPackageInstaller(null, null, null, consoleProvider.Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, services.Object, registryKeys: new[] { hkcu.Object });
            var packages = new Dictionary<string, string>();
            packages.Add(packageId, packageVersion);

            var project = TestUtils.GetProject("Foo");

            // Act
            installer.InstallPackagesFromRegistryRepository(registryKey, isPreUnzipped: false, skipAssemblyReferences: false, project: project, packageVersions: packages);

            // Assert
            Assert.Single(consoleOutput);
            Assert.True(consoleOutput.Single().Contains(string.Format(NuGet.VisualStudio.Resources.VsResources.PreinstalledPackages_VersionConflict, packageId, packageVersion)));
        }

        [Fact]
        public void InstallPackagesFromRegistryRepositoryInstallsPackages()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "PreinstalledPackages";
            var registryValue = @"C:\PreinstalledPackages";
            var hkcu_repository = new Mock<IRegistryKey>();
            var hkcu = new Mock<IRegistryKey>();
            hkcu_repository.Setup(k => k.GetValue(registryKey)).Returns(registryValue);
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);

            var consoleOutput = new List<string>();
            var console = new Mock<NuGetConsole.IConsole>();
            console.Setup(c => c.WriteLine(It.IsAny<string>())).Callback<string>(consoleOutput.Add);

            var consoleProvider = new Mock<IOutputConsoleProvider>();
            consoleProvider.Setup(c => c.CreateOutputConsole(It.IsAny<bool>())).Returns(console.Object);

            var packageId = "A";
            var packageVersion = "1.0.0";

            var services = new Mock<IVsPackageInstallerServices>();
            services.Setup(x => x.IsPackageInstalled(It.IsAny<Project>(), packageId)).Returns(false);

            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                fileSystem, 
                localRepository, 
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) { CallBase = true };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);
            packageRepositoryFactory.Setup(r => r.CreateRepository(@"x:\test")).Returns(new MockPackageRepository()).Verifiable();

            var package = NuGet.Test.PackageUtility.CreatePackage(packageId, packageVersion, new[] { "System" });
            sourceRepository.AddPackage(package);

            var installer = new VsPackageInstaller(packageManagerFactory.Object, null, null, consoleProvider.Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, services.Object, registryKeys: new[] { hkcu.Object });
            var packages = new Dictionary<string, string>();
            packages.Add(packageId, packageVersion);

            // Act
            Assert.False(localRepository.Exists(packageId, new SemanticVersion(packageVersion)));
            installer.InstallPackagesFromRegistryRepository(registryKey, isPreUnzipped: false, skipAssemblyReferences: false, project: project, packageVersions: packages);

            // Assert
            Assert.True(localRepository.Exists(packageId, new SemanticVersion(packageVersion)));
        }

        [Fact]
        public void InstallPackagesFromRegistryRepositoryInstallsDependenciesIfIgnoreDependenciesIsFalse()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "PreinstalledPackages";
            var registryValue = @"C:\PreinstalledPackages";
            var hkcu_repository = new Mock<IRegistryKey>();
            var hkcu = new Mock<IRegistryKey>();
            hkcu_repository.Setup(k => k.GetValue(registryKey)).Returns(registryValue);
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);

            var consoleOutput = new List<string>();
            var console = new Mock<NuGetConsole.IConsole>();
            console.Setup(c => c.WriteLine(It.IsAny<string>())).Callback<string>(consoleOutput.Add);

            var consoleProvider = new Mock<IOutputConsoleProvider>();
            consoleProvider.Setup(c => c.CreateOutputConsole(It.IsAny<bool>())).Returns(console.Object);

            var packageId = "A";
            var packageVersion = "1.0.0";

            var dependencyPackageId = "B";
            var dependencyPackageVersion = "2.0.0";

            var services = new Mock<IVsPackageInstallerServices>();
            services.Setup(x => x.IsPackageInstalled(It.IsAny<Project>(), packageId)).Returns(false);

            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(),
                sourceRepository,
                new Mock<IFileSystemProvider>().Object,
                fileSystem,
                localRepository,
                new Mock<IDeleteOnRestartManager>().Object,
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) { CallBase = true };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);
            packageRepositoryFactory.Setup(r => r.CreateRepository(@"x:\test")).Returns(new MockPackageRepository()).Verifiable();

            var package = NuGet.Test.PackageUtility.CreatePackage(
                packageId,
                packageVersion,
                new[] { "System" }, null, null,
                new[] { new PackageDependency(dependencyPackageId, new VersionSpec(new SemanticVersion(dependencyPackageVersion))) });

            var dependencyPackage = NuGet.Test.PackageUtility.CreatePackage(
                dependencyPackageId,
                dependencyPackageVersion,
                new[] { "System.IO" });

            sourceRepository.AddPackage(package);
            sourceRepository.AddPackage(dependencyPackage);

            var installer = new VsPackageInstaller(packageManagerFactory.Object, null, null, consoleProvider.Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, services.Object, registryKeys: new[] { hkcu.Object });
            var packages = new Dictionary<string, string>();
            packages.Add(packageId, packageVersion);

            // Act
            Assert.False(localRepository.Exists(packageId, new SemanticVersion(packageVersion)));
            installer.InstallPackagesFromRegistryRepository(registryKey, isPreUnzipped: false, skipAssemblyReferences: false, ignoreDependencies: false, project: project, packageVersions: packages);

            // Assert
            Assert.True(localRepository.Exists(packageId, new SemanticVersion(packageVersion)));
            Assert.True(localRepository.Exists(dependencyPackageId, new SemanticVersion(dependencyPackageVersion)));
        }

        [Fact]
        public void InstallPackagesFromVSExtensionRepositoryThrowsExtensionError()
        {
            // Arrange
            var extensionId = "myExtensionId";
            var project = TestUtils.GetProject("Foo");

            var extensionManagerMock = new Mock<IVsExtensionManager>();
            IInstalledExtension extension = null;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension(extensionId, out extension)).Returns(false);

            var installer = new VsPackageInstaller(null, null, null, null, null, null, null, null, extensionManagerMock.Object);
            var packages = new Dictionary<string, string>();
            packages.Add("A", "1.0.0");

            // Act & Assert            
            var exception = Assert.Throws<InvalidOperationException>(() => installer.InstallPackagesFromVSExtensionRepository(extensionId, isPreUnzipped: false, skipAssemblyReferences: false, project: project, packageVersions: packages));
            Assert.Equal(string.Format(NuGet.VisualStudio.Resources.VsResources.PreinstalledPackages_InvalidExtensionId, extensionId), exception.Message);
        }

        [Fact]
        public void InstallPackagesFromVSExtensionRepositoryThrowsWhenPackageIsMissing()
        {
            // Arrange
            var extensionId = "myExtensionId";

            var extensionManagerMock = new Mock<IVsExtensionManager>();
            var extensionMock = new Mock<IInstalledExtension>();
            extensionMock.Setup(e => e.InstallPath).Returns(@"C:\Extension\Dir");
            var extension = extensionMock.Object;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension(extensionId, out extension)).Returns(true);

            var services = new Mock<IVsPackageInstallerServices>();
            services.Setup(x => x.IsPackageInstalled(It.IsAny<Project>(), It.IsAny<string>())).Returns(false);

            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                fileSystem, 
                localRepository, 
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) { CallBase = true };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);
            packageRepositoryFactory.Setup(r => r.CreateRepository(@"x:\test")).Returns(new MockPackageRepository()).Verifiable();

            var installer = new VsPackageInstaller(packageManagerFactory.Object, null, null, null, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, services.Object, extensionManagerMock.Object);
            var packages = new Dictionary<string, string>();
            packages.Add("A", "1.0.0");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => installer.InstallPackagesFromVSExtensionRepository(extensionId, isPreUnzipped: false, skipAssemblyReferences: false, project: project, packageVersions: packages));
            Assert.True(exception.Message.Contains("A.1.0.0 : "));
        }

        [Fact]
        public void InstallPackagesFromVSExtensionRepositoryRaisesWarningsIfDifferentVersionInstalled()
        {
            // Arrange
            var extensionId = "myExtensionId";

            var extensionManagerMock = new Mock<IVsExtensionManager>();
            var extensionMock = new Mock<IInstalledExtension>();
            extensionMock.Setup(e => e.InstallPath).Returns(@"C:\Extension\Dir");
            var extension = extensionMock.Object;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension(extensionId, out extension)).Returns(true);

            var consoleOutput = new List<string>();
            var console = new Mock<NuGetConsole.IConsole>();
            console.Setup(c => c.WriteLine(It.IsAny<string>())).Callback<string>(consoleOutput.Add);

            var consoleProvider = new Mock<IOutputConsoleProvider>();
            consoleProvider.Setup(c => c.CreateOutputConsole(It.IsAny<bool>())).Returns(console.Object);

            var packageId = "A";
            var packageVersion = "1.0.0";

            var services = new Mock<IVsPackageInstallerServices>();
            services.Setup(x => x.IsPackageInstalled(It.IsAny<Project>(), packageId)).Returns(true);
            services.Setup(x => x.IsPackageInstalled(It.IsAny<Project>(), packageId, It.IsAny<SemanticVersion>())).Returns(false);

            var installer = new VsPackageInstaller(null, null, null, consoleProvider.Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, services.Object, extensionManagerMock.Object);
            var packages = new Dictionary<string, string>();
            packages.Add(packageId, packageVersion);

            var project = TestUtils.GetProject("Foo");

            // Act
            installer.InstallPackagesFromVSExtensionRepository(extensionId, isPreUnzipped: false, skipAssemblyReferences: false, project: project, packageVersions: packages);

            // Assert
            Assert.Single(consoleOutput);
            Assert.True(consoleOutput.Single().Contains(string.Format(NuGet.VisualStudio.Resources.VsResources.PreinstalledPackages_VersionConflict, packageId, packageVersion)));
        }

        [Fact]
        public void InstallPackagesFromVSExtensionRepositoryInstallsPackages()
        {
            // Arrange
            var extensionId = "myExtensionId";

            var extensionManagerMock = new Mock<IVsExtensionManager>();
            var extensionMock = new Mock<IInstalledExtension>();
            extensionMock.Setup(e => e.InstallPath).Returns(@"C:\Extension\Dir");
            var extension = extensionMock.Object;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension(extensionId, out extension)).Returns(true);

            var consoleOutput = new List<string>();
            var console = new Mock<NuGetConsole.IConsole>();
            console.Setup(c => c.WriteLine(It.IsAny<string>())).Callback<string>(consoleOutput.Add);

            var consoleProvider = new Mock<IOutputConsoleProvider>();
            consoleProvider.Setup(c => c.CreateOutputConsole(It.IsAny<bool>())).Returns(console.Object);

            var packageId = "A";
            var packageVersion = "1.0.0";

            var services = new Mock<IVsPackageInstallerServices>();
            services.Setup(x => x.IsPackageInstalled(It.IsAny<Project>(), packageId)).Returns(false);

            var localRepository = new Mock<MockPackageRepository>() { CallBase = true }.As<ISharedPackageRepository>().Object;
            var sourceRepository = new MockPackageRepository();
            var projectRepository = new MockProjectPackageRepository(localRepository);
            var fileSystem = new MockFileSystem();
            var projectSystem = new MockProjectSystem();
            var pathResolver = new DefaultPackagePathResolver(new MockProjectSystem());
            var project = TestUtils.GetProject("Foo");
            var projectManager = new ProjectManager(localRepository, pathResolver, projectSystem, projectRepository);
            var packageManager = new Mock<VsPackageManager>(
                TestUtils.GetSolutionManager(), 
                sourceRepository, 
                new Mock<IFileSystemProvider>().Object, 
                fileSystem, 
                localRepository, 
                new Mock<IDeleteOnRestartManager>().Object, 
                new Mock<VsPackageInstallerEvents>().Object,
                /* multiFrameworkTargeting */ null) { CallBase = true };
            var packageManagerFactory = new Mock<IVsPackageManagerFactory>();
            var packageRepositoryFactory = new Mock<IPackageRepositoryFactory>(MockBehavior.Strict);
            packageManagerFactory.Setup(m => m.CreatePackageManager(It.IsAny<IPackageRepository>(), false)).Returns(packageManager.Object);
            packageManager.Setup(m => m.GetProjectManager(project)).Returns(projectManager);
            packageRepositoryFactory.Setup(r => r.CreateRepository(@"x:\test")).Returns(new MockPackageRepository()).Verifiable();

            var package = NuGet.Test.PackageUtility.CreatePackage(packageId, packageVersion, new[] { "System" });
            sourceRepository.AddPackage(package);

            var installer = new VsPackageInstaller(packageManagerFactory.Object, null, null, consoleProvider.Object, new Mock<IVsCommonOperations>().Object, new Mock<ISolutionManager>().Object, null, services.Object, extensionManagerMock.Object);
            var packages = new Dictionary<string, string>();
            packages.Add(packageId, packageVersion);

            // Act
            Assert.False(localRepository.Exists(packageId, new SemanticVersion(packageVersion)));
            installer.InstallPackagesFromVSExtensionRepository(extensionId, isPreUnzipped: false, skipAssemblyReferences: false, project: project, packageVersions: packages);

            // Assert
            Assert.True(localRepository.Exists(packageId, new SemanticVersion(packageVersion)));
        }
    }
}