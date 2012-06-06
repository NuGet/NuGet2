using System;
using System.Linq;
using Moq;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;
using Xunit;
using System.Collections.Generic;

namespace NuGet.PowerShell.Commands.Test
{
    public class GetPackageIdCommandTest
    {
        [Fact]
        public void WillWriteThePackageIdsReturnedFromTheApiCall()
        {
            var cmdlet = new TestableGetPackageIdCommand
            {
                StubPackageIds = new string[]
                {
                    "theFirstPackageId",
                    "theSecondPackageId"
                }
            };

            var result = cmdlet.GetResults().Cast<string>();

            Assert.Equal("theFirstPackageId", result.First());
            Assert.Equal("theSecondPackageId", result.ElementAt(1));
        }

        [Fact]
        public void WillUseTheActivePackageSourceToBuildTheUriWhenNoSourceParameterIsSpecified()
        {
            var cmdlet = new TestableGetPackageIdCommand
            {
                StubApiUri = new Uri("http://theActivePackageSourceUri")
            };

            cmdlet.GetResults().Cast<string>();

            Assert.Equal("http://theactivepackagesourceuri", cmdlet.ActualApiUri.GetLeftPart(UriPartial.Authority));
        }

        [Fact]
        public void WillUseTheSourceParameterWhenSpecified()
        {
            var cmdlet = new TestableGetPackageIdCommand
            {
                Source = "http://theSourceParameterUri"
            };

            cmdlet.GetResults().Cast<string>();

            Assert.Equal("http://thesourceparameteruri", cmdlet.ActualApiUri.GetLeftPart(UriPartial.Authority));
        }

        [Fact]
        public void WillAppendTheApiPathToTheApiUri()
        {
            var cmdlet = new TestableGetPackageIdCommand();

            cmdlet.GetResults().Cast<string>();

            Assert.Equal("/api/v2/package-ids?", cmdlet.ActualApiUri.PathAndQuery);
        }

        [Fact]
        public void WillIncludeAPartialIdQueryStringParameterInApiUriWhenFilterParameterIsNotEmpty()
        {
            var cmdlet = new TestableGetPackageIdCommand
            {
                Filter = "thePartialId"
            };

            cmdlet.GetResults().Cast<string>();

            Assert.Contains("partialId=thePartialId", cmdlet.ActualApiUri.ToString());
        }

        [Fact]
        public void WillNotIncludeAPartialIdQueryStringParameterInApiUriWhenFilterParameterIsNotEmpty()
        {
            var cmdlet = new TestableGetPackageIdCommand
            {
                Filter = null
            };

            cmdlet.GetResults().Cast<string>();

            Assert.DoesNotContain("partialId", cmdlet.ActualApiUri.ToString());
        }

        [Fact]
        public void WillIncludeAPrereleaseQueryStringParameterInApiUriWhenPrereleaseParameterIsTrue()
        {
            var cmdlet = new TestableGetPackageIdCommand
            {
                IncludePrerelease = true
            };

            cmdlet.GetResults().Cast<string>();

            Assert.Contains("includePrerelease=true", cmdlet.ActualApiUri.ToString());
        }

        [Fact]
        public void WillNotIncludeAPrereleaseQueryStringParameterInApiUriWhenPrereleaseParameterIsFalse()
        {
            var cmdlet = new TestableGetPackageIdCommand
            {
                IncludePrerelease = false
            };

            cmdlet.GetResults().Cast<string>();

            Assert.DoesNotContain("includePrerelease", cmdlet.ActualApiUri.ToString());
        }
        
        private static IPackageRepository CreateActiveRepository()
        {
            var remotePackages = new[]
                                     {
                                         NuGet.Test.PackageUtility.CreatePackage("P0", "1.1"),
                                         NuGet.Test.PackageUtility.CreatePackage("P1", "1.1"),
                                         NuGet.Test.PackageUtility.CreatePackage("P2", "1.2"), 
                                         NuGet.Test.PackageUtility.CreatePackage("P3")
                                     };
            var remoteRepo = new Mock<IPackageRepository>();
            remoteRepo.Setup(c => c.GetPackages()).Returns(remotePackages.AsQueryable());
            return remoteRepo.Object;
        }

        private static IVsPackageSourceProvider CreateSourceProvider(string activeSourceName)
        {
            Mock<IVsPackageSourceProvider> sourceProvider = new Mock<IVsPackageSourceProvider>();
            sourceProvider.Setup(c => c.ActivePackageSource).Returns(new PackageSource(activeSourceName,
                                                                                       activeSourceName));
            return sourceProvider.Object;
        }

        private static IVsPackageManager CreateStubPackageManager(IEnumerable<IPackage> localPackages = null)
        {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<ISharedPackageRepository>();
            localPackages = localPackages ??
                            new[] { NuGet.Test.PackageUtility.CreatePackage("P1", "0.9"), NuGet.Test.PackageUtility.CreatePackage("P2") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            return new VsPackageManager(TestUtils.GetSolutionManager(), CreateActiveRepository(), new Mock<IFileSystemProvider>().Object, fileSystem.Object,
                                        localRepo.Object, new Mock<IRecentPackageRepository>().Object,
                                        new Mock<VsPackageInstallerEvents>().Object);
        }
        
        private static IVsPackageManagerFactory CreateStubPackageManagerFactory()
        {
            var mockFactory = new Mock<IVsPackageManagerFactory>();
            mockFactory.Setup(m => m.CreatePackageManager()).Returns(() => CreateStubPackageManager());

            return mockFactory.Object;
        }
        
        public class TestableGetPackageIdCommand : GetPackageIdCommand
        {
            public TestableGetPackageIdCommand()
                : base(TestUtils.GetSolutionManager(), CreateStubPackageManagerFactory(), null, CreateSourceProvider("http://aUri"))
            {
            }

            public Uri ActualApiUri { get; private set; }
            public string[] StubPackageIds { get; set; }
            public Uri StubApiUri { get; set; }

            protected override string[] GetPackageIds(Uri uri)
            {
                ActualApiUri = uri;
                return StubPackageIds ?? new string[] {};
            }

            protected override Uri GetUri()
            {
                return StubApiUri ?? base.GetUri();
            }
        }
    }
}