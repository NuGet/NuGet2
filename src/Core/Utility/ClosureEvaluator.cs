using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NuGet {
    /// <summary>
    /// This class walks an expression tree and replaces compiler generated closure member accesses with thier value.
    /// </summary>
    internal class ClosureEvaluator : ExpressionVisitor {
        // For unit testing. We want to circumvent the assembly check during unit testing since
        // closures will be generated in that assembly.
        private bool _checkAssemly;

        internal ClosureEvaluator(bool checkAssembly = true) {
            _checkAssemly = checkAssembly;
        }

        protected override Expression VisitMember(MemberExpression node) {
            if (IsGeneratedClosureMember(node)) {
                var constantExpression = (ConstantExpression)node.Expression;
                var fieldInfo = (FieldInfo)node.Member;
                // Evaluate the closure member
                return Expression.Constant(GetValue(node, fieldInfo, constantExpression.Value));
            }
            return base.VisitMember(node);
        }

        private object GetValue(MemberExpression node, FieldInfo fieldInfo, object obj) {
            if (_checkAssemly) {
                Type parentType = node.Expression.Type.DeclaringType;
                Debug.Assert(parentType != null, "Not in a compiler generated closure type");

                // Since the closure class is private sealed, we're going to look for an eval method on that class
                // where it's ok to look up field info.
                MethodInfo evalMethodInfo = parentType.GetMethod("Eval", BindingFlags.NonPublic | BindingFlags.Static);
                Debug.Assert(evalMethodInfo != null, "Eval method cannot be found. Please add and Eval(FieldInfo info, object value) to " + parentType.FullName);

                // Invoke that method
                return evalMethodInfo.Invoke(null, new object[] { fieldInfo, obj });
            }

            // This only happens in the unit test
            return fieldInfo.GetValue(obj);
        }

        protected override Expression VisitConstant(ConstantExpression node) {
            return base.VisitConstant(node);
        }

        private bool IsGeneratedClosureMember(MemberExpression node) {
            // Closure types are internal classes that are compiler generated in our own assembly
            return node.Expression != null &&
                   node.Member != null &&
                   node.Expression.NodeType == ExpressionType.Constant &&
                   node.Member.MemberType == MemberTypes.Field &&
                   !node.Expression.Type.IsVisible &&
                   CheckAssembly(node.Member) &&
                   IsCompilerGenerated(node.Expression.Type);
        }

        private bool CheckAssembly(MemberInfo member) {
            if (_checkAssemly) {
                // Make sure we're in our assembly
                return member.DeclaringType.Assembly == typeof(ClosureEvaluator).Assembly;
            }
            // This is only the case for unit tests
            return true;
        }

        private static bool IsCompilerGenerated(Type type) {
            return type.GetCustomAttributes(inherit: true)
                       .OfType<CompilerGeneratedAttribute>()
                       .Any();
        }
    }
}
