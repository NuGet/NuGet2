using System;
using System.Reflection;

namespace NuGet {
    public static class ExceptionUtility {
        public static Exception Unwrap(Exception exception) {
            if (exception == null) {
                throw new ArgumentNullException("exception");
            }

            if (exception.InnerException == null) {
                return exception;
            }

            // Always return the inner exception from a target invocation exception
            if (exception.GetType() == typeof(TargetInvocationException)) {
                return exception.InnerException;
            }

            // Flatten the aggregate before getting the inner exception
            if (exception.GetType() == typeof(AggregateException)) {
                return ((AggregateException)exception).Flatten().InnerException;
            }

            return exception;
        }
    }
}
