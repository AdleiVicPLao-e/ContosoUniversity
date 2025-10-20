using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;

namespace ContosoUniversity.Controllers
{
    public class InstructorController : BaseController
    {
        // GET: Instructor/Dashboard
        public ActionResult Dashboard()
        {
            RequireRole(Person.UserRole.Instructor);

            var instructor = db.Instructors
                .Include(i => i.Courses.Select(c => c.Department))
                .Include(i => i.Courses.Select(c => c.Enrollments))
                .Include(i => i.OfficeAssignment)
                .First(i => i.ID == CurrentUser.ID);

            var viewModel = new InstructorDashboardViewModel
            {
                Instructor = instructor,
                TotalStudents = instructor.Courses.Sum(c => c.Enrollments.Count),
                ActiveCourses = instructor.Courses.Count(c => c.IsActive),
                UpcomingDeadlines = GetUpcomingDeadlines(instructor),
                RecentEnrollments = GetRecentEnrollments(instructor),
                CoursesNeedingGrades = GetCoursesNeedingGrades(instructor)
            };

            return View(viewModel);
        }

        private List<string> GetUpcomingDeadlines(Instructor instructor)
        {
            var deadlines = new List<string>();

            // Example deadlines - you can customize this based on your business logic
            var coursesWithUpcomingWork = instructor.Courses
                .Where(c => c.IsActive && c.Enrollments.Count > 0)
                .ToList();

            foreach (var course in coursesWithUpcomingWork.Take(3))
            {
                deadlines.Add($"{course.Title}: Assignment due Friday");
            }

            return deadlines;
        }

        private List<Enrollment> GetRecentEnrollments(Instructor instructor)
        {
            return db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .Where(e => e.Course.Instructors.Any(i => i.ID == instructor.ID))
                .OrderByDescending(e => e.EnrollmentID)
                .Take(5)
                .ToList();
        }

        private int GetCoursesNeedingGrades(Instructor instructor)
        {
            return instructor.Courses
                .Count(c => c.IsActive &&
                           c.Enrollments.Any(e => !e.Grade.HasValue));
        }

        // GET: Instructor/MyCourses
        public ActionResult MyCourses(string status)
        {
            RequireRole(Person.UserRole.Instructor);

            var instructor = db.Instructors
                .Include(i => i.Courses.Select(c => c.Department))
                .Include(i => i.Courses.Select(c => c.Enrollments))
                .First(i => i.ID == CurrentUser.ID);

            var courses = instructor.Courses.AsQueryable();

            // Filter by status - using switch statement instead of switch expression
            if (!string.IsNullOrEmpty(status))
            {
                switch (status)
                {
                    case "active":
                        courses = courses.Where(c => c.IsActive);
                        break;
                    case "inactive":
                        courses = courses.Where(c => !c.IsActive);
                        break;
                    case "full":
                        courses = courses.Where(c => c.Enrollments.Count >= c.Capacity);
                        break;
                    default:
                        // No filter applied
                        break;
                }
            }

            ViewBag.StatusFilter = new SelectList(new[]
            {
                new { Value = "", Text = "All Courses" },
                new { Value = "active", Text = "Active" },
                new { Value = "inactive", Text = "Inactive" },
                new { Value = "full", Text = "Full" }
            }, "Value", "Text", status);

            return View(courses.OrderBy(c => c.CourseID).ToList());
        }

        // GET: Instructor/CourseDetails/5
        public ActionResult CourseDetails(int? id)
        {
            RequireRole(Person.UserRole.Instructor);

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            // Verify the instructor teaches this course
            var course = db.Courses
                .Include(c => c.Department)
                .Include(c => c.Enrollments.Select(e => e.Student))
                .FirstOrDefault(c => c.CourseID == id && c.Instructors.Any(i => i.ID == CurrentUser.ID));

            if (course == null)
            {
                return HttpNotFound();
            }

            return View(course);
        }

        // GET: Instructor/CourseStudents/5
        public ActionResult CourseStudents(int? id)
        {
            RequireRole(Person.UserRole.Instructor);

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            // Verify the instructor teaches this course
            var course = db.Courses
                .Include(c => c.Enrollments.Select(e => e.Student))
                .FirstOrDefault(c => c.CourseID == id && c.Instructors.Any(i => i.ID == CurrentUser.ID));

            if (course == null)
            {
                return HttpNotFound();
            }

            return View(course);
        }

        [HttpPost]
        public ActionResult UpdateGrade(int enrollmentId, Grade grade)
        {
            RequireRole(Person.UserRole.Instructor);

            var enrollment = db.Enrollments.Find(enrollmentId);
            var course = db.Courses
                .Include(c => c.Instructors)
                .First(c => c.Enrollments.Any(e => e.EnrollmentID == enrollmentId));

            // Verify the instructor teaches this course
            if (!course.Instructors.Any(i => i.ID == CurrentUser.ID))
            {
                return new HttpStatusCodeResult(403, "Not authorized to grade this course");
            }

            enrollment.Grade = grade;
            db.SaveChanges();

            TempData["Success"] = "Grade updated successfully";
            return RedirectToAction("CourseStudents", new { id = course.CourseID });
        }

        // GET: Instructor/MySchedule
        public ActionResult MySchedule()
        {
            RequireRole(Person.UserRole.Instructor);

            var instructor = db.Instructors
                .Include(i => i.Courses.Select(c => c.Department))
                .First(i => i.ID == CurrentUser.ID);

            return View(instructor.Courses.Where(c => c.IsActive).ToList());
        }
    }
}