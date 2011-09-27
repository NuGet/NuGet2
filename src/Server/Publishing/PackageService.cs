using System;
using System.Net;
using System.Web;
using System.Web.Routing;
using NuGet.Server.DataServices;
using NuGet.Server.Infrastructure;

namespace NuGet.Server {
    public class PackageService {
        private readonly IServerPackageRepository _serverRepository;
        private readonly IPackageAuthenticationService _authenticationService;

        public PackageService(IServerPackageRepository repository,
                              IPackageAuthenticationService authenticationService) {
            _serverRepository = repository;
            _authenticationService = authenticationService;
        }

        public void CreatePackage(HttpContextBase context) {
            RouteData routeData = GetRouteData(context);
            // Get the api key from the route
            string apiKey = routeData.GetRequiredString("apiKey");

            // Get the package from the request body
            var package = new ZipPackage(context.Request.InputStream);

            // Make sure they can access this package
            Authenticate(context, apiKey, package.Id,
                         () => _serverRepository.AddPackage(package));
        }

        public void PublishPackage(HttpContextBase context) {
            // No-op
        }

        public void DeletePackage(HttpContextBase context) {
            // Only accept delete requests
            if (!context.Request.HttpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase)) {
                context.Response.StatusCode = 404;
                return;
            }

            RouteData routeData = GetRouteData(context);

            // Extract the apiKey, packageId and make sure the version if a valid version string
            // (fail to parse if it's not)
            string apiKey = routeData.GetRequiredString("apiKey");
            string packageId = routeData.GetRequiredString("packageId");
            var version = new SemanticVersion(routeData.GetRequiredString("version"));

            IPackage requestedPackage = _serverRepository.FindPackage(packageId, version);

            if (requestedPackage != null) {
                // Make sure they can access this package
                Authenticate(context, apiKey, packageId,
                             () => _serverRepository.RemovePackage(packageId, version));
            }
            else {
                // Package not found
                WritePackageNotFound(context, packageId, version);
            }
        }

        public void DownloadPackage(HttpContextBase context) {
            RouteData routeData = GetRouteData(context);
            // Get the package file name from the route
            string packageId = routeData.GetRequiredString("packageId");
            var version = new SemanticVersion(routeData.GetRequiredString("version"));

            IPackage requestedPackage = _serverRepository.FindPackage(packageId, version);

            if (requestedPackage != null) {
                Package packageMetatada = _serverRepository.GetMetadataPackage(requestedPackage);
                context.Response.AddHeader("content-disposition", String.Format("attachment; filename={0}", packageMetatada.Path));
                context.Response.ContentType = "application/zip";
                context.Response.TransmitFile(packageMetatada.FullPath);
            }
            else {
                // Package not found
                WritePackageNotFound(context, packageId, version);
            }
        }

        private static void WritePackageNotFound(HttpContextBase context, string packageId, SemanticVersion version) {
            WriteStatus(context, HttpStatusCode.NotFound, String.Format("'Package {0} {1}' Not found.", packageId, version));
        }

        private void Authenticate(HttpContextBase context, string apiKey, string packageId, Action action) {
            if (_authenticationService.IsAuthenticated(context.User, apiKey, packageId)) {
                action();
            }
            else {
                WriteForbidden(context, packageId);
            }
        }

        private static void WriteForbidden(HttpContextBase context, string packageId) {
            WriteStatus(context, HttpStatusCode.Forbidden, String.Format("Access denied for package '{0}'.", packageId));
        }

        private static void WriteStatus(HttpContextBase context, HttpStatusCode statusCode, string body = null) {
            context.Response.StatusCode = (int)statusCode;
            if (!String.IsNullOrEmpty(body)) {
                context.Response.Write(body);
            }
        }

        private RouteData GetRouteData(HttpContextBase context) {
            return RouteTable.Routes.GetRouteData(context);
        }
    }
}