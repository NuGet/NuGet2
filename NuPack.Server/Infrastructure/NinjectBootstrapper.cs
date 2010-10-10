using System.Web.Mvc;
using Ninject;

namespace NuPack.Server.Infrastructure {
    public static class NinjectBootstrapper {
        public static void RegisterNinjectControllerFactory() {
            var kernel = new StandardKernel(new Bindings());
            ControllerBuilder.Current.SetControllerFactory(new NinjectControllerFactory(kernel));
        }
    }
}