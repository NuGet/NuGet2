using System;
using System.Linq;
using System.Net;

namespace NuGet
{
    /// <summary>
    /// Represents a package repository that 
    /// </summary>
    class BlobStoragePackageRepository : PackageRepositoryBase, IPackageLookup
    {
        public override string Source
        {
            get { return NuGetConstants.DefaultFeedUrl; }
        }

        public override IQueryable<IPackage> GetPackages()
        {
            throw new NotSupportedException();
        }

        public override bool SupportsPrereleasePackages
        {
            get { return true; }
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            var uri = GetPackageUri(packageId, version);
            var httpClient = new HttpClient(uri)
            {
                Method = "GET"
            };
            try
            {
                using (var response = (HttpWebResponse)httpClient.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return new ZipPackage(response.GetResponseStream());
                    }
                }
            }
            catch (WebException)
            {
                
            }
            return null;
        }

        public bool Exists(string packageId, SemanticVersion version)
        {
            var uri = GetPackageUri(packageId, version);
            var httpClient = new HttpClient(uri)
            {
                Method = "HEAD"
            };

            try
            {
                using (var response = (HttpWebResponse) httpClient.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException exception)
            {
                using (var response = (HttpWebResponse)exception.Response)
                {
                    if (response == null || response.StatusCode != HttpStatusCode.NotFound)
                    {
                        throw;
                    }
                    return false;
                }
            }
        }

        private static Uri GetPackageUri(string packageId, SemanticVersion version)
        {
            return new Uri(String.Format("https://az320820.vo.msecnd.net/packages/{0}.{1}{2}", packageId, version, Constants.PackageExtension));
        }
    }
}
