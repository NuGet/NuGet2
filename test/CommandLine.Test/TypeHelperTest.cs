using System;
using Xunit;

namespace NuGet.Test.NuGetCommandLine
{

    public class TypeHelperTest
    {
        [Fact]
        public void RemoveNullableFromType_ReturnsNonNullableVerson()
        {
            // Arrange
            Type expectType = typeof(int);
            // Act
            Type actualType = TypeHelper.RemoveNullableFromType(typeof(int?));
            // Assert
            Assert.Equal(expectType, actualType);
        }

        [Fact]
        public void RemoveNullableFromType_ReturnsSameTypeIfNotNullable()
        {
            // Arrange
            Type expectType = typeof(int);
            // Act
            Type actualType = TypeHelper.RemoveNullableFromType(typeof(int));
            // Assert
            Assert.Equal(expectType, actualType);
        }

        [Fact]
        public void RemoveNullableFromType_ReturnsSameTypeIfNoNonNullableVerson()
        {
            // Arrange
            Type expectType = typeof(string);
            // Act
            Type actualType = TypeHelper.RemoveNullableFromType(typeof(string));
            // Assert
            Assert.Equal(expectType, actualType);
        }

        [Fact]
        public void TypeAllowsNulls_ReturnsTrueForNullableTypes()
        {
            // Act
            bool actual = TypeHelper.TypeAllowsNull(typeof(int?));
            // Assert
            Assert.True(actual);
        }

        [Fact]
        public void TypeAllowsNulls_ReturnsFalaseForNonNullableTypes()
        {
            // Act
            bool actual = TypeHelper.TypeAllowsNull(typeof(int));
            // Assert
            Assert.False(actual);
        }

        [Fact]
        public void ChangeType_ThrowsIfTypeIsNull()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => TypeHelper.ChangeType(new object(), null), "type");
        }

        [Fact]
        public void ChangeType_ThrowsIfTypeDoesNotAllowNulls()
        {
            // Act & Assert
            ExceptionAssert.Throws<InvalidCastException>(() => TypeHelper.ChangeType(null, typeof(int)));
        }

        [Fact]
        public void ChangeType_ReturnsNullIfTypeAllowsNulls()
        {
            // Act
            var actual = TypeHelper.ChangeType(null, typeof(int?));
            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void ChangeType_ReturnsValueIfTypesMatch()
        {
            // Act
            var actual = TypeHelper.ChangeType(3, typeof(int));
            // Assert
            Assert.Equal(typeof(int), actual.GetType());
            Assert.Equal(3, actual);
        }

        [Fact]
        public void ChangeType_ReturnsConvertedTypeWhenThereIsAConverterFromTheType()
        {
            // Act
            var actual = TypeHelper.ChangeType("3", typeof(int));
            // Assert
            Assert.Equal(typeof(int), actual.GetType());
            Assert.Equal(3, actual);
        }

        [Fact]
        public void ChangeType_ReturnsConvertedTypeWhenThereIsAConverterFromTheValue()
        {
            // Act
            var actual = TypeHelper.ChangeType(3, typeof(string));
            // Assert
            Assert.Equal(typeof(string), actual.GetType());
            Assert.Equal("3", actual);
        }

        [Fact]
        public void ChangeType_ThrowWhenThereIsNoConverterForEither()
        {
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => TypeHelper.ChangeType(false, typeof(MockClass)));
        }

        private class MockClass { }
    }
}
