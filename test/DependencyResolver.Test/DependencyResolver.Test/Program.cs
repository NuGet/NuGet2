using NuGet;
using NuGet.Resolver;
using NuGet.Test.Mocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ResolverTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceRepo = new DataServicePackageRepository(
                new Uri("https://www.nuget.org/api/v2/"));

            string[] Templates = Directory.GetFiles(@"C:\temp\TestConfigs\templates\", "*.config");
            ResolveConfig(Templates, sourceRepo);

        }

        static void ResolveConfig(string[] files, DataServicePackageRepository sourceRepo)
        {
            foreach (string config in files)
            {
                //read XML
                var path = @"c:\temp\packages";
                StreamWriter writer = File.AppendText(config + "Report.txt");
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(config);
                XmlNode packageList = xmlDoc.SelectSingleNode("//packages");

                //one package at a time in a list
                var packagesFolder = new PhysicalFileSystem(path);
                var pathResolver = new DefaultPackagePathResolver(packagesFolder);
                var packageManager = new PackageManager(sourceRepo, pathResolver, packagesFolder);
                var resolver = new ActionResolver();

                var projectLocalRepo = new PackageReferenceRepository(
                    config,
                    packageManager.LocalRepository);
                Program.RestorePackages(packageManager, projectLocalRepo);

                var projectSystem = new MockProjectSystem();
                var projectManager = new ProjectManager(
                    packageManager,
                    pathResolver,
                    projectSystem,
                    projectLocalRepo);
                ProjectManager VPManager = new ProjectManager(packageManager, pathResolver, projectSystem, projectLocalRepo);

                foreach (XmlElement packageNode in packageList)
                {
                    string id = packageNode.GetAttribute("id");
                    string version = packageNode.GetAttribute("version");
                    var package = sourceRepo.FindPackage(id.ToString(), versionSpec: null, allowPrereleaseVersions: false, allowUnlisted: false);

                    try
                    {
                        resolver.AddOperation(
                            NuGet.PackageAction.Install,
                            package,
                            VPManager);

                        IEnumerable<NuGet.Resolver.PackageAction> actions = resolver.ResolveActions();
                        writer.WriteLine("\nResolved operations:");
                        foreach (NuGet.Resolver.PackageAction action in actions)
                        {
                            Console.WriteLine(action.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine("Exception: {0}", ex);
                    }
                }

                writer.Close();

            }
        }

        private static void RestorePackages(
            PackageManager packageManager,
            PackageReferenceRepository repo)
        {
            var executor = new ActionExecutor();
            var packageReferences = repo.ReferenceFile.GetPackageReferences();
            foreach (var p in packageReferences)
            {
                if (packageManager.LocalRepository.Exists(p.Id, p.Version))
                {
                    continue;
                }

                var package = packageManager.SourceRepository.FindPackage(p.Id, p.Version);
                if (package == null)
                {
                    continue;
                }

                Console.WriteLine("Restoring {0} {1}", p.Id, p.Version);

                var operation = new NuGet.Resolver.PackageSolutionAction(
                    PackageActionType.AddToPackagesFolder, 
                    package,
                    packageManager: packageManager);
                executor.Execute(new[] { operation });
            }
        }
    }
}
