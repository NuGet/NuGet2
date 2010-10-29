using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuGet {
    internal static class QueryableHelper {
        private static readonly string[] _orderMethods = new[] { "OrderBy", "ThenBy", "OrderByDescending", "ThenByDescending" };       
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

        public static bool IsOrderingMethod(Expression expression) {
            return _orderMethods.Any(method => IsQueryableMethod(expression, method));
        }
    }
}
