using System;
using System.Net;
using System.Security;
using Console = System.Console;

namespace NuGet {
    public class ConsoleCredentialProvider : ICredentialProvider {
        public CredentialResult GetCredentials(Uri uri, IWebProxy proxy) {
            if (uri == null) {
                throw new ArgumentNullException("uri");
            }
            Console.WriteLine(NuGetResources.Credentials_ConsolePromptMessage, uri.OriginalString);
            Console.Write(NuGetResources.Credentials_UserName);
            string username = Console.ReadLine();
            Console.Write(NuGetResources.Credentials_Password);
            SecureString password = ReadLineAsSecureString();
            ICredentials credentials = new NetworkCredential {
                UserName = username,
                SecurePassword = password
            };
            return CredentialResult.Create(CredentialState.HasCredentials, credentials);
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
                        Console.Write(' ');
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        secureString.RemoveAt(secureString.Length - 1);
                    }
                    else {
                        secureString.AppendChar(keyInfo.KeyChar);
                        Console.Write('*');
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