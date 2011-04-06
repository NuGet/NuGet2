using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Dialog.Extensions;

namespace NuGet.Dialog.Test {
    [TestClass]
    public class QueryExtensionsTest {
        [TestMethod]
        public void GetSortExpressionForSingleParameter() {
            // Arrange
            var source = new[] { new MockQueryClass() }.AsQueryable();
            var expected = source.OrderBy(p => p.Id).Expression as MethodCallExpression;

            // Act
            var expression = QueryExtensions.GetSortExpression(source, new[] { "Id" }, ListSortDirection.Ascending);

            AreExpressionsEqual(expected, expression);
        }

        [TestMethod]
        public void GetSortExpressionForChainedParameter() {
            // Arrange
            var source = new[] { new MockQueryClass() }.AsQueryable();
            var expected = source.OrderBy(p => String.Concat(p.Id, p.Name)).Expression as MethodCallExpression;

            // Act
            var expression = QueryExtensions.GetSortExpression(source, new[] { "Id", "Name" }, ListSortDirection.Ascending);

            AreExpressionsEqual(expected, expression);
        }

        [TestMethod]
        public void GetSortExpressionDescendingForChainedParameter() {
            // Arrange
            var source = new[] { new MockQueryClass() }.AsQueryable();
            var expected = source.OrderByDescending(p => String.Concat(p.Name, p.Id)).Expression as MethodCallExpression;

            // Act
            var expression = QueryExtensions.GetSortExpression(source, new[] { "Name", "Id" }, ListSortDirection.Descending);

            AreExpressionsEqual(expected, expression);
        }

        private static void AreExpressionsEqual(MethodCallExpression a, MethodCallExpression b) {
            // An expression visitor should be the way to do this, but keeping it simple.

            Assert.AreEqual(a.Method, b.Method);

            var aLambda = (a.Arguments[1] as UnaryExpression).Operand as LambdaExpression;
            var bLambda = (b.Arguments[1] as UnaryExpression).Operand as LambdaExpression;


            if (aLambda.Body.NodeType == ExpressionType.MemberAccess) {
                Assert.AreEqual((aLambda.Body as MemberExpression).Member, (bLambda.Body as MemberExpression).Member);
            }
            else {
                var aConcatCall = aLambda.Body as MethodCallExpression;
                var bConcatCall = bLambda.Body as MethodCallExpression;

                Assert.AreEqual((aConcatCall.Arguments[0] as MemberExpression).Member, (bConcatCall.Arguments[0] as MemberExpression).Member);
                Assert.AreEqual((aConcatCall.Arguments[1] as MemberExpression).Member, (bConcatCall.Arguments[1] as MemberExpression).Member);
            }
        }

        public class MockQueryClass {
            public string Id { get; set; }

            public string Name { get; set; }
        }
    }
}
