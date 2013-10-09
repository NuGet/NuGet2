using System;
using System.Text;

namespace NuGet.WebMatrix
{
    internal class RequirementViewModel
    {
        private static string[, , , ,] _descriptionFormats;
        private Requirement _requirement;

        /// <summary>
        /// Construct a new requirement view model from the provided requirement string.
        /// </summary>
        /// <param name="requirement"></param>
        internal RequirementViewModel(string requirementTag)
        {
            _requirement = new Requirement(requirementTag);
        }

        /// <summary>
        /// Construct a requirement view model from a requirement class
        /// </summary>
        /// <param name="requirement"></param>
        internal RequirementViewModel(Requirement requirement)
        {
            _requirement = requirement;
        }

        /// <summary>
        /// Returns the original requirement specification
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _requirement.ToString();
        }

        /// <summary>
        /// Product name -- Windows
        /// </summary>
        public string Product
        {
            get
            {
                return _requirement.Product;
            }
        }

        /// <summary>
        /// The required product version number.
        /// </summary>
        public Version Version
        {
            get
            {
                return _requirement.Version;
            }
        }

        /// <summary>
        /// The required product Service Pack version number or null.
        /// </summary>
        public Version ServicePack
        {
            get
            {
                return _requirement.ServicePack;
            }
        }

        /// <summary>
        /// Named version string or null.
        /// </summary>
        public string NamedVersion
        {
            get
            {
                string namedVersion = _requirement.NamedVersion;

                if (namedVersion == "Windows XP")
                {
                    return Resources.WindowsXP;
                }
                else if (namedVersion == "Windows Vista")
                {
                    return Resources.WindowsVista;
                }
                else if (namedVersion == "Windows 7")
                {
                    return Resources.Windows7;
                }
                else if (namedVersion == "Windows 8")
                {
                    return Resources.Windows8;
                }

                return namedVersion;
            }
        }

        /// <summary>
        /// Indicates any OS with a higher Marjor.Minor version number meets the requirement.
        /// </summary>
        public bool OrGreater
        {
            get
            {
                return _requirement.OrGreater;
            }
        }

        /// <summary>
        /// Indicates the required product type -- either Client, Server, or null.
        /// </summary>
        public string Type
        {
            get
            {
                return _requirement.Type;
            }
        }

        /// <summary>
        /// Indicates the required product architecture -- either x86, x64, or null
        /// </summary>
        public string Architecture
        {
            get
            {
                return _requirement.Architecture;
            }
        }

        /// <summary>
        /// Determines if this requirement is met by the provided operating system information.
        /// </summary>
        /// <param name="OSVersion"></param>
        /// <param name="SPVersion"></param>
        /// <param name="type"></param>
        /// <param name="architecture"></param>
        /// <returns></returns>
        public bool IsMet(Version OSVersion, Version SPVersion, string type, string architecture)
        {
            return _requirement.IsMet(OSVersion, SPVersion, type, architecture);
        }

        /// <summary>
        /// Parses the provided requirement.
        /// </summary>
        /// <returns></returns>
        public bool Parse()
        {
            return _requirement.Parse();
        }

        /// <summary>
        /// Posible types of version specifications
        /// </summary>
        private enum VersionType
        {
            Named,
            Numeric,
            Count
        }

        /// <summary>
        /// Posible types of Service Pack specifications
        /// </summary>
        private enum ServicePackType
        {
            Any,
            Minimum,
            Count
        }

        /// <summary>
        /// Possible types of product type specifications
        /// </summary>
        private enum ProductType
        {
            Any,
            Client,
            Server,
            Count
        }

        /// <summary>
        /// Possible types of architecture specifications
        /// </summary>
        private enum ArchitectureType
        {
            Any,
            x86,
            x64,
            Count
        }

        /// <summary>
        /// Possible types of OrGreater specifications
        /// </summary>
        private enum IncludeType
        {
            Specified,
            Newer,
            Count
        }
                
        /// <summary>
        /// Get a human readable version of the requirement
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            VersionType versionType = GetVersionType();
            ServicePackType servicePackType = GetServicePackType();
            ProductType productType = GetProductType();
            ArchitectureType architectureType = GetArchitectureType();
            IncludeType includeType = GetIncludeType();
            string format = GetDescriptionFormat(versionType, servicePackType, productType, architectureType, includeType);
            string version = GetVersionString();
            string servicePack = GetServicePackVersionString();
            
            return string.Format(format, NamedVersion, version, servicePack);
        }

        /// <summary>
        /// Gets the version type for the requirement
        /// </summary>
        /// <returns></returns>
        private VersionType GetVersionType()
        {
            if (NamedVersion != null)
            {
                return VersionType.Named;
            }

            return VersionType.Numeric;
        }

        /// <summary>
        /// Gets the service pack type for the requirement
        /// </summary>
        /// <returns></returns>
        private ServicePackType GetServicePackType()
        {
            if (ServicePack != null)
            {
                return ServicePackType.Minimum;
            }

            return ServicePackType.Any;
        }

        /// <summary>
        /// Gets the product type for the requirement
        /// </summary>
        /// <returns></returns>
        private ProductType GetProductType()
        {
            if (Type != null)
            {
                if (Type == "Client")
                {
                    return ProductType.Client;
                }
                else if (Type == "Server")
                {
                    return ProductType.Server;
                }
            }
            
            return ProductType.Any;
        }

        /// <summary>
        /// Gets the architecture type for the requirement
        /// </summary>
        /// <returns></returns>
        private ArchitectureType GetArchitectureType()
        {
            if (Architecture != null)
            {
                if (Architecture == "x86")
                {
                    return ArchitectureType.x86;
                }
                else if (Architecture == "x64")
                {
                    return ArchitectureType.x64;
                }
            }

            return ArchitectureType.Any;
        }

        /// <summary>
        /// Gets the include type for the requirement
        /// </summary>
        /// <returns></returns>
        private IncludeType GetIncludeType()
        {
            if (OrGreater)
            {
                return IncludeType.Newer;
            }

            return IncludeType.Specified;
        }

        /// <summary>
        /// Gets the version as a string in the simplest format
        /// </summary>
        /// <returns></returns>
        private string GetVersionString()
        {
            if (Version != null)
            {
                return Version.ToString(2);
            }

            return null;
        }

        /// <summary>
        /// Gets the service pack version as a string in the simplest format
        /// </summary>
        /// <returns></returns>
        private string GetServicePackVersionString()
        {
            if (ServicePack != null)
            {
                if (ServicePack.Minor != 0)
                {
                    return ServicePack.ToString(2);
                }
                else
                {
                    return ServicePack.ToString(1);
                }
            }

            return null;
        }

        /// <summary>
        /// Locates the appropriate format string for the requirement type
        /// </summary>
        /// <param name="versionType"></param>
        /// <param name="spType"></param>
        /// <param name="productType"></param>
        /// <param name="architectureType"></param>
        /// <param name="includeType"></param>
        /// <returns></returns>
        private string GetDescriptionFormat(VersionType versionType, ServicePackType spType, ProductType productType, ArchitectureType architectureType, IncludeType includeType)
        {
            return DescriptionFormats[(int)versionType, (int)spType, (int)productType, (int)architectureType, (int)includeType];
        }

        /// <summary>
        /// Gets the requirement description formats array
        /// </summary>
        public static string[, , , ,] DescriptionFormats
        {
            get
            {
                if (_descriptionFormats == null)
                {
                    _descriptionFormats = new string[(int)VersionType.Count, (int)ServicePackType.Count, (int)ProductType.Count, (int)ArchitectureType.Count, (int)IncludeType.Count];
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNamedAnyAnyAnySpecified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNamedAnyAnyAnyNewer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNamedAnyAnyx86Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNamedAnyAnyx86Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNamedAnyAnyx64Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNamedAnyAnyx64Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNamedAnyClientAnySpecified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNamedAnyClientAnyNewer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNamedAnyClientx86Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNamedAnyClientx86Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNamedAnyClientx64Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNamedAnyClientx64Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNamedAnyServerAnySpecified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNamedAnyServerAnyNewer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNamedAnyServerx86Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNamedAnyServerx86Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNamedAnyServerx64Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNamedAnyServerx64Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNamedMinimumAnyAnySpecified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNamedMinimumAnyAnyNewer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNamedMinimumAnyx86Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNamedMinimumAnyx86Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNamedMinimumAnyx64Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNamedMinimumAnyx64Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNamedMinimumClientAnySpecified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNamedMinimumClientAnyNewer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNamedMinimumClientx86Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNamedMinimumClientx86Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNamedMinimumClientx64Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNamedMinimumClientx64Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNamedMinimumServerAnySpecified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNamedMinimumServerAnyNewer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNamedMinimumServerx86Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNamedMinimumServerx86Newer;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNamedMinimumServerx64Specified;
                    _descriptionFormats[(int)VersionType.Named, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNamedMinimumServerx64Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNumericAnyAnyAnySpecified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNumericAnyAnyAnyNewer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNumericAnyAnyx86Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNumericAnyAnyx86Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNumericAnyAnyx64Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Any, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNumericAnyAnyx64Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNumericAnyClientAnySpecified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNumericAnyClientAnyNewer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNumericAnyClientx86Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNumericAnyClientx86Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNumericAnyClientx64Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Client, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNumericAnyClientx64Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNumericAnyServerAnySpecified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNumericAnyServerAnyNewer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNumericAnyServerx86Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNumericAnyServerx86Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNumericAnyServerx64Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Any, (int)ProductType.Server, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNumericAnyServerx64Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNumericMinimumAnyAnySpecified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNumericMinimumAnyAnyNewer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNumericMinimumAnyx86Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNumericMinimumAnyx86Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNumericMinimumAnyx64Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Any, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNumericMinimumAnyx64Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNumericMinimumClientAnySpecified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNumericMinimumClientAnyNewer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNumericMinimumClientx86Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNumericMinimumClientx86Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNumericMinimumClientx64Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Client, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNumericMinimumClientx64Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.Any, (int)IncludeType.Specified] = Resources.WindowsNumericMinimumServerAnySpecified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.Any, (int)IncludeType.Newer] = Resources.WindowsNumericMinimumServerAnyNewer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.x86, (int)IncludeType.Specified] = Resources.WindowsNumericMinimumServerx86Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.x86, (int)IncludeType.Newer] = Resources.WindowsNumericMinimumServerx86Newer;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.x64, (int)IncludeType.Specified] = Resources.WindowsNumericMinimumServerx64Specified;
                    _descriptionFormats[(int)VersionType.Numeric, (int)ServicePackType.Minimum, (int)ProductType.Server, (int)ArchitectureType.x64, (int)IncludeType.Newer] = Resources.WindowsNumericMinimumServerx64Newer;
                }

                return _descriptionFormats;
            }
        }
    }
}
