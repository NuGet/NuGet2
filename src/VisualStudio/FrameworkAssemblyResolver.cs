using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security;
using System.Xml;
using Microsoft.Build.Utilities;

namespace NuGet.VisualStudio
{
    public static class FrameworkAssemblyResolver
    {
        // (dotNetFrameworkVersion + dotNetFrameworkProfile) is the key
        private readonly static ConcurrentDictionary<string, List<AssemblyName>> FrameworkAssembliesDictionary = new ConcurrentDictionary<string, List<AssemblyName>>();
        private const string NETFrameworkIdentifier = ".NETFramework";
        internal const string FrameworkListFileName = "RedistList\\FrameworkList.xml";

        /// <summary>
        /// This function checks if there is a framework assembly of assemblyName and of version > availableVersion
        /// in targetFramework. NOTE that this function is only applicable for .NETFramework and returns false for 
        /// all the other targetFrameworks
        /// </summary>
        public static bool IsHigherAssemblyVersionInFramework(string simpleAssemblyName, Version availableVersion, FrameworkName targetFrameworkName, IFileSystemProvider fileSystemProvider)
        {
            return IsHigherAssemblyVersionInFramework(simpleAssemblyName, availableVersion, targetFrameworkName, fileSystemProvider, 
                ToolLocationHelper.GetPathToReferenceAssemblies, FrameworkAssembliesDictionary);
        }

        /// <summary>
        /// This function checks if there is a framework assembly of assemblyName and of version > availableVersion
        /// in targetFramework. NOTE that this function is only applicable for .NETFramework and returns false for 
        /// all the other targetFrameworks
        /// </summary>
        internal static bool IsHigherAssemblyVersionInFramework(string simpleAssemblyName, 
            Version availableVersion,
            FrameworkName targetFrameworkName,
            IFileSystemProvider fileSystemProvider,
            Func<FrameworkName, IList<string>> getPathToReferenceAssembliesFunc,
            ConcurrentDictionary<string, List<AssemblyName>> frameworkAssembliesDictionary)
        {
            if (!String.Equals(targetFrameworkName.Identifier, NETFrameworkIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string dotNetFrameworkVersion = targetFrameworkName.Version + targetFrameworkName.Profile;

            if (!frameworkAssembliesDictionary.ContainsKey(dotNetFrameworkVersion))
            {
                IList<string> frameworkListFiles = getPathToReferenceAssembliesFunc(targetFrameworkName);
                List<AssemblyName> frameworkAssemblies = GetFrameworkAssemblies(frameworkListFiles, fileSystemProvider);
                frameworkAssembliesDictionary.AddOrUpdate(dotNetFrameworkVersion, frameworkAssemblies, (d, f) => frameworkAssemblies);
            }

            // Find a frameworkAssembly with the same name as assemblyName. If one exists, see if its version is greater than that of the availableversion
            return frameworkAssembliesDictionary[dotNetFrameworkVersion].Any(p => (String.Equals(p.Name, simpleAssemblyName, StringComparison.OrdinalIgnoreCase) && p.Version > availableVersion));
        }

        /// <summary>
        /// Returns the list of framework assemblies as specified in frameworklist.xml under the ReferenceAssemblies
        /// for .NETFramework. If the file is not present, an empty list is returned
        /// </summary>
        private static List<AssemblyName> GetFrameworkAssemblies(IList<string> pathToFrameworkListFiles, IFileSystemProvider fileSystemProvider)
        {
            List<AssemblyName> frameworkAssemblies = new List<AssemblyName>();
            foreach(var pathToFrameworkListFile in pathToFrameworkListFiles)
            {
                if (!String.IsNullOrEmpty(pathToFrameworkListFile))
                {
                    var fileSystemFrameworkListFile = fileSystemProvider.GetFileSystem(pathToFrameworkListFile);
                    frameworkAssemblies.AddRange(GetFrameworkAssemblies(fileSystemFrameworkListFile));
                }
            }

            return frameworkAssemblies;
        }

        /// <summary>
        /// Given a fileSystem to the path containing RedistList\Frameworklist.xml file
        /// return the list of assembly names read out from the xml file
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification ="We want to return an empty list if any error occured")]
        internal static List<AssemblyName> GetFrameworkAssemblies(IFileSystem fileSystemFrameworkListFile)
        {
            List<AssemblyName> frameworkAssemblies = new List<AssemblyName>();
            try
            {
                if (fileSystemFrameworkListFile.FileExists(FrameworkListFileName))
                {
                    using (Stream stream = fileSystemFrameworkListFile.OpenFile(FrameworkListFileName))
                    {                        
                        var document = XmlUtility.LoadSafe(stream);
                        var root = document.Root;
                        if (root.Name.LocalName.Equals("FileList", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var element in root.Elements("File"))
                            {
                                string simpleAssemblyName = element.GetOptionalAttributeValue("AssemblyName");
                                string version = element.GetOptionalAttributeValue("Version");
                                if (simpleAssemblyName == null || version == null)
                                {
                                    // Skip this file. Return an empty list
                                    // Clear frameworkAssemblies since we don't want partial results
                                    frameworkAssemblies.Clear();
                                    break;
                                }
                                else
                                {
                                    AssemblyName assemblyName = new AssemblyName();
                                    assemblyName.Name = simpleAssemblyName;
                                    assemblyName.Version = new Version(version);
                                    frameworkAssemblies.Add(assemblyName);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return frameworkAssemblies;
        }
    }
}
