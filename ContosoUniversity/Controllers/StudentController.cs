using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;

namespace ContosoUniversity.Controllers
{
    public class StudentController : BaseController
    {
        // GET: Student/Dashboard
        public ActionResult Dashboard()
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var student = db.Students
                    .Where(s => !s.IsDeleted && s.ID == CurrentUser.ID)
                    .Include(s => s.Enrollments.Select(e => e.Course.Department))
                    .Include(s => s.Enrollments.Select(e => e.Course.Instructors))
                    .Include(s => s.Enrollments.Select(e => e.Course.Instructors.Select(i => i.OfficeAssignment)))
                    .FirstOrDefault();

                if (student == null)
                {
                    TempData["Error"] = "Student not found";
                    return RedirectToAction("Logout", "Account");
                }

                return View(student);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred while loading your dashboard: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Dashboard error: {ex.Message}\n{ex.StackTrace}");
                return View("Error");
            }
        }

        // GET: Student/MyGrades
        public ActionResult MyGrades()
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var enrollments = db.Enrollments
                    .Include(e => e.Course.Department)
                    .Include(e => e.Course.Instructors)
                    .Include(e => e.Course.Instructors.Select(i => i.OfficeAssignment))
                    .Include(e => e.Student)
                    .Where(e => e.StudentID == CurrentUser.ID &&
                               !e.Student.IsDeleted &&
                               !e.Course.IsDeleted &&
                               !e.Course.Department.IsDeleted)
                    .ToList();

                return View(enrollments);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred while loading your grades: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"MyGrades error: {ex.Message}\n{ex.StackTrace}");
                return View("Error");
            }
        }

        // GET: Student/AvailableCourses
        public ActionResult AvailableCourses()
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var courses = db.Courses
                    .Include(c => c.Department)
                    .Include(c => c.Instructors)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .Include(c => c.Enrollments)
                    .Where(c => c.IsActive &&
                               !c.IsDeleted &&
                               !c.Department.IsDeleted &&
                               c.Instructors.Any(i => !i.IsDeleted) &&
                               c.Enrollments.Count(e => !e.Student.IsDeleted) < c.Capacity)
                    .ToList();

                return View(courses);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred while loading available courses: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"AvailableCourses error: {ex.Message}\n{ex.StackTrace}");
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnrollInCourse(int courseId)
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var student = db.Students
                    .Where(s => !s.IsDeleted && s.ID == CurrentUser.ID)
                    .Include(s => s.Enrollments.Select(e => e.Course))
                    .FirstOrDefault();

                if (student == null)
                {
                    TempData["Error"] = "Student not found";
                    return RedirectToAction("AvailableCourses");
                }

                var course = db.Courses
                    .Where(c => !c.IsDeleted && c.CourseID == courseId && c.IsActive && !c.Department.IsDeleted)
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .FirstOrDefault();

                if (course == null)
                {
                    TempData["Error"] = "Course not found or not available";
                    return RedirectToAction("AvailableCourses");
                }

                if (student.Enrollments.Any(e => e.CourseID == courseId && e.Course != null && !e.Course.IsDeleted))
                {
                    TempData["Error"] = "You are already enrolled in this course";
                    return RedirectToAction("AvailableCourses");
                }

                var activeEnrollmentsCount = course.Enrollments.Count(e => e.Student != null && !e.Student.IsDeleted);
                if (activeEnrollmentsCount >= course.Capacity)
                {
                    TempData["Error"] = "This course has reached its capacity";
                    return RedirectToAction("AvailableCourses");
                }

                var enrollment = new Enrollment
                {
                    StudentID = student.ID,
                    CourseID = courseId,
                    Grade = null
                };

                db.Enrollments.Add(enrollment);
                db.SaveChanges();

                TempData["Success"] = $"Successfully enrolled in {course.Title}";
                return RedirectToAction("Dashboard");
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                var errorMessages = dbEx.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                TempData["Error"] = $"Validation error: {fullErrorMessage}";
                System.Diagnostics.Debug.WriteLine($"Enrollment validation error: {fullErrorMessage}");
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbUpEx)
            {
                TempData["Error"] = "Database update error occurred while enrolling in the course.";
                System.Diagnostics.Debug.WriteLine($"Enrollment update error: {dbUpEx.Message}\n{dbUpEx.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while enrolling in the course: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Enrollment error: {ex.Message}\n{ex.StackTrace}");
            }

            return RedirectToAction("AvailableCourses");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DropCourse(int enrollmentId)
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var enrollment = db.Enrollments
                    .Include(e => e.Course)
                    .Include(e => e.Student)
                    .Where(e => e.EnrollmentID == enrollmentId &&
                               e.StudentID == CurrentUser.ID &&
                               !e.Student.IsDeleted &&
                               !e.Course.IsDeleted)
                    .FirstOrDefault();

                if (enrollment == null)
                {
                    TempData["Error"] = "Enrollment not found";
                    return RedirectToAction("Dashboard");
                }

                var courseTitle = enrollment.Course?.Title ?? "the course";

                db.Enrollments.Remove(enrollment);
                db.SaveChanges();

                TempData["Success"] = $"Successfully dropped {courseTitle}";
                return RedirectToAction("Dashboard");
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbUpEx)
            {
                TempData["Error"] = "Database error occurred while dropping the course.";
                System.Diagnostics.Debug.WriteLine($"Drop course update error: {dbUpEx.Message}\n{dbUpEx.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while dropping the course: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Drop course error: {ex.Message}\n{ex.StackTrace}");
            }

            return RedirectToAction("Dashboard");
        }

        // GET: Student/MySchedule
        public ActionResult MySchedule()
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var enrollments = db.Enrollments
                    .Include(e => e.Course.Department)
                    .Include(e => e.Course.Instructors)
                    .Include(e => e.Course.Instructors.Select(i => i.OfficeAssignment))
                    .Include(e => e.Student)
                    .Where(e => e.StudentID == CurrentUser.ID &&
                               !e.Student.IsDeleted &&
                               !e.Course.IsDeleted &&
                               !e.Course.Department.IsDeleted &&
                               e.Course.Instructors.Any(i => !i.IsDeleted))
                    .ToList();

                return View(enrollments);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred while loading your schedule: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"MySchedule error: {ex.Message}\n{ex.StackTrace}");
                return View("Error");
            }
        }

        // GET: Student/CourseDetails/{id}
        public ActionResult CourseDetails(int id)
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var course = db.Courses
                    .Include(c => c.Department)
                    .Include(c => c.Instructors)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .Include(c => c.Enrollments)
                    .Where(c => c.CourseID == id &&
                               !c.IsDeleted &&
                               !c.Department.IsDeleted &&
                               c.Instructors.Any(i => !i.IsDeleted))
                    .FirstOrDefault();

                if (course == null)
                {
                    TempData["Error"] = "Course not found";
                    return RedirectToAction("AvailableCourses");
                }

                return View(course);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred while loading course details: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"CourseDetails error: {ex.Message}\n{ex.StackTrace}");
                return View("Error");
            }
        }

        // GET: Student/MyProfile
        public ActionResult MyProfile()
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var student = db.Students
                    .Where(s => !s.IsDeleted && s.ID == CurrentUser.ID)
                    .Include(s => s.Enrollments.Select(e => e.Course.Department))
                    .Include(s => s.Enrollments.Select(e => e.Course.Instructors))
                    .Include(s => s.Enrollments.Select(e => e.Course.Instructors.Select(i => i.OfficeAssignment)))
                    .FirstOrDefault();

                if (student == null)
                {
                    TempData["Error"] = "Student not found";
                    return RedirectToAction("Logout", "Account");
                }

                return View(student);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred while loading your profile: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"MyProfile error: {ex.Message}\n{ex.StackTrace}");
                return View("Error");
            }
        }

        // GET: Student/AcademicProgress
        public ActionResult AcademicProgress()
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var enrollments = db.Enrollments
                    .Include(e => e.Course.Department)
                    .Include(e => e.Course)
                    .Include(e => e.Student)
                    .Where(e => e.StudentID == CurrentUser.ID &&
                               !e.Student.IsDeleted &&
                               !e.Course.IsDeleted &&
                               !e.Course.Department.IsDeleted)
                    .ToList();

                var progress = new StudentProgressViewModel
                {
                    Enrollments = enrollments,
                    TotalCreditsAttempted = enrollments.Where(e => !e.Course.IsDeleted).Sum(e => e.Course.Credits),
                    TotalCreditsEarned = enrollments.Where(e => e.Grade.HasValue &&
                                                               e.Grade.Value != Grade.F &&
                                                               !e.Course.IsDeleted)
                                                   .Sum(e => e.Course.Credits),
                    GPA = CalculateGPA(enrollments)
                };

                return View(progress);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred while loading academic progress: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"AcademicProgress error: {ex.Message}\n{ex.StackTrace}");
                return View("Error");
            }
        }

        // GET: Student/CourseCatalog
        public ActionResult CourseCatalog(string departmentFilter, string searchString)
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var courses = db.Courses
                    .Include(c => c.Department)
                    .Include(c => c.Instructors)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .Include(c => c.Enrollments)
                    .Where(c => c.IsActive &&
                               !c.IsDeleted &&
                               !c.Department.IsDeleted &&
                               c.Instructors.Any(i => !i.IsDeleted))
                    .AsQueryable();

                if (!string.IsNullOrEmpty(departmentFilter) && int.TryParse(departmentFilter, out int deptId))
                {
                    courses = courses.Where(c => c.DepartmentID == deptId && !c.Department.IsDeleted);
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    courses = courses.Where(c =>
                        c.Title.Contains(searchString) ||
                        c.Description.Contains(searchString) ||
                        c.CourseID.ToString().Contains(searchString));
                }

                ViewBag.DepartmentFilter = new SelectList(
                    db.Departments.Where(d => !d.IsDeleted).OrderBy(d => d.Name),
                    "DepartmentID",
                    "Name",
                    departmentFilter);

                ViewBag.SearchString = searchString;

                return View(courses.OrderBy(c => c.CourseID).ToList());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred while loading the course catalog: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"CourseCatalog error: {ex.Message}\n{ex.StackTrace}");
                return View("Error");
            }
        }

        // GET: Student/Transcript
        public ActionResult Transcript()
        {
            try
            {
                RequireRole(Person.UserRole.Student);

                var enrollments = db.Enrollments
                    .Include(e => e.Course.Department)
                    .Include(e => e.Course)
                    .Include(e => e.Student)
                    .Where(e => e.StudentID == CurrentUser.ID &&
                               e.Grade.HasValue &&
                               !e.Student.IsDeleted &&
                               !e.Course.IsDeleted &&
                               !e.Course.Department.IsDeleted)
                    .OrderBy(e => e.Course.CourseID)
                    .ToList();

                var transcript = new StudentTranscriptViewModel
                {
                    StudentName = CurrentUser.FullName,
                    StudentID = CurrentUser.ID.ToString(),
                    Enrollments = enrollments,
                    CumulativeGPA = CalculateGPA(enrollments),
                    TotalCreditsEarned = enrollments.Where(e => e.Grade.Value != Grade.F && !e.Course.IsDeleted)
                                                   .Sum(e => e.Course.Credits)
                };

                return View(transcript);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred while loading your transcript: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Transcript error: {ex.Message}\n{ex.StackTrace}");
                return View("Error");
            }
        }

        private decimal CalculateGPA(List<Enrollment> enrollments)
        {
            try
            {
                var gradedEnrollments = enrollments
                    .Where(e => e.Grade.HasValue && !e.Course.IsDeleted)
                    .ToList();

                if (!gradedEnrollments.Any()) return 0.0m;

                var totalGradePoints = gradedEnrollments.Sum(e => GetGradePoints(e.Grade.Value) * e.Course.Credits);
                var totalCredits = gradedEnrollments.Sum(e => e.Course.Credits);

                return totalCredits > 0 ? totalGradePoints / totalCredits : 0.0m;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CalculateGPA error: {ex.Message}");
                return 0.0m;
            }
        }

        private decimal GetGradePoints(Grade grade)
        {
            switch (grade)
            {
                case Grade.A: return 4.0m;
                case Grade.B: return 3.0m;
                case Grade.C: return 2.0m;
                case Grade.D: return 1.0m;
                case Grade.F: return 0.0m;
                default: return 0.0m;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    db.Dispose();
                }
                base.Dispose(disposing);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dispose error: {ex.Message}");
            }
        }
    }
}