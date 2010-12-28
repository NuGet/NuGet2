using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class EnumerableExtensionsTest {

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
    }
}
