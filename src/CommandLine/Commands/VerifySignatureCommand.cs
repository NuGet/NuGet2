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
    [Command("verify", "Verifies a signed NuGet package", MinArgs = 1, MaxArgs = 1)]
    public class VerifySignatureCommand : Command
    {
        public override void ExecuteCommand()
        {
            // Check the package file
            if (!File.Exists(Arguments[0]))
            {
                throw new CommandLineException("File not found: " + Arguments[0]);
            }

            // Open the package
            using (var package = System.IO.Packaging.ZipPackage.Open(Arguments[0]))
            {
                var manager = new PackageDigitalSignatureManager(package);

                Console.WriteLine("Found the following signatures in the package:");
                foreach (var signature in manager.Signatures)
                {
                    var cert = new X509Certificate2(signature.Signer);
                    Console.WriteLine("----");
                    Console.WriteLine(" Subject: " + cert.Subject);
                    Console.WriteLine(" Signed: " + signature.SigningTime.ToString("O"));
                    Console.WriteLine(" Expires: " + cert.GetExpirationDateString());
                    Console.WriteLine(" Chain:");

                    X509Chain chain = new X509Chain();
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 30);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    chain.Build(cert);
                    foreach (var element in chain.ChainElements)
                    {
                        if (element.ChainElementStatus.Length > 0 && element.ChainElementStatus[0].Status == X509ChainStatusFlags.UntrustedRoot)
                        {
                            Console.WriteLine("  Untrusted Subject: " + element.Certificate.Subject);
                        }
                        else
                        {
                            if (element.ChainElementStatus.Length != 0)
                            {
                                Console.WriteLine("  Status: " + element.ChainElementStatus[0].Status);
                            }
                            Console.WriteLine("  Subject: " + element.Certificate.Subject);
                        }
                    }
                }
            }
        }
    }
}
