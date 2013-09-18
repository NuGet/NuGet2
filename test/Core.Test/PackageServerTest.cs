using System;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PackageServerTest
    {
        [Theory]
        [InlineData("http://nuget.org", "", "http://nuget.org/api/v2/package/")]
        [InlineData("http://nuget.org/", "", "http://nuget.org/api/v2/package/")]
        [InlineData("http://www.nuget.org", "", "http://www.nuget.org/api/v2/package/")]
        [InlineData("http://www.nuget.org/", "", "http://www.nuget.org/api/v2/package/")]
        [InlineData("http://localhost:8080", "", "http://localhost:8080/api/v2/package/")]
        [InlineData("http://localhost:8081", "NuGet.Core/1.0", "http://localhost:8081/api/v2/package/NuGet.Core/1.0")]
        public void GetEndPointUrlAppendsServicePathIfOnlyHostNameIsSpecified(string baseUrl, string path, string expectedEndpoint)
        {
            // Act
            var value = PackageServer.GetServiceEndpointUrl(new Uri(baseUrl), path);

            // Assert
            Assert.Equal(expectedEndpoint, value.OriginalString);
        }

        [Theory]
        [InlineData("http://nuget.org/PackageFiles", "", "http://nuget.org/PackageFiles")]
        [InlineData("http://www.nuget.org/PackageFiles", "", "http://www.nuget.org/PackageFiles")]
        [InlineData("http://www.myget.org/F/2c042c750fe047a1b5ed74bbd75bcd89/", "NuGet.Core/1.0", "http://www.myget.org/F/2c042c750fe047a1b5ed74bbd75bcd89/NuGet.Core/1.0")]
        public void GetEndPointUrlDoesNotAppendServicePathIfOnlyHostNameIsSpecified(string baseUrl, string path, string expectedEndpoint)
        {
            // Act
            var value = PackageServer.GetServiceEndpointUrl(new Uri(baseUrl), path);

            // Assert
            Assert.Equal(expectedEndpoint, value.OriginalString);
        }

    }
}
