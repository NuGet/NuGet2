using System;
using System.Net;
using System.Security;

namespace NuGet
{
    public class ConsoleCredentialProvider : ICredentialProvider
    {
        public ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            string message = credentialType == CredentialType.ProxyCredentials ?
                    NuGetResources.ResourceManager.GetString("Credentials_ProxyCredentials") :
                    NuGetResources.ResourceManager.GetString("Credentials_RequestCredentials");
            Console.WriteLine(message, uri.OriginalString);
            Console.Write(NuGetResources.ResourceManager.GetString("Credentials_UserName"));
            string username = Console.ReadLine();
            Console.Write(NuGetResources.ResourceManager.GetString("Credentials_Password"));
            SecureString password = ReadLineAsSecureString();
            var credentials = new NetworkCredential
            {
                UserName = username,
                SecurePassword = password
            };

            return credentials;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "Caller's responsibility to dispose.")]
        public static SecureString ReadLineAsSecureString()
        {
            var secureString = new SecureString();

            try
            {
                ConsoleKeyInfo keyInfo;
                while ((keyInfo = Console.ReadKey(true)).Key != ConsoleKey.Enter)
                {
                    if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (secureString.Length < 1)
                        {
                            continue;
                        }
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(' ');
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        secureString.RemoveAt(secureString.Length - 1);
                    }
                    else
                    {
                        secureString.AppendChar(keyInfo.KeyChar);
                        Console.Write('*');
                    }
                }
                Console.WriteLine(String.Empty);
            }
            catch (InvalidOperationException)
            {
                // This can happen when you redirect nuget.exe input, either from the shell with "<" or 
                // from code with ProcessStartInfo. 
                // In this case, just read data from Console.ReadLine()
                foreach (var c in Console.ReadLine())
                {
                    secureString.AppendChar(c);
                }
            }

            secureString.MakeReadOnly();
            return secureString;
        }
    }
}