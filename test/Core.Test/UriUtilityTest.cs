using System;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class UriUtilityTest
    {
        [Theory]
        [InlineData(new object[] { "http://nuget.org/api/v2", "http://nuget.org/api/v2" })]
        [InlineData(new object[] { "http://nuget.org/api/v2/", "http://nuget.org/api/v2" })]
        [InlineData(new object[] { "http://nuget.org:80/api/v2/", "http://nuget.org/api/v2" })]
        [InlineData(new object[] { "http://nuget.org/api/v2", "http://nuget.org/api/v2?test" })]
        [InlineData(new object[] { "http://www.nuget.org/api/v2", "http://www.nuget.org/api/v2" })]
        [InlineData(new object[] { "http://www.nuget.org/api/v2/", "http://www.nuget.org/api/v2" })]
        public void EqualsReturnsTrueForMatchingUris(string uriString1, string uriString2)
        {
            // Arrange
            var uri1 = new Uri(uriString1);
            var uri2 = new Uri(uriString2);

            // Act
            var result = UriUtility.UriEquals(uri1, uri2);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(new object[] { "http://nuget.org/api/v2", "http://nuget.org/api/v2/$metadata" })]
        [InlineData(new object[] { "http://nuget.org/api/v2/", "http://nuget.org/api/v2/$metadata" })]
        [InlineData(new object[] { "http://nuget.org/api/v2", "http://nuget.org/api/v2/$metadata/" })]
        [InlineData(new object[] { "http://nuget.org/api/v2/", "http://nuget.org/api/v2/$metadata/" })]
        [InlineData(new object[] { "http://nuget.org:80/api/v2", "http://nuget.org/api/v2/$metadata" })]
        [InlineData(new object[] { "http://nuget.org:80/api/v2/", "http://nuget.org/api/v2/$metadata" })]
        [InlineData(new object[] { "http://nuget.org:80/api/v2", "http://nuget.org/api/v2/$metadata/" })]
        [InlineData(new object[] { "http://nuget.org:80/api/v2/", "http://nuget.org/api/v2/$metadata/" })]
        [InlineData(new object[] { "http://nuget.org/api/v2/$metadata", "http://nuget.org/api/v2" })]
        [InlineData(new object[] { "http://nuget.org/api/v2/$metadata", "http://nuget.org/api/v2/" })]
        [InlineData(new object[] { "http://nuget.org/api/v2/$metadata/", "http://nuget.org/api/v2" })]
        [InlineData(new object[] { "http://nuget.org/api/v2/$metadata/", "http://nuget.org/api/v2/" })]
        [InlineData(new object[] { "http://nuget.org:80/api/v2/$metadata", "http://nuget.org/api/v2" })]
        [InlineData(new object[] { "http://nuget.org:80/api/v2/$metadata", "http://nuget.org/api/v2/" })]
        [InlineData(new object[] { "http://nuget.org:80/api/v2/$metadata/", "http://nuget.org/api/v2" })]
        [InlineData(new object[] { "http://nuget.org:80/api/v2/$metadata/", "http://nuget.org/api/v2/" })]
        public void EqualsReturnsTrueForODataSpecificUris(string uriString1, string uriString2)
        {
            // Arrange
            var uri1 = new Uri(uriString1);
            var uri2 = new Uri(uriString2);

            // Act
            var result = UriUtility.UriEquals(uri1, uri2);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(new object[] { "http://nuget.org/api/v1", "http://nuget.org/api/v2" })]
        [InlineData(new object[] { "https://nuget.org/api/v2/", "http://nuget.org/api/v2" })]
        [InlineData(new object[] { "http://nuget.org/api/v2/", "http://nuget.org/api/v2/Packages()" })]
        [InlineData(new object[] { "http://nuget.org:8080/api/v2/", "http://nuget.org/api/v2" })]
        [InlineData(new object[] { "http://preview.nuget.org:80/api/v2", "http://nuget.org/api/v2?test" })]
        [InlineData(new object[] { "http://www.nuget.org/api/v1", "http://www.nuget.org/api/v2" })]
        [InlineData(new object[] { "https://www.nuget.org/api/v2", "http://www.nuget.org/api/v2" })]
        public void EqualsReturnsFalseForNonMatchingUris(string uriString1, string uriString2)
        {
            // Arrange
            var uri1 = new Uri(uriString1);
            var uri2 = new Uri(uriString2);

            // Act
            var result = UriUtility.UriEquals(uri1, uri2);

            // Assert
            Assert.False(result);
        }
    }
}
