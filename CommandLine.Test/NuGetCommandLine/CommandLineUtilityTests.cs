using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test.NuGetCommandLine {
    [TestClass]
    public class CommandLineUtilityTests {
        [TestMethod]
        public void RemoveNullableFromType_ReturnsNonNullableVerson() {
            // Arrange
            Type expectType = typeof(int);
            // Act
            Type actualType = CommandLineUtility.RemoveNullableFromType(typeof(int?));
            // Assert
            Assert.AreEqual(expectType, actualType);
        }

        [TestMethod]
        public void RemoveNullableFromType_ReturnsSameTypeIfNotNullable() {
            // Arrange
            Type expectType = typeof(int);
            // Act
            Type actualType = CommandLineUtility.RemoveNullableFromType(typeof(int));
            // Assert
            Assert.AreEqual(expectType, actualType);
        }

        [TestMethod]
        public void RemoveNullableFromType_ReturnsSameTypeIfNoNonNullableVerson() {
            // Arrange
            Type expectType = typeof(string);
            // Act
            Type actualType = CommandLineUtility.RemoveNullableFromType(typeof(string));
            // Assert
            Assert.AreEqual(expectType, actualType);
        }

        [TestMethod]
        public void TypeAllowsNulls_ReturnsTrueForNullableTypes() {
            // Act
            bool actual = CommandLineUtility.TypeAllowsNull(typeof(int?));
            // Assert
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void TypeAllowsNulls_ReturnsFalaseForNonNullableTypes() {
            // Act
            bool actual = CommandLineUtility.TypeAllowsNull(typeof(int));
            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ChangeType_ThrowsIfTypeIsNull() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => CommandLineUtility.ChangeType(new object(), null), "type");
        }

        [TestMethod]
        public void ChangeType_ThrowsIfTypeDoesNotAllowNulls() {
            // Act & Assert
            ExceptionAssert.Throws<InvalidCastException>(() => CommandLineUtility.ChangeType(null, typeof(int)));
        }

        [TestMethod]
        public void ChangeType_ReturnsNullIfTypeAllowsNulls() {
            // Act
            var actual = CommandLineUtility.ChangeType(null, typeof(int?));
            // Assert
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void ChangeType_ReturnsValueIfTypesMatch() {
            // Act
            var actual = CommandLineUtility.ChangeType(3, typeof(int));
            // Assert
            Assert.AreEqual(typeof(int), actual.GetType());
            Assert.AreEqual(3, actual);
        }

        [TestMethod]
        public void ChangeType_ReturnsConvertedTypeWhenThereIsAConverterFromTheType() {
            // Act
            var actual = CommandLineUtility.ChangeType("3", typeof(int));
            // Assert
            Assert.AreEqual(typeof(int), actual.GetType());
            Assert.AreEqual(3, actual);
        }

        [TestMethod]
        public void ChangeType_ReturnsConvertedTypeWhenThereIsAConverterFromTheValue() {
            // Act
            var actual = CommandLineUtility.ChangeType(3, typeof(string));
            // Assert
            Assert.AreEqual(typeof(string), actual.GetType());
            Assert.AreEqual("3", actual);
        }

        [TestMethod]
        public void ChangeType_ThrowWhenThereIsNoConverterForEither() {
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => CommandLineUtility.ChangeType(false, typeof(MockClass)));
        }

        [TestMethod]
        public void GetLocalizedString_ThrowsArgumentExceptionForNullType() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => CommandLineUtility.GetLocalizedString(null, "foo"), "resourceType");
        }

        [TestMethod]
        public void GetLocalizedString_ThrowsArgumentExceptionForNullName() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => CommandLineUtility.GetLocalizedString(typeof(string), null), "resourceName");
        }

        [TestMethod]
        public void GetLocalizedString_ThrowsArgumentExceptionForEmptyName() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => CommandLineUtility.GetLocalizedString(typeof(string), ""), "resourceName");
        }

        [TestMethod]
        public void GetLocalizedString_ThrowsIfNoPropteryByResourceName() {
            // Arrage 
            Type resourceType = typeof(MockResourceType);
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => CommandLineUtility.GetLocalizedString(resourceType, "DoesntExist"),
                "The resource type 'NuGet.Test.NuGetCommandLine.CommandLineUtilityTests+MockResourceType' does not have an accessible static property named 'DoesntExist'.");
        }

        [TestMethod]
        public void GetLocalizedString_ThrowsIfPropertyIsNotOfStringType() {
            // Arrage 
            Type resourceType = typeof(MockResourceType);
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => CommandLineUtility.GetLocalizedString(resourceType, "NotValid"),
                "The property 'NotValid' on resource type 'NuGet.Test.NuGetCommandLine.CommandLineUtilityTests+MockResourceType' is not a string type.");
        }

        [TestMethod]
        public void GetLocalizedString_ThrowsIfGetPropertyIsNotAvalible() {
            // Arrage 
            Type resourceType = typeof(MockResourceType);
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => CommandLineUtility.GetLocalizedString(resourceType, "NoGet"),
                "The resource type 'NuGet.Test.NuGetCommandLine.CommandLineUtilityTests+MockResourceType' does not have an accessible get for the 'NoGet' property.");
        }

        [TestMethod]
        public void GetLocalizedString_ReturnsResourceWithValidName() {
            // Arrange
            Type resourceType = typeof(MockResourceType);
            // Act
            var actual = CommandLineUtility.GetLocalizedString(resourceType, "Message");
            // Assert
            Assert.AreEqual("This is a Message.", actual);
        }

        private class MockResourceType {
            public static string Message { get { return "This is a Message."; } }
            public static string MessageTwo { get { return "This is Message Two."; } }
            public static int NotValid { get { return 0; } }
            public static string NoGet { private get { return "No Public Get."; } set{} }
        }

        private class MockClass { }
    }
}
