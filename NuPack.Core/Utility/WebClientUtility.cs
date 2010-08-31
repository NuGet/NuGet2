namespace NuPack {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;

    internal static class WebClientUtility {
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The caller is responsible for disposing the package")]
        public static Package DownloadPackage(Uri packageUri) {
            using (WebClient client = new WebClient()) {
                using (MemoryStream stream = new MemoryStream(client.DownloadData(packageUri))) {
                    return new ZipPackage(stream);
                }
            }
        }
    }
}
