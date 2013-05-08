using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Commands
{
    [Command("sign", "Signs the specified package with a certificate", MinArgs = 1, MaxArgs = 1)]
    public class SignCommand : Command
    {
        [Option("The thumbprint of the certificate to use to sign the package", AltName="t")]
        public string Thumbprint { get; set; }

        [Option("The name of the store which contains the certificate", AltName = "s")]
        public string StoreName { get; set; }

        public override void ExecuteCommand()
        {
            StoreName = StoreName ?? "My";

            if (String.IsNullOrEmpty(Thumbprint))
            {
                throw new CommandLineException("Missing required parameter: 'thumbprint'");
            }
            
            // Check the package
            if (!File.Exists(Arguments[0]))
            {
                throw new CommandLineException("File not found: " + Arguments[0]);
            }

            // Get the certificate
            X509Store store = new X509Store(StoreName);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2 cert = store.Certificates.Find(X509FindType.FindByThumbprint, Thumbprint, validOnly: false).Cast<X509Certificate2>().FirstOrDefault();
            if (cert == null)
            {
                throw new CommandLineException("Could not find certificate: " + Thumbprint);
            }

            // Do optional chain validation
            X509Chain chain = new X509Chain();
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 30);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            if (!chain.Build(cert))
            {
                Console.WriteWarning("The certificate you provided is not valid. We'll still use it but it may not be validated correctly.");
                Console.WriteWarning("The validation error was: " + chain.ChainStatus.First().StatusInformation);
            }

            // Open the package as an OPC package
            using (var package = System.IO.Packaging.ZipPackage.Open(Arguments[0]))
            {
                var manager = new PackageDigitalSignatureManager(package);
                manager.CertificateOption = CertificateEmbeddingOption.InCertificatePart;
                var partsToSign = package.GetParts().Where(p => 
                    !PackUriHelper.IsRelationshipPartUri(p.Uri) &&
                    !p.Uri.OriginalString.StartsWith("/package")).Select(p => p.Uri);
                
                // Sign the package
                manager.Sign(partsToSign, cert, Enumerable.Empty<PackageRelationshipSelector>(), "NuGet.Attached");
                // Done!
                package.Close();
            }

            Console.WriteLine("Signed!");
        }
    }
}
