using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using NuGet.Common;

namespace NuGet.Commands {
    [Command(typeof(NuGetResources), "push", "PushCommandDescription",
        MinArgs = 2, MaxArgs = 2, UsageDescriptionResourceName = "PushCommandUsageDescription",
        UsageSummaryResourceName = "PushCommandUsageSummary")]
    public class PushCommand : Command {
        private string _apiKey;
        private string _packagePath;

        [Option(typeof(NuGetResources), "PushCommandCreateOnlyDescription")]
        public bool CreateOnly { get; set; }

        [Option(typeof(NuGetResources), "PushCommandSourceDescription")]
        public string Source { get; set; }

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

            ZipPackage package = new ZipPackage(_packagePath);

            Console.WriteLine(NuGetResources.PushCommandCreatingPackage, package.GetFullName());

            using (Stream pkgStream = package.GetStream()) {
                gallery.CreatePackage(_apiKey, pkgStream);
            }

            if (!CreateOnly) {
                var cmd = new PublishCommand();
                cmd.Console = Console;
                cmd.Source = Source;
                cmd.Arguments = new List<string> { package.Id, package.Version.ToString(), _apiKey };
                cmd.Execute();
            }
            else {
                Console.WriteLine(NuGetResources.PushCommandPackageCreated);
            }
        }
    }
}