using System;
using System.Data.Services.Client;
using System.Linq;
using System.Linq.Expressions;
using Moq;
using Xunit;

namespace NuGet.Test
{
    public class SmartDataServiceQueryTest
    {
        [Fact]
        public void GetEnumeratorExecutesBatchIfRequiresBatchTrue()
        {
            // Arrange
            var mockContext = new Mock<IDataServiceContext>();
            var mockQuery = new Mock<IDataServiceQuery<int>>();
            mockQuery.Setup(m => m.RequiresBatch(It.IsAny<Expression>())).Returns(true);
            mockContext.Setup(m => m.CreateQuery<int>("Foo")).Returns(mockQuery.Object);
            mockContext.Setup(m => m.ExecuteBatch<int>(It.IsAny<DataServiceQuery>())).Returns(new[] { 1 }).Verifiable();
            var query = new SmartDataServiceQuery<int>(mockContext.Object, "Foo");

            // Act
            query.GetEnumerator();

            // Assert
            mockContext.VerifyAll();
        }

        [Fact]
        public void ProjectionTest()
        {
            // Arrange
            var mockContext = new Mock<IDataServiceContext>();
            var mockStringQuery = new Mock<IDataServiceQuery<string>>();
            var mockIntQuery = new Mock<IDataServiceQuery<int>>();
            mockStringQuery.Setup(m => m.CreateQuery<int>(It.IsAny<Expression>())).Returns(mockIntQuery.Object);
            mockStringQuery.Setup(m => m.RequiresBatch(It.IsAny<Expression>())).Returns(false);
            mockIntQuery.Setup(m => m.GetEnumerator()).Verifiable();
            mockContext.Setup(m => m.CreateQuery<string>("Foo")).Returns(mockStringQuery.Object);
            var query = from s in new SmartDataServiceQuery<string>(mockContext.Object, "Foo")
                        select Int32.Parse(s);

            // Act
            query.GetEnumerator();

            // Assert
            mockIntQuery.VerifyAll();
        }
    }
}
