using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace NuGet.Dialog.Test
{

    public class QueryExtensionsTest
    {
        [Fact]
        public void SortBySortsOnOneItem()
        {
            // Arrange
            var list = (new[] { 
                new MockQueryClass { Id = "B" }, new MockQueryClass { Id = "A" }, new MockQueryClass { Id = "C" }
            }).AsQueryable();

            // Act
            var result = list.SortBy(new[] { "Id" }, ListSortDirection.Ascending);

            // Assert
            Assert.Equal(result.ElementAt(0).Id, "A");
            Assert.Equal(result.ElementAt(1).Id, "B");
            Assert.Equal(result.ElementAt(2).Id, "C");
        }

        [Fact]
        public void SortBySortsOnOnePropertyDescending()
        {
            // Arrange
            var list = (new[] { 
                new MockQueryClass { Id = "A" }, new MockQueryClass { Id = "B" }, new MockQueryClass { Id = "C" }
            }).AsQueryable();

            // Act
            var result = list.SortBy(new[] { "Id" }, ListSortDirection.Descending);

            // Assert
            Assert.Equal(result.ElementAt(0).Id, "C");
            Assert.Equal(result.ElementAt(1).Id, "B");
            Assert.Equal(result.ElementAt(2).Id, "A");
        }

        [Fact]
        public void SortBySortsOnMultiplePropertyAscending()
        {
            // Arrange
            var list = (new[] { 
                new MockQueryClass { Id = "X", Name = "A" }, 
                new MockQueryClass { Id = "Z", Name = "" }, 
                new MockQueryClass { Id = "P", Name = null },
                new MockQueryClass { Id = "Q", Name = "R" }
            }).AsQueryable();

            // Act
            var result = list.SortBy(new[] { "Name", "Id" }, ListSortDirection.Ascending);

            // Assert
            Assert.Equal(result.ElementAt(0).Id, "X");
            Assert.Equal(result.ElementAt(1).Id, "P");
            Assert.Equal(result.ElementAt(2).Id, "Q");
            Assert.Equal(result.ElementAt(3).Id, "Z");
        }

        [Fact]
        public void SortBySortsOnMultiplePropertyDescending()
        {
            // Arrange
            var list = (new[] { 
                new MockQueryClass { Id = "X", Name = "A" }, 
                new MockQueryClass { Id = "Z", Name = "" }, 
                new MockQueryClass { Id = "P", Name = null },
                new MockQueryClass { Id = "Q", Name = "R" }
            }).AsQueryable();

            // Act
            var result = list.SortBy(new[] { "Name", "Id" }, ListSortDirection.Descending);

            // Assert
            Assert.Equal(result.ElementAt(0).Id, "Z");
            Assert.Equal(result.ElementAt(1).Id, "Q");
            Assert.Equal(result.ElementAt(2).Id, "P");
            Assert.Equal(result.ElementAt(3).Id, "X");
        }

        [Fact]
        public void SortBySortsOnMoreThanTwoProperties()
        {
            // Arrange
            var list = (new[] { 
                new MockQueryClass { Id = "X", Name = "A", Description = "D0" }, 
                new MockQueryClass { Id = "Z", Name = "" , Description = null }, 
                new MockQueryClass { Id = "P", Name = null, Description = "" },
                new MockQueryClass { Id = "Q", Name = "R", Description = "D1" }
            }).AsQueryable();

            // Act
            var result = list.SortBy(new[] { "Description", "Name", "Id" }, ListSortDirection.Ascending);

            // Assert
            Assert.Equal(result.ElementAt(0).Id, "X");
            Assert.Equal(result.ElementAt(1).Id, "Q");
            Assert.Equal(result.ElementAt(2).Id, "P");
            Assert.Equal(result.ElementAt(3).Id, "Z");
        }

        [Fact]
        public void GetSortExpressionForSingleParameter()
        {
            // Arrange
            var source = new[] { new MockQueryClass() }.AsQueryable();
            var expected = source.OrderBy(p => p.Id).Expression as MethodCallExpression;

            // Act
            var expression = QueryExtensions.GetSortExpression(source, new[] { "Id" }, ListSortDirection.Ascending);

            AreExpressionsEqual(expected, expression);
        }

        [Fact]
        public void GetSortExpressionForChainedParameter()
        {
            // Arrange
            var source = new[] { new MockQueryClass() }.AsQueryable();
            var expected = source.OrderBy(p => String.Concat(p.Id, p.Name)).Expression as MethodCallExpression;

            // Act
            var expression = QueryExtensions.GetSortExpression(source, new[] { "Id", "Name" }, ListSortDirection.Ascending);

            AreExpressionsEqual(expected, expression);
        }

        [Fact]
        public void GetSortExpressionDescendingForChainedParameter()
        {
            // Arrange
            var source = new[] { new MockQueryClass() }.AsQueryable();
            var expected = source.OrderByDescending(p => String.Concat(p.Name, p.Id)).Expression as MethodCallExpression;

            // Act
            var expression = QueryExtensions.GetSortExpression(source, new[] { "Name", "Id" }, ListSortDirection.Descending);

            AreExpressionsEqual(expected, expression);
        }

        private static void AreExpressionsEqual(MethodCallExpression a, MethodCallExpression b)
        {
            // An expression visitor should be the way to do this, but keeping it simple.

            Assert.Equal(a.Method, b.Method);

            var aLambda = (a.Arguments[1] as UnaryExpression).Operand as LambdaExpression;
            var bLambda = (b.Arguments[1] as UnaryExpression).Operand as LambdaExpression;


            if (aLambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                Assert.Equal((aLambda.Body as MemberExpression).Member, (bLambda.Body as MemberExpression).Member);
            }
            else
            {
                var aConcatCall = aLambda.Body as MethodCallExpression;
                var bConcatCall = bLambda.Body as MethodCallExpression;

                Assert.Equal((aConcatCall.Arguments[0] as MemberExpression).Member, (bConcatCall.Arguments[0] as MemberExpression).Member);
                Assert.Equal((aConcatCall.Arguments[1] as MemberExpression).Member, (bConcatCall.Arguments[1] as MemberExpression).Member);
            }
        }

        public class MockQueryClass
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }
        }
    }
}
