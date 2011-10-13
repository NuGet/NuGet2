using System;
using Microsoft.Internal.Web.Utils;
using Xunit;

namespace NuGet.Test
{
    public static class ExceptionAssert
    {
        private const string ArgumentExceptionMessageFormat = "{0}\r\nParameter name: {1}";

        public static void Throws<TException>(Assert.ThrowsDelegate act) where TException : Exception
        {
            Throws<TException>(act, ex => true);
        }

        public static void Throws<TException>(Assert.ThrowsDelegate act, Func<TException, bool> condition) where TException : Exception
        {
            Exception ex = Record.Exception(act);
            Assert.NotNull(ex);
            TException tex = Assert.IsAssignableFrom<TException>(ex);
            Assert.True(condition(tex), String.Format(@"Exception did not match the specified condition
Actual Exception: {0}", ex));
        }

        public static void Throws<TException>(Assert.ThrowsDelegate action, string expectedMessage) where TException : Exception
        {
            Throws<TException>(action, ex => String.Equals(ex.Message, expectedMessage, StringComparison.Ordinal));
        }

        public static void ThrowsArgNull(Assert.ThrowsDelegate act, string paramName)
        {
            Throws<ArgumentNullException>(act, CreateArgNullChecker(paramName));
        }

        public static void ThrowsArgNullOrEmpty(Assert.ThrowsDelegate act, string paramName)
        {
            ThrowsArgumentException<ArgumentException>(act, paramName, CommonResources.Argument_Cannot_Be_Null_Or_Empty);
        }

        public static void ThrowsArgEmpty(Assert.ThrowsDelegate act, string paramName)
        {
            ThrowsArgumentException<ArgumentException>(act, paramName, CommonResources.Argument_Must_Be_Null_Or_Non_Empty);
        }

        public static void ThrowsArgGreaterThan(Assert.ThrowsDelegate act, string paramName, string value)
        {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, string.Format(CommonResources.Argument_Must_Be_GreaterThan, value));
        }

        public static void ThrowsArgGreaterThanOrEqualTo(Assert.ThrowsDelegate act, string paramName, string value)
        {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, string.Format(CommonResources.Argument_Must_Be_GreaterThanOrEqualTo, value));
        }

        public static void ThrowsArgLessThan(Assert.ThrowsDelegate act, string paramName, string value)
        {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, string.Format(CommonResources.Argument_Must_Be_LessThan, value));
        }

        public static void ThrowsArgLessThanOrEqualTo(Assert.ThrowsDelegate act, string paramName, string value)
        {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, string.Format(CommonResources.Argument_Must_Be_LessThanOrEqualTo, value));
        }

        public static void ThrowsEnumArgOutOfRange<TEnumType>(Assert.ThrowsDelegate act, string paramName)
        {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, String.Format(CommonResources.Argument_Must_Be_Enum_Member,
                                                                 typeof(TEnumType).Name));
        }

        public static void ThrowsArgOutOfRange(Assert.ThrowsDelegate act, string paramName, object minimum, object maximum, bool equalAllowed)
        {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, BuildOutOfRangeMessage(paramName, minimum, maximum, equalAllowed));
        }

        internal static Func<ArgumentNullException, bool> CreateArgNullChecker(string paramName)
        {
            return ex => ex.ParamName.Equals(paramName);
        }

        private static string BuildOutOfRangeMessage(string paramName, object minimum, object maximum, bool equalAllowed)
        {
            if (minimum == null)
            {
                return String.Format(equalAllowed ? CommonResources.Argument_Must_Be_LessThanOrEqualTo : CommonResources.Argument_Must_Be_LessThan, maximum);
            }
            else if (maximum == null)
            {
                return String.Format(equalAllowed ? CommonResources.Argument_Must_Be_GreaterThanOrEqualTo : CommonResources.Argument_Must_Be_GreaterThan, minimum);
            }
            else
            {
                return String.Format(CommonResources.Argument_Must_Be_Between, minimum, maximum);
            }
        }

        public static void ThrowsArgumentException(Assert.ThrowsDelegate act, string message)
        {
            ThrowsArgumentException<ArgumentException>(act, message);
        }

        public static void ThrowsArgumentException<TArgException>(Assert.ThrowsDelegate act, string message) where TArgException : ArgumentException
        {
            Throws<TArgException>(act, ex =>
                ex.Message.Equals(message));
        }

        public static void ThrowsArgumentException(Assert.ThrowsDelegate act, string paramName, string message)
        {
            ThrowsArgumentException<ArgumentException>(act, paramName, message);
        }

        public static void ThrowsArgumentException<TArgException>(Assert.ThrowsDelegate act, string paramName, string message) where TArgException : ArgumentException
        {
            Throws<TArgException>(act, ex =>
                ex.ParamName.Equals(paramName) &&
                ex.Message.Equals(String.Format(ArgumentExceptionMessageFormat, message, paramName)));
        }
    }
}
