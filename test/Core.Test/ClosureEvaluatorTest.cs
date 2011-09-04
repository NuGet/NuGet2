using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace NuGet.Test {
    
    public class ClosureEvaluatorQueryTest {
        [Fact]
        public void ClosureEvaluatorReplacesClosureVariablesWithTheirValue() {
            // Arrange
            int value = 2;
            var query = from i in new[] { 1, 2, 3 }.AsSafeQueryable()
                        where i > value
                        select i;

            Expression expression = new ClosureEvaluator(checkAssembly: false).Visit(query.Expression);
            // Assert
            Assert.Equal("System.Int32[].Where(i => (i > 2))", expression.ToString());
        }
    }
}
