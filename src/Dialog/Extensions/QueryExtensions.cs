using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NuGet.Dialog.Providers;

namespace NuGet.Dialog.Extensions {
    internal static class QueryExtensions {
        internal static IOrderedQueryable<T> SortBy<T>(this IQueryable<T> source, PackageSortDescriptor descriptor, params Type[] knownTypes) {
            return source.SortBy<T>(descriptor.Name, descriptor.Direction, knownTypes);
        }

        internal static IOrderedQueryable<T> SortBy<T>(this IQueryable<T> source, string propertyName, ListSortDirection direction, params Type[] knownTypes) {
            // Get the property being sorted on
            PropertyInfo propertyInfo = GetProperty(source.ElementType, propertyName, knownTypes);

            Debug.Assert(propertyInfo != null, "Unable to find property");

            // Make a parameter expression with the element type
            ParameterExpression parameter = Expression.Parameter(source.ElementType);

            // Convert to the parameter to the declaring type (we need this in case the property doesn't exist on ElementType, maybe because it's an inherited interface)
            Expression convertExpression = parameter;

            if (source.ElementType != propertyInfo.DeclaringType) {
                convertExpression = Expression.Convert(parameter, propertyInfo.DeclaringType);
            }

            // Get the member access expression 
            MemberExpression property = Expression.Property(convertExpression, propertyInfo);

            // Build the expression p => p.PropertyName
            LambdaExpression lambda = Expression.Lambda(property, parameter);

            // Pick a method based on which way we're ordering
            string methodName = direction == ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";

            Expression methodCallExpression = Expression.Call(typeof(Queryable),
                                                              methodName,
                                                              new Type[] { source.ElementType, property.Type },
                                                              source.Expression,
                                                              Expression.Quote(lambda));

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(methodCallExpression);
        }

        private static PropertyInfo GetProperty(Type type, string propertyName, params Type[] knownTypes) {
            // Try to get the property from the type
            PropertyInfo property = GetPublicProperty(type, propertyName);
            if (property != null) {
                return property;
            }

            // Try the interfaces
            property = (from interfaceType in type.GetInterfaces()
                        select GetProperty(interfaceType, propertyName) into prop
                        where prop != null
                        select prop).FirstOrDefault();
            if (property != null) {
                return property;
            }

            // now try the known types
            foreach (var knownType in knownTypes) {
                if (knownType != null) {
                    property = GetPublicProperty(knownType, propertyName);
                    if (property != null) {
                        return property;
                    }
                }
            }

            return null;
        }

        private static PropertyInfo GetPublicProperty(Type type, string propertyName) {
            return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        }
    }
}
