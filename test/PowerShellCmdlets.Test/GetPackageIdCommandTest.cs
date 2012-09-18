using System;
using System.Linq;
using Moq;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Test;
using Xunit;
using System.Collections.Generic;

namespace NuGet.PowerShell.Commands.Test
{
    public class GetPackageIdCmdletTest
    {
        [Fact]
        public void WillWriteThePackageIdsReturnedFromTheApiCall()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                StubResults = new string[]
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
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                StubPackageSource = "http://theActivePackageSourceBaseUri"
            };

            cmdlet.GetResults().Cast<string>();

            Assert.Equal("http://theactivepackagesourcebaseuri", cmdlet.ActualApiEndpointUri.GetLeftPart(UriPartial.Authority));
        }

        [Fact]
        public void WillUseTheSourceParameterWhenSpecified()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                Source = "http://theSourceParameterUri"
            };

            cmdlet.GetResults().Cast<string>();

            Assert.Equal("http://thesourceparameteruri", cmdlet.ActualApiEndpointUri.GetLeftPart(UriPartial.Authority));
        }

        [Fact]
        public void WillAppendTheApiPathToTheApiUri()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand();

            cmdlet.GetResults().Cast<string>();

            Assert.Equal("/api/v2/package-ids", cmdlet.ActualApiEndpointUri.PathAndQuery);
        }

        [Fact]
        public void WillIncludeAPartialIdQueryStringParameterInApiUriWhenFilterParameterIsNotEmpty()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                Filter = "thePartialId"
            };

            cmdlet.GetResults().Cast<string>();

            Assert.Contains("partialId=thePartialId", cmdlet.ActualApiEndpointUri.ToString());
        }

        [Fact]
        public void WillNotIncludeAPartialIdQueryStringParameterInApiUriWhenFilterParameterIsNotEmpty()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                Filter = null
            };

            cmdlet.GetResults().Cast<string>();

            Assert.DoesNotContain("partialId", cmdlet.ActualApiEndpointUri.ToString());
        }

        [Fact]
        public void WillIncludeAPrereleaseQueryStringParameterInApiUriWhenPrereleaseParameterIsTrue()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                IncludePrerelease = true
            };

            cmdlet.GetResults().Cast<string>();

            Assert.Contains("includePrerelease=true", cmdlet.ActualApiEndpointUri.ToString());
        }

        [Fact]
        public void WillNotIncludeAPrereleaseQueryStringParameterInApiUriWhenPrereleaseParameterIsFalse()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                IncludePrerelease = false
            };

            cmdlet.GetResults().Cast<string>();

            Assert.DoesNotContain("includePrerelease", cmdlet.ActualApiEndpointUri.ToString());
        }

        [Fact]
        public void WillUseTheRepositoryPackagesWhenTheRepositoryIsNotHttpBased()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                StubPackageSource = "c:\\aPackageDir",
                StubRepositoryPackages = new IPackage[]
                {
                    CreateStubPackage("theFirstPackageId"),
                    CreateStubPackage("theSecondPackageId"),
                }
            };

            var result = cmdlet.GetResults().Cast<string>();

            Assert.Equal("theFirstPackageId", result.First());
            Assert.Equal("theSecondPackageId", result.ElementAt(1));
        }

        [Fact]
        public void WillFilterRepositoryPackages()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                Filter = "theFirst",
                StubPackageSource = "c:\\aPackageDir",
                StubRepositoryPackages = new IPackage[]
                {
                    CreateStubPackage("theFirstPackageId"),
                    CreateStubPackage("theSecondPackageId"),
                }
            };

            var result = cmdlet.GetResults().Cast<string>();

            Assert.Equal("theFirstPackageId", result.First());
            Assert.Equal(1, result.Count());
        }

        [Fact]
        public void WillIncludeRepositoryPackagesWithPrereleaseVersionsWhenFlagged()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                IncludePrerelease = true,
                StubPackageSource = "c:\\aPackageDir",
                StubRepositoryPackages = new IPackage[]
                {
                    CreateStubPackage("theFirstPackageId"),
                    CreateStubPackage("theSecondPackageId", "0.1-pre"),
                }
            };

            var result = cmdlet.GetResults().Cast<string>();

            Assert.Equal("theFirstPackageId", result.First());
            Assert.Equal("theSecondPackageId", result.ElementAt(1));
        }

        [Fact]
        public void WillNotIncludeRepositoryPackagesWithPrereleaseVersionsWhenNotFlagged()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                IncludePrerelease = false,
                StubPackageSource = "c:\\aPackageDir",
                StubRepositoryPackages = new IPackage[]
                {
                    CreateStubPackage("theFirstPackageId"),
                    CreateStubPackage("theSecondPackageId", "0.1-pre"),
                }
            };

            var result = cmdlet.GetResults().Cast<string>();

            Assert.Equal("theFirstPackageId", result.First());
            Assert.Equal(1, result.Count());
        }

        [Fact]
        public void WillAggregateResultsWhenThePackageRepositoryIsAnAggregateRepository()
        {
            var cmdlet = new TestableGetRemotePackageIdCommand
            {
                StubResults = new []
                {
                    "theFirstPackageId",
                    "theSecondPackageId"
                },
                StubPackageRepository = new AggregateRepository(
                    new []
                    {
                        CreateStubPackageRepository(
                            new []
                            {
                                CreateStubPackage("theFirstPackageId"),
                                CreateStubPackage("theSecondPackageId"),
                            },
                            "http://theuri"),
                        CreateStubPackageRepository(
                            new []
                            {
                                CreateStubPackage("theThirdPackageId"),
                                CreateStubPackage("theFourthPackageId"),
                            },
                            "c:\\packages"),
                    }),
            };

            var result = cmdlet.GetResults().Cast<string>();

            Assert.Contains("theFirstPackageId", result);
            Assert.Contains("theSecondPackageId", result);
            Assert.Contains("theThirdPackageId", result);
            Assert.Contains("theFourthPackageId", result);
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

        private static IPackage CreateStubPackage(string id, string version = "1.0")
        {
            var stubPackage = new Mock<IPackage>();
            stubPackage.Setup(stub => stub.Id).Returns(id);
            stubPackage.Setup(stub => stub.Version).Returns(new SemanticVersion(version));
            return stubPackage.Object;
        }

        private static IPackageRepository CreateStubPackageRepository(IEnumerable<IPackage> packages, string source)
        {
            var stubPackageRepository = new Mock<IPackageRepository>();
            stubPackageRepository.Setup(stub => stub.Source).Returns(source ?? "http://aUri");
            stubPackageRepository.Setup(stub => stub.GetPackages()).Returns((packages ?? new IPackage[] { }).AsQueryable());
            return stubPackageRepository.Object;
        }
        
        private static IVsPackageManager CreateStubPackageManager(IEnumerable<IPackage> localPackages = null)
        {
            var fileSystem = new Mock<IFileSystem>();
            var localRepo = new Mock<ISharedPackageRepository>();
            localPackages = localPackages ??
                            new[] { NuGet.Test.PackageUtility.CreatePackage("P1", "0.9"), NuGet.Test.PackageUtility.CreatePackage("P2") };
            localRepo.Setup(c => c.GetPackages()).Returns(localPackages.AsQueryable());

            return new VsPackageManager(TestUtils.GetSolutionManager(), CreateActiveRepository(), new Mock<IFileSystemProvider>().Object, fileSystem.Object,
                                        localRepo.Object, new Mock<IDeleteOnRestartManager>().Object, new Mock<VsPackageInstallerEvents>().Object);
        }

        private static IVsPackageManagerFactory CreateStubPackageManagerFactory()
        {
            var mockFactory = new Mock<IVsPackageManagerFactory>();
            mockFactory.Setup(m => m.CreatePackageManager()).Returns(() => CreateStubPackageManager());

            return mockFactory.Object;
        }

        public class TestableGetRemotePackageIdCommand : GetRemotePackageIdCommand
        {
            public TestableGetRemotePackageIdCommand()
                : base(TestUtils.GetSolutionManager(), CreateStubPackageManagerFactory(), null, null, CreateSourceProvider("http://aUri"))
            {
            }
            
            public Uri ActualApiEndpointUri { get; private set; }
            public IPackageRepository StubPackageRepository { get; set; }
            public string StubPackageSource { get; set; }
            public IEnumerable<IPackage> StubRepositoryPackages { get; set; }
            public string[] StubResults { get; set; }

            protected override IPackageRepository GetPackageRepository()
            {
                if (StubPackageRepository != null)
                    return StubPackageRepository;
                
                var stubPackageRepository = new Mock<IPackageRepository>();
                stubPackageRepository.Setup(stub => stub.Source).Returns(Source ?? StubPackageSource ?? "http://aUri");
                stubPackageRepository.Setup(stub => stub.GetPackages()).Returns((StubRepositoryPackages ?? new IPackage[]{}).AsQueryable());
                return stubPackageRepository.Object;
            }

            protected override IEnumerable<string> GetResults(Uri apiEndpointUri)
            {
                ActualApiEndpointUri = apiEndpointUri;
                return StubResults ?? new string[] { };
            }
        }
    }
}