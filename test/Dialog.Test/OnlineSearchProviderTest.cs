using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using System.Windows;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Dialog.Test
{
    public class OnlineSearchProviderTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IncludePreleaseVersionPropertyGetterReturnsTheValueOfBaseProvider(bool includePrerelease)
        {
            // Arrange
            var baseProvider = new Mock<PackagesProviderBase>(
                new MockPackageRepository(), 
                new ResourceDictionary(),
                new ProviderServices(null, null, null, null, null, null, null), 
                new Mock<IProgressProvider>().Object, 
                new Mock<ISolutionManager>().Object);

            baseProvider.Setup(p => p.IncludePrerelease).Returns(includePrerelease);

            var provider = new OnlineSearchProvider(baseProvider.Object);

            // Act && Assert
            Assert.Equal(includePrerelease, provider.IncludePrerelease);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IncludePreleaseVersionPropertySetterSetsTheValueOfBaseProvider(bool includePrerelease)
        {
            // Arrange
            bool? assignedValue = null;

            var baseProvider = new Mock<PackagesProviderBase>(
                 new MockPackageRepository(),
                 new ResourceDictionary(),
                 new ProviderServices(null, null, null, null, null, null, null),
                 new Mock<IProgressProvider>().Object,
                 new Mock<ISolutionManager>().Object);
            baseProvider.SetupSet(p => p.IncludePrerelease = includePrerelease).Callback<bool>(a => assignedValue = a);

            var provider = new OnlineSearchProvider(baseProvider.Object);
            provider.IncludePrerelease = includePrerelease;

            // Act && Assert
            Assert.Equal(includePrerelease, assignedValue);
        }
    }
}
