using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Resources;

namespace NuGet
{
    public class PackageType
    {
        private static readonly PackageType _defaultType = new PackageType("LegacyConventions", version: new Version());
        private static readonly PackageType _managedCodeConventions = new PackageType("Managed", version: new Version(2, 0));

        public PackageType(string name, Version version)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "name");
            }

            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            Name = name;
            Version = version;
        }

        /// <summary>
        /// Gets the legacy\default package type.
        /// </summary>
        public static PackageType Default
        {
            get { return _defaultType; }
        }

        /// <summary>
        /// Gets the package type that supports managed code conventions.
        /// </summary>
        public static PackageType Managed
        {
            get { return _managedCodeConventions; }
        }

        public string Name { get; private set; }

        public Version Version { get; private set; }
    }
}
