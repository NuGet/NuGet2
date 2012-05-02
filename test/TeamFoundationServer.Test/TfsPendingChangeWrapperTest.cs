using System;
using Xunit;

namespace NuGet.TeamFoundationServer
{
    public class TfsPendingChangeWrapperTest
    {
        [Fact]
        public void ConstructorThrowsArgumentNullExceptionIfPendingChangeIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new TfsPendingChangeWrapper(null));
            Assert.Equal<string>("pendingChange", exception.ParamName, StringComparer.Ordinal);
        }
    }
}