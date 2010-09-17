using System.Web.Mvc;
using Ninject;
using Ninject.Mvc3;

namespace $rootnamespace$ {
    public class NinjectMVC3 {
        public static void RegisterServices(IKernel kernel) {
            //kernel.Bind<IThingRepository>().To<SqlThingRepository>();
        }

        public static void SetupDependencyInjection() {
            // Create Ninject DI Kernel 
            IKernel kernel = new StandardKernel();

            // Register services with our Ninject DI Container
            RegisterServices(kernel);

            // Tell ASP.NET MVC 3 to use our Ninject DI Container 
            MvcServiceLocator.SetCurrent(new NinjectServiceLocator(kernel));
        }
    }
}