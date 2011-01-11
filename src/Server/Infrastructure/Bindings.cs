using System.Web.Security;
using Ninject.Modules;
using NuGet.Server.Models;

namespace NuGet.Server.Infrastructure {
    public class Bindings : NinjectModule {
        public override void Load() {
            IServerPackageRepository packageRepository = new ServerPackageRepository(PackageUtility.PackagePhysicalPath);
            Bind<IHashProvider>().To<CryptoHashProvider>();
            Bind<IServerPackageRepository>().ToConstant(packageRepository);
            Bind<IFormsAuthenticationService>().To<FormsAuthenticationService>();
            Bind<IMembershipService>().To<AccountMembershipService>();
            Bind<MembershipProvider>().ToConstant(Membership.Provider);
        }
    }
}
