using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class BufferedEnumerableTest {
        public void CtorThrowsIfSourceIsNull() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => new BufferedEnumerable<object>(null, 100), "source");
        }

        [TestMethod]
        public void TakingMoreThanBufferSizesReturnsItems() {
            // Arrange
            var e = new BufferedEnumerable<int>(Enumerable.Range(0, 10000).AsQueryable(), 5);

            // Act
            var items = e.Take(20).ToList();

            // Assert
            Assert.AreEqual(20, items.Count);
        }

        [TestMethod]
        public void BufferedEnumeratorTakingLessThanBufferSizeOnlyQueriesSourceOnce() {
            // Arrange
            var state = new BufferedEnumerable<int>.QueryState<int>(5);
            var query = Enumerable.Range(0, 10000).AsQueryable();
            var e = new BufferedEnumerable<int>.BufferedEnumerator<int>(state, query, 5);
            e.Reset();

            // Act
            for (int i = 0; i < 4; i++) {
                e.MoveNext();
            }

            // Assert
            Assert.AreEqual(5, state.Cache.Count);
        }

        [TestMethod]
        public void BufferedEnumeratorTakingMoreThanBufferSizeQueriesSourceMoreThanOnce() {
            // Arrange
            var state = new BufferedEnumerable<int>.QueryState<int>(5);
            var query = Enumerable.Range(0, 10000).AsQueryable();
            var e = new BufferedEnumerable<int>.BufferedEnumerator<int>(state, query, 5);
            e.Reset();

            // Act
            for (int i = 0; i < 6; i++) {
                e.MoveNext();
            }

            // Assert
            Assert.AreEqual(10, state.Cache.Count);
        }


        [TestMethod]
        public void IfNoMoreItemsInSourceSetsIsEmpty() {
            // Arrange
            var state = new BufferedEnumerable<int>.QueryState<int>(5);
            var query = Enumerable.Range(0, 5).AsQueryable();
            var e = new BufferedEnumerable<int>.BufferedEnumerator<int>(state, query, 1);
            e.Reset();

            // Act
            for (int i = 0; i < 5; i++) {
                e.MoveNext();
            }

            // Assert
            Assert.IsTrue(e.IsEmpty);
            Assert.AreEqual(5, state.Cache.Count);
        }
    }
}
