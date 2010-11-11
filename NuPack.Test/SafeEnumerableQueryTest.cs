using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class SafeEnumerableQueryTest {
        [TestMethod]
        public void SafeEnumerableReplacesClosureVariablesWithTheirValue() {
            // Arrange
            int value = 2;
            var query = from i in new[] { 1, 2, 3 }.AsSafeQueryable()
                        where i > value
                        select i;

            // Assert
            Assert.AreEqual("System.Int32[].Where(i => (i > 2))", ((SafeEnumerableQuery<int>)query).InnerExpression.ToString());
        }

        [TestMethod]
        public void SafeEnumerableWithEagerQuery() {
            // Arrange
            int value = 2;
            var count = (from i in new[] { 1, 2, 3, 4 }.AsSafeQueryable()
                         where i > value
                         select i).Count();

            // Assert
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void SafeEnumerableWithProjection() {
            // Arrange
            int value = 2;
            var query = from i in new[] { 1, 2, 3 }.AsSafeQueryable()
                         where i > value
                         select new {
                             Value = value + 1
                         };

            // Assert
            Assert.AreEqual("System.Int32[].Where(i => (i > 2)).Select(i => new <>f__AnonymousType0`1(Value = (2 + 1)))", ((dynamic)query).InnerExpression.ToString());
        }
    }
}
