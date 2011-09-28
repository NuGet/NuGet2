using System.Collections.Generic;
using NuGet.Commands;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test {
    public class ProjectFactoryTest {
        [Theory]
        [PropertyData("SemanticVersionData")]
        public void ConvertToStrictSemanitcVersionConvertsTest(SemanticVersion original, SemanticVersion expected) {
            // Act
            var actual = ProjectFactory.ConvertToStrictSemanticVersion(original);

            // Assert
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> SemanticVersionData {
            get {
                yield return new object[] { new SemanticVersion("1.0.0.0"), new SemanticVersion("1.0.0") };
                yield return new object[] { new SemanticVersion("1.0.0.1"), new SemanticVersion("1.0.0.1") };
                yield return new object[] { new SemanticVersion("1.0"), new SemanticVersion("1.0.0") };
                yield return new object[] { new SemanticVersion("1.0.0.0beta"), new SemanticVersion("1.0.0beta") };
            }
        }
    }
}
