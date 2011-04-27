using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {

    [TestClass]
    public class PackageSourceProviderTest {

        [TestMethod]
        public void TestNoPackageSourcesAreReturnedIfUserSettingsIsEmpty() {
            // Arrange
            var provider = CreatePackageSourceProvider();

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.AreEqual(0, values.Count);
        }

        [TestMethod]
        public void CallSaveMethodAndLoadMethodShouldReturnTheSamePackageSet() {
            // Arrange
            var provider = CreatePackageSourceProvider();

            var sources = new[] { new PackageSource("one"), new PackageSource("two"), new PackageSource("three") };
            provider.SavePackageSources(sources);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.AreEqual(sources.Length, values.Count);
            for (int i = 0; i < sources.Length; i++) {
                AssertPackageSource(values[i], sources[i].Name, sources[i].Source);
            }
        }

        [TestMethod]
        public void LoadPackageSourcesReturnCorrectDataFromSettings() {
            // Arrange
            var settings = new MockUserSettingsManager();
            settings.SetValues(PackageSourceProvider.FileSettingsSectionName,
                new[] {
                    new KeyValuePair<string, string>("one", "onesource"),
                    new KeyValuePair<string, string>("two", "twosource"),
                    new KeyValuePair<string, string>("three", "threesource")
                });

            var provider = CreatePackageSourceProvider(settings);

            // Act
            var values = provider.LoadPackageSources().ToList();

            // Assert
            Assert.AreEqual(3, values.Count);
            AssertPackageSource(values[0], "one", "onesource");
            AssertPackageSource(values[1], "two", "twosource");
            AssertPackageSource(values[2], "three", "threesource");
        }

        [TestMethod]
        public void SavePackageSourcesSaveCorrectDataToSettings() {
            // Arrange
            var settings = new MockUserSettingsManager();
            var provider = CreatePackageSourceProvider(settings);

            var sources = new[] { new PackageSource("one"), new PackageSource("two"), new PackageSource("three") };

            // Act
            provider.SavePackageSources(sources);

            // Assert
            var values = settings.GetValues(PackageSourceProvider.FileSettingsSectionName);
            Assert.AreEqual(3, values.Count);
            Assert.AreEqual("one", values[0].Key);
            Assert.AreEqual("two", values[1].Key);
            Assert.AreEqual("three", values[2].Key);
        }

        private void AssertPackageSource(PackageSource ps, string name, string source) {
            Assert.AreEqual(name, ps.Name);
            Assert.AreEqual(source, ps.Source);
        }

        private IPackageSourceProvider CreatePackageSourceProvider(ISettings settings = null) {
            settings = settings ?? new MockUserSettingsManager();
            return new PackageSourceProvider(settings);
        }
    }
}