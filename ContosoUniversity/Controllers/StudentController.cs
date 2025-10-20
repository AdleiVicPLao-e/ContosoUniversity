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
            RequireRole(Person.UserRole.Student);

            var student = db.Students
                .Include(s => s.Enrollments.Select(e => e.Course))
                .First(s => s.ID == CurrentUser.ID);

            return View(student);
        }

        // GET: Student/MyGrades
        public ActionResult MyGrades()
        {
            RequireRole(Person.UserRole.Student);

            var student = db.Students
                .Include(s => s.Enrollments.Select(e => e.Course))
                .First(s => s.ID == CurrentUser.ID);

            return View(student.Enrollments.ToList());
        }

        // GET: Student/AvailableCourses
        public ActionResult AvailableCourses()
        {
            RequireRole(Person.UserRole.Student);

            var courses = db.Courses
                .Include(c => c.Department)
                .Include(c => c.Instructors)
                .Where(c => c.Enrollments.Count < 50) // Example: capacity limit
                .ToList();

            return View(courses);
        }

        [HttpPost]
        public ActionResult EnrollInCourse(int courseId)
        {
            RequireRole(Person.UserRole.Student);

            var student = db.Students.Find(CurrentUser.ID);
            var course = db.Courses.Find(courseId);

            // Check if already enrolled
            if (student.Enrollments.Any(e => e.CourseID == courseId))
            {
                TempData["Error"] = "You are already enrolled in this course";
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

            TempData["Success"] = "Successfully enrolled in course";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public ActionResult DropCourse(int enrollmentId)
        {
            RequireRole(Person.UserRole.Student);

            var enrollment = db.Enrollments
                .First(e => e.EnrollmentID == enrollmentId && e.StudentID == CurrentUser.ID);

            db.Enrollments.Remove(enrollment);
            db.SaveChanges();

            TempData["Success"] = "Successfully dropped course";
            return RedirectToAction("Dashboard");
        }
    }
}