using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Helpers;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System;

namespace ContosoUniversity.Controllers
{
    public class BaseController : Controller
    {
        protected Person CurrentUser => Session["CurrentUser"] as Person;
        protected SchoolContext db = new SchoolContext();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Check if user session exists but user is not logged in database
            if (CurrentUser != null)
            {
                var currentUserInDb = db.People
                    .Where(p => !p.IsDeleted && p.ID == CurrentUser.ID) // Filter deleted users
                    .FirstOrDefault();

                if (currentUserInDb == null || !currentUserInDb.IsLoggedIn)
                {
                    // User was forcibly logged out, deleted, or session is invalid
                    Session.Clear();
                    System.Web.Security.FormsAuthentication.SignOut();

                    if (!AllowAnonymous(filterContext))
                    {
                        filterContext.Result = RedirectToAction("Login", "Account");
                        return;
                    }
                }
                else
                {
                    // Update session with fresh user data (excluding deleted users)
                    Session["CurrentUser"] = currentUserInDb;
                }
            }

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
            if (CurrentUser == null || CurrentUser.IsDeleted || !CurrentUser.HasRole(role)) // Check if user is deleted
            {
                throw new HttpException(403, "Access denied");
            }
        }

        protected void RequireAnyRole(params Person.UserRole[] roles)
        {
            if (CurrentUser == null || CurrentUser.IsDeleted || !roles.Any(role => CurrentUser.HasRole(role))) // Check if user is deleted
            {
                throw new HttpException(403, "Access denied");
            }
        }

        protected bool IsUserLoggedInElsewhere()
        {
            if (CurrentUser == null || CurrentUser.IsDeleted) return false; // Check if user is deleted

            var currentUserInDb = db.People
                .Where(p => !p.IsDeleted && p.ID == CurrentUser.ID) // Filter deleted users
                .FirstOrDefault();

            return currentUserInDb != null && currentUserInDb.IsLoggedIn &&
                   !LoginManager.IsUserSessionValid(CurrentUser.ID, Session.SessionID);
        }

        protected void CheckConcurrentLogin()
        {
            if (IsUserLoggedInElsewhere())
            {
                Session.Clear();
                System.Web.Security.FormsAuthentication.SignOut();
                throw new HttpException(440, "Session expired due to concurrent login");
            }
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            // Check for concurrent logins on every request (except anonymous)
            if (CurrentUser != null && !CurrentUser.IsDeleted && !AllowAnonymous(new ActionExecutingContext // Check if user is deleted
            {
                ActionDescriptor = filterContext.ActionDescriptor,
                Controller = filterContext.Controller
            }))
            {
                try
                {
                    CheckConcurrentLogin();
                }
                catch (HttpException ex) when (ex.GetHttpCode() == 440)
                {
                    filterContext.Result = RedirectToAction("ConcurrentLogin", "Account");
                }
            }

            base.OnActionExecuted(filterContext);
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