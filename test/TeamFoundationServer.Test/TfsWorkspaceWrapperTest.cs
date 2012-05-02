using System;
using Xunit;

namespace NuGet.TeamFoundationServer
{
    public class TfsWorkspaceWrapperTest
    {
        [Fact]
        public void ConstructorThrowsArgumentNullExceptionIfWorkspaceIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new TfsWorkspaceWrapper(null));
            Assert.Equal<string>("workspace", exception.ParamName, StringComparer.Ordinal);
        }
    }
}