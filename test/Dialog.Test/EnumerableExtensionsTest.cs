using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Dialog.Extensions;

namespace NuGet.Dialog.Test {
    [TestClass]
    public class EnumerableExtensionsTest {
        [TestMethod]
        public void DiscintLastNoElements() {
            // Arrange
            var items = Enumerable.Empty<Item>();

            // Act
            var result = items.DistinctLast(new ItemNameComparer()).ToList();

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void DiscintLastOneElement() {
            // Arrange
            var items = new Item[] { new Item { Name = "John", Age = 10 } };

            // Act
            var result = items.DistinctLast(new ItemNameComparer()).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("John", result[0].Name);
            Assert.AreEqual(10, result[0].Age);
        }

        [TestMethod]
        public void DiscintLastThreeSimilarElements() {
            // Arrange
            var items = new Item[] { new Item { Name = "John", Age = 10 },
                                     new Item { Name = "John", Age = 20 },
                                     new Item { Name = "John", Age = 30 }};

            // Act
            var result = items.DistinctLast(new ItemNameComparer()).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("John", result[0].Name);
            Assert.AreEqual(30, result[0].Age);
        }

        [TestMethod]
        public void DiscintLastMultipleSimilarElements() {
            // Arrange
            var items = new Item[] { new Item { Name = "Phil", Age = 1 },
                                     new Item { Name = "John", Age = 10 },
                                     new Item { Name = "John", Age = 20 },
                                     new Item { Name = "John", Age = 30 },
                                     new Item { Name = "David", Age = 10 },
                                     new Item { Name = "David", Age = 20 }};

            // Act
            var result = items.DistinctLast(new ItemNameComparer()).ToList();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("Phil", result[0].Name);
            Assert.AreEqual(1, result[0].Age);
            Assert.AreEqual("John", result[1].Name);
            Assert.AreEqual(30, result[1].Age);
            Assert.AreEqual("David", result[2].Name);
            Assert.AreEqual(20, result[2].Age);
        }

        private class Item {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private class ItemNameComparer : IEqualityComparer<Item> {
            public bool Equals(Item x, Item y) {
                return x.Name.Equals(y.Name);
            }

            public int GetHashCode(Item obj) {
                return obj.Name.GetHashCode();
            }
        }
    }
}
