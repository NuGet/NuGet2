using System;
using Xunit;
using System.Resources;

namespace NuGet.Test.NuGetCommandLine
{

    public class ResourceHelperTests
    {

        [Fact]
        public void GetLocalizedString_ThrowsArgumentExceptionForNullType()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgNull(() => ResourceHelper.GetLocalizedString(null, "foo"), "resourceType");
        }

        [Fact]
        public void GetLocalizedString_ThrowsArgumentExceptionForNullName()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => ResourceHelper.GetLocalizedString(typeof(string), null), "resourceNames");
        }

        [Fact]
        public void GetLocalizedString_ThrowsArgumentExceptionForEmptyName()
        {
            // Act & Assert
            ExceptionAssert.ThrowsArgNullOrEmpty(() => ResourceHelper.GetLocalizedString(typeof(string), ""), "resourceNames");
        }

        [Fact]
        public void GetLocalizedString_ThrowsIfNoPropteryByResourceName()
        {
            // Arrange 
            Type resourceType = typeof(MockResourceType);
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => ResourceHelper.GetLocalizedString(resourceType, "DoesntExist"),
                "The resource type 'NuGet.Test.MockResourceType' does not have an accessible static property named 'DoesntExist'.");
        }

        [Fact]
        public void GetLocalizedString_ThrowsIfResourceManagerIsNotAProperty()
        {
            // Arrange 
            Type resourceType = typeof(BadResourceType);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => ResourceHelper.GetLocalizedString(resourceType, "NoGet"),
                "The resource type 'NuGet.Test.NuGetCommandLine.ResourceHelperTests+BadResourceType' does not have an accessible static property named 'ResourceManager'.");
        }

        [Fact]
        public void GetLocalizedString_ThrowsIfGetPropertyIsNotAvalible()
        {
            // Arrange 
            Type resourceType = typeof(ResourceTypeWithNoGetter);
            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => ResourceHelper.GetLocalizedString(resourceType, "NoGet"),
                "The resource type 'NuGet.Test.NuGetCommandLine.ResourceHelperTests+ResourceTypeWithNoGetter' does not have an accessible static property named 'ResourceManager'.");
        }

        [Fact]
        public void GetLocalizedString_ReturnsResourceWithValidName()
        {
            // Arrange
            Type resourceType = typeof(MockResourceType);
            // Act
            var actual = ResourceHelper.GetLocalizedString(resourceType, "Message");
            // Assert
            Assert.Equal("This is a Message.", actual);
        }

        private class BadResourceType
        {

        }

        private class ResourceTypeWithNoGetter
        {
            public static ResourceManager ResourceManager { set { } } 
        }


    }
}
