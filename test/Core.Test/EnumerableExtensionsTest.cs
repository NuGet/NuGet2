using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NuGet.Test {
    
    public class EnumerableExtensionsTest {
        [Fact]
        public void DistinctLastNoElements() {
            // Arrange
            var items = Enumerable.Empty<Item>();

            // Act
            var result = items.DistinctLast(new ItemNameComparer(), new ItemAgeComparer()).ToList();

            // Assert
            Assert.Equal(0, result.Count);
        }

        [Fact]
        public void DistinctLastOneElement() {
            // Arrange
            var items = new Item[] { new Item { Name = "John", Age = 10 } };

            // Act
            var result = items.DistinctLast(new ItemNameComparer(), new ItemAgeComparer()).ToList();

            // Assert
            Assert.Equal(1, result.Count);
            Assert.Equal("John", result[0].Name);
            Assert.Equal(10, result[0].Age);
        }

        [Fact]
        public void DistinctLastThreeSimilarElements() {
            // Arrange
            var items = new Item[] { new Item { Name = "John", Age = 410 },
                                     new Item { Name = "John", Age = 550 },
                                     new Item { Name = "John", Age = 30 }};

            // Act
            var result = items.DistinctLast(new ItemNameComparer(), new ItemAgeComparer()).ToList();

            // Assert
            Assert.Equal(1, result.Count);
            Assert.Equal("John", result[0].Name);
            Assert.Equal(550, result[0].Age);
        }

        [Fact]
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
            Assert.Equal(3, result.Count);
            Assert.Equal("Phil", result[0].Name);
            Assert.Equal(1, result[0].Age);
            Assert.Equal("John", result[1].Name);
            Assert.Equal(40, result[1].Age);
            Assert.Equal("David", result[2].Name);
            Assert.Equal(20, result[2].Age);
        }


        [Fact]
        public void SafeQueryableThrowsIfSourceIsNull() {
            // Arrange
            IEnumerable<int> source = null;

            // Act 
            ExceptionAssert.ThrowsArgNull(() => source.AsSafeQueryable(), "source");
        }

        [Fact]
        public void SafeQueryableReturnsOriginalIQueryableWhenNotRewritingQueries() {
            // Arrange
            IQueryable<int> source = Enumerable.Range(0, 4).AsQueryable();

            // Act 
            IQueryable<int> result = source.AsSafeQueryable(rewriteQuery: false);

            // Assert
            Assert.Equal(result, source);
        }

        [Fact]
        public void SafeQueryableWrapsIEnumerableWhenNotRewritingQueries() {
            // Arrange
            IEnumerable<int> source = Enumerable.Range(0, 4);

            // Act 
            IQueryable<int> result = source.AsSafeQueryable(rewriteQuery: false);

            // Assert
            Assert.Equal(result.GetType(), typeof(EnumerableQuery<int>));
        }

        [Fact]
        public void SafeQueryableReturnsSafeEnumerableQueryWhenRewriting() {
            // Arrange
            IEnumerable<int> source = Enumerable.Range(0, 4);

            // Act 
            IQueryable<int> result = source.AsSafeQueryable(rewriteQuery: true);

            // Assert
            Assert.Equal(result.GetType(), typeof(SafeEnumerableQuery<int>));
        }

        [Fact]
        public void SafeIterateWithFailingElementAtTheBeginningOfSequence() {
            // Arrange
            var enumerable = Enumerable.Range(0, 4).Select(e => {
                if (e == 0) {
                    throw new Exception();
                }
                return e * e;
            });

            // Act
            var result = EnumerableExtensions.SafeIterate(enumerable);

            // Assert
            Assert.Equal(new [] { 1, 4, 9 }, result.ToArray());
        }

        [Fact]
        public void SafeIterateWithFailingElementInMiddleOfSequence() {
            // Arrange
            var enumerable = Enumerable.Range(0, 4).Select(e => {
                if (e == 1 || e == 3) {
                    throw new Exception();
                }
                return e * e;
            });

            // Act
            var result = EnumerableExtensions.SafeIterate(enumerable);

            // Assert
            Assert.Equal(new[] { 0, 4 }, result.ToArray());
        }

        [Fact]
        public void SafeIterateWithFailingElementAtEndOfSequence() {
            // Arrange
            var enumerable = Enumerable.Range(0, 4).Select(e => {
                if (e == 3) {
                    throw new Exception();
                }
                return e * e;
            });

            // Act
            var result = EnumerableExtensions.SafeIterate(enumerable);

            // Assert
            Assert.Equal(new[] { 0, 1, 4 }, result.ToArray());
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
