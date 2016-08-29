// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;
using Xunit.Extensions;

namespace NuGet
{
    public class VersionComparerTests
    {
        [Theory]
        [InlineData("1.0.0", "1.0.0")]
        [InlineData("1.0.0-BETA", "1.0.0-beta")]
        [InlineData("1.0.0-BETA+AA", "1.0.0-beta+aa")]
        [InlineData("1.0.0-BETA+AA", "1.0.0-beta+aa")]
        [InlineData("1.0.0-BETA.X.y.5.77.0+AA", "1.0.0-beta.x.y.5.77.0+aa")]
        public void VersionComparisonDefaultEqual(string version1, string version2)
        {
            // Arrange & Act
            var match = Equals(version1, version2);

            // Assert
            Assert.True(match);
        }

        [Theory]
        [InlineData("0.0.0", "1.0.0")]
        [InlineData("1.1.0", "1.0.0")]
        [InlineData("1.0.1", "1.0.0")]
        [InlineData("1.0.1", "1.0.0")]
        [InlineData("1.0.0-BETA", "1.0.0-beta2")]
        [InlineData("1.0.0+AA", "1.0.0-beta+aa")]
        [InlineData("1.0.0-BETA1+AA", "1.0.0-beta")]
        [InlineData("1.0.0-BETA.X.y.5.77.0+AA", "1.0.0-beta.x.y.5.79.0+aa")]
        public void VersionComparisonDefaultNotEqual(string version1, string version2)
        {
            // Arrange & Act
            var match = !Equals(version1, version2);

            // Assert
            Assert.True(match);
        }

        [Theory]
        [InlineData("0.0.0", "1.0.0")]
        [InlineData("1.0.0", "1.1.0")]
        [InlineData("1.0.0", "1.0.1")]
        [InlineData("1.999.9999", "2.1.1")]
        [InlineData("1.0.0-BETA", "1.0.0-beta2")]
        [InlineData("1.0.0-beta+AA", "1.0.0+aa")]
        [InlineData("1.0.0-BETA", "1.0.0-beta.1+AA")]
        [InlineData("1.0.0-BETA.X.y.5.77.0+AA", "1.0.0-beta.x.y.5.79.0+aa")]
        [InlineData("1.0.0-BETA.X.y.5.79.0+AA", "1.0.0-beta.x.y.5.790.0+abc")]
        public void VersionComparisonDefaultLess(string version1, string version2)
        {
            // Arrange & Act
            var result = Compare(version1, version2);

            // Assert
            Assert.True(result < 0);
        }

        private static int Compare(string version1, string version2)
        {
            // Act
            var x = CompareOneWay(version1, version2);
            var y = CompareOneWay(version2, version1) * -1;

            // Assert
            Assert.Equal(x, y);

            return x;
        }

        private static int Compare(SemanticVersion version1, SemanticVersion version2)
        {
            return version1.CompareTo(version2);
        }

        private static int CompareOneWay(string version1, string version2)
        {
            var a = SemanticVersion.Parse(version1);
            var b = SemanticVersion.Parse(version2);

            return Compare(a, b);
        }

        private static bool Equals(string version1, string version2)
        {
            return EqualsOneWay(version1, version2) && EqualsOneWay(version2, version1);
        }

        private static bool EqualsOneWay(string version1, string version2)
        {
            var a = SemanticVersion.Parse(version1);
            var b = SemanticVersion.Parse(version2);

            return a == b;
        }
    }
}
