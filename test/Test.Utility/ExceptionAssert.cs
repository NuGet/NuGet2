using System;
using Microsoft.Internal.Web.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    public static class ExceptionAssert {
        private const string ArgumentExceptionMessageFormat = "{0}\r\nParameter name: {1}";

        public static void Throws<TException>(Action act) where TException : Exception {
            Throws<TException>(act, ex => true);
        }

        public static void Throws<TException>(Action act, Func<TException,bool> condition) where TException : Exception {
            Exception ex = Capture.Exception(act);
            Assert.IsNotNull(ex, "The expected exception was not thrown");
            Assert.IsInstanceOfType(ex, typeof(TException), "The exception thrown was not of the expected type");
            Assert.IsTrue(condition((TException)ex), String.Format(@"Exception did not match the specified condition
Actual Exception: {0}", ex));
        }

        public static void Throws<TException>(Action action, string expectedMessage) where TException : Exception {
            Throws<TException>(action, ex => String.Equals(ex.Message, expectedMessage, StringComparison.Ordinal));
        }

        public static void ThrowsArgNull(Action act, string paramName) {
            Throws<ArgumentNullException>(act, CreateArgNullChecker(paramName));
        }

        public static void ThrowsArgNullOrEmpty(Action act, string paramName) {
            ThrowsArgumentException<ArgumentException>(act, paramName, CommonResources.Argument_Cannot_Be_Null_Or_Empty);
        }

        public static void ThrowsArgEmpty(Action act, string paramName) {
            ThrowsArgumentException<ArgumentException>(act, paramName, CommonResources.Argument_Must_Be_Null_Or_Non_Empty);
        }

        public static void ThrowsArgGreaterThan(Action act, string paramName, string value) {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, string.Format(CommonResources.Argument_Must_Be_GreaterThan, value));
        }

        public static void ThrowsArgGreaterThanOrEqualTo(Action act, string paramName, string value) {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, string.Format(CommonResources.Argument_Must_Be_GreaterThanOrEqualTo, value));
        }

        public static void ThrowsArgLessThan(Action act, string paramName, string value) {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, string.Format(CommonResources.Argument_Must_Be_LessThan, value));
        }

        public static void ThrowsArgLessThanOrEqualTo(Action act, string paramName, string value) {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, string.Format(CommonResources.Argument_Must_Be_LessThanOrEqualTo, value));
        }

        public static void ThrowsEnumArgOutOfRange<TEnumType>(Action act, string paramName) {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, String.Format(CommonResources.Argument_Must_Be_Enum_Member,
                                                                 typeof(TEnumType).Name));
        }

        public static void ThrowsArgOutOfRange(Action act, string paramName, object minimum, object maximum, bool equalAllowed) {
            ThrowsArgumentException<ArgumentOutOfRangeException>(act, paramName, BuildOutOfRangeMessage(paramName, minimum, maximum, equalAllowed));
        }

        internal static Func<ArgumentNullException, bool> CreateArgNullChecker(string paramName) {
            return ex => ex.ParamName.Equals(paramName);
        }

        private static string BuildOutOfRangeMessage(string paramName, object minimum, object maximum, bool equalAllowed) {
            if (minimum == null) {
                return String.Format(equalAllowed ? CommonResources.Argument_Must_Be_LessThanOrEqualTo : CommonResources.Argument_Must_Be_LessThan, maximum);
            }
            else if (maximum == null) {
                return String.Format(equalAllowed ? CommonResources.Argument_Must_Be_GreaterThanOrEqualTo : CommonResources.Argument_Must_Be_GreaterThan, minimum);
            }
            else {
                return String.Format(CommonResources.Argument_Must_Be_Between, minimum, maximum);
            }
        }

        public static void ThrowsArgumentException(Action act, string message) {
            ThrowsArgumentException<ArgumentException>(act, message);
        }

        public static void ThrowsArgumentException<TArgException>(Action act, string message) where TArgException : ArgumentException {
            Throws<TArgException>(act, ex =>
                ex.Message.Equals(message));
        }

        public static void ThrowsArgumentException(Action act, string paramName, string message) {
            ThrowsArgumentException<ArgumentException>(act, paramName, message);
        }

        public static void ThrowsArgumentException<TArgException>(Action act, string paramName, string message) where TArgException : ArgumentException {
            Throws<TArgException>(act, ex =>
                ex.ParamName.Equals(paramName) &&
                ex.Message.Equals(String.Format(ArgumentExceptionMessageFormat, message, paramName)));
        }
    }
}
