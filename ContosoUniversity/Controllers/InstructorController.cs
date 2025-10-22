using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
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
            try
            {
                RequireRole(Person.UserRole.Instructor);

                var instructor = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Courses.Select(c => c.Department))
                    .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                    .Include(i => i.Courses.Select(c => c.Instructors))
                    .Include(i => i.OfficeAssignment)
                    .First(i => i.ID == CurrentUser.ID);

                var viewModel = new InstructorDashboardViewModel
                {
                    Instructor = instructor,
                    TotalStudents = instructor.Courses
                        .Where(c => !c.IsDeleted)
                        .Sum(c => c.Enrollments.Count(e => !e.Student.IsDeleted)),
                    ActiveCourses = instructor.Courses
                        .Where(c => !c.IsDeleted && c.IsActive)
                        .Count(),
                    RecentEnrollments = GetRecentEnrollments(instructor),
                    CoursesNeedingGrades = GetCoursesNeedingGrades(instructor)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading dashboard: {ex.Message}";
                return View("Error");
            }
        }


        private List<Enrollment> GetRecentEnrollments(Instructor instructor)
        {
            try
            {
                return db.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course.Department)
                    .Include(e => e.Course.Instructors)
                    .Where(e => e.Course.Instructors.Any(i => i.ID == instructor.ID && !i.IsDeleted) &&
                               !e.Course.IsDeleted && !e.Student.IsDeleted && !e.Course.Department.IsDeleted)
                    .OrderByDescending(e => e.EnrollmentID)
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                ViewBag.WarningMessage = $"Error retrieving recent enrollments: {ex.Message}";
                return new List<Enrollment>();
            }
        }

        private int GetCoursesNeedingGrades(Instructor instructor)
        {
            try
            {
                return instructor.Courses
                    .Where(c => !c.IsDeleted && c.IsActive)
                    .Count(c => c.Enrollments.Any(e => !e.Grade.HasValue && !e.Student.IsDeleted));
            }
            catch (Exception ex)
            {
                ViewBag.WarningMessage = $"Error counting courses needing grades: {ex.Message}";
                return 0;
            }
        }

        // GET: Instructor/MyCourses
        public ActionResult MyCourses(string status)
        {
            try
            {
                RequireRole(Person.UserRole.Instructor);

                var instructor = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Courses.Select(c => c.Department))
                    .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                    .Include(i => i.Courses.Select(c => c.Instructors))
                    .First(i => i.ID == CurrentUser.ID);

                var courses = instructor.Courses
                    .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
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
                            courses = courses.Where(c => c.Enrollments.Count(e => !e.Student.IsDeleted) >= c.Capacity);
                            break;
                        default:
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
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading courses: {ex.Message}";
                return View(new List<Course>());
            }
        }

        // GET: Instructor/CourseDetails/5
        public ActionResult CourseDetails(int? id)
        {
            try
            {
                RequireRole(Person.UserRole.Instructor);

                if (id == null)
                {
                    ViewBag.ErrorMessage = "Course ID is required";
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
                }

                var course = db.Courses
                    .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
                    .Include(c => c.Department)
                    .Include(c => c.Instructors)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .FirstOrDefault(c => c.CourseID == id &&
                                       c.Instructors.Any(i => i.ID == CurrentUser.ID && !i.IsDeleted));

                if (course == null)
                {
                    ViewBag.ErrorMessage = "Course not found or you don't have permission to access it";
                    return HttpNotFound();
                }

                return View(course);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading course details: {ex.Message}";
                return View("Error");
            }
        }

        // GET: Instructor/CourseStudents/5
        public ActionResult CourseStudents(int? id)
        {
            try
            {
                RequireRole(Person.UserRole.Instructor);

                if (id == null)
                {
                    ViewBag.ErrorMessage = "Course ID is required";
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
                }

                var course = db.Courses
                    .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
                    .Include(c => c.Department)
                    .Include(c => c.Instructors)
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .FirstOrDefault(c => c.CourseID == id &&
                                       c.Instructors.Any(i => i.ID == CurrentUser.ID && !i.IsDeleted));

                if (course == null)
                {
                    ViewBag.ErrorMessage = "Course not found or you don't have permission to access it";
                    return HttpNotFound();
                }

                return View(course);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading course students: {ex.Message}";
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateGrade(int enrollmentId, Grade grade)
        {
            try
            {
                RequireRole(Person.UserRole.Instructor);

                var enrollment = db.Enrollments
                    .Where(e => !e.Student.IsDeleted && !e.Course.IsDeleted)
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .FirstOrDefault(e => e.EnrollmentID == enrollmentId);

                if (enrollment == null)
                {
                    TempData["Error"] = "Enrollment not found";
                    return RedirectToAction("MyCourses");
                }

                var course = db.Courses
                    .Where(c => !c.IsDeleted)
                    .Include(c => c.Instructors)
                    .First(c => c.CourseID == enrollment.CourseID);

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
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                TempData["Error"] = $"Validation error: {fullErrorMessage}";
                return RedirectToAction("MyCourses");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating grade: {ex.Message}";
                return RedirectToAction("MyCourses");
            }
        }

        // GET: Instructor/MySchedule
        public ActionResult MySchedule()
        {
            try
            {
                RequireRole(Person.UserRole.Instructor);

                var instructor = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Courses.Select(c => c.Department))
                    .Include(i => i.Courses.Select(c => c.Instructors))
                    .Include(i => i.Courses.Select(c => c.Enrollments))
                    .First(i => i.ID == CurrentUser.ID);

                var courses = instructor.Courses
                    .Where(c => !c.IsDeleted && c.IsActive && !c.Department.IsDeleted)
                    .ToList();

                return View(courses);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading schedule: {ex.Message}";
                return View(new List<Course>());
            }
        }

        // GET: Instructor/StudentSearch
        public ActionResult StudentSearch(string searchString)
        {
            try
            {
                RequireRole(Person.UserRole.Instructor);

                var instructor = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                    .First(i => i.ID == CurrentUser.ID);

                var students = instructor.Courses
                    .Where(c => !c.IsDeleted)
                    .SelectMany(c => c.Enrollments)
                    .Where(e => !e.Student.IsDeleted)
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
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error searching students: {ex.Message}";
                return View(new List<Student>());
            }
        }

        // GET: Instructor/Gradebook
        public ActionResult Gradebook(int? courseId)
        {
            try
            {
                RequireRole(Person.UserRole.Instructor);

                var instructor = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Courses.Select(c => c.Department))
                    .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                    .First(i => i.ID == CurrentUser.ID);

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
                        .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
                        .ToList(),
                    SelectedCourse = selectedCourse,
                    Enrollments = selectedCourse.Enrollments
                        .Where(e => !e.Student.IsDeleted)
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading gradebook: {ex.Message}";
                return View("Error");
            }
        }

        // POST: Instructor/UpdateMultipleGrades
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateMultipleGrades(Dictionary<int, Grade> grades)
        {
            try
            {
                RequireRole(Person.UserRole.Instructor);

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var gradeEntry in grades)
                        {
                            var enrollment = db.Enrollments
                                .Where(e => !e.Student.IsDeleted && !e.Course.IsDeleted)
                                .Include(e => e.Course.Instructors)
                                .FirstOrDefault(e => e.EnrollmentID == gradeEntry.Key);

                            if (enrollment != null && enrollment.Course.Instructors.Any(i => i.ID == CurrentUser.ID && !i.IsDeleted))
                            {
                                enrollment.Grade = gradeEntry.Value;
                            }
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        TempData["Success"] = "Grades updated successfully";
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                TempData["Error"] = $"Validation error while updating grades: {fullErrorMessage}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while updating grades: {ex.Message}";
            }

            return RedirectToAction("Gradebook");
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
                System.Diagnostics.Debug.WriteLine($"Error disposing controller: {ex.Message}");
            }
        }
    }
}