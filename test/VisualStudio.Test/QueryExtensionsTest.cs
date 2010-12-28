using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.VisualStudio.Test {

    [TestClass]
    public class QueryExtensionsTest {

        [TestMethod]
        public void GetAllReturnsAllItems() {

            // Arrange
            var range = Enumerable.Range(1, 25).AsQueryable();
            var filtered = new ThrottledQueryable<int>(range);

            // Act
            var list = filtered.GetAll(0, null).ToList();

            // Assert
            AssertArray(list, 1, 25);
        }

        [TestMethod]
        public void GetAllReturnsCorrectItemsWhenSkipIsSet() {

            // Arrange
            var range = Enumerable.Range(1, 25).AsQueryable();
            var filtered = new ThrottledQueryable<int>(range);

            // Act
            var list = filtered.GetAll(10, null).ToList();

            // Assert
            AssertArray(list, 11, 15);
        }

        [TestMethod]
        public void GetAllReturnsNothingWhenSkipIsLargerThanTotalNumberOfItems() {

            // Arrange
            var range = Enumerable.Range(1, 5).AsQueryable();
            var filtered = new ThrottledQueryable<int>(range);

            // Act
            var list = filtered.GetAll(6, null).ToList();

            // Assert
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void GetAllReturnsCorrectItemsWhenSkipAndFirstIsSet() {

            // Arrange
            var range = Enumerable.Range(1, 25).AsQueryable();
            var filtered = new ThrottledQueryable<int>(range);

            // Act
            var list = filtered.GetAll(10, 12).ToList();

            // Assert
            AssertArray(list, 11, 12);
        }

        [TestMethod]
        public void GetAllReturnsCorrectItemsWhenSkipAndFirstIsSetAndFirstIsZero() {

            // Arrange
            var range = Enumerable.Range(1, 5).AsQueryable();
            var filtered = new ThrottledQueryable<int>(range);

            // Act
            var list = filtered.GetAll(0, 0).ToList();

            // Assert
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void GetAllReturnsAllItemsWhenFirstIsLargerThanTheNumberOfItems() {

            // Arrange
            var range = Enumerable.Range(1, 15).AsQueryable();
            var filtered = new ThrottledQueryable<int>(range);

            // Act
            var list = filtered.GetAll(0, 20).ToList();

            // Assert
            AssertArray(list, 1, 15);
        }

        [TestMethod]
        public void GetAllReturnsEmptyCollectionWhenTheSourceIsEmpty() {

            // Arrange
            var range = Enumerable.Empty<int>().AsQueryable();
            var filtered = new ThrottledQueryable<int>(range);

            // Act
            var list = filtered.GetAll(5, 20).ToList();

            // Assert
            Assert.AreEqual(0, list.Count);
        }

        private void AssertArray(IList<int> list, int min, int count) {
            Assert.AreEqual(count, list.Count);
            for (int i = 0; i < count; i++) {
                Assert.AreEqual(min + i, list[i]);
            }
        }
    }
}