using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace NuGet.Test
{
    public class XmlExtensionsTest
    {
        [Fact]
        public void MergingWithSameTagDifferentAttributesWithNoConflictsMergesAttributes()
        {
            // Arrange
            XElement a = XElement.Parse(@"<foo><bar a=""aValue"" /></foo>");
            XElement b = XElement.Parse(@"<foo><bar b=""bValue"" /></foo>");

            // Act
            var result = a.MergeWith(b);

            // Assert
            Assert.Equal(1, result.Elements("bar").Count());
            XElement barElement = result.Element("bar");
            Assert.NotNull(barElement);
            Assert.Equal(2, barElement.Attributes().Count());
            AssertAttributeValue(barElement, "a", "aValue");
            AssertAttributeValue(barElement, "b", "bValue");
        }

        [Fact]
        public void MergingWithNodeActions()
        {
            // Arrange
            XElement a = XElement.Parse(@"<foo><baz /></foo>");
            XElement b = XElement.Parse(@"<foo><bar /></foo>");

            // Act
            var result = a.MergeWith(b, new Dictionary<XName, Action<XElement, XElement>> {
                { "bar", (parent, element) => parent.AddFirst(element) }
            });

            // Assert
            var elements = result.Elements().ToList();
            Assert.Equal(2, elements.Count);
            Assert.Equal("bar", elements[0].Name);
            Assert.Equal("baz", elements[1].Name);
        }

        [Fact]
        public void MergingWithoutInsertionMappingsAddsToEnd()
        {
            // Arrange
            XElement a = XElement.Parse(@"<foo><baz /></foo>");
            XElement b = XElement.Parse(@"<foo><bar /></foo>");

            // Act
            var result = a.MergeWith(b);

            // Assert
            var elements = result.Elements().ToList();
            Assert.Equal(2, elements.Count);
            Assert.Equal("baz", elements[0].Name);
            Assert.Equal("bar", elements[1].Name);
        }

        [Fact]
        public void MergingElementsWithMultipleSameAttributeNamesAndValuesDoesntDuplicateEntries()
        {
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
            Assert.Equal(2, elements.Count);
            AssertAttributeValue(elements[0], "name", "one");
            AssertAttributeValue(elements[0], "value", "foo");
            AssertAttributeValue(elements[1], "name", "two");
            AssertAttributeValue(elements[1], "value", "bar");
        }

        [Fact]
        public void MergingElementsMergeComments()
        {
            // Act
            XElement a = XElement.Parse(@"<tests>
    <test name=""one"" value=""foo"" />
    <special>
    </special>
    <!-- old comment -->
</tests>");

            XElement b = XElement.Parse(@"<tests>
    <!-- this is a comment -->
    <test name=""one"" value=""foo"">
        <!-- this is a comment inside element -->
        <child>
            <!-- this is a nested comment -->
        </child>
        <!-- comment before new element -->
        <!-- second comment before new element -->
        <rock />
    </test>
    <!-- comment at the end -->
</tests>");

            // Act
            var result = a.MergeWith(b).ToString();

            // Assert            
            Assert.Equal(@"<tests>
  <!-- this is a comment -->
  <test name=""one"" value=""foo"">
    <!-- this is a comment inside element -->
    <child>
      <!-- this is a nested comment -->
    </child>
    <!-- comment before new element -->
    <!-- second comment before new element -->
    <rock />
  </test>
  <special></special>
  <!-- old comment -->
  <!-- comment at the end -->
</tests>", result);
        }

        [Fact]
        public void MergingElementsMergeCommentsWhenThereIsNoChildElement()
        {
            // Act
            XElement a = XElement.Parse(@"<tests>
    <test name=""one"" value=""foo"" />
</tests>");

            XElement b = XElement.Parse(@"<tests>
    <!-- this file contains only comment
         hahaha, you like that? -->
    <!-- dark knight rises -->
</tests>");

            // Act
            var result = a.MergeWith(b).ToString();

            // Assert            
            Assert.Equal(@"<tests>
  <test name=""one"" value=""foo"" />
  <!-- this file contains only comment
         hahaha, you like that? -->
  <!-- dark knight rises -->
</tests>", result);
        }

        [Fact]
        public void MergingElementsWithMultipleEntiresAddsEntryIfNotExists()
        {
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
            Assert.Equal(3, elements.Count);
            AssertAttributeValue(elements[0], "name", "one");
            AssertAttributeValue(elements[0], "value", "foo");
            AssertAttributeValue(elements[1], "name", "two");
            AssertAttributeValue(elements[1], "value", "bar");
            AssertAttributeValue(elements[2], "name", "three");
            AssertAttributeValue(elements[2], "value", "baz");
        }

        [Fact]
        public void MergingTagWithConflictsAddsTag()
        {
            // Arrange
            XElement a = XElement.Parse(@"<connectionStrings><add name=""sqlce"" connectionString=""|DataDirectory|\foo.sdf"" /></connectionStrings>");
            XElement b = XElement.Parse(@"<connectionStrings><add name=""sqlserver"" connectionString=""foo.bar"" /></connectionStrings>");

            // Act
            var result = a.MergeWith(b);

            // Assert
            var elements = result.Elements("add").ToList();
            Assert.Equal(2, elements.Count);
            AssertAttributeValue(elements[0], "name", "sqlce");
            AssertAttributeValue(elements[0], "connectionString", @"|DataDirectory|\foo.sdf");
            AssertAttributeValue(elements[1], "name", "sqlserver");
            AssertAttributeValue(elements[1], "connectionString", "foo.bar");
        }

        [Fact]
        public void ExceptWithTagsNoConflicts()
        {
            // Arrange
            XElement a = XElement.Parse(@"<foo><bar b=""2"" /></foo>");
            XElement b = XElement.Parse(@"<foo><bar a=""1"" b=""2"" /></foo>");

            // Act
            var result = a.Except(b);

            // Assert
            Assert.Equal(0, result.Elements("bar").Count());
        }

        [Fact]
        public void ExceptWithTagsWithConflicts()
        {
            // Arrange
            XElement a = XElement.Parse(@"<foo><bar b=""2"" a=""g"" /></foo>");
            XElement b = XElement.Parse(@"<foo><bar a=""1"" b=""2"" /></foo>");

            // Act
            var result = a.Except(b);

            // Assert
            Assert.Equal(1, result.Elements("bar").Count());
            XElement barElement = result.Element("bar");
            Assert.NotNull(barElement);
            Assert.Equal(2, barElement.Attributes().Count());
            AssertAttributeValue(barElement, "a", "g");
            AssertAttributeValue(barElement, "b", "2");
        }

        [Fact]
        public void ExceptRemoveComments()
        {
            // Act
            XElement a = XElement.Parse(@"<tests>
    <test name=""One"" value=""foo"" />
    <test name=""two"" value=""bar"" />
    <!-- comment -->
</tests>");

            XElement b = XElement.Parse(@"<tests>
    <!-- comment -->
    <test name=""two"" value=""bar"" />
</tests>");

            // Act
            var result = a.Except(b);

            // Assert
            Assert.Equal(@"<tests>
  <test name=""One"" value=""foo"" />
</tests>", result.ToString());
        }

        [Fact]
        public void ExceptDoNotRemoveCommentIfCommentsDoNotMatch()
        {
            // Act
            XElement a = XElement.Parse(@"<tests>
    <test name=""One"" value=""foo"" />
    <test name=""two"" value=""bar"" />
    <!-- this is a comment -->
</tests>");

            XElement b = XElement.Parse(@"<tests>
    <!-- comment -->
    <test name=""two"" value=""bar"" />
</tests>");

            // Act
            var result = a.Except(b);

            // Assert
            Assert.Equal(@"<tests>
  <test name=""One"" value=""foo"" />
  <!-- this is a comment -->
</tests>", result.ToString());
        }

        [Fact]
        public void ExceptWithSimilarTagsRemovesTagsThatChanged()
        {
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
            Assert.Equal(@"<tests>
  <test name=""One"" value=""foo"" />
</tests>", result.ToString());
        }

        [Fact]
        public void ExceptWithSimilarTagsRemovesTagsThatWereReordered()
        {
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
            Assert.Equal("<configuration />", result.ToString());
        }

        [Fact]
        public void AddWithIndentWorksForSelfEnclosedElement()
        {
            // Arrange
            const string xml = @"<root>
  <container />
</root>";
            XElement root = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
            XElement container = root.Elements().First();
            XElement content = XElement.Parse("<a><b>text</b><c/></a>");

            // Act
            container.AddIndented(content);

            // Assert
            Assert.Equal(@"<root>
  <container>
    <a>
      <b>text</b>
      <c />
    </a>
  </container>
</root>", root.ToString());
        }

        [Fact]
        public void AddWithIndentWorksForEmptyElement()
        {
            // Arrange
            const string xml = @"<root>
  <container>
  </container>
</root>";
            XElement root = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
            XElement container = root.Elements().First();
            XElement content = XElement.Parse("<a><b>text</b><c/></a>");

            // Act
            container.AddIndented(content);

            // Assert
            Assert.Equal(@"<root>
  <container>
    <a>
      <b>text</b>
      <c />
    </a>
  </container>
</root>", root.ToString());
        }

        [Fact]
        public void AddWithIndentWorksForElementWithChildren()
        {
            // Arrange
            const string xml = @"<root>
  <container>
    <child />
  </container>
</root>";
            XElement root = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
            XElement container = root.Elements().First();
            XElement content = XElement.Parse("<a><b>text</b><c/></a>");

            // Act
            container.AddIndented(content);

            // Assert
            Assert.Equal(@"<root>
  <container>
    <child />
    <a>
      <b>text</b>
      <c />
    </a>
  </container>
</root>", root.ToString());
        }

        [Fact]
        public void AddWithIndentUsesTabs()
        {
            // Arrange
            string xml = @"<root>
  <container>
    <child />
  </container>
</root>".Replace("  ", "\t");
            XElement root = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
            XElement container = root.Elements().First();
            XElement content = XElement.Parse("<a><b>text</b><c/></a>");

            // Act
            container.AddIndented(content);

            // Assert
            Assert.Equal(@"<root>
  <container>
    <child />
    <a>
      <b>text</b>
      <c />
    </a>
  </container>
</root>".Replace("  ", "\t"), root.ToString());
        }

        private static void AssertAttributeValue(XElement element, string attributeName, string expectedAttributeValue)
        {
            XAttribute attr = element.Attribute(attributeName);
            Assert.NotNull(attr);
            Assert.Equal(expectedAttributeValue, attr.Value);
        }
    }
}
