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
                .Where(s => !s.IsDeleted && s.ID == CurrentUser.ID) // Filter deleted students
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

        // GET: Student/MyGrades
        public ActionResult MyGrades()
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
                           !e.Course.Department.IsDeleted) // Filter deleted students, courses, and departments
                .ToList();

            return View(enrollments);
        }

        // GET: Student/AvailableCourses
        public ActionResult AvailableCourses()
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
                           c.Instructors.Any(i => !i.IsDeleted) && // Only show courses with active instructors
                           c.Enrollments.Count(e => !e.Student.IsDeleted) < c.Capacity) // Filter deleted students in capacity count
                .ToList();

            return View(courses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnrollInCourse(int courseId)
        {
            RequireRole(Person.UserRole.Student);

            try
            {
                var student = db.Students
                    .Where(s => !s.IsDeleted && s.ID == CurrentUser.ID)
                    .Include(s => s.Enrollments.Select(e => e.Course)) // Ensure Course is loaded
                    .FirstOrDefault();

                if (student == null)
                {
                    TempData["Error"] = "Student not found";
                    return RedirectToAction("AvailableCourses");
                }

                var course = db.Courses
                    .Where(c => !c.IsDeleted && c.CourseID == courseId && c.IsActive && !c.Department.IsDeleted)
                    .Include(c => c.Enrollments.Select(e => e.Student)) // Include enrollments and students for capacity check
                    .FirstOrDefault();

                if (course == null)
                {
                    TempData["Error"] = "Course not found or not available";
                    return RedirectToAction("AvailableCourses");
                }

                // Check if already enrolled - safer null checking
                if (student.Enrollments.Any(e => e.CourseID == courseId && e.Course != null && !e.Course.IsDeleted))
                {
                    TempData["Error"] = "You are already enrolled in this course";
                    return RedirectToAction("AvailableCourses");
                }

                // Check course capacity with active students only - safer null checking
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
            catch (Exception ex)
            {
                // Log the full exception details for debugging
                System.Diagnostics.Debug.WriteLine($"Enrollment error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["Error"] = "An error occurred while enrolling in the course. Please try again.";
                return RedirectToAction("AvailableCourses");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DropCourse(int enrollmentId)
        {
            RequireRole(Person.UserRole.Student);

            try
            {
                var enrollment = db.Enrollments
                    .Include(e => e.Course)
                    .Include(e => e.Student)
                    .Where(e => e.EnrollmentID == enrollmentId &&
                               e.StudentID == CurrentUser.ID &&
                               !e.Student.IsDeleted &&
                               !e.Course.IsDeleted) // Filter deleted students and courses
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
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while dropping the course " + ex;
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Student/MySchedule
        public ActionResult MySchedule()
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
                           e.Course.Instructors.Any(i => !i.IsDeleted)) // Filter deleted records and ensure active instructors
                .ToList();

            return View(enrollments);
        }

        // GET: Student/CourseDetails/{id}
        public ActionResult CourseDetails(int id)
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
                           c.Instructors.Any(i => !i.IsDeleted)) // Filter deleted courses, departments, and ensure active instructors
                .FirstOrDefault();

            if (course == null)
            {
                TempData["Error"] = "Course not found";
                return RedirectToAction("AvailableCourses");
            }

            return View(course);
        }

        // GET: Student/MyProfile
        public ActionResult MyProfile()
        {
            RequireRole(Person.UserRole.Student);

            var student = db.Students
                .Where(s => !s.IsDeleted && s.ID == CurrentUser.ID) // Filter deleted students
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

        // GET: Student/AcademicProgress
        public ActionResult AcademicProgress()
        {
            RequireRole(Person.UserRole.Student);

            var enrollments = db.Enrollments
                .Include(e => e.Course.Department)
                .Include(e => e.Course)
                .Include(e => e.Student)
                .Where(e => e.StudentID == CurrentUser.ID &&
                           !e.Student.IsDeleted &&
                           !e.Course.IsDeleted &&
                           !e.Course.Department.IsDeleted) // Filter deleted records
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

        // GET: Student/CourseCatalog
        public ActionResult CourseCatalog(string departmentFilter, string searchString)
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
                           c.Instructors.Any(i => !i.IsDeleted)) // Filter deleted records and ensure active instructors
                .AsQueryable();

            // Apply department filter
            if (!string.IsNullOrEmpty(departmentFilter) && int.TryParse(departmentFilter, out int deptId))
            {
                courses = courses.Where(c => c.DepartmentID == deptId && !c.Department.IsDeleted);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(c =>
                    c.Title.Contains(searchString) ||
                    c.Description.Contains(searchString) ||
                    c.CourseID.ToString().Contains(searchString));
            }

            // Get departments for filter dropdown (only non-deleted departments)
            ViewBag.DepartmentFilter = new SelectList(
                db.Departments.Where(d => !d.IsDeleted).OrderBy(d => d.Name),
                "DepartmentID",
                "Name",
                departmentFilter);

            ViewBag.SearchString = searchString;

            return View(courses.OrderBy(c => c.CourseID).ToList());
        }

        // GET: Student/UpcomingAssignments
        public ActionResult UpcomingAssignments()
        {
            RequireRole(Person.UserRole.Student);

            var enrollments = db.Enrollments
                .Include(e => e.Course.Department)
                .Include(e => e.Course.Instructors)
                .Include(e => e.Student)
                .Where(e => e.StudentID == CurrentUser.ID &&
                           !e.Grade.HasValue &&
                           !e.Student.IsDeleted &&
                           !e.Course.IsDeleted &&
                           !e.Course.Department.IsDeleted &&
                           e.Course.Instructors.Any(i => !i.IsDeleted)) // Only current courses with active records
                .ToList();

            // This would typically come from an Assignments table
            // For now, we'll create sample data
            var assignments = new List<StudentAssignmentViewModel>();

            foreach (var enrollment in enrollments)
            {
                assignments.Add(new StudentAssignmentViewModel
                {
                    CourseTitle = enrollment.Course.Title,
                    AssignmentName = "Midterm Exam",
                    DueDate = DateTime.Now.AddDays(7),
                    Status = "Not Submitted"
                });

                assignments.Add(new StudentAssignmentViewModel
                {
                    CourseTitle = enrollment.Course.Title,
                    AssignmentName = "Final Project",
                    DueDate = DateTime.Now.AddDays(21),
                    Status = "In Progress"
                });
            }

            return View(assignments.OrderBy(a => a.DueDate).ToList());
        }

        // GET: Student/Transcript
        public ActionResult Transcript()
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
                           !e.Course.Department.IsDeleted) // Only completed courses with active records
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

        private decimal CalculateGPA(List<Enrollment> enrollments)
        {
            var gradedEnrollments = enrollments
                .Where(e => e.Grade.HasValue && !e.Course.IsDeleted) // Filter deleted courses
                .ToList();

            if (!gradedEnrollments.Any()) return 0.0m;

            var totalGradePoints = gradedEnrollments.Sum(e => GetGradePoints(e.Grade.Value) * e.Course.Credits);
            var totalCredits = gradedEnrollments.Sum(e => e.Course.Credits);

            return totalCredits > 0 ? totalGradePoints / totalCredits : 0.0m;
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
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}