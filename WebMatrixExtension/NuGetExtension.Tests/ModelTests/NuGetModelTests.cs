using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.WebMatrix.Extensibility;
using NuGet;
using System.Threading.Tasks;
using Microsoft.WebMatrix.Utility;
using Xunit;
using Moq;

namespace NuGet.WebMatrix.DependentTests
{
    //[DeploymentItem(@"ViewModelTests\featured.xml", "ViewModelTests")]
    public class NuGetModelTests
    {
        public NuGetModelTests()
        {
            // Force clear of cache
            NuGetModel.ClearCache();

            this.DescriptorStub = new Mock<INuGetGalleryDescriptor>();
            this.DescriptorStub.Setup(d => d.GalleryId).Returns(0);
        }

        internal INuGetGalleryDescriptor Descriptor
        {
            get
            {
                return this.DescriptorStub.Object;
            }
        }

        internal Mock<INuGetGalleryDescriptor> DescriptorStub
        {
            get;
            private set;
        }

        internal FeedSource DefaultFeedSource
        {
            get
            {
                // TODO: Have separate unit test projects when there is less common code between 
                // NuGetGallery and Extension Gallery
                // This property below should be in sync with GalleryDescriptors.cs of project NuGetExtension
                // GalleryDescriptors.cs is only present in NuGetExtension and a reference to NuGetExtension cannot
                // be added since it will cause type conflicts
                return new FeedSource(new Uri(FWLink.GetLinkWithClcid(294092)), Resources.CuratedFeedSourceTitle)
                {
                    IsBuiltIn = true,
                };
            }
        }

        internal FeedSource FakeFeedSource
        {
            get
            {
                return new FeedSource("c:\\", "fake");
            }
        }

        private List<string> fakePackageIds = new[]
        {
            "microsoft-web-helpers",
            "Facebook.Helper",
            "Twitter.Helper",
            "PayPal.Helper",
            "RazorDebugger",
            "BBCode",
            "IE9.Helper",
            "jQuery",
            "EntityFramework",
            "51Degrees.mobi-WebMatrix"
        }.ToList();

        [Fact]
        public void NuGetModel_CachingTest()
        {
            FeedSource source = new FeedSource(@"c:\", "test");
            string destination = @"c:\";

            var model = NuGetModel.GetModel(this.Descriptor, new IWebMatrixHostMock(), source, destination, null, TaskScheduler.Default);
            Assert.False(model.FromCache);

            model = NuGetModel.GetModel(this.Descriptor, new IWebMatrixHostMock(), source, destination, null, TaskScheduler.Default);
            Assert.True(model.FromCache);

            model = null;
            NuGetModel.ClearCache();

            model = NuGetModel.GetModel(this.Descriptor, new IWebMatrixHostMock(), source, destination, null, TaskScheduler.Default);
            Assert.False(model.FromCache);

            model = NuGetModel.GetModel(this.Descriptor, new IWebMatrixHostMock(), source, destination, null, TaskScheduler.Default);
            Assert.True(model.FromCache);
        }

        [Fact]
        public void NuGetModel_FindDependencyTest()
        {
            IPackageManagerMock packageManagerMock = new IPackageManagerMock();
            var model = new NuGetModel(this.Descriptor, new IWebMatrixHostMock(), this.FakeFeedSource, string.Empty, packageManagerMock, TaskScheduler.Default);

            string expected = "test";

            // Setup the mock call
            packageManagerMock.FindDependenciesToBeInstalledFunc = (package) => new List<IPackage>() { new IPackageMock(expected) };

            var result = model.FindDependenciesToBeInstalled(new IPackageMock(expected)).ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(expected, result[0].Id);
        }

        [Fact]
        public void NuGetModel_InstallPackageTest()
        {
            IPackageManagerMock packageManagerMock = new IPackageManagerMock();
            var model = new NuGetModel(this.Descriptor, new IWebMatrixHostMock(), this.FakeFeedSource, string.Empty, packageManagerMock, TaskScheduler.Default);

            // Setup the mock call
            string expected = new Random(DateTime.Now.Millisecond).Next().ToString();
            packageManagerMock.InstallPackageFunc = (package) => new List<string>() { package.Id };

            var result = model.InstallPackage(new IPackageMock(expected), false);

            Assert.Equal(1, result.Count());
            Assert.Equal(expected, result.First());
        }

        [Fact]
        public void NuGetModel_UninstallPackageTest()
        {
            IPackageManagerMock packageManagerMock = new IPackageManagerMock();
            var model = new NuGetModel(this.Descriptor, new IWebMatrixHostMock(), this.FakeFeedSource, string.Empty, packageManagerMock, TaskScheduler.Default);

            // Setup the mock call
            string expected = new Random(DateTime.Now.Millisecond).Next().ToString();
            packageManagerMock.UninstallPackageFunc = (package) => new List<string>() { package.Id };

            var result = model.UninstallPackage(new IPackageMock(expected), false);

            Assert.Equal(1, result.Count());
            Assert.Equal(expected, result.First());
        }

        [Fact]
        public void NuGetModel_UpdatePackageTest()
        {
            IPackageManagerMock packageManagerMock = new IPackageManagerMock();
            var model = new NuGetModel(this.Descriptor, new IWebMatrixHostMock(), this.FakeFeedSource, string.Empty, packageManagerMock, TaskScheduler.Default);

            // Setup the mock call
            string expected = new Random(DateTime.Now.Millisecond).Next().ToString();
            packageManagerMock.UpdatePackageFunc = (package) => new List<string>() { package.Id };

            var result = model.UpdatePackage(new IPackageMock(expected), false);

            Assert.Equal(1, result.Count());
            Assert.Equal(expected, result.First());
        }

        [Fact]
        public void NuGetModel_IsPackageInstalledTest()
        {
            IPackageManagerMock packageManagerMock = new IPackageManagerMock();
            var model = new NuGetModel(this.Descriptor, new IWebMatrixHostMock(), this.FakeFeedSource, string.Empty, packageManagerMock, TaskScheduler.Default);

            // Setup the mock call
            bool expected = true;
            packageManagerMock.IsPackageInstalledFunc = (package) => expected;

            var result = model.IsPackageInstalled(new IPackageMock(string.Empty));
            Assert.Equal(expected, result);

            expected = true;
            result = model.IsPackageInstalled(new IPackageMock(string.Empty));
            Assert.Equal(expected, result);
        }

        [Fact]
        public void NuGetModel_GetInstalledPackagesTest()
        {
            IPackageManagerMock packageManagerMock = new IPackageManagerMock();
            var model = new NuGetModel(this.Descriptor, new IWebMatrixHostMock(), this.FakeFeedSource, string.Empty, packageManagerMock, TaskScheduler.Default);

            // Setup the mock call
            string expected = new Random(DateTime.Now.Millisecond).Next().ToString();
            packageManagerMock.GetInstalledPackagesFunc = () => new List<IPackage>() { new IPackageMock(expected) }.AsQueryable();

            var result = model.GetInstalledPackages();

            Assert.Equal(1, result.Count());
            Assert.Equal(expected, result.First().Id);
        }

        [Fact]
        public void NuGetModel_GetPackagesWithUpdatesTest()
        {
            IPackageManagerMock packageManagerMock = new IPackageManagerMock();
            var model = new NuGetModel(this.Descriptor, new IWebMatrixHostMock(), this.FakeFeedSource, string.Empty, packageManagerMock, TaskScheduler.Default);

            // Setup the mock call
            string expected = new Random(DateTime.Now.Millisecond).Next().ToString();
            packageManagerMock.GetPackagesWithUpdatesFunc = () => new List<IPackage>() { new IPackageMock(expected) };

            var result = model.GetPackagesWithUpdates();

            Assert.Equal(1, result.Count());
            Assert.Equal(expected, result.First().Id);
        }

        [Fact]
        public void NuGetModel_FindPackagesTest()
        {
            IPackageManagerMock packageManagerMock = new IPackageManagerMock();
            var model = new NuGetModel(this.Descriptor, new IWebMatrixHostMock(), this.FakeFeedSource, string.Empty, packageManagerMock, TaskScheduler.Default);

            // Setup the mock call
            var ran = new Random(DateTime.Now.Millisecond);
            IList<string> expected = new List<string>()
            {
                ran.Next().ToString(),
                ran.Next().ToString(),
                ran.Next().ToString(),
                ran.Next().ToString(),
                ran.Next().ToString(),
            };

            packageManagerMock.FindPackagesFunc = (ids) => from id in ids
                                                           select new IPackageMock(id);

            var result = model.FindPackages(expected).ToList();

            Assert.Equal(expected.Count, result.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i], result[i].Id);
            }
        }

        [Fact]
        public void GetInstalledPackages()
        {
            string siteRoot = @"c:\";

            IPackageManagerMock packageManagerMock = new IPackageManagerMock();
            var model = new NuGetModel(
                this.Descriptor,
                new IWebMatrixHostMock(),
                this.DefaultFeedSource,
                siteRoot,
                packageManagerMock,
                TaskScheduler.Default);

            // Setup the mock call to FindPackages
            packageManagerMock.FindPackagesFunc = (ids) => Enumerable.Empty<IPackage>();
            packageManagerMock.GetInstalledPackagesFunc = () => (from id in fakePackageIds
                                                                 select new IPackageMock(id)).AsQueryable();

            Assert.Equal<int>(fakePackageIds.Count, model.GetInstalledPackages().Count());
        }
    }
}
