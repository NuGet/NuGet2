using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuGet.Dialog
{
    public static class QueryExtensions
    {
        public static IOrderedQueryable<T> SortBy<T>(this IQueryable<T> source, IEnumerable<string> sortProperties, ListSortDirection direction, params Type[] knownTypes)
        {
            var sortExpression = GetSortExpression(source, sortProperties, direction, knownTypes);
            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(sortExpression);
        }

        internal static MethodCallExpression GetSortExpression<T>(this IQueryable<T> source, IEnumerable<string> sortProperties, ListSortDirection direction, params Type[] knownTypes)
        {
            Debug.Assert(sortProperties != null && sortProperties.Any());

            // Get the property being sorted on
            PropertyInfo propertyInfo = GetProperty(source.ElementType, sortProperties.First(), knownTypes);

            Debug.Assert(propertyInfo != null, "Unable to find property");

            // Make a parameter expression with the element type
            ParameterExpression parameter = Expression.Parameter(source.ElementType);

            // Convert to the parameter to the declaring type (we need this in case the property doesn't exist on ElementType, maybe because it's an inherited interface)
            Expression convertExpression = parameter;

            if (source.ElementType != propertyInfo.DeclaringType)
            {
                convertExpression = Expression.Convert(parameter, propertyInfo.DeclaringType);
            }

            // Get the member access expression 
            Expression expression = Expression.Property(convertExpression, propertyInfo);

            MethodInfo concatMethod = typeof(string).GetMethod("Concat", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string) }, null);
            // Build the expression p => p.PropertyName1 + p.PropertyName2 if the IEnumerable contains more than one item.
            foreach (var item in sortProperties.Skip(1))
            {
                propertyInfo = GetProperty(source.ElementType, item);
                Debug.Assert(propertyInfo.PropertyType == typeof(string), "Chaining only works for strings");
                expression = Expression.Call(concatMethod,
                    expression,
                    Expression.Property(convertExpression, propertyInfo)
                );
            }

            // Build the expression p => p.PropertyName or p => String.Concat(p.Property1, p.Property2, ... )
            LambdaExpression lambda = Expression.Lambda(expression, parameter);

            // Pick a method based on which way we're ordering
            string methodName = direction == ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";

            return Expression.Call(typeof(Queryable),
                        methodName,
                        new Type[] { source.ElementType, expression.Type },
                        source.Expression,
                        Expression.Quote(lambda));
        }


        private static PropertyInfo GetProperty(Type type, string propertyName, params Type[] knownTypes)
        {
            // Try to get the property from the type
            PropertyInfo property = GetPublicProperty(type, propertyName);
            if (property != null)
            {
                return property;
            }

            // Try the interfaces
            property = (from interfaceType in type.GetInterfaces()
                        select GetProperty(interfaceType, propertyName) into prop
                        where prop != null
                        select prop).FirstOrDefault();
            if (property != null)
            {
                return property;
            }

            // now try the known types
            foreach (var knownType in knownTypes)
            {
                if (knownType != null)
                {
                    property = GetPublicProperty(knownType, propertyName);
                    if (property != null)
                    {
                        return property;
                    }
                }
            }

            return null;
        }

        private static PropertyInfo GetPublicProperty(Type type, string propertyName)
        {
            return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        }
    }
}
