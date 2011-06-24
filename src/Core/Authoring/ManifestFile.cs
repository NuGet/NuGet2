using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NuGet.Resources;

namespace NuGet {
    [XmlType("file")]
    public class ManifestFile {
        private static readonly char[] _invalidPathCharacters = Path.GetInvalidPathChars();
        private static readonly IEnumerable<char> _invalidPathCharactersExceptWildCards = _invalidPathCharacters.Except(new[] { '*', '?' });

        [Required(ErrorMessageResourceType = typeof(NuGetResources), ErrorMessageResourceName = "Manifest_RequiredMetadataMissing")]
        [XmlAttribute("src")]
        public string Source { get; set; }

        [XmlAttribute("target")]
        public string Target { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (!String.IsNullOrEmpty(Source) && Source.Any(c => _invalidPathCharactersExceptWildCards.Contains(c))) {
                yield return new ValidationResult(String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_SourceContainsInvalidCharacters, Source), new[] { "Source" });
            }
            
            if (!String.IsNullOrEmpty(Target) && Target.Any(c => _invalidPathCharacters.Contains(c))) {
                yield return new ValidationResult(String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_TargetContainsInvalidCharacters, Target), new[] { "Target" });
            }
        }
     }
 }
