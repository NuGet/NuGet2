using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Test;
using System;

namespace NuGet.VisualStudio.Test {
    [TestClass]
    public class VsPackageSourceProviderTest {
        [TestMethod]
        public void AddSourceThrowsIfSourceIsNull() {
            // Arrange
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            var userSettingsManager = new MockUserSettingsManager();
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            ExceptionAssert.ThrowsArgNull(() => provider.AddPackageSource(null), "source");
        }

        [TestMethod]
        public void CtorIfFirstRunningAddsDefaultSource() {
            // Arrange
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            var userSettingsManager = new MockUserSettingsManager();
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            var sources = provider.GetPackageSources().ToList();


            // Assert
            Assert.AreEqual(2, sources.Count);
            Assert.AreEqual(VsPackageSourceProvider.DefaultPackageSource, sources[1].Source);
        }

        [TestMethod]
        public void CtorAddsDefaultSourceIfAnotherDefaultWasPreviouslyRegistered() {
            // Arrange
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            var userSettingsManager = new MockUserSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><Name>NuGet official package source</Name><Source>http://some/old/feed</Source></PackageSource></ArrayOfPackageSource>";
            registrySettingsManager.ActivePackageSourceString = "<PackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><IsAggregate>true</IsAggregate><Name>All</Name><Source>(Aggregate source)</Source></PackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            var sources = provider.GetPackageSources().ToList();


            // Assert
            Assert.AreEqual(2, sources.Count);
            Assert.AreEqual(VsPackageSourceProvider.DefaultPackageSource, sources[1].Source);
        }

        [TestMethod]
        public void CtorAddsAggregrateIfNothingWasPersistedIntoSettingsManager() {
            // Arrange
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            var userSettingsManager = new MockUserSettingsManager();
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            var sources = provider.GetPackageSources().ToList();


            // Assert
            Assert.AreEqual(2, sources.Count);
            Assert.AreEqual(provider.AggregateSource, sources[0]);
        }

        [TestMethod]
        public void CtorAddsAggregrateIfAggregateWasPersistedIntoSettingsManager() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><IsAggregate>false</IsAggregate><Name>a</Name><Source>a</Source></PackageSource></ArrayOfPackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            var sources = provider.GetPackageSources().ToList();


            // Assert
            Assert.AreEqual(2, sources.Count);
            Assert.AreEqual(provider.AggregateSource, sources[0]);
            Assert.AreEqual(new PackageSource("a", "a"), sources[1]);
        }

        [TestMethod]
        public void CtorDoesNotAddNewAggregrateIfAggregatePersistedIntoSettingsManager() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><IsAggregate>true</IsAggregate><Name>All</Name><Source>(Aggregate source)</Source></PackageSource></ArrayOfPackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            var sources = provider.GetPackageSources().ToList();


            // Assert
            Assert.AreEqual(2, sources.Count);
            Assert.AreEqual(provider.AggregateSource, sources[0]);
        }

        [TestMethod]
        public void CtorDoesNotAddNewAggregrateIfAggregatePersistedIntoSettingsManagerAndAggregateIsActivePackageSource() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><IsAggregate>true</IsAggregate><Name>All</Name><Source>(Aggregate source)</Source></PackageSource></ArrayOfPackageSource>";
            registrySettingsManager.ActivePackageSourceString = "<PackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><IsAggregate>true</IsAggregate><Name>All</Name><Source>(Aggregate source)</Source></PackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            var sources = provider.GetPackageSources().ToList();


            // Assert
            Assert.AreEqual(2, sources.Count);
            Assert.AreEqual(provider.AggregateSource, sources[0]);
        }

        [TestMethod]
        public void PackageSourcesAreMigratedToUserSettings() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><IsAggregate>false</IsAggregate><Name>name1</Name><Source>source1</Source></PackageSource><PackageSource><IsAggregate>false</IsAggregate><Name>name2</Name><Source>source2</Source></PackageSource><PackageSource><IsAggregate>false</IsAggregate><Name>name3</Name><Source>source3</Source></PackageSource></ArrayOfPackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            var values = userSettingsManager.GetValues(VsPackageSourceProvider.FileSettingsSectionName);

            // Assert
            Assert.AreEqual(3, values.Count);
            AssertPackageSource(values[0], "name1", "source1");
            AssertPackageSource(values[1], "name2", "source2");
            AssertPackageSource(values[2], "name3", "source3");

            // also verify that the registry setting is deleted
            Assert.IsNull(registrySettingsManager.PackageSourcesString);
        }

        [TestMethod]
        public void ActivePackageSourceIsMigratedToUserSettings() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><IsAggregate>false</IsAggregate><Name>name1</Name><Source>source1</Source></PackageSource><PackageSource><IsAggregate>false</IsAggregate><Name>name2</Name><Source>source2</Source></PackageSource><PackageSource><IsAggregate>false</IsAggregate><Name>name3</Name><Source>source3</Source></PackageSource></ArrayOfPackageSource>";
            registrySettingsManager.ActivePackageSourceString = "<PackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><IsAggregate>false</IsAggregate><Name>name2</Name><Source>source2</Source></PackageSource>";

            // Act
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Assert
            var activeValue = userSettingsManager.GetValue(VsPackageSourceProvider.FileSettingsActiveSectionName, "name2");
            Assert.AreEqual("source2", activeValue);

            var invalidActiveValue = userSettingsManager.GetValue(VsPackageSourceProvider.FileSettingsActiveSectionName, "invalidName");
            Assert.IsNull(invalidActiveValue);

            // also verify that the registry setting is deleted
            Assert.IsNull(registrySettingsManager.ActivePackageSourceString);
        }

        [TestMethod]
        public void AddSourceSetsPersistsSourcesToSettingsManager() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            for (int i = 0; i < 10; i++) {
                provider.AddPackageSource(new PackageSource("source" + i, "name" + i));
            }

            // Assert
            var values = userSettingsManager.GetValues(VsPackageSourceProvider.FileSettingsSectionName);
            
            // 11 = 10 package sources that we added + NuGet offical source
            Assert.AreEqual(11, values.Count);
            Assert.AreEqual(Resources.VsResources.OfficialSourceName, values[0].Key);
            for (int i = 0; i < 10; i++) {
                AssertPackageSource(values[i + 1], "name" + i, "source" + i);
            }

            Assert.IsNull(registrySettingsManager.ActivePackageSourceString);
            Assert.IsNull(registrySettingsManager.PackageSourcesString);
        }

        [TestMethod]
        public void SetActivePackageSourcePersistsItToSettingsManager() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><IsAggregate>false</IsAggregate><Name>name</Name><Source>source</Source></PackageSource></ArrayOfPackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            provider.ActivePackageSource = new PackageSource("source", "name");

            // Assert
            var activeValue = userSettingsManager.GetValue(VsPackageSourceProvider.FileSettingsActiveSectionName, "name");
            Assert.AreEqual("source", activeValue);

            var invalidActiveValue = userSettingsManager.GetValue(VsPackageSourceProvider.FileSettingsActiveSectionName, "invalidName");
            Assert.IsNull(invalidActiveValue);

            Assert.IsNull(registrySettingsManager.ActivePackageSourceString);
        }

        [TestMethod]
        public void RemoveSourceThrowsIfSourceIsNull() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            ExceptionAssert.ThrowsArgNull(() => provider.RemovePackageSource(null), "source");
        }

        [TestMethod]
        public void RemovingUnknownPackageSourceReturnsFalse() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            bool result = provider.RemovePackageSource(new PackageSource("a", "a"));

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RemovingExistingPackageSourceReturnsFalse() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><IsAggregate>false</IsAggregate><Name>a</Name><Source>a</Source></PackageSource></ArrayOfPackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);
            var packageSource = new PackageSource("a", "a");

            // Act
            bool result = provider.RemovePackageSource(packageSource);

            // Assert
            Assert.IsTrue(result);

            // values should be null because we don't persist aggregate source into user settings file
            var values = userSettingsManager.GetValues(VsPackageSourceProvider.FileSettingsSectionName);
            Assert.IsNull(values);
        }

        [TestMethod]
        public void RemovingActivePackageSourceSetsActivePackageSourceToNull() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><IsAggregate>false</IsAggregate><Name>a</Name><Source>a</Source></PackageSource></ArrayOfPackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);
            var packageSource = new PackageSource("a", "a");
            provider.ActivePackageSource = packageSource;

            // Act
            bool result = provider.RemovePackageSource(packageSource);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNull(provider.ActivePackageSource);

            // values should be null because we don't persist aggregate source into user settings file
            var values = userSettingsManager.GetValues(VsPackageSourceProvider.FileSettingsSectionName);
            Assert.IsNull(values);
        }

        [TestMethod]
        public void SettingActivePackageSourceToNonExistantSourceThrows() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            ExceptionAssert.ThrowsArgumentException(() => provider.ActivePackageSource = new PackageSource("a", "a"), "value", "The package source does not belong to the collection of available sources.");
        }

        [TestMethod]
        public void SettingActivePackageSourceSetsActivePackageStringOnSettingsManager() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><IsAggregate>false</IsAggregate><Name>name</Name><Source>source</Source></PackageSource></ArrayOfPackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            provider.ActivePackageSource = new PackageSource("source", "name");

            // Assert
            var value = userSettingsManager.GetValue(VsPackageSourceProvider.FileSettingsActiveSectionName, "name");
            Assert.AreEqual("source", value);
        }

        [TestMethod]
        public void AggregateSourceWithoutIsAggregateFlagSetFlagToTrue() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><Name>All</Name><Source>(Aggregate source)</Source></PackageSource><PackageSource><IsAggregate>false</IsAggregate><Name>NuGet official package source</Name><Source>https://go.microsoft.com/fwlink/?LinkID=206669</Source></PackageSource></ArrayOfPackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            var sources = provider.GetPackageSources().ToList();

            // Assert
            Assert.AreEqual(2, sources.Count);
            Assert.IsTrue(sources[0].IsAggregate);
        }

        [TestMethod]
        public void SettingsWithMoreThanOneAggregateSourceAreModifiedToHaveOneOnly() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "<ArrayOfPackageSource xmlns=\"http://schemas.datacontract.org/2004/07/NuGet\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><PackageSource><IsAggregate>true</IsAggregate><Name>All</Name><Source>(Aggregate source)</Source></PackageSource><PackageSource><IsAggregate>true</IsAggregate><Name>NuGet official package source</Name><Source>https://go.microsoft.com/fwlink/?LinkID=206669</Source></PackageSource></ArrayOfPackageSource>";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);

            // Act
            var sources = provider.GetPackageSources().ToList();

            // Assert
            Assert.AreEqual(2, sources.Count);
            Assert.IsTrue(sources[0].IsAggregate);
            Assert.IsFalse(sources[1].IsAggregate);
        }

        [TestMethod]
        public void SettingPackageSourcesWithAggregateWillNotAddAggregate() {
            // Arrange
            var userSettingsManager = new MockUserSettingsManager();
            var registrySettingsManager = new MockPackageSourceSettingsManager();
            registrySettingsManager.PackageSourcesString = "";
            var provider = new VsPackageSourceProvider(registrySettingsManager, userSettingsManager);
            var packageSources = new List<PackageSource> {
                                                             provider.AggregateSource,
                                                             new PackageSource("source", "name")
                                                         };

            // Act
            provider.SetPackageSources(packageSources);

            // Assert
            var values = userSettingsManager.GetValues(VsPackageSourceProvider.FileSettingsSectionName);

            Assert.AreEqual(1, values.Count);
            AssertPackageSource(values[0], "name", "source");
        }

        private void AssertPackageSource(KeyValuePair<string, string> ps, string name, string source) {
            Assert.AreEqual(name, ps.Key);
            Assert.AreEqual(source, ps.Value);
        }

        private class MockPackageSourceSettingsManager : IPackageSourceSettingsManager {
            public string ActivePackageSourceString {
                get;
                set;
            }

            public string PackageSourcesString {
                get;
                set;
            }
        }
    }
}