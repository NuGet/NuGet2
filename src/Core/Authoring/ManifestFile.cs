using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NuGet.Authoring;
using NuGet.Resources;

namespace NuGet
{
    [XmlType("file")]
    public class ManifestFile : IValidatableObject
    {
        private static readonly char[] _invalidTargetChars = Path.GetInvalidFileNameChars().Except(new[] { '\\', '/', '.' }).ToArray();
        private static readonly char[] _invalidSourceCharacters = Path.GetInvalidPathChars();

        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_RequiredMetadataMissing")]
        [XmlAttribute("src")]
        public string Source { get; set; }

        [XmlAttribute("target")]
        public string Target { get; set; }

        [XmlAttribute("exclude")]
        public string Exclude { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is needed for xml serialization")]
        [XmlArray("properties")]
        public List<ManifestFileProperty> Properties
        {
            get;
            set;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!String.IsNullOrEmpty(Source) && Source.IndexOfAny(_invalidSourceCharacters) != -1)
            {
                yield return new ValidationResult(String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_SourceContainsInvalidCharacters, Source));
            }

            if (!String.IsNullOrEmpty(Target) && Target.IndexOfAny(_invalidTargetChars) != -1)
            {
                yield return new ValidationResult(String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_TargetContainsInvalidCharacters, Target));
            }

            if (!String.IsNullOrEmpty(Exclude) && Exclude.IndexOfAny(_invalidSourceCharacters) != -1)
            {
                yield return new ValidationResult(String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_ExcludeContainsInvalidCharacters, Exclude));
            }
        }
    }
}