namespace NuGet.Test {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class XmlExtensionsTest {
        [TestMethod]
        public void MergingWithSameTagDifferentAttributesWithNoConflictsMergesAttributes() {
            // Arrange
            XElement a = XElement.Parse(@"<foo><bar a=""aValue"" /></foo>");
            XElement b = XElement.Parse(@"<foo><bar b=""bValue"" /></foo>");

            // Act
            var result = a.MergeWith(b);

            // Assert
            Assert.AreEqual(1, result.Elements("bar").Count());
            XElement barElement = result.Element("bar");
            Assert.IsNotNull(barElement);
            Assert.AreEqual(2, barElement.Attributes().Count());
            AssertAttributeValue(barElement, "a", "aValue");
            AssertAttributeValue(barElement, "b", "bValue");
        }

        [TestMethod]
        public void MergingWithNodeActions() {
            // Arrange
            XElement a = XElement.Parse(@"<foo><baz /></foo>");
            XElement b = XElement.Parse(@"<foo><bar /></foo>");

            // Act
            var result = a.MergeWith(b, new Dictionary<XName, Action<XElement, XElement>> {
                { "bar", (parent, element) => parent.AddFirst(element) }
            });

            // Assert
            var elements = result.Elements().ToList();
            Assert.AreEqual(2, elements.Count);
            Assert.AreEqual("bar", elements[0].Name);
            Assert.AreEqual("baz", elements[1].Name);
        }

        [TestMethod]
        public void MergingWithoutInsertionMappingsAddsToEnd() {
            // Arrange
            XElement a = XElement.Parse(@"<foo><baz /></foo>");
            XElement b = XElement.Parse(@"<foo><bar /></foo>");

            // Act
            var result = a.MergeWith(b);

            // Assert
            var elements = result.Elements().ToList();
            Assert.AreEqual(2, elements.Count);
            Assert.AreEqual("baz", elements[0].Name);
            Assert.AreEqual("bar", elements[1].Name);
        }

        [TestMethod]
        public void MergingElementsWithMultipleSameAttributeNamesAndValuesDoesntDuplicateEntries() {
            // Act
            XElement a = XElement.Parse(@"<tests>
    <test name=""one"" value=""foo"" />
    <test name=""two"" value=""bar"" />
</tests>");

            XElement b = XElement.Parse(@"<tests>
    <test name=""one"" value=""foo"" />
    <test name=""two"" value=""bar"" />
</tests>");

            // Act
            var result = a.MergeWith(b);

            // Assert
            var elements = result.Elements("test").ToList();
            Assert.AreEqual(2, elements.Count);
            AssertAttributeValue(elements[0], "name", "one");
            AssertAttributeValue(elements[0], "value", "foo");
            AssertAttributeValue(elements[1], "name", "two");
            AssertAttributeValue(elements[1], "value", "bar");
        }

        [TestMethod]
        public void MergingElementsWithMultipleEntiresAddsEntryIfNotExists() {
            // Act
            XElement a = XElement.Parse(@"<tests>
    <test name=""one"" value=""foo"" />
    <test name=""two"" value=""bar"" />
</tests>");

            XElement b = XElement.Parse(@"<tests>
    <test name=""one"" value=""foo"" />
    <test name=""two"" value=""bar"" />
    <test name=""three"" value=""baz"" />
</tests>");

            // Act
            var result = a.MergeWith(b);

            // Assert
            var elements = result.Elements("test").ToList();
            Assert.AreEqual(3, elements.Count);
            AssertAttributeValue(elements[0], "name", "one");
            AssertAttributeValue(elements[0], "value", "foo");
            AssertAttributeValue(elements[1], "name", "two");
            AssertAttributeValue(elements[1], "value", "bar");
            AssertAttributeValue(elements[2], "name", "three");
            AssertAttributeValue(elements[2], "value", "baz");
        }

        [TestMethod]
        public void MergingTagWithConflictsAddsTag() {
            // Arrange
            XElement a = XElement.Parse(@"<connectionStrings><add name=""sqlce"" connectionString=""|DataDirectory|\foo.sdf"" /></connectionStrings>");
            XElement b = XElement.Parse(@"<connectionStrings><add name=""sqlserver"" connectionString=""foo.bar"" /></connectionStrings>");

            // Act
            var result = a.MergeWith(b);

            // Assert
            var elements = result.Elements("add").ToList();
            Assert.AreEqual(2, elements.Count);
            AssertAttributeValue(elements[0], "name", "sqlce");
            AssertAttributeValue(elements[0], "connectionString", @"|DataDirectory|\foo.sdf");
            AssertAttributeValue(elements[1], "name", "sqlserver");
            AssertAttributeValue(elements[1], "connectionString", "foo.bar");
        }

        [TestMethod]
        public void ExceptWithTagsNoConflicts() {
            // Arrange
            XElement a = XElement.Parse(@"<foo><bar b=""2"" /></foo>");
            XElement b = XElement.Parse(@"<foo><bar a=""1"" b=""2"" /></foo>");

            // Act
            var result = a.Except(b);

            // Assert
            Assert.AreEqual(0, result.Elements("bar").Count());
        }

        [TestMethod]
        public void ExceptWithTagsWithConflicts() {
            // Arrange
            XElement a = XElement.Parse(@"<foo><bar b=""2"" a=""g"" /></foo>");
            XElement b = XElement.Parse(@"<foo><bar a=""1"" b=""2"" /></foo>");

            // Act
            var result = a.Except(b);

            // Assert
            Assert.AreEqual(1, result.Elements("bar").Count());
            XElement barElement = result.Element("bar");
            Assert.IsNotNull(barElement);
            Assert.AreEqual(2, barElement.Attributes().Count());
            AssertAttributeValue(barElement, "a", "g");
            AssertAttributeValue(barElement, "b", "2");
        }

        [TestMethod]
        public void ExceptWithSimilarTagsRemovesTagsThatChanged() {
            // Act
            XElement a = XElement.Parse(@"<tests>
    <test name=""One"" value=""foo"" />
    <test name=""two"" value=""bar"" />
</tests>");

            XElement b = XElement.Parse(@"<tests>
    <test name=""one"" value=""foo"" />
    <test name=""two"" value=""bar"" />
</tests>");

            // Act
            var result = a.Except(b);

            // Assert
            Assert.AreEqual(@"<tests>
  <test name=""One"" value=""foo"" />
</tests>", result.ToString());
        }

        [TestMethod]
        public void ExceptWithSimilarTagsRemovesTagsThatWereReordered() {
            // Act
            XElement a = XElement.Parse(@"
<configuration>
<tests>
    <test name=""one"" value=""foo"" />
    <test name=""two"" value=""bar"" />
</tests>
</configuration>");

            XElement b = XElement.Parse(@"
<configuration>
<tests>
    <test name=""two"" value=""bar"" />
    <test name=""one"" value=""foo"" />
</tests>
</configuration>");

            // Act
            var result = a.Except(b);

            // Assert
            Assert.AreEqual("<configuration />", result.ToString());
        }

        private static void AssertAttributeValue(XElement element, string attributeName, string expectedAttributeValue) {
            XAttribute attr = element.Attribute(attributeName);
            Assert.IsNotNull(attr);
            Assert.AreEqual(expectedAttributeValue, attr.Value);
        }
    }
}
