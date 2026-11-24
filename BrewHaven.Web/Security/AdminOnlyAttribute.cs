using System.Web.Mvc;
using System.Web.Routing;

namespace BrewHaven.Web.Security
{
    /// <summary>
    /// Owner gate (no user accounts): staff actions redirect to /Admin/Unlock
    /// until the passcode is entered for this session. The flag lives in the
    /// session, so closing the browser locks the area again automatically.
    /// </summary>
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;
            if (session != null && true.Equals(session["BrewHaven.IsAdmin"]))
            {
                return;
            }

            filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
            {
                { "controller", "Admin" },
                { "action", "Unlock" },
                { "returnUrl", filterContext.HttpContext.Request.RawUrl }
            });
        }
    }
}
