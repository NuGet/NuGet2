using System;
using System.Web.Mvc;
using Ninject;
using System.Web.Routing;

namespace NuPack.Server.Infrastructure {
    public class NinjectControllerFactory : DefaultControllerFactory {
        IKernel _kernel;
        
        public NinjectControllerFactory(IKernel kernel) {
            _kernel = kernel;
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType) {
            return _kernel.TryGet(controllerType) as IController ?? base.GetControllerInstance(requestContext, controllerType);
        }

        public virtual IController CreateControllerInstance(RequestContext requestContext, Type controllerType) {
            return GetControllerInstance(requestContext, controllerType);
        }
    }
}