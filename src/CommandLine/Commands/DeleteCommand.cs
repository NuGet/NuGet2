namespace NuGet {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using NuGet.Common;

    [Export(typeof(ICommand))]
    [Command(typeof(NuGetResources), "delete", "DeleteCommandDescription", AltName = "del",
        MinArgs = 3, MaxArgs = 3, UsageDescriptionResourceName = "DeleteCommandUsageDescription",
        UsageSummaryResourceName = "DeleteCommandUsageSummary")]
    public class DeleteCommand : ICommand {
        
        private string _apiKey;
        private string _packageId;
        private string _packageVersion;

        public List<string> Arguments { get; set; }
        public IConsole Console { get; set; }

        [Option(typeof(NuGetResources), "DeleteCommandSourceDescription", AltName = "src")]
        public string Source { get; set; }

        [ImportingConstructor]
        public DeleteCommand(IConsole console) {
            Console = console;
        }

        public void Execute() {
            //Frist argument should be the package ID
            _packageId = Arguments[0];
            //Second argument should be the package Version
            _packageVersion = Arguments[1];
            //Third argument should be the API Key
            _apiKey = Arguments[2];


            GalleryServer gallery;
            if (String.IsNullOrEmpty(Source)) {
                gallery = new GalleryServer();
            }
            else { 
                gallery = new GalleryServer(Source); 
            }

            Console.WriteLine(NuGetResources.DeleteCommandDeletingPackage, _packageId, _packageVersion);
            gallery.DeletePackage(_apiKey, _packageId, _packageVersion);
            Console.WriteLine(NuGetResources.DeleteCommandDeletedPackage, _packageId, _packageVersion);
        }
    }
}