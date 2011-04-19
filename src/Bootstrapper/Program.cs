using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using NuGet.NuGetService;

namespace Bootstrapper {
    public class Program {
        private const string NuGetCommandLinePackageId = "nuget.commandline";
        private const string GalleryUrl = "http://packages.nuget.org/v1/FeedService.svc";
        private const string NuGetExeFilePath = "/tools/NuGet.exe";

        public static void Main(string[] args) {
            Console.WriteLine("NuGet bootstrapper {0}", typeof(Program).Assembly.GetName().Version);

            // Get the package from the feed
            var context = new GalleryFeedContext(new Uri(GalleryUrl));
            var packageMetadata = context.Packages.Where(p => p.Id.ToLower() == NuGetCommandLinePackageId)
                                         .AsEnumerable()
                                         .OrderByDescending(p => Version.Parse(p.Version))
                                         .FirstOrDefault();

            if (packageMetadata != null) {
                Console.WriteLine("Found NuGet.exe version {0}.", packageMetadata.Version);
                Console.WriteLine("Downloading...");

                Uri uri = context.GetReadStreamUri(packageMetadata);
                // TODO: Handle proxys
                WebClient client = new WebClient();
                var packageStream = new MemoryStream(client.DownloadData(uri));

                using (Package package = Package.Open(packageStream)) {
                    var fileUri = PackUriHelper.CreatePartUri(new Uri(NuGetExeFilePath, UriKind.Relative));
                    PackagePart nugetExePart = package.GetPart(fileUri);

                    if (nugetExePart != null) {
                        // Get the exe path and move it to a temp file (NuGet.exe.old) so we can replace the running exe with the bits we got 
                        // from the package repository
                        string exePath = typeof(Program).Assembly.Location;
                        string renamedPath = exePath + ".old";
                        Move(exePath, renamedPath);

                        // Update the file
                        UpdateFile(exePath, nugetExePart);
                        Console.WriteLine("Update complete.");
                    }
                }
            }
        }

        private static void UpdateFile(string exePath, PackagePart part) {
            using (Stream fromStream = part.GetStream(), toStream = File.Create(exePath)) {
                fromStream.CopyTo(toStream);
            }
        }

        private static void Move(string oldPath, string newPath) {
            try {
                if (File.Exists(newPath)) {
                    File.Delete(newPath);
                }
            }
            catch (FileNotFoundException) {

            }

            File.Move(oldPath, newPath);
        }
    }
}
