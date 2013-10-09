using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet;
using Xunit;

namespace NuGet.WebMatrix.DependentTests.CoreTests
{
    
    public class FilterManagerTest
    {
        [Fact]
        public void FilterManager_FilterOnTag_Empty()
        {
            List<IPackage> packages = new List<IPackage>()
            {
                new IPackageMock("0") { Tags = "test1 test2 " },
                new IPackageMock("1") { Tags = "" },
                new IPackageMock("2") { Tags = null }
            };

            List<IPackage> filtered = FilterManager.FilterOnTag(packages.AsQueryable(), null).ToList();
            Assert.Equal(packages, filtered);
        }

        [Fact]
        public void FilterManager_FilterOnTag_NonEmpty()
        {
            List<IPackage> packages = new List<IPackage>()
            {
                new IPackageMock("0") { Tags = " Dummy1 Dummy2 " },
                new IPackageMock("1") { Tags = "" },
                new IPackageMock("2") { Tags = null },
                new IPackageMock("3") { Tags = " Dummy2 Dummy3" }
            };

            List<IPackage> expected = new List<IPackage>()
            {
                packages[0]
            };

            List<IPackage> filtered = FilterManager.FilterOnTag(packages.AsQueryable(), "Dummy1").ToList();
            Assert.Equal(expected, filtered);
        }

        [Fact]
        public void FilterManager_FilterOnTag_Whitespace()
        {
            List<IPackage> packages = new List<IPackage>()
            {
                new IPackageMock("0") { Tags = " Dummy1 Dummy2 " },
                new IPackageMock("1") { Tags = "" },
                new IPackageMock("2") { Tags = null },
                new IPackageMock("3") { Tags = " Dummy2 Dummy3 " }
            };

            List<IPackage> expected = new List<IPackage>()
            {
                packages[0]
            };

            // we remove any whitespace around the search tag -- this enforces 'exact match' semantics
            List<IPackage> filtered = FilterManager.FilterOnTag(packages.AsQueryable(), " Dummy1 ").ToList();
            Assert.Equal(expected, filtered);
        }

        [Fact]
        public void FilterManager_FilterOnTag_MixedCase()
        {
            List<IPackage> packages = new List<IPackage>()
            {
                new IPackageMock("0") { Tags = " Dummy1 Dummy2 " },
                new IPackageMock("1") { Tags = "" },
                new IPackageMock("2") { Tags = null },
                new IPackageMock("3") { Tags = " Dummy2 Dummy3 " }
            };

            List<IPackage> expected = new List<IPackage>()
            {
                packages[0]
            };

            // we remove any whitespace around the search tag
            List<IPackage> filtered = FilterManager.FilterOnTag(packages.AsQueryable(), " DumMY1 ").ToList();
            Assert.Equal(expected, filtered);
        }
    }
}
