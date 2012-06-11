using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test
{
    public class PackageOperationExtensionsTest
    {
        public static IEnumerable<object[]> OperationsWithoutSatellitePackages
        {
            get
            {
                yield return new object[]
                {
                    new [] 
                    { 
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Install),
                    },
                    Enumerable.Empty<PackageOperation>()
                };

                yield return new object[]
                {
                    new [] 
                    { 
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Install),
                    },
                    new[] 
                    {
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Uninstall),
                    }
                };

                yield return new object[]
                {
                    new [] 
                    { 
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("B", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("C", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Install),
                        new PackageOperation(PackageUtility.CreatePackage("B", "1.2"), PackageAction.Install),
                    },
                    new[] 
                    {
                        new PackageOperation(PackageUtility.CreatePackage("B", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("C", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("B", "1.2"), PackageAction.Install),
                    }
                };
            }
        }

        [Theory]
        [PropertyData("OperationsWithoutSatellitePackages")]
        public void ReduceRemovesOpposingActionsFromTheList(IEnumerable<PackageOperation> source, IEnumerable<PackageOperation> expected)
        {
            // Act
            var result = source.Reduce();

            // Assert
            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> OperationsWithSatellitePackages
        {
            get
            {
                yield return new object[]
                {
                    new [] 
                    { 
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A.fr", "1.0", language: "fr",
                            dependencies: new[] { new PackageDependency("A", VersionUtility.ParseVersionSpec("[1.0]")) }), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.2"), PackageAction.Install),
                        new PackageOperation(PackageUtility.CreatePackage("A.fr", "1.2", language: "fr",
                            dependencies: new[] { new PackageDependency("A", VersionUtility.ParseVersionSpec("[1.0]")) }), PackageAction.Install),
                    },
                    new [] 
                    { 
                        new PackageOperation(PackageUtility.CreatePackage("A.fr", "1.0", language: "fr",
                            dependencies: new[] { new PackageDependency("A", VersionUtility.ParseVersionSpec("[1.0]")) }), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.2"), PackageAction.Install),
                        new PackageOperation(PackageUtility.CreatePackage("A.fr", "1.2", language: "fr",
                            dependencies: new[] { new PackageDependency("A", VersionUtility.ParseVersionSpec("[1.0]")) }), PackageAction.Install),
                    },
                };

                yield return new object[]
                {
                     new [] 
                    { 
                        new PackageOperation(PackageUtility.CreatePackage("B", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A.fr", "1.0", language: "fr", 
                            dependencies: new[] { new PackageDependency("A", VersionUtility.ParseVersionSpec("[1.0]")) }), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A.fr", "1.2", language: "fr",
                            dependencies: new[] { new PackageDependency("A", VersionUtility.ParseVersionSpec("[1.0]")) }), PackageAction.Install),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.2"), PackageAction.Install),
                    },
                    new[] 
                    {
                        new PackageOperation(PackageUtility.CreatePackage("A.fr", "1.0", language: "fr",
                            dependencies: new[] { new PackageDependency("A", VersionUtility.ParseVersionSpec("[1.0]")) }), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("B", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.0"), PackageAction.Uninstall),
                        new PackageOperation(PackageUtility.CreatePackage("A", "1.2"), PackageAction.Install),
                        new PackageOperation(PackageUtility.CreatePackage("A.fr", "1.2", language: "fr",
                            dependencies: new[] { new PackageDependency("A", VersionUtility.ParseVersionSpec("[1.0]")) }), PackageAction.Install),
                    }
                };
            }
        }

        [Theory]
        [PropertyData("OperationsWithSatellitePackages")]
        public void ReduceRemovesOpposingActionsFromTheListReordersSatellitePackages(IEnumerable<PackageOperation> source, IEnumerable<PackageOperation> expected)
        {
            // Act
            var result = source.Reduce();

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
