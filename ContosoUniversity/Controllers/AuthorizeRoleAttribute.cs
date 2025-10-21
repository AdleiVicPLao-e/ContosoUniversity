using System;
using System.Web;
using System.Web.Mvc;
using ContosoUniversity.Models;

namespace ContosoUniversity.Controllers
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        private readonly Person.UserRole _requiredRole;

        public AuthorizeRoleAttribute(Person.UserRole requiredRole)
        {
            _requiredRole = requiredRole;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!base.AuthorizeCore(httpContext))
                return false;

            var user = httpContext.Session["CurrentUser"] as Person;
            return user != null && user.HasRole(_requiredRole);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var user = filterContext.HttpContext.Session["CurrentUser"] as Person;
            if (user == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Login" }
                    });
            }
            else
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Unauthorized" }
                    });
            }
        }
    }
}