using System;
using System.Linq;

namespace NuGet.VisualStudio
{
    public static class UriHelper
    {
        public static void OpenExternalLink(Uri url)
        {
            if (url == null || !url.IsAbsoluteUri)
            {
                return;
            }

            // mitigate security risk
            if (url.IsFile || url.IsLoopback || url.IsUnc)
            {
                return;
            }

            if (IsHttpUrl(url))
            {
                // REVIEW: Will this allow a package author to execute arbitrary program on user's machine?
                // We have limited the url to be HTTP only, but is it sufficient?
                System.Diagnostics.Process.Start(url.AbsoluteUri);
            }
        }

        public static bool IsHttpSource(string source)
        {
            if (String.IsNullOrEmpty(source))
            {
                return false;
            }

            Uri uri;
            if (Uri.TryCreate(source, UriKind.Absolute, out uri))
            {
                return IsHttpUrl(uri);
            }
            else
            {
                return false;
            }
        }

        public static bool IsHttpSource(IVsPackageSourceProvider packageSourceProvider)
        {
            var activeSource = packageSourceProvider.ActivePackageSource;
            if (activeSource == null)
            {
                return false;
            }

            if (activeSource.IsAggregate())
            {
                return packageSourceProvider.GetEnabledPackageSources().Any(s => UriHelper.IsHttpSource(s.Source));
            }
            else
            {
                return UriHelper.IsHttpSource(activeSource.Source);
            }
        }

        public static bool IsHttpSource(string source, IVsPackageSourceProvider packageSourceProvider)
        {
            if (source != null)
            {
                if (UriHelper.IsHttpSource(source))
                {
                    return true;
                }

                var packageSource = packageSourceProvider.GetEnabledPackageSourcesWithAggregate()
                                                          .FirstOrDefault(p => p.Name.Equals(source, StringComparison.CurrentCultureIgnoreCase));
                return (packageSource != null) ? UriHelper.IsHttpSource(packageSource.Source) : false;
            }

            return IsHttpSource(packageSourceProvider);
        }

        private static bool IsHttpUrl(Uri uri)
        {
            return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}