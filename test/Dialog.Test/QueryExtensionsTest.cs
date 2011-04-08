using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Dialog.Extensions;
using NuGet.Dialog.Providers;

namespace NuGet.Dialog.Test {
    [TestClass]
    public class QueryExtensionsTest {
        [TestMethod]
        public void SortBySortsOnOneItem() {
            // Arrange
            var list = (new[] { 
                new MockQueryClass { Id = "B" }, new MockQueryClass { Id = "A" }, new MockQueryClass { Id = "C" }
            }).AsQueryable();

            // Act
            var result = list.SortBy(new PackageSortDescriptor(null, "Id", ListSortDirection.Ascending));

            // Assert
            Assert.AreEqual(result.ElementAt(0).Id, "A");
            Assert.AreEqual(result.ElementAt(1).Id, "B");
            Assert.AreEqual(result.ElementAt(2).Id, "C");
        }

        [TestMethod]
        public void SortBySortsOnOnePropertyDescending() {
            // Arrange
            var list = (new[] { 
                new MockQueryClass { Id = "A" }, new MockQueryClass { Id = "B" }, new MockQueryClass { Id = "C" }
            }).AsQueryable();

            // Act
            var result = list.SortBy(new PackageSortDescriptor(null, "Id", ListSortDirection.Descending));

            // Assert
            Assert.AreEqual(result.ElementAt(0).Id, "C");
            Assert.AreEqual(result.ElementAt(1).Id, "B");
            Assert.AreEqual(result.ElementAt(2).Id, "A");
        }

        [TestMethod]
        public void SortBySortsOnMultiplePropertyAscending() {
            // Arrange
            var list = (new[] { 
                new MockQueryClass { Id = "X", Name = "A" }, 
                new MockQueryClass { Id = "Z", Name = "" }, 
                new MockQueryClass { Id = "P", Name = null },
                new MockQueryClass { Id = "Q", Name = "R" }
            }).AsQueryable();

            // Act
            var result = list.SortBy(new PackageSortDescriptor(null, new[] { "Name", "Id" }, ListSortDirection.Ascending));

            // Assert
            Assert.AreEqual(result.ElementAt(0).Id, "X");
            Assert.AreEqual(result.ElementAt(1).Id, "P");
            Assert.AreEqual(result.ElementAt(2).Id, "Q");
            Assert.AreEqual(result.ElementAt(3).Id, "Z");
        }

        [TestMethod]
        public void SortBySortsOnMultiplePropertyDescending() {
            // Arrange
            var list = (new[] { 
                new MockQueryClass { Id = "X", Name = "A" }, 
                new MockQueryClass { Id = "Z", Name = "" }, 
                new MockQueryClass { Id = "P", Name = null },
                new MockQueryClass { Id = "Q", Name = "R" }
            }).AsQueryable();

            // Act
            var result = list.SortBy(new PackageSortDescriptor(null, new[] { "Name", "Id" }, ListSortDirection.Descending));

            // Assert
            Assert.AreEqual(result.ElementAt(0).Id, "Z");
            Assert.AreEqual(result.ElementAt(1).Id, "Q");
            Assert.AreEqual(result.ElementAt(2).Id, "P");
            Assert.AreEqual(result.ElementAt(3).Id, "X");
        }

        [TestMethod]
        public void SortBySortsOnMoreThanTwoProperties() {
            // Arrange
            var list = (new[] { 
                new MockQueryClass { Id = "X", Name = "A", Description = "D0" }, 
                new MockQueryClass { Id = "Z", Name = "" , Description = null }, 
                new MockQueryClass { Id = "P", Name = null, Description = "" },
                new MockQueryClass { Id = "Q", Name = "R", Description = "D1" }
            }).AsQueryable();

            // Act
            var result = list.SortBy(new PackageSortDescriptor(null, new[] { "Description", "Name", "Id" }, ListSortDirection.Ascending));

            // Assert
            Assert.AreEqual(result.ElementAt(0).Id, "X");
            Assert.AreEqual(result.ElementAt(1).Id, "Q");
            Assert.AreEqual(result.ElementAt(2).Id, "P");
            Assert.AreEqual(result.ElementAt(3).Id, "Z");
        }

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

            public string Description { get; set; }
        }
    }
}
