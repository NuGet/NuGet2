using System;
using System.Data.Services;
using System.ServiceModel.Activation;
using System.Web.Routing;
using NuGet.Server.DataServices;

namespace NuGet.Server {
    public class Global : System.Web.HttpApplication {
        protected void Application_Start(object sender, EventArgs e) {
            RegisterRoutes(RouteTable.Routes);
        }

        private static void RegisterRoutes(RouteCollection routes) {
            var factory = new DataServiceHostFactory();
            var serviceRoute = new ServiceRoute("odata/v1", factory, typeof(Packages));
            serviceRoute.Defaults = new RouteValueDictionary { { "serviceType", "odata" } };
            serviceRoute.Constraints = new RouteValueDictionary { { "serviceType", "odata" } };
            routes.Add("odata", serviceRoute);
        }
    }
}