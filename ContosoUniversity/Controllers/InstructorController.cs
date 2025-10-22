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
                .Where(i => !i.IsDeleted) // Filter deleted instructors
                .Include(i => i.Courses.Select(c => c.Department))
                .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                .Include(i => i.Courses.Select(c => c.Instructors))
                .Include(i => i.OfficeAssignment)
                .First(i => i.ID == CurrentUser.ID);

            var viewModel = new InstructorDashboardViewModel
            {
                Instructor = instructor,
                TotalStudents = instructor.Courses
                    .Where(c => !c.IsDeleted) // Filter deleted courses
                    .Sum(c => c.Enrollments.Count(e => !e.Student.IsDeleted)), // Filter deleted students
                ActiveCourses = instructor.Courses
                    .Where(c => !c.IsDeleted && c.IsActive) // Filter deleted courses
                    .Count(),
                UpcomingDeadlines = GetUpcomingDeadlines(instructor),
                RecentEnrollments = GetRecentEnrollments(instructor),
                CoursesNeedingGrades = GetCoursesNeedingGrades(instructor)
            };

            return View(viewModel);
        }

        private List<string> GetUpcomingDeadlines(Instructor instructor)
        {
            var deadlines = new List<string>();

            var coursesWithUpcomingWork = instructor.Courses
                .Where(c => !c.IsDeleted && c.IsActive && c.Enrollments.Count(e => !e.Student.IsDeleted) > 0) // Filter deleted courses and students
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
                .Include(e => e.Course.Department)
                .Include(e => e.Course.Instructors)
                .Where(e => e.Course.Instructors.Any(i => i.ID == instructor.ID && !i.IsDeleted) && // Filter deleted instructors
                           !e.Course.IsDeleted && !e.Student.IsDeleted && !e.Course.Department.IsDeleted) // Filter deleted courses, students, and departments
                .OrderByDescending(e => e.EnrollmentID)
                .Take(5)
                .ToList();
        }

        private int GetCoursesNeedingGrades(Instructor instructor)
        {
            return instructor.Courses
                .Where(c => !c.IsDeleted && c.IsActive) // Filter deleted courses
                .Count(c => c.Enrollments.Any(e => !e.Grade.HasValue && !e.Student.IsDeleted)); // Filter deleted students
        }

        // GET: Instructor/MyCourses
        public ActionResult MyCourses(string status)
        {
            RequireRole(Person.UserRole.Instructor);

            var instructor = db.Instructors
                .Where(i => !i.IsDeleted) // Filter deleted instructors
                .Include(i => i.Courses.Select(c => c.Department))
                .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                .Include(i => i.Courses.Select(c => c.Instructors))
                .First(i => i.ID == CurrentUser.ID);

            var courses = instructor.Courses
                .Where(c => !c.IsDeleted && !c.Department.IsDeleted) // Filter deleted courses and departments
                .AsQueryable();

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
                        courses = courses.Where(c => c.Enrollments.Count(e => !e.Student.IsDeleted) >= c.Capacity); // Filter deleted students
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

            var course = db.Courses
                .Where(c => !c.IsDeleted && !c.Department.IsDeleted) // Filter deleted courses and departments
                .Include(c => c.Department)
                .Include(c => c.Instructors)
                .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                .Include(c => c.Enrollments.Select(e => e.Student))
                .FirstOrDefault(c => c.CourseID == id &&
                                   c.Instructors.Any(i => i.ID == CurrentUser.ID && !i.IsDeleted)); // Filter deleted instructors

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

            var course = db.Courses
                .Where(c => !c.IsDeleted && !c.Department.IsDeleted) // Filter deleted courses and departments
                .Include(c => c.Department)
                .Include(c => c.Instructors)
                .Include(c => c.Enrollments.Select(e => e.Student))
                .FirstOrDefault(c => c.CourseID == id &&
                                   c.Instructors.Any(i => i.ID == CurrentUser.ID && !i.IsDeleted)); // Filter deleted instructors

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
                .Where(e => !e.Student.IsDeleted && !e.Course.IsDeleted) // Filter deleted students and courses
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefault(e => e.EnrollmentID == enrollmentId);

            if (enrollment == null)
            {
                TempData["Error"] = "Enrollment not found";
                return RedirectToAction("MyCourses");
            }

            var course = db.Courses
                .Where(c => !c.IsDeleted) // Filter deleted courses
                .Include(c => c.Instructors)
                .First(c => c.CourseID == enrollment.CourseID);

            // Verify the instructor teaches this course and is not deleted
            if (!course.Instructors.Any(i => i.ID == CurrentUser.ID && !i.IsDeleted))
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
                .Where(i => !i.IsDeleted) // Filter deleted instructors
                .Include(i => i.Courses.Select(c => c.Department))
                .Include(i => i.Courses.Select(c => c.Instructors))
                .Include(i => i.Courses.Select(c => c.Enrollments))
                .First(i => i.ID == CurrentUser.ID);

            return View(instructor.Courses
                .Where(c => !c.IsDeleted && c.IsActive && !c.Department.IsDeleted) // Filter deleted courses and departments
                .ToList());
        }

        // GET: Instructor/StudentSearch
        public ActionResult StudentSearch(string searchString)
        {
            RequireRole(Person.UserRole.Instructor);

            var instructor = db.Instructors
                .Where(i => !i.IsDeleted) // Filter deleted instructors
                .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                .First(i => i.ID == CurrentUser.ID);

            var students = instructor.Courses
                .Where(c => !c.IsDeleted) // Filter deleted courses
                .SelectMany(c => c.Enrollments)
                .Where(e => !e.Student.IsDeleted) // Filter deleted students
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
                .Where(i => !i.IsDeleted) // Filter deleted instructors
                .Include(i => i.Courses.Select(c => c.Department))
                .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                .First(i => i.ID == CurrentUser.ID);

            // Get selected course or first course (filtering deleted courses and departments)
            var selectedCourse = courseId.HasValue
                ? instructor.Courses.FirstOrDefault(c => c.CourseID == courseId && !c.IsDeleted && !c.Department.IsDeleted)
                : instructor.Courses.FirstOrDefault(c => !c.IsDeleted && !c.Department.IsDeleted);

            if (selectedCourse == null)
            {
                TempData["Error"] = "No courses found";
                return RedirectToAction("Dashboard");
            }

            var viewModel = new InstructorGradebookViewModel
            {
                Courses = instructor.Courses
                    .Where(c => !c.IsDeleted && !c.Department.IsDeleted) // Filter deleted courses and departments
                    .ToList(),
                SelectedCourse = selectedCourse,
                Enrollments = selectedCourse.Enrollments
                    .Where(e => !e.Student.IsDeleted) // Filter deleted students
                    .ToList()
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
                        .Where(e => !e.Student.IsDeleted && !e.Course.IsDeleted) // Filter deleted students and courses
                        .Include(e => e.Course.Instructors)
                        .FirstOrDefault(e => e.EnrollmentID == gradeEntry.Key);

                    if (enrollment != null && enrollment.Course.Instructors.Any(i => i.ID == CurrentUser.ID && !i.IsDeleted))
                    {
                        enrollment.Grade = gradeEntry.Value;
                    }
                }

                db.SaveChanges();
                TempData["Success"] = "Grades updated successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating grades " + ex;
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