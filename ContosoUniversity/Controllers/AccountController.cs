using System.Linq;
using System.Web.Mvc;
using ContosoUniversity.Models;

namespace ContosoUniversity.Controllers
{
    [AllowAnonymous]
    public class AccountController : BaseController
    {
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            var person = db.People.FirstOrDefault(p => p.UserName == username);

            if (person != null && person.Password == password)
            {
                Session["CurrentUser"] = person;

                // Redirect based on primary role
                switch (person.PrimaryRole)
                {
                    case Person.UserRole.Administrator:
                        return RedirectToAction("Dashboard", "Administrator");

                    case Person.UserRole.Instructor:
                        return RedirectToAction("Dashboard", "Instructor");

                    case Person.UserRole.Student:
                        return RedirectToAction("Dashboard", "Student");

                    default:
                        return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View();
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}