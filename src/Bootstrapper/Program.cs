using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using NuGet;

namespace Bootstrapper {
    public class Program {
        private const string NuGetCommandLinePackageId = "nuget.commandline";
        private const string GalleryUrl = "http://packages.nuget.org/v1/FeedService.svc";
        private const string NuGetExeFilePath = "/tools/NuGet.exe";

        public static int Main(string[] args) {
            try {
                Console.WriteLine("NuGet bootstrapper {0}", typeof(Program).Assembly.GetName().Version);

                // Setup the proxy for gallery requests
                Uri galleryUri = new Uri(GalleryUrl);

                // Register a console based credentials provider so that the user get's prompted if a password
                // is required for the proxy
                var consoleCredentialProvider = new ConsoleCredentialProvider();
                HttpClient.DefaultProxyFinder.RegisterProvider(consoleCredentialProvider);
                HttpClient.DefaultRequestCredentialService.RegisterProvider(consoleCredentialProvider);
                // Setup IHttpClient for the Gallery to locate packages
                var httpClient = new HttpClient(galleryUri);

                // Get the package from the feed
                var repository = new DataServicePackageRepository(httpClient);
                var packageMetadata = repository.GetPackages().Where(p => p.Id.ToLower() == NuGetCommandLinePackageId)
                    .AsEnumerable()
                    .OrderByDescending(p => Version.Parse(p.Version))
                    .FirstOrDefault();

                if (packageMetadata != null) {
                    Console.WriteLine("Found NuGet.exe version {0}.", packageMetadata.Version);
                    Console.WriteLine("Downloading...");

                    Uri uri = repository.GetReadStreamUri(packageMetadata);
                    var downloadClient = new HttpClient(uri);
                    var packageStream = new MemoryStream(downloadClient.DownloadData());

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

                return 0;
            }
            catch (Exception e) {
                WriteError(e);
            }

            return 1;
        }

        private static void WriteError(Exception e) {
            var currentColor = Console.ForegroundColor;
            try {                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e.GetBaseException().Message);
            }
            finally {
                Console.ForegroundColor = currentColor;
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
