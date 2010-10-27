namespace NuGet {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Xml.Linq;

    internal static class XElementExtensions {
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "We don't care about base types")]
        public static string GetOptionalAttributeValue(this XElement element, string localName, string namespaceName = null) {
            XAttribute attr = null;
            if (String.IsNullOrEmpty(namespaceName)) {
                attr = element.Attribute(localName);
            }
            else {
                attr = element.Attribute(XName.Get(localName, namespaceName));
            }
            return attr != null ? attr.Value : null;
        }

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "We don't care about base types")]
        public static string GetOptionalElementValue(this XElement element, string localName, string namespaceName = null) {
            XElement child;
            if (String.IsNullOrEmpty(namespaceName)) {
                child = element.Element(localName);
            }
            else {
                child = element.Element(XName.Get(localName, namespaceName));
            }
            return child != null ? child.Value : null;
        }

        // REVIEW: We can use a stack if the perf is bad for Except and MergeWith
        public static XElement Except(this XElement source, XElement target) {
            if (target == null) {
                return source;
            }

            var attributesToRemove = from e in source.Attributes()
                                     where AttributeEquals(e, target.Attribute(e.Name))
                                     select e;
            // Remove the attributes
            foreach (var a in attributesToRemove.ToList()) {
                a.Remove();
            }

            foreach (var sourceChild in source.Elements().ToList()) {
                var targetChild = target.Element(sourceChild.Name);
                if (targetChild != null && !HasConflict(sourceChild, targetChild)) {
                    Except(sourceChild, targetChild);
                    bool hasContent = sourceChild.HasAttributes || sourceChild.HasElements;
                    if (!hasContent) {
                        // Remove the element if there is no content
                        sourceChild.Remove();
                        targetChild.Remove();
                    }
                }
            }
            return source;
        }



        public static XElement MergeWith(this XElement source, XElement target) {
            return MergeWith(source, target, null);
        }

        public static XElement MergeWith(this XElement source, XElement target, IDictionary<XName, Action<XElement, XElement>> nodeActions) {
            if (target == null) {
                return source;
            }

            // Merge the attributes
            foreach (var targetAttribute in target.Attributes()) {
                var sourceAttribute = source.Attribute(targetAttribute.Name);
                if (sourceAttribute == null) {
                    source.Add(targetAttribute);
                }
            }

            // Go through the elements to be merged
            foreach (var targetChild in target.Elements()) {
                // See if this element is in the root document
                var sourceChild = source.Element(targetChild.Name);
                if (sourceChild != null && !HasConflict(sourceChild, targetChild)) {
                    // Other wise merge recursively
                    sourceChild.MergeWith(targetChild, nodeActions);
                }
                else {
                    Action<XElement, XElement> nodeAction;
                    if (nodeActions != null && nodeActions.TryGetValue(targetChild.Name, out nodeAction)) {
                        nodeAction(source, targetChild);
                    }
                    else {
                        // If that element is null then add that node
                        source.Add(targetChild);
                    }
                }
            }
            return source;
        }

        private static bool HasConflict(XElement source, XElement target) {
            // Get all attributes as name value pairs
            var sourceAttr = source.Attributes().ToDictionary(a => a.Name, a => a.Value);
            // Loop over all the other attributes and see if there are
            foreach (var targetAttr in target.Attributes()) {
                string sourceValue;
                // if any of the attributes are in the source (names match) but the value doesn't match then we've found a conflict
                if (sourceAttr.TryGetValue(targetAttr.Name, out sourceValue) && sourceValue != targetAttr.Value) {
                    return true;
                }
            }
            return false;
        }

        private static bool AttributeEquals(XAttribute source, XAttribute target) {
            if (source == null && target == null) {
                return true;
            }

            if (source == null || target == null) {
                return false;
            }
            return source.Name == target.Name && source.Value == target.Value;
        }
    }
}
