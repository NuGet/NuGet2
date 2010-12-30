using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuGet.VisualStudio.Test {

    internal class ModifyTakeExpressionVisitor : ExpressionVisitor {

        private readonly MethodInfo _takeMethod;
        private int _throttleValue;

        public ModifyTakeExpressionVisitor(Type elementType, int throttleValue = 10) {
            // QueryExtensions.GetAll() assumes page size limit is greater than 50
            _throttleValue = Math.Max(throttleValue, 50);
            _takeMethod = typeof(Queryable).GetMethod("Take", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(elementType);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node) {
            // Modify Take() method to take no more than _throttleValue
            if (QueryableUtility.IsQueryableMethod(node, "Take")) {

                // for unit tests, we make sure the second argument to Take() is a constant
                int count = Convert.ToInt32(((ConstantExpression)node.Arguments[1]).Value);
                count = Math.Min(count, _throttleValue);

                return Expression.Call(_takeMethod, Visit(node.Arguments[0]), Expression.Constant(count));
            }
            else {
                return base.VisitMethodCall(node);
            }
        }
    }
}