using System;
using System.Web.Mvc;

namespace NuGet.Server.Infrastructure {
    //TODO: Remove when upgraded to MVC 3.
    public class HttpStatusCodeResult : ActionResult {

        public HttpStatusCodeResult(int statusCode)
            : this(statusCode, null) {
        }

        public HttpStatusCodeResult(int statusCode, string statusDescription) {
            this.StatusCode = statusCode;
            this.StatusDescription = statusDescription;
        }

        public override void ExecuteResult(ControllerContext context) {
            if (context == null) {
                throw new ArgumentNullException("context");
            }
            context.HttpContext.Response.StatusCode = this.StatusCode;
            if (this.StatusDescription != null) {
                context.HttpContext.Response.StatusDescription = this.StatusDescription;
            }
        }

        public int StatusCode {
            get;
            private set;
        }

        public string StatusDescription {
            get;
            private set;
        }
    }
}
