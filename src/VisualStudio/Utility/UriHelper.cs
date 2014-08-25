using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

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
                NuGetEventTrigger.Instance.TriggerEvent(NuGetEvent.LinkOpened);
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

            return IsHttpSource(activeSource.Source);
        }

        public static bool IsHttpSource(string source, IVsPackageSourceProvider packageSourceProvider)
        {
            if (source != null)
            {
                if (IsHttpSource(source))
                {
                    return true;
                }

                var packageSource = packageSourceProvider.GetEnabledPackageSources()
                    .FirstOrDefault(p => p.Name.Equals(source, StringComparison.CurrentCultureIgnoreCase));
                return (packageSource != null) && IsHttpSource(packageSource.Source);
            }

            return IsHttpSource(packageSourceProvider);
        }

        private static bool IsHttpUrl(Uri uri)
        {
            return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static bool IsLocal(string currentSource)
        {
            Uri currentURI;
            if (Uri.TryCreate(currentSource, UriKind.RelativeOrAbsolute, out currentURI))
            {
                if (currentURI.IsFile)
                {
                    if (Directory.Exists(currentSource))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsUNC(string currentSource)
        {
            Uri currentURI;
            if (Uri.TryCreate(currentSource, UriKind.RelativeOrAbsolute, out currentURI))
            {
                if (currentURI.IsUnc)
                {
                    if (Directory.Exists(currentSource))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsAnySourceAvailable(IVsPackageSourceProvider packageSourceProvider, bool checkHttp)
        {
            //If any of the enabled sources is http, return true
            if (checkHttp)
            {
                bool isHttpSource;
                isHttpSource = UriHelper.IsHttpSource(packageSourceProvider);
                if (isHttpSource)
                {
                    return true;
                }
            }

            if (packageSourceProvider != null)
            {
                //If any of the active sources is UNC share or local folder and is available, return true
                PackageSource activeSource = packageSourceProvider.ActivePackageSource;
                return (IsLocal(activeSource.Source) || IsUNC(activeSource.Source));
            }

            //If none of the above matched, return false
            return false;
        }
    }
}