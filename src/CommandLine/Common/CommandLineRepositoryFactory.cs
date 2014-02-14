
using System.Windows;

namespace NuGet.Common
{
    public class CommandLineRepositoryFactory : PackageRepositoryFactory, IWeakEventListener
    {
        public static readonly string UserAgent = "NuGet Command Line";

        private readonly IConsole _console;

        public CommandLineRepositoryFactory(IConsole console)
        {
            _console = console;
        }

        public override IPackageRepository CreateRepository(string packageSource)
        {
            var repository = base.CreateRepository(packageSource);
            var httpClientEvents = repository as IHttpClientEvents;
            if (httpClientEvents != null)
            {
                SendingRequestEventManager.AddListener(httpClientEvents, this);
            }

            return repository;
        }

        public bool ReceiveWeakEvent(System.Type managerType, object sender, System.EventArgs e)
        {
            if (managerType == typeof(SendingRequestEventManager))
            {
                var args = (WebRequestEventArgs)e;
                if (_console.Verbosity == Verbosity.Detailed)
                {
                    _console.WriteLine(
                        System.ConsoleColor.Green,
                        "{0} {1}", args.Request.Method, args.Request.RequestUri);
                }
                string userAgent = HttpUtility.CreateUserAgentString(CommandLineConstants.UserAgent);
                HttpUtility.SetUserAgent(args.Request, userAgent);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}