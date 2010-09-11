using System;

namespace NuPack.Dialog {
    internal class Utilities {
        public static IServiceProvider ServiceProvider {
            get;
            set;
        }

        public static T GetService<S, T>() where T : class {
            if (ServiceProvider == null) { return null; }
            else { return ServiceProvider.GetService(typeof(S)) as T; }
        }
    }
}
