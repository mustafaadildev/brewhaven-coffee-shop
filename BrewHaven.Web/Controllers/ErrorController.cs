using System.Web.Mvc;

namespace BrewHaven.Web.Controllers
{
    /// <summary>
    /// Friendly branded error pages. customErrors in Web.config sends 404s to
    /// /Error/NotFound and everything unexpected to /Error; the global
    /// HandleError attribute also renders Shared/Error.cshtml for exceptions
    /// thrown inside actions. No stack traces ever reach the browser.
    /// </summary>
    public class ErrorController : Controller
    {
        public ActionResult Index()
        {
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;
            return View("Error");
        }

        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            return View("NotFound");
        }
    }
}
