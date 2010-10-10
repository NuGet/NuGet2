using System;
using System.Web;
using System.Web.Mvc;

namespace NuPack.Server.Infrastructure {
    public class ConditionalGetResult : ActionResult {
        public ConditionalGetResult(DateTime lastModified, Func<ActionResult> actionResult) {
            LastModified = lastModified;
            ConditionalActionResult = actionResult;
        }

        public DateTime LastModified { 
            get; 
            private set; 
        }

        public Func<ActionResult> ConditionalActionResult {
            get;
            private set;
        }

        public ActionResult GetActionResult(ControllerContext context) {
            if (context.HttpContext.Request.IsUnmodified(LastModified)) {
                return new HttpStatusCodeResult(304);
            }
            return ConditionalActionResult();
        }

        public void SetCachePolicy(ControllerContext context) {
            HttpCachePolicyBase cachePolicy = context.HttpContext.Response.Cache;
            cachePolicy.SetCacheability(HttpCacheability.Public);
            cachePolicy.SetLastModified(LastModified);
        }

        public override void ExecuteResult(ControllerContext context) {
            SetCachePolicy(context);
            ActionResult result = GetActionResult(context);
            result.ExecuteResult(context);
        }
    }
}