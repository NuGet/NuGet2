using System;
using System.Web.Mvc;
using System.Web.Routing;
using Ninject;

namespace NuGet.Server.Infrastructure {
    public class NinjectControllerFactory : DefaultControllerFactory {
        IKernel _kernel;

        public NinjectControllerFactory(IKernel kernel) {
            _kernel = kernel;
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType) {
            if (controllerType != null) {
                return (IController)_kernel.TryGet(controllerType) ?? base.GetControllerInstance(requestContext, controllerType);
            }
            return base.GetControllerInstance(requestContext, controllerType);
        }

        public virtual IController CreateControllerInstance(RequestContext requestContext, Type controllerType) {
            return GetControllerInstance(requestContext, controllerType);
        }
    }
}
