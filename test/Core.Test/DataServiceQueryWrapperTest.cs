using System;
using System.Data.Services.Client;
using System.Linq.Expressions;
using Moq;
using Xunit;

namespace NuGet.Test
{
    public class DataServiceQueryWrapperTest
    {
        private const string NuGetOfficialFeedUrl = "https://www.nuget.org/api/v2/";
        private const string NuGetOfficialFeedPackagesUrl = NuGetOfficialFeedUrl + "Packages()?$skip=";

        [Fact]
        public void RequiresBatchReturnsFalseIfQueryLengthIsSmallerThanMaxQueryLength()
        {
            // Arrange
            var context = Mock.Of<IDataServiceContext>();
            var query = CreateQuery();

            var queryWrapper = new Mock<DataServiceQueryWrapper<DataServicePackage>>(context, query) { CallBase = true };
            queryWrapper.Setup(s => s.GetRequestUri(It.IsAny<Expression>())).Returns(new Uri(NuGetOfficialFeedPackagesUrl.PadRight(2047, 'a')));

            // Act
            bool requiresBatch = queryWrapper.Object.RequiresBatch(Expression.Constant(1));

            // Assert
            Assert.False(requiresBatch);
        }

        [Fact]
        public void RequiresBatchReturnsTrueIfQueryLengthEqualsMaxQueryLength()
        {
            // Arrange
            var context = Mock.Of<IDataServiceContext>();
            var query = CreateQuery();

            var queryWrapper = new Mock<DataServiceQueryWrapper<DataServicePackage>>(context, query) { CallBase = true };
            queryWrapper.Setup(s => s.GetRequestUri(It.IsAny<Expression>())).Returns(new Uri(NuGetOfficialFeedPackagesUrl.PadRight(2048, 'a')));

            // Act
            bool requiresBatch = queryWrapper.Object.RequiresBatch(Expression.Constant(1));

            // Assert
            Assert.True(requiresBatch);
        }

        [Fact]
        public void RequiresBatchReturnsTrueIfQueryLengthExceedsMaxQueryLength()
        {
            // Arrange
            var context = Mock.Of<IDataServiceContext>();
            var query = CreateQuery();

            var queryWrapper = new Mock<DataServiceQueryWrapper<DataServicePackage>>(context, query) { CallBase = true };
            queryWrapper.Setup(s => s.GetRequestUri(It.IsAny<Expression>())).Returns(new Uri(NuGetOfficialFeedPackagesUrl.PadRight(4048, 'a')));

            // Act
            bool requiresBatch = queryWrapper.Object.RequiresBatch(Expression.Constant(1));

            // Assert
            Assert.True(requiresBatch);
        }

        [Fact]
        public void RequiresBatchUsesEncodedUriToDetermineBatching()
        {
            // Arrange
            var context = Mock.Of<IDataServiceContext>();
            var query = CreateQuery();

            var queryWrapper = new Mock<DataServiceQueryWrapper<DataServicePackage>>(context, query) { CallBase = true };
            // Spaces will use about 3 times as many characters since they'll be encoded as %2C
            queryWrapper.Setup(s => s.GetRequestUri(It.IsAny<Expression>())).Returns(new Uri(NuGetOfficialFeedPackagesUrl.PadRight(800, '|')));

            // Act
            bool requiresBatch = queryWrapper.Object.RequiresBatch(Expression.Constant(1));

            // Assert
            Assert.True(requiresBatch);
        }

        private static DataServiceQuery<DataServicePackage> CreateQuery()
        {
            var dataServiceContext = new DataServiceContext(new Uri(NuGetOfficialFeedUrl));
            var query = dataServiceContext.CreateQuery<DataServicePackage>("Package");
            return query;
        }
    }
}
