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

            if (LaunchedFromVS())
            {
                throw new InvalidOperationException(LocalizedResourceManager.GetString("Error_CannotPromptForCedentials"));
            }

            string message = credentialType == CredentialType.ProxyCredentials ?
                    LocalizedResourceManager.GetString("Credentials_ProxyCredentials") :
                    LocalizedResourceManager.GetString("Credentials_RequestCredentials");
            Console.WriteLine(message, uri.OriginalString);
            Console.Write(LocalizedResourceManager.GetString("Credentials_UserName"));
            string username = Console.ReadLine();
            Console.Write(LocalizedResourceManager.GetString("Credentials_Password"));
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

        /// <summary>
        /// When running from inside VS, no input is available to our executable locking up VS.
        /// VS sets up a couple of environment variables one of which is named VisualStudioVersion. 
        /// Every time this is setup, we will just fail.
        /// </summary>
        private static bool LaunchedFromVS()
        {
            return !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("VisualStudioVersion"));
        }
    }
}