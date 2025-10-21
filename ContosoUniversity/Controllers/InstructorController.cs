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
                .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student))) // Include enrollments and students
                .Include(i => i.Courses.Select(c => c.Instructors)) // Include course instructors
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
                .Include(e => e.Course.Department) // Include course department
                .Include(e => e.Course.Instructors) // Include course instructors
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
                .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student))) // Include enrollments and students
                .Include(i => i.Courses.Select(c => c.Instructors)) // Include course instructors
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

            // Verify the instructor teaches this course and include all related data
            var course = db.Courses
                .Include(c => c.Department)
                .Include(c => c.Instructors) // Include instructors
                .Include(c => c.Instructors.Select(i => i.OfficeAssignment)) // Include instructor office assignments
                .Include(c => c.Enrollments.Select(e => e.Student)) // Include enrollments and students
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

            // Verify the instructor teaches this course and include all related data
            var course = db.Courses
                .Include(c => c.Department) // Include department
                .Include(c => c.Instructors) // Include instructors
                .Include(c => c.Enrollments.Select(e => e.Student)) // Include enrollments and students
                .FirstOrDefault(c => c.CourseID == id && c.Instructors.Any(i => i.ID == CurrentUser.ID));

            if (course == null)
            {
                return HttpNotFound();
            }

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateGrade(int enrollmentId, Grade grade)
        {
            RequireRole(Person.UserRole.Instructor);

            var enrollment = db.Enrollments
                .Include(e => e.Student) // Include student
                .Include(e => e.Course) // Include course
                .FirstOrDefault(e => e.EnrollmentID == enrollmentId);

            if (enrollment == null)
            {
                TempData["Error"] = "Enrollment not found";
                return RedirectToAction("MyCourses");
            }

            var course = db.Courses
                .Include(c => c.Instructors) // Include instructors
                .First(c => c.CourseID == enrollment.CourseID);

            // Verify the instructor teaches this course
            if (!course.Instructors.Any(i => i.ID == CurrentUser.ID))
            {
                TempData["Error"] = "Not authorized to grade this course";
                return RedirectToAction("MyCourses");
            }

            enrollment.Grade = grade;
            db.SaveChanges();

            TempData["Success"] = $"Grade updated successfully for {enrollment.Student.FullName}";
            return RedirectToAction("CourseStudents", new { id = course.CourseID });
        }

        // GET: Instructor/MySchedule
        public ActionResult MySchedule()
        {
            RequireRole(Person.UserRole.Instructor);

            var instructor = db.Instructors
                .Include(i => i.Courses.Select(c => c.Department))
                .Include(i => i.Courses.Select(c => c.Instructors)) // Include course instructors
                .Include(i => i.Courses.Select(c => c.Enrollments)) // Include enrollments
                .First(i => i.ID == CurrentUser.ID);

            return View(instructor.Courses.Where(c => c.IsActive).ToList());
        }

        // GET: Instructor/StudentSearch
        public ActionResult StudentSearch(string searchString)
        {
            RequireRole(Person.UserRole.Instructor);

            var instructor = db.Instructors
                .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student))) // Include all related data
                .First(i => i.ID == CurrentUser.ID);

            var students = instructor.Courses
                .SelectMany(c => c.Enrollments)
                .Select(e => e.Student)
                .Distinct();

            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s =>
                    s.FirstMidName.Contains(searchString) ||
                    s.LastName.Contains(searchString) ||
                    s.UserName.Contains(searchString));
            }

            ViewBag.SearchString = searchString;
            return View(students.ToList());
        }

        // GET: Instructor/Gradebook
        public ActionResult Gradebook(int? courseId)
        {
            RequireRole(Person.UserRole.Instructor);

            var instructor = db.Instructors
                .Include(i => i.Courses.Select(c => c.Department))
                .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                .First(i => i.ID == CurrentUser.ID);

            // Get selected course or first course
            var selectedCourse = courseId.HasValue
                ? instructor.Courses.FirstOrDefault(c => c.CourseID == courseId)
                : instructor.Courses.FirstOrDefault();

            if (selectedCourse == null)
            {
                TempData["Error"] = "No courses found";
                return RedirectToAction("Dashboard");
            }

            var viewModel = new InstructorGradebookViewModel
            {
                Courses = instructor.Courses.ToList(),
                SelectedCourse = selectedCourse,
                Enrollments = selectedCourse.Enrollments.ToList()
            };

            return View(viewModel);
        }

        // POST: Instructor/UpdateMultipleGrades
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateMultipleGrades(Dictionary<int, Grade> grades)
        {
            RequireRole(Person.UserRole.Instructor);

            try
            {
                foreach (var gradeEntry in grades)
                {
                    var enrollment = db.Enrollments
                        .Include(e => e.Course.Instructors)
                        .FirstOrDefault(e => e.EnrollmentID == gradeEntry.Key);

                    if (enrollment != null && enrollment.Course.Instructors.Any(i => i.ID == CurrentUser.ID))
                    {
                        enrollment.Grade = gradeEntry.Value;
                    }
                }

                db.SaveChanges();
                TempData["Success"] = "Grades updated successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating grades";
            }

            return RedirectToAction("Gradebook");
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