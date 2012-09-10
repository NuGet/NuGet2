using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security;
using System.Xml;
using System.Xml.Linq;

namespace NuGet
{
    public static class NetPortableProfileTable
    {
        private static NetPortableProfileCollection _portableProfiles;

        public static NetPortableProfile GetProfile(string profileName)
        {
            if (String.IsNullOrEmpty(profileName))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "profileName");
            }

            if (Profiles.Contains(profileName))
            {
                return Profiles[profileName];
            }

            return null;
        }

        internal static NetPortableProfileCollection Profiles
        {
            get
            {
                if (_portableProfiles == null)
                {
                    _portableProfiles = BuildPortableProfileCollection();
                }

                return _portableProfiles;
            }
            set
            {
                // This setter is only for Unit Tests.
                _portableProfiles = value;
            }
        }

        private static NetPortableProfileCollection BuildPortableProfileCollection()
        {
            var profileCollection = new NetPortableProfileCollection();
            profileCollection.AddRange(LoadProfilesFromFramework("v4.0"));
            profileCollection.AddRange(LoadProfilesFromFramework("v4.5"));

            return profileCollection;
        }

        private static IEnumerable<NetPortableProfile> LoadProfilesFromFramework(string version)
        {
            try
            {
                string profileFilesPath =
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolderOption.DoNotVerify),
                        @"Reference Assemblies\Microsoft\Framework\.NETPortable\" + version + @"\Profile\");

                if (!Directory.Exists(profileFilesPath))
                {
                    return Enumerable.Empty<NetPortableProfile>();
                }

                return Directory.EnumerateDirectories(profileFilesPath, "Profile*").Select(LoadPortableProfile);
            }
            catch (IOException)
            {
            }
            catch (SecurityException)
            {
            }

            return Enumerable.Empty<NetPortableProfile>();
        }

        private static NetPortableProfile LoadPortableProfile(string profileDirectory)
        {
            string profileName = Path.GetFileName(profileDirectory);

            string supportedFrameworkDirectory = Path.Combine(profileDirectory, "SupportedFrameworks");
            if (!Directory.Exists(supportedFrameworkDirectory))
            {
                return new NetPortableProfile(profileName, Enumerable.Empty<FrameworkName>());
            }

            var supportedFrameworks = Directory.EnumerateFiles(supportedFrameworkDirectory, "*.xml")
                                               .Select(LoadSupportedFramework)
                                               .Where(p => p != null);

            return new NetPortableProfile(profileName, supportedFrameworks);
        }

        private static FrameworkName LoadSupportedFramework(string frameworkFile)
        {
            using (Stream stream = File.OpenRead(frameworkFile))
            {
                return LoadSupportedFramework(stream);
            }
        }

        internal static FrameworkName LoadSupportedFramework(Stream stream)
        {
            try
            {
                var document = XDocument.Load(stream);
                var root = document.Root;
                if (root.Name.LocalName.Equals("Framework", StringComparison.Ordinal))
                {
                    string identifer = root.GetOptionalAttributeValue("Identifier");
                    if (identifer == null)
                    {
                        return null;
                    }

                    string versionString = root.GetOptionalAttributeValue("MinimumVersion");
                    if (versionString == null)
                    {
                        return null;
                    }

                    Version version;
                    if (!Version.TryParse(versionString, out version))
                    {
                        return null;
                    }

                    string profile = root.GetOptionalAttributeValue("Profile");
                    if (profile == null)
                    {
                        profile = "";
                    }

                    if (profile.EndsWith("*", StringComparison.Ordinal))
                    {
                        profile = profile.Substring(0, profile.Length - 1);

                        // special case, if it was 'WindowsPhone7*', we want it to be WindowsPhone71
                        if (profile.Equals("WindowsPhone7", StringComparison.OrdinalIgnoreCase))
                        {
                            profile = "WindowsPhone71";
                        }
                        else if (identifer.Equals("Silverlight", StringComparison.OrdinalIgnoreCase) &&
                                 profile.Equals("WindowsPhone", StringComparison.OrdinalIgnoreCase) &&
                                 version == new Version(4, 0))
                        {
                            // Since the beginning of NuGet, we have been using "SL3-WP" as the moniker to target WP7 project. 
                            // However, it's been discovered recently that the real TFM for WP7 project is "Silverlight, Version=4.0, Profile=WindowsPhone".
                            // This is how the Portable Library xml describes a WP7 platform, as shown here:
                            // 
                            // <Framework
                            //     Identifier="Silverlight"
                            //     Profile="WindowsPhone*"
                            //     MinimumVersion="4.0"
                            //     DisplayName="Windows Phone"
                            //     MinimumVersionDisplayName="7" />
                            //
                            // To maintain consistent behavior with previous versions of NuGet, we want to change it back to "SL3-WP" nonetheless.

                            version = new Version(3, 0);
                        }
                    }

                    return new FrameworkName(identifer, version, profile);
                }
            }
            catch (XmlException)
            {
            }
            catch (IOException)
            {
            }
            catch (SecurityException)
            {
            }

            return null;
        }
    }
}