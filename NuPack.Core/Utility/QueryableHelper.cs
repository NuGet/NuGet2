using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

namespace NuGet {
    internal static class QueryableHelper {
        private static readonly MethodInfo[] _methods = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static);

        private static MethodInfo GetQueryableMethod(Expression expression) {
            if (expression.NodeType == ExpressionType.Call) {
                var call = (MethodCallExpression)expression;
                if (call.Method.IsStatic && call.Method.DeclaringType == typeof(Queryable)) {
                    return call.Method.GetGenericMethodDefinition();
                }
            }
            return null;
        }

        public static bool IsQueryableMethod(Expression expression, string method) {
            return _methods.Where(m => m.Name == method).Contains(GetQueryableMethod(expression));
        }
    }


}
