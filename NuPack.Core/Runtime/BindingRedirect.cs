using System;
using System.Xml.Linq;

namespace NuGet.Runtime {
    public class BindingRedirect : IEquatable<BindingRedirect> {
        public static readonly string Namespace = "urn:schemas-microsoft-com:asm.v1";
        private string _oldVersion;
        private string _culture;

        private BindingRedirect() {
        }

        public BindingRedirect(IAssembly assembly) {
            Name = assembly.Name;
            PublicKeyToken = assembly.PublicKeyToken;
            NewVersion = assembly.Version.ToString();
            Culture = assembly.Culture;
        }

        public string AppliesTo {
            get;
            private set;
        }

        public string Name {
            get;
            private set;
        }

        public string PublicKeyToken {
            get;
            private set;
        }

        public string Culture {
            get {
                return _culture ?? "neutral";
            }
            set {
                _culture = value;
            }
        }

        public string NewVersion {
            get;
            private set;
        }

        public string OldVersion {
            get {
                return _oldVersion ?? "0.0.0.0-" + NewVersion;
            }
            set {
                _oldVersion = value;
            }
        }

        public XElement ToXElement() {
            //   <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1" appliesTo="{AppliesTo}">
            //      <dependentAssembly> 
            //         <assemblyIdentity name="{Name}" publicKeyToken="{PublicKeyToken}" culture="{Culture}" />
            //         <bindingRedirect oldVersion="{OldVersion}" newVersion="{NewVersion}"/>
            //      </dependentAssembly>
            //   </assemblyBinding>
            XElement assemblyBinding = new XElement(GetFullName("assemblyBinding"),
                         new XAttribute("appliesTo", AppliesTo ?? String.Empty),
                         new XElement(GetFullName("dependentAssembly"),
                             new XElement(GetFullName("assemblyIdentity"),
                                 new XAttribute("name", Name),
                                 new XAttribute("publicKeyToken", PublicKeyToken),
                                 new XAttribute("culture", Culture)),
                             new XElement(GetFullName("bindingRedirect"),
                                 new XAttribute("oldVersion", OldVersion),
                                 new XAttribute("newVersion", NewVersion))));


            // Remove empty attributes
            assemblyBinding.RemoveAttributes(a => String.IsNullOrEmpty(a.Value));


            return assemblyBinding;
        }

        public override string ToString() {
            return ToXElement().ToString();
        }

        public static BindingRedirect Parse(XElement assemblyBinding) {
            XElement dependentAssembly = assemblyBinding.Element(GetFullName("dependentAssembly"));
            if (dependentAssembly != null) {
                XElement assemblyIdentity = dependentAssembly.Element(GetFullName("assemblyIdentity"));
                XElement bindingRedirect = dependentAssembly.Element(GetFullName("bindingRedirect"));

                if (assemblyIdentity != null && bindingRedirect != null) {
                    BindingRedirect binding = new BindingRedirect {
                        AppliesTo = assemblyBinding.GetOptionalAttributeValue("appliesTo"),
                        Name = assemblyIdentity.Attribute("name").Value,
                        PublicKeyToken = assemblyIdentity.Attribute("publicKeyToken").Value,
                        Culture = assemblyIdentity.GetOptionalAttributeValue("culture"),
                        OldVersion = bindingRedirect.Attribute("oldVersion").Value,
                        NewVersion = bindingRedirect.Attribute("newVersion").Value
                    };

                    return binding;
                }
            }

            return null;
        }

        private static XName GetFullName(string name) {
            return XName.Get(name, Namespace);
        }

        public bool Equals(BindingRedirect other) {
            bool equals = Name.Equals(other.Name) &&
                          NewVersion.Equals(other.NewVersion) &&
                          PublicKeyToken.Equals(other.PublicKeyToken);

            if (Culture != null) {
                equals = equals && Culture.Equals(other.Culture);
            }

            if (AppliesTo != null) {
                equals = equals && AppliesTo.Equals(other.AppliesTo);
            }

            return equals;
        }

        public override bool Equals(object obj) {
            BindingRedirect other = obj as BindingRedirect;
            if (other != null) {
                return Equals(other);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode() {
            int hashCode = Name.GetHashCode() ^ NewVersion.GetHashCode() ^ PublicKeyToken.GetHashCode();

            if (Culture != null) {
                hashCode ^= Culture.GetHashCode();
            }

            if (AppliesTo != null) {
                hashCode ^= AppliesTo.GetHashCode();
            }

            return hashCode;
        }
    }
}
