using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class ClosureEvaluatorQueryTest {
        [TestMethod]
        public void ClosureEvaluatorReplacesClosureVariablesWithTheirValue() {
            // Arrange
            int value = 2;
            var query = from i in new[] { 1, 2, 3 }.AsSafeQueryable()
                        where i > value
                        select i;

            Expression expression = new ClosureEvaluator(checkAssembly: false).Visit(query.Expression);
            // Assert
            Assert.AreEqual("System.Int32[].Where(i => (i > 2))", expression.ToString());
        }
    }
}
