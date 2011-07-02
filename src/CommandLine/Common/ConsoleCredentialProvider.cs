using System;
using System.Net;
using System.Security;
using Console = System.Console;

namespace NuGet {
    public class ConsoleCredentialProvider : ICredentialProvider {
        public CredentialState GetCredentials(Uri uri, out ICredentials credentials) {
            return GetCredentials(uri, null, out credentials);
        }
        public CredentialState GetCredentials(Uri uri, IWebProxy proxy, out ICredentials credentials) {
            Console.WriteLine("Please provide credentials for: {0}", uri.OriginalString);
            Console.Write("Username: ");
            string username = Console.ReadLine();
            Console.Write("Password: ");
            SecureString password = ReadLineAsSecureString();
            credentials = new NetworkCredential {
                UserName = username,
                SecurePassword = password
            };
            return CredentialState.HasCredentials;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "Caller's responsibility to dispose.")]
        public static SecureString ReadLineAsSecureString() {
            try {
                var secureString = new SecureString();
                ConsoleKeyInfo keyInfo;
                while ((keyInfo = Console.ReadKey(true)).Key != ConsoleKey.Enter) {
                    if (keyInfo.Key == ConsoleKey.Backspace) {
                        if (secureString.Length < 1) {
                            continue;
                        }
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        secureString.RemoveAt(secureString.Length - 1);
                    }
                    else {
                        secureString.AppendChar(keyInfo.KeyChar);
                        Console.Write("*");
                    }
                }
                secureString.MakeReadOnly();
                return secureString;
            }
            finally {
                Console.WriteLine(String.Empty);
            }
        }
    }
}