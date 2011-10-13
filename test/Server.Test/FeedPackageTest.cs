using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NuGet;
using Xunit;

namespace Server.Test
{
    public class FeedPackageTest
    {
        [Fact]
        public void FeedPackageHasSameMembersAsDataServicePackage()
        {
            // Arrange
            // This is not pretty but it's the most effective way.
            var excludedProperties = new[] { "Owners", "ReportAbuseUrl", "GalleryDetailsUrl", "DownloadUrl", "Rating", "RatingsCount", "Language", 
                                             "AssemblyReferences", "FrameworkAssemblies", 
            };
            var feedPackageProperties = new HashSet<string>(typeof(NuGet.Server.DataServices.Package).GetProperties().Select(p => p.Name), StringComparer.Ordinal);
            var dataServiceProperties = typeof(DataServicePackage).GetProperties()
                                                                  .Select(p => p.Name)
                                                                  .ToList();

            // Assert
            // Assert.Equal(feedPackageProperties.Count, dataServiceProperties.Count);
            foreach (var property in dataServiceProperties)
            {
                if (excludedProperties.Contains(property))
                {
                    continue;
                }
                Assert.True(feedPackageProperties.Contains(property), String.Format(CultureInfo.InvariantCulture,
                    "Property {0} could not be found in NuGet.Server package.", property));
            }
        }
    }
}
