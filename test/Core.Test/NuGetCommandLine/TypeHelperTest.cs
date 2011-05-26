using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test.NuGetCommandLine {
    [TestClass]
    public class TypeHelperTest {
        [TestMethod]
        public void RemoveNullableFromType_ReturnsNonNullableVerson() {
            // Arrange
            Type expectType = typeof(int);
            // Act
            Type actualType = TypeHelper.RemoveNullableFromType(typeof(int?));
            // Assert
            Assert.AreEqual(expectType, actualType);
        }

        [TestMethod]
        public void RemoveNullableFromType_ReturnsSameTypeIfNotNullable() {
            // Arrange
            Type expectType = typeof(int);
            // Act
            Type actualType = TypeHelper.RemoveNullableFromType(typeof(int));
            // Assert
            Assert.AreEqual(expectType, actualType);
        }

        [TestMethod]
        public void RemoveNullableFromType_ReturnsSameTypeIfNoNonNullableVerson() {
            // Arrange
            Type expectType = typeof(string);
            // Act
            Type actualType = TypeHelper.RemoveNullableFromType(typeof(string));
            // Assert
            Assert.AreEqual(expectType, actualType);
        }

        [TestMethod]
        public void TypeAllowsNulls_ReturnsTrueForNullableTypes() {
            // Act
            bool actual = TypeHelper.TypeAllowsNull(typeof(int?));
            // Assert
            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void TypeAllowsNulls_ReturnsFalaseForNonNullableTypes() {
            // Act
            bool actual = TypeHelper.TypeAllowsNull(typeof(int));
            // Assert
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ChangeType_ThrowsIfTypeIsNull() {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => TypeHelper.ChangeType(new object(), null), "type");
        }

        [TestMethod]
        public void ChangeType_ThrowsIfTypeDoesNotAllowNulls() {
            // Act & Assert
            ExceptionAssert.Throws<InvalidCastException>(() => TypeHelper.ChangeType(null, typeof(int)));
        }

        [TestMethod]
        public void ChangeType_ReturnsNullIfTypeAllowsNulls() {
            // Act
            var actual = TypeHelper.ChangeType(null, typeof(int?));
            // Assert
            Assert.IsNull(actual);
        }

        [TestMethod]
        public void ChangeType_ReturnsValueIfTypesMatch() {
            // Act
            var actual = TypeHelper.ChangeType(3, typeof(int));
            // Assert
            Assert.AreEqual(typeof(int), actual.GetType());
            Assert.AreEqual(3, actual);
        }

        [TestMethod]
        public void ChangeType_ReturnsConvertedTypeWhenThereIsAConverterFromTheType() {
            // Act
            var actual = TypeHelper.ChangeType("3", typeof(int));
            // Assert
            Assert.AreEqual(typeof(int), actual.GetType());
            Assert.AreEqual(3, actual);
        }

        [TestMethod]
        public void ChangeType_ReturnsConvertedTypeWhenThereIsAConverterFromTheValue() {
            // Act
            var actual = TypeHelper.ChangeType(3, typeof(string));
            // Assert
            Assert.AreEqual(typeof(string), actual.GetType());
            Assert.AreEqual("3", actual);
        }

        [TestMethod]
        public void ChangeType_ThrowWhenThereIsNoConverterForEither() {
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => TypeHelper.ChangeType(false, typeof(MockClass)));
        }

        private class MockClass { }
    }
}
