using Ninject.Modules;
using NuPack.Server.Models;
using System.Web.Security;

namespace NuPack.Server.Infrastructure {
    public class Bindings : NinjectModule {
        public override void Load() {
            var packageStore = new FileBasedPackageStore(PackageUtility.PackagePhysicalPath);
            var packageRepository = new LocalPackageRepository(PackageUtility.PackagePhysicalPath);
            Bind<IPackageStore>().ToConstant(packageStore);
            Bind<IPackageRepository>().ToConstant(packageRepository);
            Bind<IFormsAuthenticationService>().To<FormsAuthenticationService>();
            Bind<IMembershipService>().To<AccountMembershipService>();
            Bind<MembershipProvider>().ToConstant(Membership.Provider);
        }
    }
}