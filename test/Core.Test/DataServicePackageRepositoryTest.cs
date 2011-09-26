using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;

namespace NuGet.Test {
    
    public class DataServicePackageRepositoryTest {
        [Fact]
        public void SearchUsesDefaultSearchLogicIfServerDoesnotSupportServiceMethod() {
            // Arrange
            var client = new Mock<IHttpClient>();
            var context = new Mock<IDataServiceContext>();
            var repository = new Mock<DataServicePackageRepository>(client.Object) { CallBase = true };
            var packages = new[] { 
                PackageUtility.CreatePackage("A", description:"New and aweseome"),
                PackageUtility.CreatePackage("B", description:"old and bad"),
                PackageUtility.CreatePackage("C", description:"rich")
            };
            repository.Setup(m => m.GetPackages()).Returns(packages.AsQueryable());
            repository.Object.Context = context.Object;
            context.Setup(m => m.SupportsServiceMethod("Search")).Returns(false);

            // Act
            var results = repository.Object.Search("old").ToList();

            // Assert
            Assert.Equal(1, results.Count);
            Assert.Equal("B", results[0].Id);
        }

        [Fact]
        public void SearchUsesServiceMethodIfServerSupportsIt() {
            // Arrange
            var client = new Mock<IHttpClient>();
            var context = new Mock<IDataServiceContext>();
            var repository = new Mock<DataServicePackageRepository>(client.Object) { CallBase = true };
            var packages = new[] { 
                PackageUtility.CreatePackage("A", description:"New and aweseome"),
                PackageUtility.CreatePackage("B", description:"old and bad"),
                PackageUtility.CreatePackage("C", description:"rich")
            };
            repository.Setup(m => m.GetPackages()).Returns(packages.AsQueryable());
            repository.Object.Context = context.Object;
            context.Setup(m => m.SupportsServiceMethod("Search")).Returns(true);
            context.Setup(m => m.CreateQuery<DataServicePackage>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
                   .Callback<string, IDictionary<string, object>>((entitySet, parameters) => {
                       // Assert
                       Assert.Equal("Search", entitySet);
                       Assert.Equal(2, parameters.Count);
                       Assert.Equal("'old'", parameters["searchTerm"]);
                       Assert.Equal("''", parameters["targetFramework"]);
                   })
                   .Returns(new Mock<IDataServiceQuery<DataServicePackage>>().Object);

            // Act
            repository.Object.Search("old");
        }

        [Fact]
        public void SearchEscapesSingleQuotesInParameters() {
            // Arrange
            var client = new Mock<IHttpClient>();
            var context = new Mock<IDataServiceContext>();
            var repository = new Mock<DataServicePackageRepository>(client.Object) { CallBase = true };
            repository.Object.Context = context.Object;
            context.Setup(m => m.SupportsServiceMethod("Search")).Returns(true);
            context.Setup(m => m.CreateQuery<DataServicePackage>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
                   .Callback<string, IDictionary<string, object>>((entitySet, parameters) => {
                       // Assert
                       Assert.Equal("Search", entitySet);
                       Assert.Equal(2, parameters.Count);
                       Assert.Equal("'dante''s inferno'", parameters["searchTerm"]);
                       Assert.Equal("''", parameters["targetFramework"]);
                   })
                   .Returns(new Mock<IDataServiceQuery<DataServicePackage>>().Object);

            // Act
            repository.Object.Search("dante's inferno");
        }

        [Fact]
        public void SearchSendsShortTargetFrameworkNames() {
            // Arrange
            var client = new Mock<IHttpClient>();
            var context = new Mock<IDataServiceContext>();
            var repository = new Mock<DataServicePackageRepository>(client.Object) { CallBase = true };
            repository.Object.Context = context.Object;
            context.Setup(m => m.SupportsServiceMethod("Search")).Returns(true);
            context.Setup(m => m.CreateQuery<DataServicePackage>(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
                   .Callback<string, IDictionary<string, object>>((entitySet, parameters) => {
                       // Assert
                       Assert.Equal("Search", entitySet);
                       Assert.Equal(2, parameters.Count);
                       Assert.Equal("'dante''s inferno'", parameters["searchTerm"]);
                       Assert.Equal("'net40|sl40|sl30-wp|netmf11'", parameters["targetFramework"]);
                   })
                   .Returns(new Mock<IDataServiceQuery<DataServicePackage>>().Object);

            // Act
            repository.Object.Search("dante's inferno", new[] {
                VersionUtility.ParseFrameworkName("net40").FullName,
                VersionUtility.ParseFrameworkName("sl40").FullName,
                VersionUtility.ParseFrameworkName("sl3-wp").FullName,
                VersionUtility.ParseFrameworkName("netmf11").FullName,
            });
        }

        [Fact]
        public void ExtractMethodNamesFromSchemaFindsMethodNames() {
            // Act
            var schemaNoMethods = DataServiceContextWrapper.ExtractMethodNamesFromSchema(NuGetFeedSchema.SchemaWithNoMethods).ToList();
            var schemaWithMethods = DataServiceContextWrapper.ExtractMethodNamesFromSchema(NuGetFeedSchema.SchemaWithMethod).ToList();
            var emptySchema = DataServiceContextWrapper.ExtractMethodNamesFromSchema("").ToList();
            var nullSchema = DataServiceContextWrapper.ExtractMethodNamesFromSchema(null).ToList();
            var badSchema = DataServiceContextWrapper.ExtractMethodNamesFromSchema("<xml>DEADBEEF").ToList();

            // Assert
            Assert.Equal(0, schemaNoMethods.Count);
            Assert.Equal(1, schemaWithMethods.Count);
            Assert.Equal("Search", schemaWithMethods[0]);
            Assert.Equal(0, emptySchema.Count);
            Assert.Equal(0, nullSchema.Count);
            Assert.Equal(0, badSchema.Count);
        }


        private class NuGetFeedSchema {
            public const string SchemaWithMethod = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<edmx:Edmx SemVer=""1.0"" xmlns:edmx=""http://schemas.microsoft.com/ado/2007/06/edmx"">
  <edmx:DataServices xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" m:DataServiceVersion=""2.0"">
    <Schema Namespace=""NuGet.Server.DataServices"" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"" xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns=""http://schemas.microsoft.com/ado/2006/04/edm"">
      <EntityType Name=""Package"" m:HasStream=""true"">
        <Key>
          <PropertyRef Name=""Id"" />
          <PropertyRef Name=""SemVer"" />
        </Key>
        <Property Name=""Id"" Type=""Edm.String"" Nullable=""false"" m:FC_TargetPath=""SyndicationTitle"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""false"" />
        <Property Name=""SemVer"" Type=""Edm.String"" Nullable=""false"" />
        <Property Name=""Title"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Authors"" Type=""Edm.String"" Nullable=""true"" m:FC_TargetPath=""SyndicationAuthorName"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""false"" />
        <Property Name=""IconUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""LicenseUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""ProjectUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""ReportAbuseUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""DownloadCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""VersionDownloadCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""RatingsCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""VersionRatingsCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""Rating"" Type=""Edm.Double"" Nullable=""false"" />
        <Property Name=""VersionRating"" Type=""Edm.Double"" Nullable=""false"" />
        <Property Name=""RequireLicenseAcceptance"" Type=""Edm.Boolean"" Nullable=""false"" />
        <Property Name=""Description"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Summary"" Type=""Edm.String"" Nullable=""true"" m:FC_TargetPath=""SyndicationSummary"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""false"" />
        <Property Name=""ReleaseNotes"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Language"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Published"" Type=""Edm.DateTime"" Nullable=""false"" />
        <Property Name=""LastUpdated"" Type=""Edm.DateTime"" Nullable=""false"" m:FC_TargetPath=""SyndicationUpdated"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""false"" />
        <Property Name=""Price"" Type=""Edm.Decimal"" Nullable=""false"" />
        <Property Name=""Dependencies"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageHash"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageSize"" Type=""Edm.Int64"" Nullable=""false"" />
        <Property Name=""ExternalPackageUri"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Categories"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Copyright"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageType"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Tags"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""IsLatestVersion"" Type=""Edm.Boolean"" Nullable=""false"" />
      </EntityType>
      <EntityContainer Name=""PackageContext"" m:IsDefaultEntityContainer=""true"">
        <EntitySet Name=""Packages"" EntityType=""NuGet.Server.DataServices.Package"" />
        <FunctionImport Name=""Search"" EntitySet=""Packages"" ReturnType=""Collection(NuGet.Server.DataServices.Package)"" m:HttpMethod=""GET"">
          <Parameter Name=""searchTerm"" Type=""Edm.String"" Mode=""In"" />
          <Parameter Name=""targetFramework"" Type=""Edm.String"" Mode=""In"" />
        </FunctionImport>
      </EntityContainer>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

            public const string SchemaWithNoMethods = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<edmx:Edmx SemVer=""1.0"" xmlns:edmx=""http://schemas.microsoft.com/ado/2007/06/edmx"">
  <edmx:DataServices xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" m:DataServiceVersion=""1.0"">
    <Schema Namespace=""Gallery.Infrastructure.FeedModels"" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"" xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns=""http://schemas.microsoft.com/ado/2006/04/edm"">
      <EntityType Name=""PublishedPackage"" m:HasStream=""true"">
        <Key>
          <PropertyRef Name=""Id"" />
          <PropertyRef Name=""SemVer"" />
        </Key>
        <Property Name=""Id"" Type=""Edm.String"" Nullable=""false"" />
        <Property Name=""SemVer"" Type=""Edm.String"" Nullable=""false"" />
        <Property Name=""Title"" Type=""Edm.String"" Nullable=""true"" m:FC_TargetPath=""SyndicationTitle"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""true"" />
        <Property Name=""Authors"" Type=""Edm.String"" Nullable=""true"" m:FC_TargetPath=""SyndicationAuthorName"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""true"" />
        <Property Name=""PackageType"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Summary"" Type=""Edm.String"" Nullable=""true"" m:FC_TargetPath=""SyndicationSummary"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""true"" />
        <Property Name=""Description"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Copyright"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageHashAlgorithm"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageHash"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PackageSize"" Type=""Edm.Int64"" Nullable=""false"" />
        <Property Name=""Price"" Type=""Edm.Decimal"" Nullable=""false"" />
        <Property Name=""RequireLicenseAcceptance"" Type=""Edm.Boolean"" Nullable=""false"" />
        <Property Name=""IsLatestVersion"" Type=""Edm.Boolean"" Nullable=""false"" />
        <Property Name=""VersionRating"" Type=""Edm.Double"" Nullable=""false"" />
        <Property Name=""VersionRatingsCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""VersionDownloadCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""Created"" Type=""Edm.DateTime"" Nullable=""false"" />
        <Property Name=""LastUpdated"" Type=""Edm.DateTime"" Nullable=""false"" m:FC_TargetPath=""SyndicationUpdated"" m:FC_ContentKind=""text"" m:FC_KeepInContent=""true"" />
        <Property Name=""Published"" Type=""Edm.DateTime"" Nullable=""true"" />
        <Property Name=""ExternalPackageUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""ProjectUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""LicenseUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""IconUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Rating"" Type=""Edm.Double"" Nullable=""false"" />
        <Property Name=""RatingsCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""DownloadCount"" Type=""Edm.Int32"" Nullable=""false"" />
        <NavigationProperty Name=""Screenshots"" Relationship=""Gallery.Infrastructure.FeedModels.PublishedPackage_Screenshots"" FromRole=""PublishedPackage"" ToRole=""Screenshots"" />
        <Property Name=""Categories"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Tags"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Dependencies"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""ReportAbuseUrl"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""GalleryDetailsUrl"" Type=""Edm.String"" Nullable=""true"" />
      </EntityType>
      <EntityType Name=""PublishedScreenshot"">
        <Key>
          <PropertyRef Name=""Id"" />
        </Key>
        <Property Name=""Id"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""PublishedPackageId"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""PublishedPackageVersion"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""ScreenshotUri"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""Caption"" Type=""Edm.String"" Nullable=""true"" />
      </EntityType>
      <Association Name=""PublishedPackage_Screenshots"">
        <End Role=""PublishedPackage"" Type=""Gallery.Infrastructure.FeedModels.PublishedPackage"" Multiplicity=""*"" />
        <End Role=""Screenshots"" Type=""Gallery.Infrastructure.FeedModels.PublishedScreenshot"" Multiplicity=""*"" />
      </Association>
      <EntityContainer Name=""GalleryFeedContext"" m:IsDefaultEntityContainer=""true"">
        <EntitySet Name=""Packages"" EntityType=""Gallery.Infrastructure.FeedModels.PublishedPackage"" />
        <EntitySet Name=""Screenshots"" EntityType=""Gallery.Infrastructure.FeedModels.PublishedScreenshot"" />
        <AssociationSet Name=""PublishedPackage_Screenshots"" Association=""Gallery.Infrastructure.FeedModels.PublishedPackage_Screenshots"">
          <End Role=""PublishedPackage"" EntitySet=""Packages"" />
          <End Role=""Screenshots"" EntitySet=""Screenshots"" />
        </AssociationSet>
      </EntityContainer>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
        }
    }
}
