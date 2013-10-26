using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.TemplateWizard;
using Moq;
using NuGet.Test;
using NuGet.VisualStudio.Resources;
using NuGetConsole;
using Xunit;
using Xunit.Extensions;

namespace NuGet.VisualStudio.Test
{
    public class VsTemplateWizardTest
    {
        private static readonly XNamespace VSTemplateNamespace = "http://schemas.microsoft.com/developer/vstemplate/2005";

        private class VsTemplatePackagesNode
        {
            public VsTemplatePackagesNode(string repository, XObject[] repositoryChildren)
            {
                Repository = repository;
                ChildNodes = repositoryChildren;
            }

            public string Repository { get; private set; }
            public XObject[] ChildNodes { get; private set; }
        }

        private static XDocument BuildDocument(string repository = "template", params XObject[] packagesChildren)
        {
            return BuildDocument(new[] { new VsTemplatePackagesNode(repository, packagesChildren) });
        }

        private static XDocument BuildDocument(IEnumerable<VsTemplatePackagesNode> repositoriesWithPackages)
        {
            var elements = new List<XObject>();

            if (repositoriesWithPackages != null && repositoriesWithPackages.Any())
            {
                foreach (var repository in repositoriesWithPackages)
                {
                    var children = new List<XObject>();
                    if (repository.Repository != null)
                    {
                        children.Add(new XAttribute("repository", repository.Repository));
                    }

                    children.AddRange(repository.ChildNodes);

                    elements.AddRange(new[] { new XElement(VSTemplateNamespace + "packages", children) });
                }
            }

            return new XDocument(new XElement("VSTemplate",
                new XElement(VSTemplateNamespace + "WizardData", elements)));
        }

        private static XDocument BuildDocumentWithPackage(string repository, XObject additionalChild = null)
        {
            return BuildDocument(repository, BuildPackageElement("pack", "1.0"), additionalChild);
        }

        private static XElement BuildPackageElement(string id = null, string version = null, bool skipAssemblyReferences = false, bool includeDependencies = false)
        {
            var packageElement = new XElement(VSTemplateNamespace + "package");
            if (id != null)
            {
                packageElement.Add(new XAttribute("id", id));
            }
            if (version != null)
            {
                packageElement.Add(new XAttribute("version", version));
            }
            if (skipAssemblyReferences)
            {
                packageElement.Add(new XAttribute("skipAssemblyReferences", skipAssemblyReferences.ToString()));
            }
            if (includeDependencies)
            {
                packageElement.Add(new XAttribute("includeDependencies", includeDependencies.ToString()));
            }
            return packageElement;
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithMissingWizardDataElement()
        {
            // Arrange
            var document = new XDocument(new XElement(VSTemplateNamespace + "VSTemplate"));
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Empty(results);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void GetConfigurationRecognizeIsPreunzippedAttribute(string preunzippedValue, bool expectedResult)
        {
            // Arrange
            var document = BuildDocument();
            document.Element("VSTemplate")
                    .Element(VSTemplateNamespace + "WizardData")
                    .Element(VSTemplateNamespace + "packages")
                    .Add(new XAttribute("isPreunzipped", preunzippedValue));

            var wizard = new VsTemplateWizard(null, null, null, null, null, null);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"c:\some\file.vstemplate");

            // Assert
            Assert.Equal(1, results.Count());
            Assert.Equal(expectedResult, results.First().IsPreunzipped);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithMissingPackagesElement()
        {
            // Arrange
            var document = new XDocument(
                new XElement(VSTemplateNamespace + "VSTemplate",
                    new XElement(VSTemplateNamespace + "WizardData")
                    ));
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithEmptyPackagesElement()
        {
            // Arrange
            var document = BuildDocument((string)null);
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(1, results.Count());
            Assert.Empty(results.Single().Packages);
            Assert.Equal(null, results.Single().RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithTemplateRepository()
        {
            // Arrange
            var document = BuildDocumentWithPackage("template");
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(1, results.Count());
            Assert.Equal(1, results.Single().Packages.Count);
            Assert.Equal(@"C:\Some", results.Single().RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithExtensionRepository()
        {
            // Arrange
            var document = BuildDocumentWithPackage("extension", new XAttribute("repositoryId", "myExtensionId"));
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);
            var extensionManagerMock = new Mock<IVsExtensionManager>();
            var extensionMock = new Mock<IInstalledExtension>();
            extensionMock.Setup(e => e.InstallPath).Returns(@"C:\Extension\Dir");
            var extension = extensionMock.Object;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension("myExtensionId", out extension)).Returns(true);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate", vsExtensionManager: extensionManagerMock.Object);

            // Assert
            Assert.Equal(1, results.Count());
            Assert.Equal(1, results.Single().Packages.Count);
            Assert.Equal(@"C:\Extension\Dir\Packages", results.Single().RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithMultipleRepositories()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "AspNetMvc4";
            var registryValue = @"C:\AspNetMvc4\Packages";

            var extensionRepository = new VsTemplatePackagesNode("extension", new XObject[] { new XAttribute("repositoryId", "myExtensionId"), BuildPackageElement("packageFromExtension", "1.0") });
            var registryRepository = new VsTemplatePackagesNode("registry", new XObject[] { new XAttribute("keyName", registryKey), BuildPackageElement("packageFromRegistry", "2.0") });

            var document = BuildDocument(new[] { extensionRepository, registryRepository });
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);
            var extensionManagerMock = new Mock<IVsExtensionManager>();
            var extensionMock = new Mock<IInstalledExtension>();
            extensionMock.Setup(e => e.InstallPath).Returns(@"C:\Extension\Dir");
            var extension = extensionMock.Object;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension("myExtensionId", out extension)).Returns(true);

            var hkcu_repository = new Mock<IRegistryKey>();
            var hkcu = new Mock<IRegistryKey>();
            hkcu_repository.Setup(k => k.GetValue(registryKey)).Returns(registryValue);
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate", vsExtensionManager: extensionManagerMock.Object, registryKeys: new[] { hkcu.Object });

            // Assert
            Assert.Equal(2, results.Count());
            var extensionResult = results.First();
            var registryResult = results.Last();

            Assert.Equal(1, extensionResult.Packages.Count);
            Assert.Equal(@"C:\Extension\Dir\Packages", extensionResult.RepositoryPath);
            Assert.Equal("packageFromExtension", extensionResult.Packages.Single().Id);
            Assert.Equal(new SemanticVersion("1.0"), extensionResult.Packages.Single().Version);

            Assert.Equal(1, registryResult.Packages.Count);
            Assert.Equal(registryValue, registryResult.RepositoryPath);
            Assert.Equal("packageFromRegistry", registryResult.Packages.Single().Id);
            Assert.Equal(new SemanticVersion("2.0"), registryResult.Packages.Single().Version);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_IsPreunzippedDefaultsToFalseFromMultipleRepositories()
        {
            // Arrange
            var extensionRepository = new VsTemplatePackagesNode("registry", new XObject[] { new XAttribute("isPreunzipped", "true") });
            var registryRepository = new VsTemplatePackagesNode("extension", new XObject[0]);

            var document = BuildDocument(new[] { extensionRepository, registryRepository });
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);
            var extensionManagerMock = new Mock<IVsExtensionManager>();
            var extensionMock = new Mock<IInstalledExtension>();
            var extension = extensionMock.Object;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension("myExtensionId", out extension)).Returns(true);

            var hkcu = new Mock<IRegistryKey>();

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate", vsExtensionManager: extensionManagerMock.Object, registryKeys: new[] { hkcu.Object });

            // Assert
            Assert.Equal(2, results.Count());
            var registryResult = results.First();
            var extensionResult = results.Last();

            Assert.False(extensionResult.IsPreunzipped);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_IsPreunzippedCanBeSetOnMultipleRepositories()
        {
            // Arrange
            var firstRepository = new VsTemplatePackagesNode("", new XObject[] { new XAttribute("isPreunzipped", "true") });
            var secondRepository = new VsTemplatePackagesNode("", new XObject[] { new XAttribute("isPreunzipped", "false") });
            var thirdRepository = new VsTemplatePackagesNode("", new XObject[] { new XAttribute("isPreunzipped", "true") });


            var document = BuildDocument(new[] { firstRepository, secondRepository, thirdRepository });
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);
            var extensionManagerMock = new Mock<IVsExtensionManager>();
            var extensionMock = new Mock<IInstalledExtension>();
            var extension = extensionMock.Object;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension("myExtensionId", out extension)).Returns(true);

            var hkcu = new Mock<IRegistryKey>();

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate", vsExtensionManager: extensionManagerMock.Object, registryKeys: new[] { hkcu.Object });

            // Assert
            Assert.Equal(3, results.Count());
            Assert.True(results.First().IsPreunzipped);
            Assert.False(results.Skip(1).First().IsPreunzipped);
            Assert.True(results.Last().IsPreunzipped);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ShowErrorForMissingRepositoryIdAttributeWhenInExtensionRepositoryMode()
        {
            // Arrange
            var document = BuildDocumentWithPackage("extension");
            var wizard = new TestableVsTemplateWizard();

            // Act
            // Use .ToList() to force enumeration of the yielded results
            ExceptionAssert.Throws<WizardBackoutException>(() =>
                                                           wizard.GetConfigurationsFromXmlDocument(document,
                                                               @"C:\Some\file.vstemplate").ToList());

            // Assert
            Assert.Equal(                
                VsResources.TemplateWizard_MissingExtensionId,
                wizard.ErrorMessages.Single());
        }


        [Fact]
        public void GetConfigurationFromXmlDocument_ShowErrorForInvalidRepositoryIdAttributeWhenInExtensionRepositoryMode()
        {
            // Arrange
            var document = BuildDocumentWithPackage("extension", new XAttribute("repositoryId", "myExtensionId"));
            var wizard = new TestableVsTemplateWizard();
            var extensionManagerMock = new Mock<IVsExtensionManager>();
            IInstalledExtension extension = null;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension("myExtensionId", out extension)).Returns(false);

            // Act
            // Use .ToList() to force enumeration of the yielded results
            ExceptionAssert.Throws<WizardBackoutException>(() => wizard.GetConfigurationsFromXmlDocument(document,
                @"C:\Some\file.vstemplate",
                vsExtensionManager: extensionManagerMock.Object).ToList());

            // Assert
            Assert.Equal(
                String.Format(VsResources.PreinstalledPackages_InvalidExtensionId, "myExtensionId"),
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithRegistryRepository()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "AspNetMvc4";
            var registryValue = @"C:\AspNetMvc4\Packages";

            var document = BuildDocumentWithPackage("registry", new XAttribute("keyName", registryKey));
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);

            var hkcu_repository = new Mock<IRegistryKey>();
            var hkcu = new Mock<IRegistryKey>();
            hkcu_repository.Setup(k => k.GetValue(registryKey)).Returns(registryValue);
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate", registryKeys: new[] { hkcu.Object });

            // Assert
            Assert.Equal(1, results.Count());
            Assert.Equal(registryValue, results.Single().RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_FallsBackWhenHKCURegistryKeyDoesNotExist()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "AspNetMvc4";
            var registryValue = @"C:\AspNetMvc4\Packages";

            var document = BuildDocumentWithPackage("registry", new XAttribute("keyName", registryKey));
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);

            // HKCU key doesn't exist
            var hkcu = new Mock<IRegistryKey>();
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns<string>(null);

            // HKLM key is configured
            var hklm_repository = new Mock<IRegistryKey>();
            var hklm = new Mock<IRegistryKey>();
            hklm_repository.Setup(k => k.GetValue(registryKey)).Returns(registryValue);
            hklm.Setup(r => r.OpenSubKey(registryPath)).Returns(hklm_repository.Object);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate", registryKeys: new[] { hkcu.Object, hklm.Object });

            // Assert
            Assert.Equal(1, results.Count());
            Assert.Equal(registryValue, results.Single().RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_FallsBackWhenHKCURegistryValueDoesNotExist()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "AspNetMvc4";
            var registryValue = @"C:\AspNetMvc4\Packages";

            var document = BuildDocumentWithPackage("registry", new XAttribute("keyName", registryKey));
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);

            // HKCU key exists, but the value does not
            var hkcu_repository = new Mock<IRegistryKey>();
            hkcu_repository.Setup(r => r.GetValue(registryKey)).Returns<string>(null);

            var hkcu = new Mock<IRegistryKey>();
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);

            // HKLM key is configured
            var hklm_repository = new Mock<IRegistryKey>();
            var hklm = new Mock<IRegistryKey>();
            hklm_repository.Setup(k => k.GetValue(registryKey)).Returns(registryValue);
            hklm.Setup(r => r.OpenSubKey(registryPath)).Returns(hklm_repository.Object);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate", registryKeys: new[] { hkcu.Object, hklm.Object });

            // Assert
            Assert.Equal(1, results.Count());
            Assert.Equal(registryValue, results.Single().RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_FallsBackWhenHKCURegistryValueIsEmpty()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKeyName = "AspNetMvc4";
            var registryValue = @"C:\AspNetMvc4\Packages";

            var document = BuildDocumentWithPackage("registry", new XAttribute("keyName", registryKeyName));
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);

            // HKCU key exists, but the value does not
            var hkcu_repository = new Mock<IRegistryKey>();
            hkcu_repository.Setup(r => r.GetValue(registryKeyName)).Returns(String.Empty);

            var hkcu = new Mock<IRegistryKey>();
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);

            // HKLM key is configured
            var hklm_repository = new Mock<IRegistryKey>();
            var hklm = new Mock<IRegistryKey>();
            hklm_repository.Setup(k => k.GetValue(registryKeyName)).Returns(registryValue);
            hklm.Setup(r => r.OpenSubKey(registryPath)).Returns(hklm_repository.Object);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate", registryKeys: new[] { hkcu.Object, hklm.Object });

            // Assert
            Assert.Equal(1, results.Count());
            Assert.Equal(registryValue, results.Single().RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ShowErrorForMissingKeyNameAttributeWhenInRegistryRepositoryMode()
        {
            // Arrange
            var document = BuildDocumentWithPackage("registry");
            var wizard = new TestableVsTemplateWizard();

            // Act
            // Use .ToList() to force enumeration of the yielded results
            ExceptionAssert.Throws<WizardBackoutException>(() =>
                                                           wizard.GetConfigurationsFromXmlDocument(document,
                                                               @"C:\Some\file.vstemplate", registryKeys: Enumerable.Empty<IRegistryKey>()).ToList());

            // Assert
            Assert.Equal(
                VsResources.TemplateWizard_MissingRegistryKeyName,
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ShowErrorForMissingRegistryKeyWhenInRegistryRepositoryMode()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKeyName = @"ThisRegistryKeyDoesNotExist";
            var document = BuildDocumentWithPackage("registry", new XAttribute("keyName", registryKeyName));
            var wizard = new TestableVsTemplateWizard();

            var hkcu = new Mock<IRegistryKey>();
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns<IRegistryKey>(null);

            var registryKey = new Mock<IRegistryKey>();
            registryKey.Setup(r => r.OpenSubKey(registryPath)).Returns<IRegistryKey>(null);

            // Act
            // Use .ToList() to force enumeration of the yielded results
            ExceptionAssert.Throws<WizardBackoutException>(() =>
                                                           wizard.GetConfigurationsFromXmlDocument(document,
                                                               @"C:\Some\file.vstemplate", registryKeys: new[] { hkcu.Object }).ToList());

            // Assert
            Assert.Equal(
                String.Format(VsResources.PreinstalledPackages_RegistryKeyError, registryPath),
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ShowErrorForMissingRegistryValueWhenInRegistryRepositoryMode()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "ThisRegistryKeyDoesNotExist";
            var document = BuildDocumentWithPackage("registry", new XAttribute("keyName", registryKey));
            var wizard = new TestableVsTemplateWizard();

            var hkcu_repository = new Mock<IRegistryKey>();
            var hkcu = new Mock<IRegistryKey>();
            hkcu_repository.Setup(k => k.GetValue(registryKey)).Returns(null);
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);

            // Act
            // Use .ToList() to force enumeration of the yielded results
            ExceptionAssert.Throws<WizardBackoutException>(() =>
                                                           wizard.GetConfigurationsFromXmlDocument(document,
                                                               registryPath, registryKeys: new[] { hkcu.Object }).ToList());

            // Assert
            Assert.Equal(
                String.Format(VsResources.PreinstalledPackages_InvalidRegistryValue, registryKey, registryPath),
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ShowsErrorForInvalidCacheAttributeValue()
        {
            // Arrange
            var document = BuildDocumentWithPackage("__invalid__");
            var wizard = new TestableVsTemplateWizard();

            // Act
            // Use .ToList() to force enumeration of the yielded results
            ExceptionAssert.Throws<WizardBackoutException>(() => wizard.GetConfigurationsFromXmlDocument(document,
                @"C:\Some\file.vstemplate").ToList());

            // Assert
            Assert.Equal(
                "The \"repository\" attribute of the package element has an invalid value: '__invalid__'. Valid values are: 'template' or 'extension'.",
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_IncludesValidPackageElements()
        {
            var content = new[] {
                BuildPackageElement("MyPackage", "1.0"),
                BuildPackageElement("MyOtherPackage", "2.0")
            };
            var expectedPackages = new[] {
                new PreinstalledPackageInfo("MyPackage", "1.0"),
                new PreinstalledPackageInfo("MyOtherPackage", "2.0")
            };
            var document = BuildDocument("template", content);

            VerifyParsedPackages(document, expectedPackages);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithDocumentWithNoNamespace()
        {
            var expectedPackages = new[] {
                new PreinstalledPackageInfo("MyPackage", "1.0"),
            };
            var document =
                new XDocument(new XElement("VSTemplate",
                    new XElement("WizardData",
                        new XElement("packages",
                            new XElement("package", new XAttribute("id", "MyPackage"), new XAttribute("version", "1.0"))))));

            VerifyParsedPackages(document, expectedPackages);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithSemanticVersions()
        {
            var expectedPackages = new[] {
                new PreinstalledPackageInfo("MyPackage", "4.0.0-ctp-2"),
            };
            var document =
                new XDocument(new XElement("VSTemplate",
                    new XElement("WizardData",
                        new XElement("packages",
                            new XElement("package", new XAttribute("id", "MyPackage"), new XAttribute("version", "4.0.0-ctp-2"))))));

            VerifyParsedPackages(document, expectedPackages);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void DetermineSolutionDirectory_UsesDestinationDirectoryIfSpecifiedSolutionNameNullOrEmpty(string value)
        {
            // Arrange
            Dictionary<string, string> replacementsDictionary = new Dictionary<string, string>()
            {
                { "$specifiedsolutionname$", value },
                { "$destinationdirectory$", "DestDir" },
                { "$solutiondirectory$", "SlnDir" }
            };

            // Act
            string actual = VsTemplateWizard.DetermineSolutionDirectory(replacementsDictionary);

            // Assert
            Assert.Equal("DestDir", actual);
        }

        [Fact]
        public void DetermineSolutionDirectory_UsesSolutionDirectoryIfSpecifiedSolutionNameNonEmpty()
        {
            // Arrange
            Dictionary<string, string> replacementsDictionary = new Dictionary<string, string>()
            {
                { "$specifiedsolutionname$", "SlnName" },
                { "$destinationdirectory$", "DestDir" },
                { "$solutiondirectory$", "SlnDir" }
            };

            // Act
            string actual = VsTemplateWizard.DetermineSolutionDirectory(replacementsDictionary);

            // Assert
            Assert.Equal("SlnDir", actual);
        }

        [Fact]
        public void DetermineSolutionDirectory_UsesDestinationDirectoryIfSolutionDirectoryNotSpecified()
        {
            // Arrange
            Dictionary<string, string> replacementsDictionary = new Dictionary<string, string>()
            {
                { "$specifiedsolutionname$", "SlnName" },
                { "$destinationdirectory$", "DestDir" }
            };

            // Act
            string actual = VsTemplateWizard.DetermineSolutionDirectory(replacementsDictionary);

            // Assert
            Assert.Equal("DestDir", actual);
        }

        [Theory]
        [InlineData("http://localhost", "http://localhost", @"packages\")]
        [InlineData("http://localhost/website", "http://localhost", @"..\packages\")]
        [InlineData("http://localhost:2302/", @"x:\\dir\me\", @"x:\\dir\me\packages\")]
        [InlineData("http://localhost:2302/website/", @"x:\\dir\me\", @"x:\\dir\me\packages\")]
        public void AddNuGetPackageFolderTemplateParameterIsCorrectWhenProjectWebsiteIsHttpBased(string directoryPath, string solutionPath, string expectedPath)
        {
            // Arrange
            var template = new TestableVsTemplateWizard();
            var parameters = new Dictionary<string, string>();

            parameters["$destinationdirectory$"] = directoryPath;
            parameters["$solutiondirectory$"] = solutionPath;

            // Act
            ((IVsTemplateWizard)template).RunStarted(new Mock<DTE>().Object, parameters, WizardRunKind.AsNewProject, new object[0]);

            string nugetFolder;
            bool result = parameters.TryGetValue("$nugetpackagesfolder$", out nugetFolder);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedPath, nugetFolder);
        }

        private static void VerifyParsedPackages(XDocument document, IEnumerable<PreinstalledPackageInfo> expectedPackages)
        {
            // Arrange
            var wizard = new VsTemplateWizard(null, null, null, null, null, null);

            // Act
            var results = wizard.GetConfigurationsFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(1, results.Count());
            Assert.Equal(expectedPackages.Count(), results.Single().Packages.Count);
            foreach (var pair in expectedPackages.Zip(results.Single().Packages,
                (expectedPackage, resultPackage) => new { expectedPackage, resultPackage }))
            {
                Assert.Equal(pair.expectedPackage.Id, pair.resultPackage.Id);
                Assert.Equal(pair.expectedPackage.Version, pair.resultPackage.Version);
            }
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithMissingIdAttribute()
        {
            var content = new[] {
                BuildPackageElement(version: "1.0"),
            };
            InvalidPackageElementHelper(content);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithEmptyIdAttribute()
        {
            var content = new[] {
                BuildPackageElement("  ", "1.0"),
            };
            InvalidPackageElementHelper(content);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithMissingVersionAttribute()
        {
            var content = new[] {
                BuildPackageElement(id: "MyPackage")
            };
            InvalidPackageElementHelper(content);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithEmptyVersionAttribute()
        {
            var content = new[] {
                BuildPackageElement("MyPackage", "  ")
            };
            InvalidPackageElementHelper(content);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithInvalidVersionAttribute()
        {
            var content = new[] {
                BuildPackageElement("MyPackage", "NotAVersionString")
            };
            InvalidPackageElementHelper(content);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithInvalidSkipAssemblyReferencesAttribute()
        {
            var packageElement = BuildPackageElement("MyPackage", "1.0.0");
            packageElement.Add(new XAttribute("skipAssemblyReferences", "sure"));
            
            InvalidPackageElementHelper(new[] { packageElement });
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ErrorOnPackageElementWithInvalidIncludeDependenciesAttribute()
        {
            var packageElement = BuildPackageElement("MyPackage", "1.0.0");
            packageElement.Add(new XAttribute("includeDependencies", "yeah"));

            InvalidPackageElementHelper(new[] { packageElement });
        }

        private static void InvalidPackageElementHelper(XElement[] content)
        {
            // Arrange
            var document = BuildDocument("template", content);
            var wizard = new TestableVsTemplateWizard();

            // Act
            // Use .ToList() to force enumeration of the yielded results
            ExceptionAssert.Throws<WizardBackoutException>(() => wizard.GetConfigurationsFromXmlDocument(document,
                @"C:\Some\file.vstemplate").ToList());

            // Assert
            Assert.Equal(
                VsResources.TemplateWizard_InvalidPackageElementAttributes,
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void RunStarted_MultiProjectRun_DisplaysErrorMessageAndBacksOut()
        {
            RunStartedForInvalidTemplateTypeHelper(WizardRunKind.AsMultiProject);
        }

        private static void RunStartedForInvalidTemplateTypeHelper(WizardRunKind runKind)
        {
            // Arrange
            var wizard = new TestableVsTemplateWizard();

            // Act
            ExceptionAssert.Throws<WizardBackoutException>(
                () => ((IWizard)wizard).RunStarted(null, null, runKind, null));

            // Assert
            Assert.Equal("This template wizard can only be applied to single-project or project-item templates.",
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void RunStarted_LoadsConfigurationFromPath()
        {
            // Arrange
            var document = new XDocument(new XElement("VSTemplate"));
            string path = null;
            var wizard = new TestableVsTemplateWizard(loadDocumentCallback: p =>
            {
                path = p;
                return document;
            });
            var dte = new Mock<DTE>().Object;

            // Act
            ((IWizard)wizard).RunStarted(dte, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\SomePath\ToFile.vstemplate" });

            // Assert
            Assert.Equal(@"C:\SomePath\ToFile.vstemplate", path);
        }

        [Fact]
        public void RunFinished_ForProject_InstallsPackages()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var installerMock = new Mock<IVsPackageInstaller>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0"),
                BuildPackageElement("MyOtherPackage", "2.0"));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectFinishedGenerating(mockProject);

            // Act
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyPackage", "1.0", true, false));
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyOtherPackage", "2.0", true, false));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0 to project...");
        }

        [Fact]
        public void RunFinished_ForProject_InstallsDependenciesWhenIncludeDependenciesIsTrue()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var installerMock = new Mock<IVsPackageInstaller>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0", includeDependencies: true),
                BuildPackageElement("MyOtherPackage", "2.0"));

            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectFinishedGenerating(mockProject);

            // Act
            wizard.RunFinished();

            // Assert (the key here is that the ignoreDependencies parameter is false for MyPackage because we said to includeDependencies on that package element)
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyPackage", "1.0", false, false));
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyOtherPackage", "2.0", true, false));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0 to project...");
        }

        [Fact]
        public void RunFinished_ForProject_InstallsPackagesUseUnzippedPackageRepositoryWhenIsPreunzippedAttributeIsTrue()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var installerMock = new Mock<IVsPackageInstaller>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0"),
                BuildPackageElement("MyOtherPackage", "2.0"));

            document.Element("VSTemplate")
                    .Element(VSTemplateNamespace + "WizardData")
                    .Element(VSTemplateNamespace + "packages")
                    .Add(new XAttribute("isPreunzipped", true));

            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectFinishedGenerating(mockProject);

            // Act
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(
                It.Is<IPackageRepository>(p => p is UnzippedPackageRepository && p.Source == @"C:\Some"), 
                mockProject, 
                "MyPackage", 
                "1.0",
                true, 
                false));
            installerMock.Verify(i => i.InstallPackage(
                It.Is<IPackageRepository>(p => p is UnzippedPackageRepository && p.Source == @"C:\Some"), 
                mockProject, 
                "MyOtherPackage", 
                "2.0",
                true,
                false));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0 to project...");
        }

        [Fact]
        public void RunFinished_ForItem_InstallsPackages()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(i => i.ContainingProject).Returns(mockProject);
            var installerMock = new Mock<IVsPackageInstaller>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0"),
                BuildPackageElement("MyOtherPackage", "2.0"));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectItemFinishedGenerating(projectItemMock.Object);

            // Act
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyPackage", "1.0", true, false));
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyOtherPackage", "2.0", true, false));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0 to project...");
        }

        [Fact]
        public void RunFinished_ForItem_InstallsPackagesWithSkipAssemblyReferencesSetIfSpecified()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(i => i.ContainingProject).Returns(mockProject);
            var installerMock = new Mock<IVsPackageInstaller>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0", skipAssemblyReferences: true),
                BuildPackageElement("MyOtherPackage", "2.0", skipAssemblyReferences: false));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectItemFinishedGenerating(projectItemMock.Object);

            // Act
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyPackage", "1.0", true, true));
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyOtherPackage", "2.0", true, false));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0 to project...");
        }

        [Fact]
        public void RunFinished_ForItem_InstallsPrereleasePackages()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(i => i.ContainingProject).Returns(mockProject);
            var installerMock = new Mock<IVsPackageInstaller>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0.0-ctp-1"),
                BuildPackageElement("MyOtherPackage", "2.0.3.4"));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectItemFinishedGenerating(projectItemMock.Object);

            // Act
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyPackage", "1.0.0-ctp-1", true, false));
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyOtherPackage", "2.0.3.4", true, false));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0.0-ctp-1 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0.3.4 to project...");
        }

        [Fact]
        public void RunFinished_ForItem_SilentlyIgnoresPackageIsExactVersionAlreadyInstalled()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(i => i.ContainingProject).Returns(mockProject);
            var installerMock = new Mock<IVsPackageInstaller>();
            var servicesMock = new Mock<IVsPackageInstallerServices>();
            var consoleProviderMock = new Mock<IOutputConsoleProvider>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0.0-ctp-1"),
                BuildPackageElement("MyOtherPackage", "2.0.3-beta4"),
                BuildPackageElement("YetAnotherPackage", "3.0.2.1"));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document, packageServices: servicesMock.Object, consoleProvider: consoleProviderMock.Object);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            
            // * Setup mocks
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            servicesMock.Setup(s => s.IsPackageInstalled(mockProject, "MyOtherPackage")).Returns(true);
            servicesMock.Setup(s => s.IsPackageInstalled(mockProject, "MyOtherPackage", new SemanticVersion("2.0.3-beta4"))).Returns(true);

            // * Setup wizard
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });

            // Act
            wizard.ProjectItemFinishedGenerating(projectItemMock.Object);
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyPackage", "1.0.0-ctp-1", true, false));
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "YetAnotherPackage", "3.0.2.1", true, false));
            installerMock.Verify(
                i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyOtherPackage", "2.0.3-beta4", true, false),
                Times.Never());
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0.0-ctp-1 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding YetAnotherPackage.3.0.2.1 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0.3-beta4 to project...", Times.Never());
            consoleProviderMock.Verify(c => c.CreateOutputConsole(It.IsAny<bool>()), Times.Never());
        }

        [Fact]
        public void RunFinished_ForItem_PrintsWarningIfInstalledVersionDoesntMatchRequestedVersion()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var projectItemMock = new Mock<ProjectItem>();
            projectItemMock.Setup(i => i.ContainingProject).Returns(mockProject);
            var installerMock = new Mock<IVsPackageInstaller>();
            var servicesMock = new Mock<IVsPackageInstallerServices>();
            var consoleProviderMock = new Mock<IOutputConsoleProvider>();
            var consoleMock = new Mock<IConsole>();
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0.0-ctp-1"),
                BuildPackageElement("MyOtherPackage", "2.0.3-beta4"),
                BuildPackageElement("YetAnotherPackage", "3.0.2.1"));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document, packageServices: servicesMock.Object, consoleProvider: consoleProviderMock.Object);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();

            // * Setup mocks
            consoleProviderMock.Setup(cp => cp.CreateOutputConsole(false)).Returns(consoleMock.Object);
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            servicesMock.Setup(s => s.IsPackageInstalled(mockProject, "MyOtherPackage")).Returns(true);
            servicesMock.Setup(s => s.IsPackageInstalled(mockProject, "MyOtherPackage", new SemanticVersion("2.0.3-beta4"))).Returns(false);

            // * Setup wizard
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });

            // Act
            wizard.ProjectItemFinishedGenerating(projectItemMock.Object);
            wizard.RunFinished();

            // Assert
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyPackage", "1.0.0-ctp-1", true, false));
            installerMock.Verify(i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "YetAnotherPackage", "3.0.2.1", true, false));
            installerMock.Verify(
                i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyOtherPackage", "2.0.3-beta4", true, false),
                Times.Never());
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0.0-ctp-1 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding YetAnotherPackage.3.0.2.1 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0.3-beta4 to project...", Times.Never());
            consoleMock.Verify(c => c.WriteLine("Attempting to install version '2.0.3-beta4' of 'MyOtherPackage' but the project already includes a different version. Skipping..."));
        }

        [Fact]
        public void RunFinished_InstallsValidPackages_ReportsInstallationErrors()
        {
            // Arrange
            var mockProject = new Mock<Project>().Object;
            var installerMock = new Mock<IVsPackageInstaller>();
            installerMock.Setup(i => i.InstallPackage(It.IsAny<IPackageRepository>(), mockProject, "MyPackage", "1.0", true, false))
                .Throws(new InvalidOperationException("But I don't want to be installed."));
            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0"),
                BuildPackageElement("MyOtherPackage", "2.0"));
            var templateWizard = new TestableVsTemplateWizard(installerMock.Object,
                loadDocumentCallback: p => document);
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });
            wizard.ProjectFinishedGenerating(mockProject);

            // Act
            wizard.RunFinished();

            // Assert
            installerMock.Verify(
                i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyPackage", "1.0", true, false));
            installerMock.Verify(
                i => i.InstallPackage(It.Is<LocalPackageRepository>(p => p.Source == @"C:\Some"), mockProject, "MyOtherPackage", "2.0", true, false));
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyPackage.1.0 to project...");
            dteMock.VerifySet(dte => dte.StatusBar.Text = "Adding MyOtherPackage.2.0 to project...");
            Assert.Equal(
                "Could not add all required packages to the project. The following packages failed to install from 'C:\\Some':\r\n\r\nMyPackage.1.0 : But I don't want to be installed.",
                templateWizard.ErrorMessages.Single());
        }

        [Fact]
        public void CreateRefreshesFilesForWebsites()
        {
            // Arrange
            var mockProject = new Mock<Project>();
            mockProject.Setup(s => s.Kind).Returns(VsConstants.WebSiteProjectTypeGuid);

            var installerMock = new Mock<IVsPackageInstaller>();
            var websiteHandler = new Mock<IVsWebsiteHandler>();
            websiteHandler.Setup(h =>
                h.AddRefreshFilesForReferences(
                    mockProject.Object,
                    It.IsAny<IFileSystem>(),
                    It.Is<IEnumerable<PackageName>>(names => names.Count() == 2 && names.First().Id == "MyPackage" && names.Last().Id == "YourPackage")
                )).Verifiable();

            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0", skipAssemblyReferences: true),
                BuildPackageElement("MyOtherPackage", "2.0"),
                BuildPackageElement("YourPackage", "3.0-alpha", skipAssemblyReferences: true),
                BuildPackageElement("YourOtherPackage", "2.0"));

            var repositorySettings = new Mock<IRepositorySettings>();
            repositorySettings.Setup(s => s.RepositoryPath).Returns("x:\\packages");

            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document, websiteHandler: websiteHandler.Object)
            {
                RepositorySettings = new Lazy<IRepositorySettings>(() => repositorySettings.Object)
            };
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });

            // Act
            wizard.ProjectFinishedGenerating(mockProject.Object);

            // Verify
            websiteHandler.Verify();
        }

        [Fact]
        public void DoNoteCreateRefreshesFilesForNonWebsites()
        {
            // Arrange
            var mockProject = new Mock<Project>();

            var installerMock = new Mock<IVsPackageInstaller>();
            var websiteHandler = new Mock<IVsWebsiteHandler>(MockBehavior.Strict);

            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0", skipAssemblyReferences: true),
                BuildPackageElement("MyOtherPackage", "2.0"),
                BuildPackageElement("YourPackage", "3.0-alpha", skipAssemblyReferences: true),
                BuildPackageElement("YourOtherPackage", "2.0"));

            var repositorySettings = new Mock<IRepositorySettings>();
            repositorySettings.Setup(s => s.RepositoryPath).Returns("x:\\packages");

            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document, websiteHandler: websiteHandler.Object)
            {
                RepositorySettings = new Lazy<IRepositorySettings>(() => repositorySettings.Object)
            };
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });

            // Act
            wizard.ProjectFinishedGenerating(mockProject.Object);

            // Verify
            websiteHandler.Verify();
        }

        [Fact]
        public void CopyNativeBinariesForWebsites()
        {
            // Arrange
            var mockProject = new Mock<Project>();
            mockProject.Setup(s => s.Kind).Returns(VsConstants.WebSiteProjectTypeGuid);

            var installerMock = new Mock<IVsPackageInstaller>();
            var websiteHandler = new Mock<IVsWebsiteHandler>();
            websiteHandler.Setup(h =>
                h.CopyNativeBinaries(
                    mockProject.Object,
                    It.IsAny<IFileSystem>(),
                    It.Is<IEnumerable<PackageName>>(names => names.Count() == 2 && names.First().Id == "MyPackage" && names.Last().Id == "YourPackage")
                )).Verifiable();

            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0", skipAssemblyReferences: true),
                BuildPackageElement("YourPackage", "3.0-alpha", skipAssemblyReferences: false));

            var repositorySettings = new Mock<IRepositorySettings>();
            repositorySettings.Setup(s => s.RepositoryPath).Returns("x:\\packages");

            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document, websiteHandler: websiteHandler.Object)
            {
                RepositorySettings = new Lazy<IRepositorySettings>(() => repositorySettings.Object)
            };
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });

            // Act
            wizard.ProjectFinishedGenerating(mockProject.Object);

            // Verify
            websiteHandler.Verify();
        }

        [Fact]
        public void DoNotCopyNativeBinariesForNonWebsites()
        {
            // Arrange
            var mockProject = new Mock<Project>();

            var installerMock = new Mock<IVsPackageInstaller>();
            var websiteHandler = new Mock<IVsWebsiteHandler>(MockBehavior.Strict);

            var document = BuildDocument("template",
                BuildPackageElement("MyPackage", "1.0", skipAssemblyReferences: true),
                BuildPackageElement("YourPackage", "3.0-alpha", skipAssemblyReferences: false));

            var repositorySettings = new Mock<IRepositorySettings>();
            repositorySettings.Setup(s => s.RepositoryPath).Returns("x:\\packages");

            var templateWizard = new TestableVsTemplateWizard(installerMock.Object, loadDocumentCallback: p => document, websiteHandler: websiteHandler.Object)
            {
                RepositorySettings = new Lazy<IRepositorySettings>(() => repositorySettings.Object)
            };
            var wizard = (IWizard)templateWizard;
            var dteMock = new Mock<DTE>();
            dteMock.SetupProperty(dte => dte.StatusBar.Text);
            wizard.RunStarted(dteMock.Object, null, WizardRunKind.AsNewProject,
                new object[] { @"C:\Some\file.vstemplate" });

            // Act
            wizard.ProjectFinishedGenerating(mockProject.Object);

            // Verify
            websiteHandler.Verify();
        }

        [Fact]
        public void ShouldAddProjectItem_AlwaysReturnsTrue()
        {
            IWizard wizard = new VsTemplateWizard(null, null, null, null, null, null);

            Assert.True(wizard.ShouldAddProjectItem(null));
            Assert.True(wizard.ShouldAddProjectItem(""));
            Assert.True(wizard.ShouldAddProjectItem("foo"));
        }

        private sealed class TestableVsTemplateWizard : VsTemplateWizard
        {
            private readonly Func<string, XDocument> _loadDocumentCallback;

            public TestableVsTemplateWizard(
                IVsPackageInstaller installer = null,
                Func<string, XDocument> loadDocumentCallback = null,
                IVsWebsiteHandler websiteHandler = null,
                IVsPackageInstallerServices packageServices = null,
                IOutputConsoleProvider consoleProvider = null)
                : base(
                    installer, 
                    websiteHandler, 
                    packageServices ?? new Mock<IVsPackageInstallerServices>().Object, 
                    consoleProvider ?? new Mock<IOutputConsoleProvider>().Object,
                    new Mock<IVsCommonOperations>().Object,
                    new Mock<ISolutionManager>().Object)
            {
                ErrorMessages = new List<string>();
                _loadDocumentCallback = loadDocumentCallback ?? (path => null);
            }

            public List<string> ErrorMessages { get; private set; }

            internal override XDocument LoadDocument(string path)
            {
                return _loadDocumentCallback(path);
            }

            internal override void ShowErrorMessage(string message)
            {
                ErrorMessages.Add(message);
            }

            internal override void ThrowWizardBackoutError(string message)
            {
                ErrorMessages.Add(message);
                throw new WizardBackoutException();
            }
        }
    }
}