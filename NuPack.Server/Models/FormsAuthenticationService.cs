using System;
using System.Web.Security;

namespace NuGet.Server.Models {
    public class FormsAuthenticationService : IFormsAuthenticationService {
        public void SignIn(string userName, bool createPersistentCookie) {
            if (String.IsNullOrEmpty(userName)) throw new ArgumentException("Value cannot be null or empty.", "userName");

            FormsAuthentication.SetAuthCookie(userName, createPersistentCookie);
        }

        public void SignOut() {
            FormsAuthentication.SignOut();
        }
    }
}
