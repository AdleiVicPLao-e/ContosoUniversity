using System.Linq;
using System.Web.Mvc;
using ContosoUniversity.Models;
using ContosoUniversity.Helpers;
using ContosoUniversity.ViewModels;
using System;

namespace ContosoUniversity.Controllers
{
    [AllowAnonymous]
    public class AccountController : BaseController
    {
        public ActionResult Login()
        {
            // If user is already logged in, redirect to appropriate dashboard
            if (CurrentUser != null && !CurrentUser.IsDeleted) // Check if user is deleted
            {
                return RedirectToDashboard(CurrentUser);
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, bool rememberMe = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError("", "Username and password are required.");
                    return View();
                }

                var person = db.People
                    .Where(p => !p.IsDeleted && p.UserName == username) // Filter deleted users
                    .FirstOrDefault();

                if (person == null)
                {
                    // Log failed login attempt (username not found or user is deleted)
                    System.Diagnostics.Debug.WriteLine($"Failed login attempt - username not found or deleted: {username}");
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View();
                }

                // Check if user is already logged in
                if (person.IsLoggedIn && LoginManager.IsUserLoggedIn(person.ID))
                {
                    ModelState.AddModelError("", "This account is already logged in elsewhere. Please contact administrator if you believe this is an error.");
                    return View();
                }

                // Verify password
                if (person.Password != password)
                {
                    // Log failed login attempt (wrong password)
                    System.Diagnostics.Debug.WriteLine($"Failed login attempt - wrong password for user: {username}");
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View();
                }

                // Check if user has at least one role
                if (person.Roles == 0)
                {
                    ModelState.AddModelError("", "User account has no assigned roles. Please contact administrator.");
                    return View();
                }

                // Update login status
                person.IsLoggedIn = true;

                // Add to active sessions
                var sessionId = Session.SessionID;
                LoginManager.TryAddSession(person.ID, sessionId);

                // Save changes to database
                db.SaveChanges();

                // Store user in session
                Session["CurrentUser"] = person;
                Session["CurrentUserId"] = person.ID;
                Session["UserRole"] = person.PrimaryRole;

                // Set authentication cookie if remember me is checked
                if (rememberMe)
                {
                    System.Web.Security.FormsAuthentication.SetAuthCookie(person.UserName, true);
                }

                // Log successful login
                System.Diagnostics.Debug.WriteLine($"Successful login: {username} ({person.PrimaryRole})");

                // Redirect based on primary role
                return RedirectToDashboard(person);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            try
            {
                var userId = Session["CurrentUserId"] as int?;
                var sessionId = Session.SessionID;

                if (CurrentUser != null && !CurrentUser.IsDeleted) // Check if user is deleted
                {
                    // Update login status in database (only if user exists and is not deleted)
                    var person = db.People
                        .Where(p => !p.IsDeleted && p.ID == CurrentUser.ID) // Filter deleted users
                        .FirstOrDefault();

                    if (person != null)
                    {
                        person.IsLoggedIn = false;
                        db.SaveChanges();
                    }

                    // Remove from active sessions
                    if (userId.HasValue)
                    {
                        LoginManager.RemoveSession(userId.Value, sessionId);
                    }

                    // Log logout
                    System.Diagnostics.Debug.WriteLine($"User logged out: {CurrentUser.UserName}");
                }

                // Clear session
                Session.Clear();
                Session.Abandon();

                // Clear authentication cookie
                System.Web.Security.FormsAuthentication.SignOut();

                // Clear session cookie
                var sessionCookie = new System.Web.HttpCookie("ASP.NET_SessionId", "");
                sessionCookie.Expires = DateTime.Now.AddYears(-1);
                Response.Cookies.Add(sessionCookie);

                TempData["Success"] = "You have been successfully logged out.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");

                // Force clear session even if there's an error
                Session.Clear();
                Session.Abandon();
                System.Web.Security.FormsAuthentication.SignOut();

                TempData["Error"] = "An error occurred during logout.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Account/MyProfile
        [HttpGet]
        public ActionResult MyProfile()
        {
            if (CurrentUser == null || CurrentUser.IsDeleted)
            {
                TempData["Error"] = "Please login to access your profile.";
                return RedirectToAction("Login");
            }

            var person = db.People
                .Where(p => !p.IsDeleted && p.ID == CurrentUser.ID)
                .FirstOrDefault();

            if (person == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Login");
            }

            var viewModel = new ProfileViewModel
            {
                ID = person.ID,
                UserName = person.UserName,
                FirstMidName = person.FirstMidName,
                LastName = person.LastName,
                Roles = person.Roles,
                PrimaryRole = person.PrimaryRole
                // The boolean properties (IsAdministrator, IsInstructor, IsStudent) 
                // are computed automatically from the Roles property
            };

            return View(viewModel);
        }

        // POST: Account/MyProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MyProfile(ProfileViewModel model)
        {
            if (CurrentUser == null || CurrentUser.IsDeleted)
            {
                TempData["Error"] = "Please login to update your profile.";
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var person = db.People
                        .Where(p => !p.IsDeleted && p.ID == CurrentUser.ID)
                        .FirstOrDefault();

                    if (person == null)
                    {
                        TempData["Error"] = "User not found.";
                        return RedirectToAction("Login");
                    }

                    // Check if username is already taken by another user
                    var existingUser = db.People
                        .Where(p => !p.IsDeleted && p.UserName == model.UserName && p.ID != CurrentUser.ID)
                        .FirstOrDefault();

                    if (existingUser != null)
                    {
                        ModelState.AddModelError("UserName", "This username is already taken. Please choose a different one.");
                        return View(model);
                    }

                    // Update user details
                    person.UserName = model.UserName;
                    person.FirstMidName = model.FirstMidName;
                    person.LastName = model.LastName;

                    db.SaveChanges();

                    // Update session with new user data
                    Session["CurrentUser"] = person;

                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction("MyProfile");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating your profile. Please try again.");
                    System.Diagnostics.Debug.WriteLine($"Profile update error: {ex.Message}");
                }
            }

            return View(model);
        }

        // GET: Account/ChangePassword
        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (CurrentUser == null || CurrentUser.IsDeleted)
            {
                TempData["Error"] = "Please login to change your password.";
                return RedirectToAction("Login");
            }

            return View(new ChangePasswordViewModel());
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (CurrentUser == null || CurrentUser.IsDeleted)
            {
                TempData["Error"] = "Please login to change your password.";
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var person = db.People
                        .Where(p => !p.IsDeleted && p.ID == CurrentUser.ID)
                        .FirstOrDefault();

                    if (person == null)
                    {
                        TempData["Error"] = "User not found.";
                        return RedirectToAction("Login");
                    }

                    // Verify current password
                    if (person.Password != model.CurrentPassword)
                    {
                        ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                        return View(model);
                    }

                    // Update password
                    person.Password = model.NewPassword;

                    db.SaveChanges();

                    TempData["Success"] = "Password changed successfully!";
                    return RedirectToAction("MyProfile");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while changing your password. Please try again.");
                    System.Diagnostics.Debug.WriteLine($"Password change error: {ex.Message}");
                }
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult ConcurrentLogin()
        {
            Session.Clear();
            System.Web.Security.FormsAuthentication.SignOut();

            ViewBag.Message = "Your account was logged in from another location. Please login again to continue.";
            return View();
        }

        [HttpGet]
        [AuthorizeRole(Person.UserRole.Administrator)]
        public ActionResult ForceLogout(int? userId)
        {
            if (!userId.HasValue)
            {
                return RedirectToAction("ManageUsers", "Administrator");
            }

            var person = db.People
                .Where(p => !p.IsDeleted && p.ID == userId.Value) // Filter deleted users
                .FirstOrDefault();

            if (person == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("ManageUsers", "Administrator");
            }

            ViewBag.TargetUser = person;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(Person.UserRole.Administrator)]
        public ActionResult ForceLogout(int userId)
        {
            try
            {
                var person = db.People
                    .Where(p => !p.IsDeleted && p.ID == userId) // Filter deleted users
                    .FirstOrDefault();

                if (person != null)
                {
                    person.IsLoggedIn = false;
                    LoginManager.ForceLogout(person.ID);
                    db.SaveChanges();

                    TempData["Success"] = $"User {person.FullName} has been forcibly logged out.";
                }
                else
                {
                    TempData["Error"] = "User not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error forcing logout: {ex.Message}";
            }

            return RedirectToAction("ManageUsers", "Administrator");
        }

        [HttpGet]
        public ActionResult Unauthorized()
        {
            return View();
        }

        [HttpGet]
        public ActionResult SessionExpired()
        {
            Session.Clear();
            TempData["Warning"] = "Your session has expired. Please login again.";
            return RedirectToAction("Login");
        }

        // Helper method to redirect to appropriate dashboard
        private ActionResult RedirectToDashboard(Person person)
        {
            if (person == null || person.IsDeleted) // Check if user is deleted
            {
                return RedirectToAction("Login");
            }

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
    }
}