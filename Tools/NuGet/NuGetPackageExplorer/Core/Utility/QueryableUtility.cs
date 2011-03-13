using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuGet {
    internal static class QueryableUtility {

        public static Expression ReplaceQueryableExpression(IQueryable query, Expression expression) {
            return new ExpressionRewriter(query).Visit(expression);
        }

        private class ExpressionRewriter : ExpressionVisitor {
            private readonly IQueryable _query;
            
            public ExpressionRewriter(IQueryable query) {
                _query = query;
            }

            protected override Expression VisitConstant(ConstantExpression node) {
                // Replace the query at the root of the expression
                if (typeof(IQueryable).IsAssignableFrom(node.Type)) {
                    return _query.Expression;
                }
                return base.VisitConstant(node);
            }
        }
    }
}