using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security;

namespace NuGet {
    public class ConsoleCredentialProvider : IProxyProvider {
        public IWebProxy GetProxy(Uri uri) {
            Uri proxyUri = WebRequest.DefaultWebProxy.GetProxy(uri);
            IWebProxy proxy = new WebProxy(proxyUri);

            proxy.Credentials = GetCredentials();
            return proxy;
        }

        private static NetworkCredential GetCredentials() {
            Console.Write(NuGetResources.Credentials_UserName);
            string username = Console.ReadLine();
            Console.Write(NuGetResources.Credentials_Password);
            SecureString password = ReadLineAsSecureString();
            return new NetworkCredential {
                UserName = username,
                SecurePassword = password
            };
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller's responsibility to dispose.")]
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