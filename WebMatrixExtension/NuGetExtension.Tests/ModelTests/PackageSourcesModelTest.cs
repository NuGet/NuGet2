﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Xunit;
using Moq;

namespace NuGet.WebMatrix.Tests.ModelTests
{
    public class PackageSourcesModelTest
    {
        public PackageSourcesModelTest()
        {
            this.DefaultSource = new FeedSource(new Uri("http://msn.com"), "foo")
            {
                IsBuiltIn = true,
            };
            this.Store = new Mock<IFeedSourceStore>();

            this.Model = new PackageSourcesModel(this.DefaultSource, this.Store.Object);
        }

        internal FeedSource DefaultSource
        {
            get;
            private set;
        }

        internal PackageSourcesModel Model
        {
            get;
            private set;
        }

        internal Mock<IFeedSourceStore> Store
        {
            get;
            private set;
        }

        [Fact]
        public void LoadPackageSources_EnsureDefaultSourcePresent()
        {
            this.Store.Setup(s => s.LoadPackageSources()).Returns(Enumerable.Empty<FeedSource>());

            var feedSources = this.Model.LoadPackageSources().ToList();

            // By Default there should be 1 FeedSource  - the default one
            Assert.True(feedSources.Count == 1);

            var result = feedSources[0];
            var expected = this.DefaultSource;

            Assert.True(result.IsBuiltIn);
            Assert.Equal(expected.IsBuiltIn, result.IsBuiltIn);
            Assert.Equal(expected.Name, result.Name);
            Assert.Equal(expected.SourceUrl, result.SourceUrl);
            Assert.Equal(expected.FilterTag, result.FilterTag);
        }

        [Fact]
        public void LoadPackageSources_EnsureSourcesLoadedCorrectly()
        {
            this.Store.Setup(s => s.LoadPackageSources()).Returns(Enumerable.Empty<FeedSource>());

            var feedSources = Model.LoadPackageSources().ToList();

            // By Default there should be 1 FeedSource  - the default one
            Assert.True(feedSources.Count == 1);

            // Build up the list of expected results
            Random rand = new Random(DateTime.Now.Millisecond);
            var expectedResults = new List<FeedSource>()
            {
                new FeedSource(GetRandomUri(rand), rand.Next().ToString()),
                new FeedSource(GetRandomUri(rand), rand.Next().ToString()),
                new FeedSource(GetRandomUri(rand), rand.Next().ToString()),
                new FeedSource(GetRandomUri(rand), rand.Next().ToString()),
                new FeedSource(GetRandomUri(rand), rand.Next().ToString()),
            };

            this.Store.ResetCalls();
            this.Store.Setup(s => s.LoadPackageSources()).Returns((IEnumerable<FeedSource>)expectedResults);

            // ask the model to load the packages
            feedSources = this.Model.LoadPackageSources().ToList();

            Assert.Equal(expectedResults.Count + 1, feedSources.Count);

            for (int i = 1; i < feedSources.Count; i++)
            {
                Assert.Equal(expectedResults[i - 1].IsBuiltIn, feedSources[i].IsBuiltIn);
                Assert.Equal(expectedResults[i - 1].Name, feedSources[i].Name);
                Assert.Equal(expectedResults[i - 1].SourceUrl, feedSources[i].SourceUrl);
                Assert.Equal(expectedResults[i - 1].FilterTag, feedSources[i].FilterTag);
                Assert.False(feedSources[i].IsBuiltIn);
            }
        }

        [Fact]
        public void SavePackageSources_EnsureDefaultSourceNotSaved()
        {
            this.Store.Setup(s => s.LoadPackageSources()).Returns(Enumerable.Empty<FeedSource>());

            var feedSources = this.Model.LoadPackageSources().ToList();

            // By Default there should be 1 FeedSource  - the default one
            Assert.Equal(1, feedSources.Count);

            // Save the feed sources and ensure that the default one is not saved

            // Set the wrapper to set the result to whatever is saved
            IEnumerable<FeedSource> result = null;
            this.Store
                .Setup(s => s.SavePackageSources(It.IsAny<IEnumerable<FeedSource>>()))
                .Callback((IEnumerable<FeedSource> packages) => result = packages);

            // Use the model to save the default sources
            this.Model.SavePackageSources(feedSources);

            // Assert that the default source was not saved
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public void SavePackageSources_EnsureSourcesSavedCorrectly()
        {
            // Set the wrapper to set the result to whatever is saved
            IEnumerable<FeedSource> result = null;
            this.Store
                .Setup(s => s.SavePackageSources(It.IsAny<IEnumerable<FeedSource>>()))
                .Callback((IEnumerable<FeedSource> packages) => result = packages);

            // Build up the list of expected results
            Random rand = new Random(DateTime.Now.Millisecond);
            List<FeedSource> expected = new List<FeedSource>()
            {
                new FeedSource(GetRandomUri(rand), rand.Next().ToString()),
                new FeedSource(GetRandomUri(rand), rand.Next().ToString()),
                new FeedSource(GetRandomUri(rand), rand.Next().ToString()),
                new FeedSource(GetRandomUri(rand), rand.Next().ToString()),
                new FeedSource(GetRandomUri(rand), rand.Next().ToString()),
            };

            // Use the model to save the default sources
            this.Model.SavePackageSources(expected);

            // Assert that all the ones that should be saved, are being saved
            Assert.Equal(expected.Count(), result.Count());

            for (int i = 0; i < expected.Count; i++)
            {
                 Assert.Equal(expected[i].IsBuiltIn, result.ElementAt(i).IsBuiltIn);
                 Assert.Equal(expected[i].Name, result.ElementAt(i).Name);
                 Assert.Equal(expected[i].SourceUrl, result.ElementAt(i).SourceUrl);
                 Assert.Equal(expected[i].FilterTag, result.ElementAt(i).FilterTag);
                 Assert.False(result.ElementAt(i).IsBuiltIn);
            }
        }

        private Uri GetRandomUri(Random r)
        {
            return new Uri("file://" + Path.GetFullPath(r.Next().ToString()));
        }
    }
}
