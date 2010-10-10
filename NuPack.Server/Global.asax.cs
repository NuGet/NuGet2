using System.Data.Services;
using System.ServiceModel.Activation;
using System.Web.Mvc;
using System.Web.Routing;
using NuPack.Server.DataServices;
using NuPack.Server.Infrastructure;

namespace NuPack.Server {
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication {
        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Add odata route
            DataServiceHostFactory factory = new DataServiceHostFactory();
            var serviceRoute = new ServiceRoute("odata/v1", factory, typeof(Packages));
            serviceRoute.Defaults = new RouteValueDictionary { { "serviceType", "odata" } };
            serviceRoute.Constraints = new RouteValueDictionary { { "serviceType", "odata" } };
            routes.Add("odata", serviceRoute);

            // Map a feed route at the root
            routes.MapRoute("feed",
                            "feed",
                            new { controller = "Packages", action = "Feed" }
            );

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start() {
            AreaRegistration.RegisterAllAreas();
            NinjectBootstrapper.RegisterNinjectControllerFactory();
            RegisterRoutes(RouteTable.Routes);
        }
    }
}