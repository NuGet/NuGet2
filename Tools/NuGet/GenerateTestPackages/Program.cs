using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CSharp;
using NuGet;

namespace GenerateTestPackages {
    class Program {
        const string keyFileName = "TestPackageKey.snk";

        static Dictionary<string, PackageInfo> _packages = new Dictionary<string, PackageInfo>();

        static void Main(string[] args) {

            var document = XDocument.Load(new StreamReader(args[0]));

            XNamespace ns = "http://schemas.microsoft.com/vs/2009/dgml";

            // Parse through the dgml file and group things by Source
            _packages = document.Descendants(ns + "Link")
                .ToLookup(l => l.Attribute("Source").Value, l => l.Attribute("Target").Value)
                .Select(group => new PackageInfo(group.Key, group))
                .ToDictionary(p => p.FullName.ToString());

            // Add all the packages that only exist as targets to the dictionary
            var allPackageNames = _packages.Values.SelectMany(p => p.Dependencies).Select(dep => dep.ToString()).Distinct().ToList();
            foreach (var dependency in allPackageNames) {
                if (!_packages.ContainsKey(dependency)) {
                    _packages.Add(dependency, new PackageInfo(dependency));
                }
            }

            // Process all the packages
            foreach (var p in _packages.Values) {
                EnsurePackageProcessed(p);
            }
        }

        static void EnsurePackageProcessed(string fullName) {
            EnsurePackageProcessed(_packages[fullName]);
        }

        static void EnsurePackageProcessed(PackageInfo package) {
            if (!package.Processed) {
                ProcessPackage(package);
                package.Processed = true;
            }
        }

        static void ProcessPackage(PackageInfo package) {
            // Make sure all its dependencies are processed first
            foreach (var dependency in package.Dependencies) {
                EnsurePackageProcessed(dependency.ToString());
            }

            Console.WriteLine("Creating package {0}", package.FullName);
            CreateAssembly(package);
            CreatePackage(package);
        }

        static void CreateAssembly(PackageInfo package) {

            // Save the snk file from the embedded resource to the disk so we can use it when we compile
            using (var resStream = typeof(Program).Assembly.GetManifestResourceStream("GenerateTestPackages." + keyFileName)) {
                using (var snkStream = File.Create(keyFileName)) {
                    resStream.CopyTo(snkStream);
                }
            }


            var codeProvider = new CSharpCodeProvider();
            var compilerParams = new CompilerParameters() {
                OutputAssembly = Path.GetFullPath(GetAssemblyFullPath(package.FullName)),
                CompilerOptions = "/keyfile:" + keyFileName
            };

            // Add all the dependencies as referenced assemblies
            foreach (FullPackageName dependency in package.Dependencies) {
                compilerParams.ReferencedAssemblies.Add(GetAssemblyFullPath(dependency));
            }

            // Create the source code and compile it using CodeDom
            var generator = new AssemblySourceFileGenerator() { Package = package };
            CompilerResults results = codeProvider.CompileAssemblyFromSource(compilerParams, generator.TransformText());

            if (results.Errors.HasErrors) {
                Console.WriteLine(results.Errors[0]);
            }

            File.Delete(keyFileName);
        }

        static void CreatePackage(PackageInfo package) {
            var packageBuilder = new PackageBuilder() {
                Id = package.Id,
                Version = package.Version,
                Description = "Some test package"
            };

            packageBuilder.Authors.Add("Outercurve Foundation");

            string assemblySourcePath = GetAssemblyFullPath(package.FullName);
            packageBuilder.Files.Add(new PhysicalPackageFile() {
                SourcePath = assemblySourcePath,
                TargetPath = @"lib\" + Path.GetFileName(assemblySourcePath)
            });

            foreach (FullPackageName dependency in package.Dependencies) {
                var versionSpec = new VersionSpec() {
                    IsMinInclusive=true,
                    MinVersion=dependency.Version
                };

                packageBuilder.Dependencies.Add(new PackageDependency(dependency.Id, versionSpec));
            }

            using (var stream = File.Create(GetPackageFileFullPath(package))) {
                packageBuilder.Save(stream);
            }
        }

        static string GetAssemblyFullPath(FullPackageName fullName) {
            string relativeDir = String.Format(@"Assemblies\{0}\{1}", fullName.Id, fullName.Version);
            string fullDir = Path.GetFullPath(relativeDir);
            Directory.CreateDirectory(fullDir);
            return Path.Combine(fullDir, fullName.Id + ".dll");
        }

        static string GetPackageFileFullPath(PackageInfo package) {
            string packagesFolder = Path.GetFullPath("Packages");
            Directory.CreateDirectory(packagesFolder);
            string packageFileName = String.Format("{0}.{1}.nupkg", package.Id, package.Version);
            return Path.Combine(packagesFolder, packageFileName);
        }
    }

    class PackageInfo {
        public PackageInfo(string nameAndVersion, IEnumerable<string> dependencies = null) {
            FullName = new FullPackageName(nameAndVersion);

            Dependencies = dependencies != null ? dependencies.Select(fullName => new FullPackageName(fullName)) : Enumerable.Empty<FullPackageName>();
        }

        public FullPackageName FullName { get; private set; }
        public string Id { get { return FullName.Id; } }
        public Version Version { get { return FullName.Version; } }
        public IEnumerable<FullPackageName> Dependencies { get; private set; }
        public bool Processed { get; set; }

        public override string ToString() {
            return FullName.ToString();
        }
    }

    class FullPackageName {
        public FullPackageName(string nameAndVersion) {
            var parts = nameAndVersion.Split(':');
            Id = parts[0];
            Version = new Version(parts[1]);
        }

        public string Id { get; private set; }
        public Version Version { get; private set; }

        public override string ToString() {
            return Id + ":" + Version;
        }
    }
}
