using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ContosoUniversity.Controllers
{
    public class BaseController : Controller
    {
        protected Person CurrentUser => Session["CurrentUser"] as Person;
        protected SchoolContext db = new SchoolContext();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (CurrentUser == null && !AllowAnonymous(filterContext))
            {
                filterContext.Result = RedirectToAction("Login", "Account");
                return;
            }

            ViewBag.CurrentUser = CurrentUser;
            ViewBag.UserRole = CurrentUser?.PrimaryRole;

            base.OnActionExecuting(filterContext);
        }

        private bool AllowAnonymous(ActionExecutingContext filterContext)
        {
            return filterContext.ActionDescriptor.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any() ||
                   filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();
        }

        protected void RequireRole(Person.UserRole role)
        {
            if (CurrentUser == null || !CurrentUser.HasRole(role))
            {
                throw new HttpException(403, "Access denied");
            }
        }

        protected void RequireAnyRole(params Person.UserRole[] roles)
        {
            if (CurrentUser == null || !roles.Any(role => CurrentUser.HasRole(role)))
            {
                throw new HttpException(403, "Access denied");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}