namespace NuGet {
    using System;
    using System.Globalization;
    using System.Text;
    using Microsoft.Internal.Web.Utils;

    public class PackageDependency {
        public PackageDependency(string id, Version minVersion, Version maxVersion)
            : this(id) {
            MinVersion = minVersion;
            MaxVersion = maxVersion;
        }

        public PackageDependency(string id, Version version)
            : this(id) {
            if (version == null) {
                throw new ArgumentNullException("version");
            }
            Version = version;
        }

        public PackageDependency(string id) {
            if (String.IsNullOrEmpty(id)) {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "id");
            }
            Id = id;
        }

        public string Id {
            get;
            private set;
        }

        public Version MaxVersion {
            get;
            private set;
        }

        public Version MinVersion {
            get;
            private set;
        }

        public Version Version {
            get;
            private set;
        }

        internal static PackageDependency CreateDependency(string id, Version minVersion = null, Version maxVersion = null, Version version = null) {
            if (version != null) {
                return new PackageDependency(id, version);
            }
            return new PackageDependency(id, minVersion, maxVersion);
        }

        public override string ToString() {
            // {Id} (= {Version})
            // {Id} (>= {MinVersion})
            // {Id} (<= {MaxVersion})
            // {Id} (>= {MinVersion} && <= {MaxVersion})

            StringBuilder versionBuilder = new StringBuilder();
            if (Version != null) {
                versionBuilder.AppendFormat(CultureInfo.InvariantCulture, "(= {0})", Version);
            }
            else {
                if (MinVersion != null) {
                    versionBuilder.AppendFormat(CultureInfo.InvariantCulture, "(>= {0}", MinVersion);
                }

                if (MaxVersion != null) {
                    if (versionBuilder.Length > 0) {
                        versionBuilder.AppendFormat(CultureInfo.InvariantCulture, " && <= {0}", MaxVersion);
                    }
                    else {
                        versionBuilder.AppendFormat(CultureInfo.InvariantCulture, "(<= {0}", MaxVersion);
                    }
                }

                if (versionBuilder.Length > 0) {
                    versionBuilder.Append(")");
                }
            }

            if (versionBuilder.Length > 0) {
                return Id + " " + versionBuilder.ToString();
            }
            return Id;
        }
    }
}
