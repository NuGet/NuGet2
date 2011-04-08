using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class EnumerableExtensionsTest {
        [TestMethod]
        public void DistinctLastNoElements() {
            // Arrange
            var items = Enumerable.Empty<Item>();

            // Act
            var result = items.DistinctLast(new ItemNameComparer(), new ItemAgeComparer()).ToList();

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void DistinctLastOneElement() {
            // Arrange
            var items = new Item[] { new Item { Name = "John", Age = 10 } };

            // Act
            var result = items.DistinctLast(new ItemNameComparer(), new ItemAgeComparer()).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("John", result[0].Name);
            Assert.AreEqual(10, result[0].Age);
        }

        [TestMethod]
        public void DistinctLastThreeSimilarElements() {
            // Arrange
            var items = new Item[] { new Item { Name = "John", Age = 410 },
                                     new Item { Name = "John", Age = 550 },
                                     new Item { Name = "John", Age = 30 }};

            // Act
            var result = items.DistinctLast(new ItemNameComparer(), new ItemAgeComparer()).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("John", result[0].Name);
            Assert.AreEqual(550, result[0].Age);
        }

        [TestMethod]
        public void DistinctLastMultipleSimilarElements() {
            // Arrange
            var items = new Item[] { new Item { Name = "Phil", Age = 1 },
                                     new Item { Name = "John", Age = 40 },
                                     new Item { Name = "John", Age = 20 },
                                     new Item { Name = "John", Age = 30 },
                                     new Item { Name = "David", Age = 10 },
                                     new Item { Name = "David", Age = 20 }};

            // Act
            var result = items.DistinctLast(new ItemNameComparer(), new ItemAgeComparer()).ToList();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("Phil", result[0].Name);
            Assert.AreEqual(1, result[0].Age);
            Assert.AreEqual("John", result[1].Name);
            Assert.AreEqual(40, result[1].Age);
            Assert.AreEqual("David", result[2].Name);
            Assert.AreEqual(20, result[2].Age);
        }


        [TestMethod]
        public void SafeQueryableThrowsIfSourceIsNull() {
            // Arrange
            IEnumerable<int> source = null;

            // Act 
            ExceptionAssert.ThrowsArgNull(() => source.AsSafeQueryable(), "source");
        }

        [TestMethod]
        public void SafeQueryableReturnsOriginalIQueryableWhenNotRewritingQueries() {
            // Arrange
            IQueryable<int> source = Enumerable.Range(0, 4).AsQueryable();

            // Act 
            IQueryable<int> result = source.AsSafeQueryable(rewriteQuery: false);

            // Assert
            Assert.AreEqual(result, source);
        }

        [TestMethod]
        public void SafeQueryableWrapsIEnumerableWhenNotRewritingQueries() {
            // Arrange
            IEnumerable<int> source = Enumerable.Range(0, 4);

            // Act 
            IQueryable<int> result = source.AsSafeQueryable(rewriteQuery: false);

            // Assert
            Assert.AreEqual(result.GetType(), typeof(EnumerableQuery<int>));
        }

        [TestMethod]
        public void SafeQueryableReturnsSafeEnumerableQueryWhenRewriting() {
            // Arrange
            IEnumerable<int> source = Enumerable.Range(0, 4);

            // Act 
            IQueryable<int> result = source.AsSafeQueryable(rewriteQuery: true);

            // Assert
            Assert.AreEqual(result.GetType(), typeof(SafeEnumerableQuery<int>));
        }

        private class Item {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private class ItemAgeComparer : IComparer<Item> {
            public int Compare(Item x, Item y) {
                return x.Age.CompareTo(y.Age);
            }
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
