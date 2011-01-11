namespace NuGet {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using NuGet.Common;

    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "push", "PushCommandDescription", AltName="pu",
        MinArgs = 2, MaxArgs = 2, UsageDescriptionResourceName = "PushCommandUsageDescription",
        UsageSummaryResourceName = "PushCommandUsageSummary")]
    public class PushCommand : ICommand {

        private string _apiKey;
        private string _packagePath;

        public List<string> Arguments { get; set; }

        public IConsole Console { get; set; }

        [Option(typeof(NuGetResources), "PushCommandPublishDescription", AltName = "pub")]
        public bool Publish { get; set; }

        [Option(typeof(NuGetResources), "PushCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        [ImportingConstructor]
        public PushCommand(IConsole console) {
            Console = console;
            Publish = true;
        }

        public void Execute() {
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
            
            if (Publish) {
                PublishCommand cmd = new PublishCommand(Console);
                cmd.Source = Source;
                cmd.Arguments = new List<string> { pkg.Id, pkg.Version.ToString(), _apiKey };
                cmd.Execute();
            }
        }
    }
}