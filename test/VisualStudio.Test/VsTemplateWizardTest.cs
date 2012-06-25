using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.TemplateWizard;
using Moq;
using NuGet.Test;
using Xunit;
using Xunit.Extensions;
using NuGetConsole;

namespace NuGet.VisualStudio.Test
{
    public class VsTemplateWizardTest
    {
        private static readonly XNamespace VSTemplateNamespace = "http://schemas.microsoft.com/developer/vstemplate/2005";

        private static XDocument BuildDocument(string repository = "template", params XObject[] packagesChildren)
        {
            var children = new List<object>();
            if (repository != null)
            {
                children.Add(new XAttribute("repository", repository));
            }
            children.AddRange(packagesChildren);
            return new XDocument(new XElement("VSTemplate",
                new XElement(VSTemplateNamespace + "WizardData",
                    new XElement(VSTemplateNamespace + "packages", children))));
        }

        private static XDocument BuildDocumentWithPackage(string repository, XObject additionalChild = null)
        {
            return BuildDocument(repository, BuildPackageElement("pack", "1.0"), additionalChild);
        }

        private static XElement BuildPackageElement(string id = null, string version = null, bool skipAssemblyReferences = false)
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
            return packageElement;
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithMissingWizardDataElement()
        {
            // Arrange
            var document = new XDocument(new XElement(VSTemplateNamespace + "VSTemplate"));
            var wizard = new VsTemplateWizard(null, null, null, null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(0, result.Packages.Count);
            Assert.Equal(null, result.RepositoryPath);
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

            var wizard = new VsTemplateWizard(null, null, null, null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"c:\some\file.vstemplate");

            // Assert
            Assert.Equal(expectedResult, result.IsPreunzipped);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithMissingPackagesElement()
        {
            // Arrange
            var document = new XDocument(
                new XElement(VSTemplateNamespace + "VSTemplate",
                    new XElement(VSTemplateNamespace + "WizardData")
                    ));
            var wizard = new VsTemplateWizard(null, null, null, null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(0, result.Packages.Count);
            Assert.Equal(null, result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithEmptyPackagesElement()
        {
            // Arrange
            var document = BuildDocument(null);
            var wizard = new VsTemplateWizard(null, null, null, null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(0, result.Packages.Count);
            Assert.Equal(null, result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithTemplateRepository()
        {
            // Arrange
            var document = BuildDocumentWithPackage("template");
            var wizard = new VsTemplateWizard(null, null, null, null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(1, result.Packages.Count);
            Assert.Equal(@"C:\Some", result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithExtensionRepository()
        {
            // Arrange
            var document = BuildDocumentWithPackage("extension", new XAttribute("repositoryId", "myExtensionId"));
            var wizard = new VsTemplateWizard(null, null, null, null);
            var extensionManagerMock = new Mock<IVsExtensionManager>();
            var extensionMock = new Mock<IInstalledExtension>();
            extensionMock.Setup(e => e.InstallPath).Returns(@"C:\Extension\Dir");
            var extension = extensionMock.Object;
            extensionManagerMock.Setup(em => em.TryGetInstalledExtension("myExtensionId", out extension)).Returns(true);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate", vsExtensionManager: extensionManagerMock.Object);

            // Assert
            Assert.Equal(1, result.Packages.Count);
            Assert.Equal(@"C:\Extension\Dir\Packages", result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ShowErrorForMissingRepositoryIdAttributeWhenInExtensionRepositoryMode()
        {
            // Arrange
            var document = BuildDocumentWithPackage("extension");
            var wizard = new TestableVsTemplateWizard();

            // Act
            ExceptionAssert.Throws<WizardBackoutException>(() =>
                                                           wizard.GetConfigurationFromXmlDocument(document,
                                                               @"C:\Some\file.vstemplate"));

            // Assert
            Assert.Equal(
                "The project template is configured to use an Extension-specific package repository but the Extension ID has not been specified. Use the \"repositoryId\" attribute to specify the Extension ID.",
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
            ExceptionAssert.Throws<WizardBackoutException>(() => wizard.GetConfigurationFromXmlDocument(document,
                @"C:\Some\file.vstemplate",
                vsExtensionManager: extensionManagerMock.Object));

            // Assert
            Assert.Equal(
                "The project template has a reference to a missing Extension. Could not find an Extension with ID 'myExtensionId'.",
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
            var wizard = new VsTemplateWizard(null, null, null, null);

            var hkcu_repository = new Mock<IRegistryKey>();
            var hkcu = new Mock<IRegistryKey>();
            hkcu_repository.Setup(k => k.GetValue(registryKey)).Returns(registryValue);
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns(hkcu_repository.Object);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate", registryKeys: new[] { hkcu.Object });

            // Assert
            Assert.Equal(registryValue, result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_FallsBackWhenHKCURegistryKeyDoesNotExist()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "AspNetMvc4";
            var registryValue = @"C:\AspNetMvc4\Packages";

            var document = BuildDocumentWithPackage("registry", new XAttribute("keyName", registryKey));
            var wizard = new VsTemplateWizard(null, null, null, null);

            // HKCU key doesn't exist
            var hkcu = new Mock<IRegistryKey>();
            hkcu.Setup(r => r.OpenSubKey(registryPath)).Returns<string>(null);

            // HKLM key is configured
            var hklm_repository = new Mock<IRegistryKey>();
            var hklm = new Mock<IRegistryKey>();
            hklm_repository.Setup(k => k.GetValue(registryKey)).Returns(registryValue);
            hklm.Setup(r => r.OpenSubKey(registryPath)).Returns(hklm_repository.Object);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate", registryKeys: new[] { hkcu.Object, hklm.Object });

            // Assert
            Assert.Equal(registryValue, result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_FallsBackWhenHKCURegistryValueDoesNotExist()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKey = "AspNetMvc4";
            var registryValue = @"C:\AspNetMvc4\Packages";

            var document = BuildDocumentWithPackage("registry", new XAttribute("keyName", registryKey));
            var wizard = new VsTemplateWizard(null, null, null, null);

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
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate", registryKeys: new[] { hkcu.Object, hklm.Object });

            // Assert
            Assert.Equal(registryValue, result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_FallsBackWhenHKCURegistryValueIsEmpty()
        {
            // Arrange
            var registryPath = @"SOFTWARE\NuGet\Repository";
            var registryKeyName = "AspNetMvc4";
            var registryValue = @"C:\AspNetMvc4\Packages";

            var document = BuildDocumentWithPackage("registry", new XAttribute("keyName", registryKeyName));
            var wizard = new VsTemplateWizard(null, null, null, null);

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
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate", registryKeys: new[] { hkcu.Object, hklm.Object });

            // Assert
            Assert.Equal(registryValue, result.RepositoryPath);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ShowErrorForMissingKeyNameAttributeWhenInRegistryRepositoryMode()
        {
            // Arrange
            var document = BuildDocumentWithPackage("registry");
            var wizard = new TestableVsTemplateWizard();

            // Act
            ExceptionAssert.Throws<WizardBackoutException>(() =>
                                                           wizard.GetConfigurationFromXmlDocument(document,
                                                               @"C:\Some\file.vstemplate", registryKeys: Enumerable.Empty<IRegistryKey>()));

            // Assert
            Assert.Equal(
                "The project template is configured to use a Registry-provided package repository but the Registry value name has not been specified. Use the \"keyName\" attribute to specify the Registry value.",
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
            ExceptionAssert.Throws<WizardBackoutException>(() =>
                                                           wizard.GetConfigurationFromXmlDocument(document,
                                                               @"C:\Some\file.vstemplate", registryKeys: new[] { hkcu.Object }));

            // Assert
            Assert.Equal(
                String.Format("The project template is configured to use a Registry-provided package repository but there was an error accessing Registry key '{0}'.", registryPath),
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
            ExceptionAssert.Throws<WizardBackoutException>(() =>
                                                           wizard.GetConfigurationFromXmlDocument(document,
                                                               registryPath, registryKeys: new[] { hkcu.Object }));

            // Assert
            Assert.Equal(
                String.Format("The project template has a reference to a missing Registry value. Could not find a Registry key with name '{0}' under '{1}'.", registryKey, registryPath),
                wizard.ErrorMessages.Single());
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_ShowsErrorForInvalidCacheAttributeValue()
        {
            // Arrange
            var document = BuildDocumentWithPackage("__invalid__");
            var wizard = new TestableVsTemplateWizard();

            // Act
            ExceptionAssert.Throws<WizardBackoutException>(() => wizard.GetConfigurationFromXmlDocument(document,
                @"C:\Some\file.vstemplate"));

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
                new VsTemplateWizardPackageInfo("MyPackage", "1.0"),
                new VsTemplateWizardPackageInfo("MyOtherPackage", "2.0")
            };
            var document = BuildDocument("template", content);

            VerifyParsedPackages(document, expectedPackages);
        }

        [Fact]
        public void GetConfigurationFromXmlDocument_WorksWithDocumentWithNoNamespace()
        {
            var expectedPackages = new[] {
                new VsTemplateWizardPackageInfo("MyPackage", "1.0"),
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
                new VsTemplateWizardPackageInfo("MyPackage", "4.0.0-ctp-2"),
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

        private static void VerifyParsedPackages(XDocument document, IEnumerable<VsTemplateWizardPackageInfo> expectedPackages)
        {
            // Arrange
            var wizard = new VsTemplateWizard(null, null, null, null);

            // Act
            var result = wizard.GetConfigurationFromXmlDocument(document, @"C:\Some\file.vstemplate");

            // Assert
            Assert.Equal(expectedPackages.Count(), result.Packages.Count);
            foreach (var pair in expectedPackages.Zip(result.Packages,
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

        private static void InvalidPackageElementHelper(XElement[] content)
        {
            // Arrange
            var document = BuildDocument("template", content);
            var wizard = new TestableVsTemplateWizard();

            // Act
            ExceptionAssert.Throws<WizardBackoutException>(() => wizard.GetConfigurationFromXmlDocument(document,
                @"C:\Some\file.vstemplate"));

            // Assert
            Assert.Equal("The project template lists one or more packages with missing, empty, or invalid values for the \"id\" or \"version\" attributes. Both attributes are required and must have valid values.", wizard.ErrorMessages.Single());
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
            IWizard wizard = new VsTemplateWizard(null, null, null, null);

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
                : base(installer, websiteHandler, packageServices ?? new Mock<IVsPackageInstallerServices>().Object, consoleProvider ?? new Mock<IOutputConsoleProvider>().Object)
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
        }
    }
}