using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NuGet.Test
{
    public class AggregateQueryTest
    {
        [Fact]
        public void AggregateQueryIgnoresInvalidRepositoriesIfFlagIsSet()
        {
            // Arrange
            IEnumerable<IQueryable<string>> sources = new[] {
                Enumerable.Range(0, 3).Select(i => i + "A").AsQueryable(),
                GetInvalidSequence("B"),
                Enumerable.Range(0, 3).Select(i => i + "C").AsQueryable(),
            };

            // Act
            var aggregateQuery = new AggregateQuery<string>(sources, StringComparer.Ordinal, NullLogger.Instance, ignoreFailures: true).OrderBy(c => c);

            // Assert
            Assert.Equal(
                new[] { "0A", "0C", "1A", "1C", "2A", "2C" },
                aggregateQuery.ToArray()
            );
        }

        [Fact]
        public void AggregateQueryConcatenatesIndividualQueriesIfNoOrderingButSequenceIsInvalid()
        {
            // Arrange
            IEnumerable<IQueryable<string>> sources = new[] {
                Enumerable.Range(0, 3).Select(i => i + "C").AsQueryable(),
                GetInvalidSequence("B"),
                Enumerable.Range(0, 3).Select(i => i + "A").AsQueryable(),
            };

            // Act
            var aggregateQuery = new AggregateQuery<string>(sources, StringComparer.Ordinal, NullLogger.Instance, ignoreFailures: false);

            // Assert
            ExceptionAssert.Throws<Exception>(() => aggregateQuery.ToArray(), "Bad sequence");
        }

        [Fact]
        public void AggregateQueryConcatenatesIndividualQueriesIfNoOrderingIsAvailable()
        {
            // Arrange
            IEnumerable<IQueryable<string>> sources = new[] {
                Enumerable.Range(0, 3).Select(i => i + "C").AsQueryable(),
                Enumerable.Range(0, 3).Select(i => i + "A").AsQueryable(),
            };

            // Act
            var aggregateQuery = new AggregateQuery<string>(sources, StringComparer.Ordinal, NullLogger.Instance, ignoreFailures: false);

            // Assert
            Assert.Equal(
                new[] { "0C", "1C", "2C", "0A", "1A", "2A" },
                aggregateQuery.ToArray()
            );
        }

        [Fact]
        public void AggregateQueryConcatenatesIndividualQueriesIfNoOrderingIsAvailableWhileSkippingInvalidSequences()
        {
            // Arrange
            IEnumerable<IQueryable<string>> sources = new[] {
                Enumerable.Range(0, 3).Select(i => i + "A").AsQueryable(),
                GetInvalidSequence("B"),
                Enumerable.Range(0, 3).Select(i => i + "C").AsQueryable(),
            };

            // Act
            var aggregateQuery = new AggregateQuery<string>(sources, StringComparer.Ordinal, NullLogger.Instance, ignoreFailures: true);

            // Assert
            Assert.Equal(
                new[] { "0A", "1A", "2A", "0C", "1C", "2C" },
                aggregateQuery.ToArray()
            );
        }

        [Fact]
        public void AggregateQueryThrowsForInvalidRepositoriesIfFlagIsSet()
        {
            // Arrange
            IEnumerable<IQueryable<string>> sources = new[] {
                Enumerable.Range(0, 3).Select(i => i + "A").AsQueryable(),
                GetInvalidSequence("B"),
                Enumerable.Range(0, 3).Select(i => i + "C").AsQueryable(),
            };

            // Act and Assert
            ExceptionAssert.Throws<AggregateException>(
                () => new AggregateQuery<string>(sources, StringComparer.Ordinal, NullLogger.Instance, ignoreFailures: false).OrderBy(c => c).ToArray());
        }

        [Fact]
        public void CountDoesNotThrowIfForInvalidRepositoriesIfFlagIsSet()
        {
            // Arrange
            IEnumerable<IQueryable<string>> sources = new[] {
                Enumerable.Range(0, 3).Select(i => i + "A").AsQueryable(),
                GetInvalidSequence("B"),
                Enumerable.Range(0, 3).Select(i => i + "C").AsQueryable(),
            };

            // Act 
            var aggregateQuery = new AggregateQuery<string>(sources, StringComparer.Ordinal, NullLogger.Instance, ignoreFailures: true).OrderBy(c => c);

            // Assert
            Assert.Equal(6, aggregateQuery.Count());
        }

        private IQueryable<string> GetInvalidSequence(string suffix)
        {
            Func<int, string> selector = (value) =>
            {
                if (value > 1)
                {
                    throw new Exception("Bad sequence");
                }
                return value + suffix;
            };

            return from item in Enumerable.Range(0, 3).AsQueryable()
                   select selector(item);
        }
    }
}
