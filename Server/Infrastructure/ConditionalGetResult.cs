using System;
using System.Web;
using System.Web.Mvc;

namespace NuGet.Server.Infrastructure {
    public class ConditionalGetResult : ActionResult {
        public ConditionalGetResult(DateTimeOffset lastModified, Func<ActionResult> actionResult) {
            LastModified = lastModified;
            ConditionalActionResult = actionResult;
        }

        public DateTimeOffset LastModified { 
            get; 
            private set; 
        }

        public Func<ActionResult> ConditionalActionResult {
            get;
            private set;
        }

        public ActionResult GetActionResult(ControllerContext context) {
            if (context.HttpContext.Request.IsUnmodified(LastModified.ToUniversalTime())) {
                return new HttpStatusCodeResult(304);
            }
            return ConditionalActionResult();
        }

        public void SetCachePolicy(ControllerContext context) {
            HttpCachePolicyBase cachePolicy = context.HttpContext.Response.Cache;
            cachePolicy.SetCacheability(HttpCacheability.Public);
            cachePolicy.SetLastModified(LastModified.UtcDateTime);
        }

        public override void ExecuteResult(ControllerContext context) {
            SetCachePolicy(context);
            ActionResult result = GetActionResult(context);
            result.ExecuteResult(context);
        }
    }
}
