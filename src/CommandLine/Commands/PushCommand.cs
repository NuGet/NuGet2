using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using NuGet.Common;

namespace NuGet.Commands {
    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "push", "PushCommandDescription", AltName="pu",
        MinArgs = 2, MaxArgs = 2, UsageDescriptionResourceName = "PushCommandUsageDescription",
        UsageSummaryResourceName = "PushCommandUsageSummary")]
    public class PushCommand : Command {

        private string _apiKey;
        private string _packagePath;

        [Option(typeof(NuGetResources), "PushCommandPublishDescription", AltName = "co")]
        public bool CreateOnly { get; set; }

        [Option(typeof(NuGetResources), "PushCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        public PushCommand() {
            CreateOnly = true;
        }

        public override void ExecuteCommand() {
            //Frist argument should be the package
            _packagePath = Arguments[0];
            //Second argument should be the API Key
            _apiKey = Arguments[1];

            GalleryServer gallery;
            if (String.IsNullOrEmpty(Source)) {
                gallery = new GalleryServer();
            }
            else {
                gallery = new GalleryServer(Source);
            }

            ZipPackage pkg = new ZipPackage(_packagePath);

            Console.WriteLine(NuGetResources.PushCommandCreatingPackage, pkg.Id, pkg.Version);
            using (Stream pkgStream = pkg.GetStream()) {
                gallery.CreatePackage(_apiKey, pkgStream);
            }
            Console.WriteLine(NuGetResources.PushCommandPackageCreated);
            
            if (!CreateOnly) {
                var cmd = new PublishCommand();
                cmd.Console = Console;
                cmd.Source = Source;
                cmd.Arguments = new List<string> { pkg.Id, pkg.Version.ToString(), _apiKey };
                cmd.Execute();
            }
        }
    }
}