using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security;
using System.Xml;
using VersionStringISetTuple = System.Tuple<System.Version, System.Collections.Generic.ISet<string>>;

namespace NuGet
{
    public static class NetPortableProfileTable
    {
        private const string PortableReferenceAssemblyPathEnvironmentVariableName = "NuGetPortableReferenceAssemblyPath";

        private static Lazy<CompiledNetPortableProfileCollection> _lazyPortableProfiles
            = new Lazy<CompiledNetPortableProfileCollection>(() => new CompiledNetPortableProfileCollection(BuildPortableProfileCollection()));
        private static CompiledNetPortableProfileCollection _portableProfiles;

        private static CompiledNetPortableProfileCollection Compiled
        {
            get
            {
                return _portableProfiles ?? _lazyPortableProfiles.Value;
            }
        }

        public static NetPortableProfile GetProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "profileName");
            }

            // Original behavior fully preserved, as we first try the original behavior.
            // NOTE: this could be a single TryGetValue if this collection was kept as a dictionary...
            if (Compiled.Profiles.Contains(profileName))
            {
                return Compiled.Profiles[profileName];
            }

            // If we didn't get a profile by the simple profile name, try now with 
            // the custom profile string (i.e. "net40-client")
            NetPortableProfile result = null;
            Compiled.PortableProfilesByCustomProfileString.TryGetValue(profileName, out result);

            return result;
        }

        internal static NetPortableProfileCollection Profiles
        {
            private get
            {
                return Compiled.Profiles;
            }

            set
            {
                if (value == null)
                {
                    _portableProfiles = null;
                }
                else
                {
                    _portableProfiles = new CompiledNetPortableProfileCollection(value);
                }

                // This setter is only for unit tests and is NOT thread-safe.
                // Reset the lazily loaded profiles.
                _lazyPortableProfiles = new Lazy<CompiledNetPortableProfileCollection>(() => new CompiledNetPortableProfileCollection(BuildPortableProfileCollection()));
            }
        }

        private static IDictionary<string, NetPortableProfile> CreatePortableProfilesByCustomProfileString(NetPortableProfileCollection profileCollection)
        {
            return profileCollection.ToDictionary(x => x.CustomProfileString);
        }

        private static IDictionary<string, List<VersionStringISetTuple>> CreateOptionalFrameworksDictionary(NetPortableProfileCollection profileCollection)
        {
            var portableProfilesSetByOptionalFrameworks = new Dictionary<string, List<VersionStringISetTuple>>();
            foreach (var portableProfile in profileCollection)
            {
                foreach (var optionalFramework in portableProfile.OptionalFrameworks)
                {
                    if (optionalFramework != null && optionalFramework.Identifier != null)
                    {
                        // Add portableProfile.Name to the list of profileName corresponding to optionalFramework.Identifier
                        if (!portableProfilesSetByOptionalFrameworks.ContainsKey(optionalFramework.Identifier))
                        {
                            portableProfilesSetByOptionalFrameworks.Add(optionalFramework.Identifier, new List<VersionStringISetTuple>());
                        }
                    }

                    List<VersionStringISetTuple> listVersionStringISetTuple = portableProfilesSetByOptionalFrameworks[optionalFramework.Identifier];
                    if (listVersionStringISetTuple != null)
                    {
                        VersionStringISetTuple versionStringITuple = listVersionStringISetTuple.Where(tuple => tuple.Item1.Equals(optionalFramework.Version)).FirstOrDefault();
                        if (versionStringITuple == null)
                        {
                            versionStringITuple = new VersionStringISetTuple(optionalFramework.Version, new HashSet<string>());
                            listVersionStringISetTuple.Add(versionStringITuple);
                        }
                        versionStringITuple.Item2.Add(portableProfile.Name);
                    }
                }
            }

            return portableProfilesSetByOptionalFrameworks;
        }

        internal static bool HasCompatibleProfileWith(NetPortableProfile packageFramework, FrameworkName projectOptionalFrameworkName)
        {
            List<VersionStringISetTuple> versionProfileISetTupleList = null;

            // In the dictionary _portableProfilesSetByOptionalFrameworks, 
            // key is the identifier of the optional framework and value is the tuple of (optional Framework Version, set of profiles in which they are optional)
            // We try to get a value with key as projectOptionalFrameworkName.Identifier. If one exists, we check if the project version is >= the version from the retrieved tuple
            // If so, then, we see if one of the profiles, in the set from the retrieved tuple, is compatible with the packageFramework profile
            if (Compiled.PortableProfilesSetByOptionalFrameworks.TryGetValue(projectOptionalFrameworkName.Identifier, out versionProfileISetTupleList))
            {
                foreach (var versionProfileISetTuple in versionProfileISetTupleList)
                {
                    if (projectOptionalFrameworkName.Version >= versionProfileISetTuple.Item1)
                    {
                        foreach (var profileName in versionProfileISetTuple.Item2)
                        {
                            NetPortableProfile profile = GetProfile(profileName);
                            if (profile != null && packageFramework.IsCompatibleWith(profile))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static NetPortableProfileCollection BuildPortableProfileCollection()
        {
            var profileCollection = new NetPortableProfileCollection();

            string portableRootDirectory;

            string portableReferencePathOverride = Environment.GetEnvironmentVariable(PortableReferenceAssemblyPathEnvironmentVariableName);
            if (!string.IsNullOrEmpty(portableReferencePathOverride))
            {
                portableRootDirectory = portableReferencePathOverride;
            }
            else
            {
                portableRootDirectory =
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolderOption.DoNotVerify),
                        @"Reference Assemblies\Microsoft\Framework\.NETPortable");
            }

            if (Directory.Exists(portableRootDirectory))
            {
                foreach (string versionDir in Directory.EnumerateDirectories(portableRootDirectory, "v*", SearchOption.TopDirectoryOnly))
                {
                    string profileFilesPath = versionDir + @"\Profile\";
                    profileCollection.AddRange(LoadProfilesFromFramework(versionDir, profileFilesPath));
                }
            }

            return profileCollection;
        }

        private static IEnumerable<NetPortableProfile> LoadProfilesFromFramework(string version, string profileFilesPath)
        {
            if (Directory.Exists(profileFilesPath))
            {
                try
                {
                    // Note the only change here is that we also pass the .NET framework version (which exists as a parent folder of the 
                    // actual profile directory, so that we don't lose that information.
                    return Directory.EnumerateDirectories(profileFilesPath, "Profile*")
                                    .Select(profileDir => LoadPortableProfile(version, profileDir))
                                    .Where(p => p != null);
                }
                catch (IOException)
                {
                }
                catch (SecurityException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return Enumerable.Empty<NetPortableProfile>();
        }

        private static NetPortableProfile LoadPortableProfile(string version, string profileDirectory)
        {
            string profileName = Path.GetFileName(profileDirectory);

            string supportedFrameworkDirectory = Path.Combine(profileDirectory, "SupportedFrameworks");
            if (!Directory.Exists(supportedFrameworkDirectory))
            {
                return null;
            }

            return LoadPortableProfile(version, profileName, new PhysicalFileSystem(supportedFrameworkDirectory),
                Directory.EnumerateFiles(supportedFrameworkDirectory, "*.xml"));
        }

        internal static NetPortableProfile LoadPortableProfile(string version, string profileName, IFileSystem fileSystem, IEnumerable<string> frameworkFiles)
        {
            var frameworks = frameworkFiles.Select(p => LoadSupportedFramework(fileSystem, p)).Where(p => p != null);
            // Bug - 2926
            var optionalFrameworks = frameworks.Where(p => IsOptionalFramework(p)).ToList();

            // If there are no optionalFrameworks, just set supportedFrameworks = frameworks
            var supportedFrameworks = optionalFrameworks.IsEmpty() ? frameworks : frameworks.Where(p => !optionalFrameworks.Contains(p));

            return new NetPortableProfile(version, profileName, supportedFrameworks, optionalFrameworks);
        }

        private static bool IsOptionalFramework(FrameworkName framework)
        {
            return framework.Identifier.StartsWith("Mono", StringComparison.OrdinalIgnoreCase) ||
                framework.Identifier.StartsWith("Xamarin", StringComparison.OrdinalIgnoreCase);
        }

        private static FrameworkName LoadSupportedFramework(IFileSystem fileSystem, string frameworkFile)
        {
            using (Stream stream = fileSystem.OpenFile(frameworkFile))
            {
                return LoadSupportedFramework(stream);
            }
        }

        internal static FrameworkName LoadSupportedFramework(Stream stream)
        {
            try
            {
                var document = XmlUtility.LoadSafe(stream);
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

        private class CompiledNetPortableProfileCollection
        {
            public CompiledNetPortableProfileCollection(NetPortableProfileCollection profileCollection)
            {
                if (profileCollection == null)
                {
                    throw new ArgumentNullException(nameof(profileCollection));
                }

                Profiles = profileCollection;
                PortableProfilesByCustomProfileString = CreatePortableProfilesByCustomProfileString(profileCollection);
                PortableProfilesSetByOptionalFrameworks = CreateOptionalFrameworksDictionary(profileCollection);
            }

            // This collection is the original indexed collection where profiles are indexed by 
            // the full "ProfileXXX" naming. 
            public NetPortableProfileCollection Profiles { get; }

            // In order to make the NetPortableProfile.Parse capable of also parsing so-called 
            // "custom profile string" version (i.e. "net40-client"), we need an alternate index
            // by this key. I used dictionary here since I saw no value in creating a custom collection 
            // like it's done already for the _portableProfiles. Not sure why it's done that way there.
            public IDictionary<string, NetPortableProfile> PortableProfilesByCustomProfileString { get; }

            // Key is the identifier of the optional framework and value is the list of tuple of
            // (optional Framework Version, set of profiles in which they are optional).
            public IDictionary<string, List<VersionStringISetTuple>> PortableProfilesSetByOptionalFrameworks { get; }
        }
    }
}
