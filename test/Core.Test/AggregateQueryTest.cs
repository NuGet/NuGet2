using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGet.Test {
    [TestClass]
    public class AggregateQueryTest {
        [TestMethod]
        public void AggregateQueryIgnoresInvalidRepositoriesIfFlagIsSet() {
            // Arrange
            IEnumerable<IQueryable<string>> sources = new[] {
                Enumerable.Range(0, 3).Select(i => i + "A").AsQueryable(),
                GetInvalidSequence("B"),
                Enumerable.Range(0, 3).Select(i => i + "C").AsQueryable(),
            };

            // Act
            var aggregateQuery = new AggregateQuery<string>(sources, StringComparer.Ordinal, ignoreFailures: true).OrderBy(c => c);

            // Assert
            CollectionAssert.AreEqual(
                new[] { "0A", "0C", "1A", "1C", "2A", "2C" },
                aggregateQuery.ToArray()
            );
        }

        [TestMethod]
        public void AggregateQueryThrowsForInvalidRepositoriesIfFlagIsSet() {
            // Arrange
            IEnumerable<IQueryable<string>> sources = new[] {
                Enumerable.Range(0, 3).Select(i => i + "A").AsQueryable(),
                GetInvalidSequence("B"),
                Enumerable.Range(0, 3).Select(i => i + "C").AsQueryable(),
            };

            // Act and Assert
            ExceptionAssert.Throws<AggregateException>(
                () => new AggregateQuery<string>(sources, StringComparer.Ordinal, ignoreFailures: false).OrderBy(c => c).ToArray());
        }


        [TestMethod]
        public void CountDoesNotThrowIfForInvalidRepositoriesIfFlagIsSet() {
            // Arrange
            IEnumerable<IQueryable<string>> sources = new[] {
                Enumerable.Range(0, 3).Select(i => i + "A").AsQueryable(),
                GetInvalidSequence("B"),
                Enumerable.Range(0, 3).Select(i => i + "C").AsQueryable(),
            };

            // Act 
            var aggregateQuery = new AggregateQuery<string>(sources, StringComparer.Ordinal, ignoreFailures: true).OrderBy(c => c);

            // Assert
            Assert.AreEqual(6, aggregateQuery.Count());
        }

        private IQueryable<string> GetInvalidSequence(string suffix) {
            Func<int, string> selector = (value) => {
                if (value > 1) {
                    throw new Exception();
                }
                return value + suffix;
            };

            return from item in Enumerable.Range(0, 3).AsQueryable()
                   select selector(item);
        }
    }
}
