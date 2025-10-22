using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ContosoUniversity.Controllers
{
    public class AdministratorController : BaseController
    {
        // GET: Administrator/Dashboard
        public ActionResult Dashboard()
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var viewModel = new AdminDashboardViewModel
                {
                    // 📊 Filter out deleted students/instructors/courses/departments
                    TotalStudents = db.Students.Count(s => !s.IsDeleted),
                    TotalInstructors = db.Instructors.Count(i => !i.IsDeleted),
                    TotalCourses = db.Courses.Count(c => !c.IsDeleted),
                    TotalDepartments = db.Departments.Count(d => !d.IsDeleted),

                    // Filter deleted enrollments if applicable
                    NewEnrollmentsThisMonth = db.Enrollments
                        .Count(e => e.EnrollmentID > 0 &&
                                   !e.Student.IsDeleted &&
                                   !e.Course.IsDeleted),

                    ActiveCourses = db.Courses
                        .Where(c => !c.IsDeleted && c.Enrollments.Count > 0)
                        .Count(),

                    RecentEnrollments = db.Enrollments
                        .Include(e => e.Student)
                        .Include(e => e.Course.Department)
                        .Include(e => e.Course.Instructors)
                        .Where(e =>
                            !e.Course.IsDeleted &&
                            !e.Student.IsDeleted &&
                            !e.Course.Department.IsDeleted)
                        .OrderByDescending(e => e.EnrollmentID)
                        .Take(10)
                        .ToList(),

                    RecentCourses = db.Courses
                        .Include(c => c.Department)
                        .Include(c => c.Instructors)
                        .Include(c => c.Enrollments)
                        .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
                        .OrderByDescending(c => c.CourseID)
                        .Take(5)
                        .ToList(),

                    ShowQuickActions = true
                };

                // 🏛 Department Statistics
                var departments = db.Departments
                    .Include(d => d.Courses.Select(c => c.Enrollments))
                    .Include(d => d.Administrator)
                    .Include(d => d.Courses.Select(c => c.Instructors))
                    .Where(d => !d.IsDeleted)
                    .ToList();

                viewModel.DepartmentStatistics = departments.Select(d => new DepartmentStats
                {
                    DepartmentName = d?.Name ?? "Unknown Department",
                    TotalCourses = d?.Courses?.Count(c => !c.IsDeleted) ?? 0,
                    TotalStudents = d?.Courses?
                        .Where(c => !c.IsDeleted)
                        .Sum(c => c?.Enrollments?
                            .Count(e => e.Student != null && !e.Student.IsDeleted) ?? 0) ?? 0,
                    TotalInstructors = db.Instructors
                        .Include(i => i.Courses.Select(c => c.Department))
                        .Count(i => !i.IsDeleted &&
                                    i.Courses.Any(c => !c.IsDeleted &&
                                                       c.DepartmentID == d.DepartmentID)),
                    BudgetUtilization = (d?.Budget ?? 0) > 0
                        ? ((d?.Courses?.Where(c => !c.IsDeleted)
                                .Sum(c => (c?.Credits ?? 0) * 1000m) ?? 0)
                            / (d?.Budget ?? 1m))
                        : 0m,
                    DepartmentHead = d?.Administrator != null && !d.Administrator.IsDeleted
                        ? $"{d.Administrator.FirstMidName} {d.Administrator.LastName}".Trim()
                        : "Not Assigned"
                }).ToList();


                // 📚 Popular Courses
                var courses = db.Courses
                    .Include(c => c.Department)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
                    .ToList();

                viewModel.PopularCourses = courses.Select(c => new CourseEnrollmentStats
                {
                    CourseTitle = c?.Title ?? "Unknown Course",
                    CourseCode = c != null ? $"CS{c.CourseID}" : "CS0",
                    EnrolledStudents = c?.Enrollments?.Count(e => !e.Student.IsDeleted) ?? 0,
                    Capacity = c?.Capacity ?? 0,
                    InstructorName = c?.Instructors?.FirstOrDefault(i => !i.IsDeleted) != null
                        ? $"{c.Instructors.First(i => !i.IsDeleted).FirstMidName} {c.Instructors.First(i => !i.IsDeleted).LastName}".Trim()
                        : "Not Assigned",
                    DepartmentName = c?.Department?.Name ?? "No Department"
                })
                .OrderByDescending(c => c.EnrolledStudents)
                .Take(5)
                .ToList();

                // 🧑‍🏫 Instructor Workload
                var instructors = db.Instructors
                    .Include(i => i.OfficeAssignment)
                    .Include(i => i.Courses.Select(c => c.Department))
                    .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                    .Where(i => !i.IsDeleted)
                    .ToList();

                viewModel.InstructorWorkload = instructors.Select(i => new InstructorStats
                {
                    InstructorName = i != null ? $"{i.FirstMidName} {i.LastName}".Trim() : "Unknown Instructor",
                    DepartmentName = i?.Courses?.FirstOrDefault(c => !c.IsDeleted)?.Department?.Name ?? "Not Assigned",
                    CoursesTeaching = i?.Courses?.Count(c => !c.IsDeleted) ?? 0,
                    TotalStudents = i?.Courses?.Where(c => !c.IsDeleted)
                                        .Sum(c => c?.Enrollments?.Count(e => !e.Student.IsDeleted) ?? 0) ?? 0,
                    OfficeLocation = i?.OfficeAssignment?.Location ?? "No Office",
                    HireDate = i?.HireDate ?? DateTime.MinValue
                })
                .OrderByDescending(i => i.TotalStudents)
                .Take(10)
                .ToList();

                viewModel.SystemAlerts = GetSystemAlerts();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading the dashboard. Please try again.";

                return View(new AdminDashboardViewModel
                {
                    ShowQuickActions = true,
                    SystemAlerts = new List<SystemAlert>()
                });
            }
        }

        #region Course Management

        // GET: Administrator/ManageCourses
        public ActionResult ManageCourses(int? departmentId, string status)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var courses = db.Courses
                    .Include(c => c.Department)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
                    .AsQueryable();

                if (departmentId.HasValue)
                {
                    courses = courses.Where(c => c.DepartmentID == departmentId.Value && !c.Department.IsDeleted);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    if (status == "active")
                    {
                        courses = courses.Where(c => c.IsActive);
                    }
                    else if (status == "inactive")
                    {
                        courses = courses.Where(c => !c.IsActive);
                    }
                    else if (status == "full")
                    {
                        courses = courses.Where(c => c.Enrollments.Count(e => !e.Student.IsDeleted) >= c.Capacity);
                    }
                    else if (status == "low")
                    {
                        courses = courses.Where(c => c.Enrollments.Count(e => !e.Student.IsDeleted) < 5);
                    }
                }

                ViewBag.Departments = new SelectList(db.Departments.Where(d => !d.IsDeleted), "DepartmentID", "Name", departmentId);
                ViewBag.StatusFilter = new SelectList(new[]
                {
                    new { Value = "", Text = "All Status" },
                    new { Value = "active", Text = "Active" },
                    new { Value = "inactive", Text = "Inactive" },
                    new { Value = "full", Text = "Full" },
                    new { Value = "low", Text = "Low Enrollment" }
                }, "Value", "Text", status);

                return View(courses.OrderBy(c => c.CourseID).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManageCourses error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading courses. Please try again.";
                return View(new List<Course>());
            }
        }

        // GET: Administrator/CreateCourse
        public ActionResult CreateCourse()
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                PopulateDepartmentsDropDownList();
                ViewBag.Instructors = new MultiSelectList(db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.OfficeAssignment)
                    .ToList(), "ID", "FullName");
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateCourse GET error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading the create course form. Please try again.";
                return View();
            }
        }

        // POST: Administrator/CreateCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCourse(Course course, int[] selectedInstructors)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                if (ModelState.IsValid)
                {
                    if (selectedInstructors != null)
                    {
                        course.Instructors = db.Instructors
                            .Where(i => !i.IsDeleted && selectedInstructors.Contains(i.ID))
                            .Include(i => i.OfficeAssignment)
                            .ToList();
                    }

                    db.Courses.Add(course);
                    db.SaveChanges();
                    TempData["Success"] = "Course created successfully";
                    return RedirectToAction("ManageCourses");
                }
            }
            catch (RetryLimitExceededException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateCourse DbUpdateException: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while saving the course. Please check your input and try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateCourse error: {ex.Message}");
                ViewBag.ErrorMessage = "An unexpected error occurred while creating the course. Please try again.";
            }

            try
            {
                PopulateDepartmentsDropDownList(course.DepartmentID);
                ViewBag.Instructors = new MultiSelectList(db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.OfficeAssignment)
                    .ToList(), "ID", "FullName", selectedInstructors);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dropdowns: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading form data. Please try again.";
            }

            return View(course);
        }

        // GET: Administrator/EditCourse/5
        public ActionResult EditCourse(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                ViewBag.ErrorMessage = "Course ID is required.";
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                Course course = db.Courses
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .Include(c => c.Department)
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
                    .FirstOrDefault(c => c.CourseID == id);

                if (course == null)
                {
                    ViewBag.ErrorMessage = "Course not found.";
                    return HttpNotFound();
                }

                PopulateDepartmentsDropDownList(course.DepartmentID);
                ViewBag.Instructors = new MultiSelectList(db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.OfficeAssignment)
                    .ToList(), "ID", "FullName",
                    course.Instructors.Where(i => !i.IsDeleted).Select(i => i.ID));
                return View(course);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditCourse GET error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading the course for editing. Please try again.";
                return RedirectToAction("ManageCourses");
            }
        }

        // POST: Administrator/EditCourse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCourse(Course course, int[] selectedInstructors)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                if (ModelState.IsValid)
                {
                    var courseToUpdate = db.Courses
                        .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                        .Include(c => c.Department)
                        .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
                        .FirstOrDefault(c => c.CourseID == course.CourseID);

                    if (courseToUpdate == null)
                    {
                        ViewBag.ErrorMessage = "Course not found.";
                        return HttpNotFound();
                    }

                    courseToUpdate.Title = course.Title;
                    courseToUpdate.Description = course.Description;
                    courseToUpdate.Credits = course.Credits;
                    courseToUpdate.Capacity = course.Capacity;
                    courseToUpdate.DepartmentID = course.DepartmentID;
                    courseToUpdate.IsActive = course.IsActive;

                    UpdateCourseInstructors(selectedInstructors, courseToUpdate);
                    db.SaveChanges();

                    TempData["Success"] = "Course updated successfully";
                    return RedirectToAction("ManageCourses");
                }
            }
            catch (RetryLimitExceededException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditCourse concurrency error: {ex.Message}");
                ViewBag.ErrorMessage = "The course was modified by another user. Please refresh and try again.";
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditCourse DbUpdateException: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while updating the course. Please check your input and try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditCourse error: {ex.Message}");
                ViewBag.ErrorMessage = "An unexpected error occurred while updating the course. Please try again.";
            }

            try
            {
                PopulateDepartmentsDropDownList(course.DepartmentID);
                ViewBag.Instructors = new MultiSelectList(db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.OfficeAssignment)
                    .ToList(), "ID", "FullName", selectedInstructors);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dropdowns: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading form data. Please try again.";
            }

            return View(course);
        }

        // GET: Administrator/CourseDetails/5
        public ActionResult CourseDetails(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                ViewBag.ErrorMessage = "Course ID is required.";
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                Course course = db.Courses
                    .Include(c => c.Department)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
                    .FirstOrDefault(c => c.CourseID == id);

                if (course == null)
                {
                    ViewBag.ErrorMessage = "Course not found.";
                    return HttpNotFound();
                }

                return View(course);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CourseDetails error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading course details. Please try again.";
                return RedirectToAction("ManageCourses");
            }
        }

        // GET: Administrator/DeleteCourse/5
        public ActionResult DeleteCourse(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                ViewBag.ErrorMessage = "Course ID is required.";
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                Course course = db.Courses
                    .Include(c => c.Department)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .Where(c => !c.IsDeleted && !c.Department.IsDeleted)
                    .FirstOrDefault(c => c.CourseID == id);

                if (course == null)
                {
                    ViewBag.ErrorMessage = "Course not found.";
                    return HttpNotFound();
                }

                return View(course);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteCourse GET error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading the course for deletion. Please try again.";
                return RedirectToAction("ManageCourses");
            }
        }

        // POST: Administrator/DeleteCourse/5
        [HttpPost, ActionName("DeleteCourse")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCourseConfirmed(int id)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                Course course = db.Courses
                    .Include(c => c.Instructors)
                    .Include(c => c.Enrollments)
                    .Where(c => !c.IsDeleted)
                    .FirstOrDefault(c => c.CourseID == id);

                if (course == null)
                {
                    ViewBag.ErrorMessage = "Course not found.";
                    return HttpNotFound();
                }

                course.IsDeleted = true;
                db.SaveChanges();
                TempData["Success"] = "Course deleted successfully";
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteCourse DbUpdateException: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the course. Please try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteCourse error: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the course. Please try again.";
            }

            return RedirectToAction("ManageCourses");
        }

        // POST: Administrator/ToggleCourseStatus/5
        [HttpPost]
        public ActionResult ToggleCourseStatus(int id)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var course = db.Courses
                    .Include(c => c.Instructors)
                    .Include(c => c.Enrollments)
                    .Where(c => !c.IsDeleted)
                    .FirstOrDefault(c => c.CourseID == id);

                if (course == null)
                {
                    TempData["ErrorMessage"] = "Course not found.";
                    return RedirectToAction("ManageCourses");
                }

                course.IsActive = !course.IsActive;
                db.SaveChanges();
                TempData["Success"] = $"Course {(course.IsActive ? "activated" : "deactivated")} successfully";
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleCourseStatus DbUpdateException: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while updating the course status. Please try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleCourseStatus error: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred while updating the course status. Please try again.";
            }

            return RedirectToAction("ManageCourses");
        }

        // Bulk course operations
        public ActionResult UpdateCourseCredits()
        {
            RequireRole(Person.UserRole.Administrator);
            return View();
        }

        [HttpPost]
        public ActionResult UpdateCourseCredits(int? multiplier)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                if (multiplier != null)
                {
                    ViewBag.RowsAffected = db.Database.ExecuteSqlCommand(
                        "UPDATE Course SET Credits = Credits * {0} WHERE IsDeleted = 0",
                        multiplier);
                    TempData["Success"] = $"{ViewBag.RowsAffected} courses updated";
                }
                else
                {
                    ViewBag.ErrorMessage = "Multiplier value is required.";
                }
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCourseCredits DbUpdateException: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while updating course credits. Please try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCourseCredits error: {ex.Message}");
                ViewBag.ErrorMessage = "An unexpected error occurred while updating course credits. Please try again.";
            }

            return View();
        }

        #endregion

        #region Department Management

        // GET: Administrator/ManageDepartments
        public async Task<ActionResult> ManageDepartments()
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var departments = db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .Include(d => d.Courses.Select(c => c.Instructors))
                    .Include(d => d.Courses.Select(c => c.Enrollments));
                return View(await departments.ToListAsync());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManageDepartments error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading departments. Please try again.";
                return View(new List<Department>());
            }
        }

        // GET: Administrator/DepartmentDetails/5
        public async Task<ActionResult> DepartmentDetails(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                ViewBag.ErrorMessage = "Department ID is required.";
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                Department department = await db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .Include(d => d.Courses.Select(c => c.Instructors.Select(i => i.OfficeAssignment)))
                    .Include(d => d.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                    .FirstOrDefaultAsync(d => d.DepartmentID == id);

                if (department == null)
                {
                    ViewBag.ErrorMessage = "Department not found.";
                    return HttpNotFound();
                }
                return View(department);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DepartmentDetails error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading department details. Please try again.";
                return RedirectToAction("ManageDepartments");
            }
        }

        // GET: Administrator/CreateDepartment
        public ActionResult CreateDepartment()
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                ViewBag.AdministratorID = new SelectList(db.Administrators
                    .Where(a => !a.IsDeleted)
                    .ToList(), "ID", "FullName");
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateDepartment GET error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading the create department form. Please try again.";
                return View();
            }
        }

        // POST: Administrator/CreateDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateDepartment([Bind(Include = "DepartmentID,Name,Budget,StartDate,AdministratorID")] Department department)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                if (ModelState.IsValid)
                {
                    db.Departments.Add(department);
                    await db.SaveChangesAsync();
                    TempData["Success"] = "Department created successfully";
                    return RedirectToAction("ManageDepartments");
                }
            }
            catch (RetryLimitExceededException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateDepartment DbUpdateException: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while saving the department. Please check your input and try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateDepartment error: {ex.Message}");
                ViewBag.ErrorMessage = "An unexpected error occurred while creating the department. Please try again.";
            }

            try
            {
                ViewBag.AdministratorID = new SelectList(db.Administrators
                    .Where(a => !a.IsDeleted)
                    .ToList(), "ID", "FullName", department.AdministratorID);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dropdown: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading form data. Please try again.";
            }

            return View(department);
        }

        // GET: Administrator/EditDepartment/5
        public async Task<ActionResult> EditDepartment(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                ViewBag.ErrorMessage = "Department ID is required.";
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                Department department = await db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .Include(d => d.Courses)
                    .FirstOrDefaultAsync(d => d.DepartmentID == id);

                if (department == null)
                {
                    ViewBag.ErrorMessage = "Department not found.";
                    return HttpNotFound();
                }

                ViewBag.AdministratorID = new SelectList(db.Administrators
                    .Where(a => !a.IsDeleted)
                    .ToList(), "ID", "FullName", department.AdministratorID);
                return View(department);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditDepartment GET error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading the department for editing. Please try again.";
                return RedirectToAction("ManageDepartments");
            }
        }

        // POST: Administrator/EditDepartment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditDepartment(int? id, byte[] rowVersion)
        {
            RequireRole(Person.UserRole.Administrator);

            string[] fieldsToBind = new string[] { "Name", "Budget", "StartDate", "AdministratorID", "RowVersion" };

            if (id == null)
            {
                ViewBag.ErrorMessage = "Department ID is required.";
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                var departmentToUpdate = await db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .Include(d => d.Courses)
                    .FirstOrDefaultAsync(d => d.DepartmentID == id);

                if (departmentToUpdate == null)
                {
                    Department deletedDepartment = new Department();
                    TryUpdateModel(deletedDepartment, fieldsToBind);
                    ModelState.AddModelError(string.Empty,
                        "Unable to save changes. The department was deleted by another user.");
                    ViewBag.AdministratorID = new SelectList(db.Administrators
                        .Where(a => !a.IsDeleted)
                        .ToList(), "ID", "FullName", deletedDepartment.AdministratorID);
                    return View(deletedDepartment);
                }

                if (TryUpdateModel(departmentToUpdate, fieldsToBind))
                {
                    try
                    {
                        db.Entry(departmentToUpdate).OriginalValues["RowVersion"] = rowVersion;
                        await db.SaveChangesAsync();
                        TempData["Success"] = "Department updated successfully";
                        return RedirectToAction("ManageDepartments");
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        var entry = ex.Entries.Single();
                        var clientValues = (Department)entry.Entity;
                        var databaseEntry = entry.GetDatabaseValues();
                        if (databaseEntry == null)
                        {
                            ModelState.AddModelError(string.Empty,
                                "Unable to save changes. The department was deleted by another user.");
                        }
                        else
                        {
                            var databaseValues = (Department)databaseEntry.ToObject();

                            if (databaseValues.Name != clientValues.Name)
                                ModelState.AddModelError("Name", "Current value: " + databaseValues.Name);
                            if (databaseValues.Budget != clientValues.Budget)
                                ModelState.AddModelError("Budget", "Current value: " + String.Format("{0:c}", databaseValues.Budget));
                            if (databaseValues.StartDate != clientValues.StartDate)
                                ModelState.AddModelError("StartDate", "Current value: " + String.Format("{0:d}", databaseValues.StartDate));
                            if (databaseValues.AdministratorID != clientValues.AdministratorID)
                            {
                                var adminName = databaseValues.AdministratorID.HasValue ?
                                    db.Administrators
                                        .Where(a => !a.IsDeleted)
                                        .FirstOrDefault(a => a.ID == databaseValues.AdministratorID)?.FullName : "None";
                                ModelState.AddModelError("AdministratorID", "Current value: " + adminName);
                            }

                            ModelState.AddModelError(string.Empty, "The record you attempted to edit was modified by another user after you got the original value. The edit operation was canceled and the current values in the database have been displayed. If you still want to edit this record, click the Save button again. Otherwise click the Back to List hyperlink.");
                            departmentToUpdate.RowVersion = databaseValues.RowVersion;
                        }
                    }
                    catch (RetryLimitExceededException)
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                    }
                    catch (DbUpdateException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"EditDepartment DbUpdateException: {ex.Message}");
                        ViewBag.ErrorMessage = "An error occurred while updating the department. Please check your input and try again.";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"EditDepartment error: {ex.Message}");
                        ViewBag.ErrorMessage = "An unexpected error occurred while updating the department. Please try again.";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditDepartment POST error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while processing your request. Please try again.";
            }

            try
            {
                ViewBag.AdministratorID = new SelectList(db.Administrators
                    .Where(a => !a.IsDeleted)
                    .ToList(), "ID", "FullName", id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dropdown: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading form data. Please try again.";
            }

            return View();
        }

        // GET: Administrator/DeleteDepartment/5
        public async Task<ActionResult> DeleteDepartment(int? id, bool? concurrencyError)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                ViewBag.ErrorMessage = "Department ID is required.";
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                Department department = await db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .Include(d => d.Courses.Select(c => c.Instructors))
                    .Include(d => d.Courses.Select(c => c.Enrollments))
                    .FirstOrDefaultAsync(d => d.DepartmentID == id);

                if (department == null)
                {
                    if (concurrencyError.GetValueOrDefault())
                    {
                        return RedirectToAction("ManageDepartments");
                    }
                    ViewBag.ErrorMessage = "Department not found.";
                    return HttpNotFound();
                }

                if (concurrencyError.GetValueOrDefault())
                {
                    ViewBag.ConcurrencyErrorMessage = "The record you attempted to delete was modified by another user after you got the original values. The delete operation was canceled and the current values in the database have been displayed. If you still want to delete this record, click the Delete button again. Otherwise click the Back to List hyperlink.";
                }

                return View(department);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteDepartment GET error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading the department for deletion. Please try again.";
                return RedirectToAction("ManageDepartments");
            }
        }

        // POST: Administrator/DeleteDepartment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteDepartment(Department department)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                department.IsDeleted = true;
                await db.SaveChangesAsync();
                TempData["Success"] = "Department deleted successfully";
                return RedirectToAction("ManageDepartments");
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "The department was modified by another user. Please try again.";
                return RedirectToAction("DeleteDepartment", new { concurrencyError = true, id = department.DepartmentID });
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteDepartment DbUpdateException: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the department. Please try again.";
            }
            catch (DataException)
            {
                ModelState.AddModelError(string.Empty, "Unable to delete. Try again, and if the problem persists contact your system administrator.");
                return View(department);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteDepartment error: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the department. Please try again.";
            }

            return RedirectToAction("ManageDepartments");
        }

        [HttpPost]
        public ActionResult AssignAdministratorToDepartment(int administratorId, int departmentId)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var department = db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .FirstOrDefault(d => d.DepartmentID == departmentId);

                if (department != null)
                {
                    department.AdministratorID = administratorId;
                    db.SaveChanges();
                    TempData["Success"] = "Administrator assigned to department successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Department not found.";
                }
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"AssignAdministratorToDepartment DbUpdateException: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while assigning the administrator. Please try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AssignAdministratorToDepartment error: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred while assigning the administrator. Please try again.";
            }

            return RedirectToAction("ManageDepartments");
        }

        #endregion

        #region User Management

        public ActionResult ManageUsers()
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var users = db.People
                    .Where(p => !p.IsDeleted)
                    .ToList();
                return View(users);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManageUsers error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading users. Please try again.";
                return View(new List<Person>());
            }
        }

        public ActionResult ManageStudents(string sortOrder, string currentFilter, string searchString, int? page)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                ViewBag.CurrentSort = sortOrder;
                ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
                ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";

                if (searchString != null)
                {
                    page = 1;
                }
                else
                {
                    searchString = currentFilter;
                }

                ViewBag.CurrentFilter = searchString;

                var students = db.Students
                    .Where(s => !s.IsDeleted)
                    .Include(s => s.Enrollments.Select(e => e.Course.Department))
                    .Include(s => s.Enrollments.Select(e => e.Course.Instructors))
                    .AsQueryable();

                if (!String.IsNullOrEmpty(searchString))
                {
                    students = students.Where(s => s.LastName.Contains(searchString) || s.FirstMidName.Contains(searchString));
                }

                switch (sortOrder)
                {
                    case "name_desc":
                        students = students.OrderByDescending(s => s.LastName);
                        break;
                    case "Date":
                        students = students.OrderBy(s => s.EnrollmentDate);
                        break;
                    case "date_desc":
                        students = students.OrderByDescending(s => s.EnrollmentDate);
                        break;
                    default:
                        students = students.OrderBy(s => s.LastName);
                        break;
                }

                int pageSize = 10;
                int pageNumber = (page ?? 1);
                return View(students.ToPagedList(pageNumber, pageSize));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManageStudents error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading students. Please try again.";
                return View(new List<Student>().ToPagedList(1, 10));
            }
        }

        // GET: Administrator/CreateStudent
        public ActionResult CreateStudent()
        {
            RequireRole(Person.UserRole.Administrator);
            return View();
        }

        // POST: Administrator/CreateStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStudent(Student student)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                if (ModelState.IsValid)
                {
                    // Generate StudentCode automatically if not provided
                    if (string.IsNullOrWhiteSpace(student.StudentCode))
                    {
                        student.StudentCode = GenerateStudentCode();
                    }

                    db.Students.Add(student);
                    db.SaveChanges();
                    TempData["Success"] = "Student created successfully";
                    return RedirectToAction("ManageStudents");
                }
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateStudent DbUpdateException: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while saving the student. Please check your input and try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateStudent error: {ex.Message}");
                ViewBag.ErrorMessage = "An unexpected error occurred while creating the student. Please try again.";
            }

            return View(student);
        }

        // GET: Administrator/StudentDetails/5
        public ActionResult StudentDetails(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                ViewBag.ErrorMessage = "Student ID is required.";
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                Student student = db.Students
                    .Where(s => !s.IsDeleted)
                    .Include(s => s.Enrollments.Select(e => e.Course.Department))
                    .Include(s => s.Enrollments.Select(e => e.Course.Instructors.Select(i => i.OfficeAssignment)))
                    .FirstOrDefault(s => s.ID == id);

                if (student == null)
                {
                    ViewBag.ErrorMessage = "Student not found.";
                    return HttpNotFound();
                }

                return View(student);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StudentDetails error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading student details. Please try again.";
                return RedirectToAction("ManageStudents");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStudent(int id)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var student = db.Students
                    .Where(s => !s.IsDeleted)
                    .FirstOrDefault(s => s.ID == id);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found.";
                    return HttpNotFound();
                }

                student.IsDeleted = true;
                db.SaveChanges();

                TempData["Success"] = "Student has been deactivated.";
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteStudent DbUpdateException: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the student. Please try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteStudent error: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the student. Please try again.";
            }

            return RedirectToAction("ManageStudents");
        }

        [HttpPost]
        public ActionResult AssignCourseToInstructor(int courseId, int instructorId)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var course = db.Courses
                    .Where(c => !c.IsDeleted)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .FirstOrDefault(c => c.CourseID == courseId);

                var instructor = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.OfficeAssignment)
                    .FirstOrDefault(i => i.ID == instructorId);

                if (course != null && instructor != null)
                {
                    course.Instructors.Add(instructor);
                    db.SaveChanges();
                    TempData["Success"] = "Course assigned to instructor successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Course or instructor not found.";
                }
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"AssignCourseToInstructor DbUpdateException: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while assigning the course. Please try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AssignCourseToInstructor error: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred while assigning the course. Please try again.";
            }

            return RedirectToAction("ManageCourses");
        }

        public ActionResult ManageInstructors(int? instructorID, int? courseID, string searchTerm, int? departmentFilter)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var viewModel = new InstructorManagementViewModel
                {
                    InstructorData = new InstructorIndexData
                    {
                        Instructors = GetFilteredInstructors(searchTerm, departmentFilter),
                        SelectedInstructorID = instructorID,
                        SelectedCourseID = courseID
                    },
                    SearchTerm = searchTerm,
                    DepartmentFilter = departmentFilter
                };

                if (instructorID.HasValue)
                {
                    viewModel.InstructorData.Courses = GetInstructorCourses(instructorID.Value);
                }

                if (courseID.HasValue)
                {
                    viewModel.InstructorData.Enrollments = GetCourseEnrollments(courseID.Value);
                }

                ViewBag.Departments = new SelectList(db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .ToList(), "DepartmentID", "Name", departmentFilter);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManageInstructors error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading instructors. Please try again.";
                return View(new InstructorManagementViewModel());
            }
        }

        // GET: Administrator/CreateInstructor
        public ActionResult CreateInstructor()
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                ViewBag.Departments = new SelectList(db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .ToList(), "DepartmentID", "Name");
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateInstructor GET error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading the create instructor form. Please try again.";
                return View();
            }
        }

        // POST: Administrator/CreateInstructor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateInstructor(Instructor instructor)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                if (ModelState.IsValid)
                {
                    db.Instructors.Add(instructor);
                    db.SaveChanges();
                    TempData["Success"] = "Instructor created successfully";
                    return RedirectToAction("ManageInstructors");
                }
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateInstructor DbUpdateException: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while saving the instructor. Please check your input and try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateInstructor error: {ex.Message}");
                ViewBag.ErrorMessage = "An unexpected error occurred while creating the instructor. Please try again.";
            }

            try
            {
                ViewBag.Departments = new SelectList(db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .ToList(), "DepartmentID", "Name");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dropdown: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading form data. Please try again.";
            }

            return View(instructor);
        }

        // GET: Administrator/AssignCoursesToInstructor/5
        public ActionResult AssignCoursesToInstructor(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                ViewBag.ErrorMessage = "Instructor ID is required.";
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            try
            {
                var instructor = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Courses.Select(c => c.Department))
                    .Include(i => i.Courses.Select(c => c.Instructors.Select(ins => ins.OfficeAssignment)))
                    .FirstOrDefault(i => i.ID == id);

                if (instructor == null)
                {
                    ViewBag.ErrorMessage = "Instructor not found.";
                    return HttpNotFound();
                }

                var instructorCourses = new HashSet<int>(instructor.Courses.Where(c => !c.IsDeleted).Select(c => c.CourseID));
                var allCourses = db.Courses
                    .Where(c => !c.IsDeleted)
                    .Include(c => c.Department)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .ToList();

                var viewModel = new AssignCoursesViewModel
                {
                    Instructor = instructor,
                    Courses = allCourses.Select(course => new AssignedCourseData
                    {
                        CourseID = course.CourseID,
                        Title = course.Title,
                        Department = course.Department.Name,
                        Assigned = instructorCourses.Contains(course.CourseID)
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AssignCoursesToInstructor GET error: {ex.Message}");
                ViewBag.ErrorMessage = "An error occurred while loading courses for assignment. Please try again.";
                return RedirectToAction("ManageInstructors");
            }
        }

        // POST: Administrator/AssignCoursesToInstructor/5
        [HttpPost]
        public ActionResult AssignCoursesToInstructor(int id, int[] selectedCourses)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var instructor = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Courses.Select(c => c.Department))
                    .FirstOrDefault(i => i.ID == id);

                if (instructor == null)
                {
                    TempData["ErrorMessage"] = "Instructor not found.";
                    return HttpNotFound();
                }

                UpdateInstructorCourses(selectedCourses, instructor);
                db.SaveChanges();

                TempData["Success"] = "Courses assigned successfully";
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"AssignCoursesToInstructor DbUpdateException: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while assigning courses. Please try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AssignCoursesToInstructor error: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred while assigning courses. Please try again.";
            }

            return RedirectToAction("ManageInstructors", new { instructorID = id });
        }

        // POST: Administrator/RemoveCourseFromInstructor
        [HttpPost]
        public ActionResult RemoveCourseFromInstructor(int instructorId, int courseId)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var instructor = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Courses.Select(c => c.Department))
                    .FirstOrDefault(i => i.ID == instructorId);

                var course = db.Courses
                    .Where(c => !c.IsDeleted)
                    .Include(c => c.Instructors)
                    .FirstOrDefault(c => c.CourseID == courseId);

                if (instructor != null && course != null)
                {
                    instructor.Courses.Remove(course);
                    db.SaveChanges();
                    TempData["Success"] = "Course removed from instructor";
                }
                else
                {
                    TempData["ErrorMessage"] = "Instructor or course not found.";
                }
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"RemoveCourseFromInstructor DbUpdateException: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while removing the course. Please try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RemoveCourseFromInstructor error: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred while removing the course. Please try again.";
            }

            return RedirectToAction("ManageInstructors", new { instructorID = instructorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteInstructor(int id)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                var instructor = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .FirstOrDefault(i => i.ID == id);

                if (instructor == null)
                {
                    TempData["ErrorMessage"] = "Instructor not found.";
                    return HttpNotFound();
                }

                instructor.IsDeleted = true;
                db.SaveChanges();

                TempData["Success"] = "Instructor has been deactivated (soft deleted).";
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteInstructor DbUpdateException: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while deleting the instructor. Please try again.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteInstructor error: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the instructor. Please try again.";
            }

            return RedirectToAction("ManageInstructors");
        }

        #endregion

        #region Helper Methods

        private List<SystemAlert> GetSystemAlerts()
        {
            var alerts = new List<SystemAlert>();

            try
            {
                var lowEnrollmentCourses = db.Courses
                    .Where(c => !c.IsDeleted)
                    .Include(c => c.Department)
                    .Include(c => c.Instructors)
                    .Where(c => c.Enrollments.Count(e => !e.Student.IsDeleted) < 5).ToList();

                if (lowEnrollmentCourses.Any())
                {
                    alerts.Add(new SystemAlert
                    {
                        AlertType = "Warning",
                        Message = $"{lowEnrollmentCourses.Count} courses have low enrollment",
                        Timestamp = DateTime.Now,
                        Priority = "Medium",
                        IsResolved = false
                    });
                }

                var departmentsWithoutHeads = db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .Where(d => d.AdministratorID == null).ToList();

                if (departmentsWithoutHeads.Any())
                {
                    alerts.Add(new SystemAlert
                    {
                        AlertType = "Info",
                        Message = $"{departmentsWithoutHeads.Count} departments need administrators",
                        Timestamp = DateTime.Now,
                        Priority = "Low",
                        IsResolved = false
                    });
                }

                var instructorsWithoutCourses = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.OfficeAssignment)
                    .Include(i => i.Courses)
                    .Where(i => i.Courses.Count(c => !c.IsDeleted) == 0).ToList();

                if (instructorsWithoutCourses.Any())
                {
                    alerts.Add(new SystemAlert
                    {
                        AlertType = "Info",
                        Message = $"{instructorsWithoutCourses.Count} instructors are not assigned to any courses",
                        Timestamp = DateTime.Now,
                        Priority = "Low",
                        IsResolved = false
                    });
                }

                var overloadedInstructors = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.OfficeAssignment)
                    .Include(i => i.Courses.Select(c => c.Enrollments))
                    .Where(i => i.Courses.Where(c => !c.IsDeleted).Sum(c => c.Enrollments.Count(e => !e.Student.IsDeleted)) > 100)
                    .ToList();

                if (overloadedInstructors.Any())
                {
                    alerts.Add(new SystemAlert
                    {
                        AlertType = "Warning",
                        Message = $"{overloadedInstructors.Count} instructors have high student loads",
                        Timestamp = DateTime.Now,
                        Priority = "Medium",
                        IsResolved = false
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSystemAlerts error: {ex.Message}");
                // Don't throw error for system alerts, just return empty list
            }

            return alerts;
        }

        private void UpdateCourseInstructors(int[] selectedInstructors, Course courseToUpdate)
        {
            try
            {
                if (selectedInstructors == null)
                {
                    courseToUpdate.Instructors = new List<Instructor>();
                    return;
                }

                var selectedInstructorsHS = new HashSet<int>(selectedInstructors);
                var courseInstructors = new HashSet<int>(courseToUpdate.Instructors.Where(i => !i.IsDeleted).Select(i => i.ID));

                foreach (var instructor in db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.OfficeAssignment)
                    .Include(i => i.Courses))
                {
                    if (selectedInstructorsHS.Contains(instructor.ID))
                    {
                        if (!courseInstructors.Contains(instructor.ID))
                        {
                            courseToUpdate.Instructors.Add(instructor);
                        }
                    }
                    else
                    {
                        if (courseInstructors.Contains(instructor.ID))
                        {
                            courseToUpdate.Instructors.Remove(instructor);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCourseInstructors error: {ex.Message}");
                throw;
            }
        }

        private void UpdateInstructorCourses(int[] selectedCourses, Instructor instructor)
        {
            try
            {
                if (selectedCourses == null)
                {
                    instructor.Courses = new List<Course>();
                    return;
                }

                var selectedCoursesHS = new HashSet<int>(selectedCourses);
                var instructorCourses = new HashSet<int>(instructor.Courses.Where(c => !c.IsDeleted).Select(c => c.CourseID));

                foreach (var course in db.Courses
                    .Where(c => !c.IsDeleted)
                    .Include(c => c.Instructors)
                    .Include(c => c.Department))
                {
                    if (selectedCoursesHS.Contains(course.CourseID))
                    {
                        if (!instructorCourses.Contains(course.CourseID))
                        {
                            instructor.Courses.Add(course);
                        }
                    }
                    else
                    {
                        if (instructorCourses.Contains(course.CourseID))
                        {
                            instructor.Courses.Remove(course);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateInstructorCourses error: {ex.Message}");
                throw;
            }
        }

        // Helper method to generate unique student code
        private string GenerateStudentCode()
        {
            try
            {
                string code;
                var rnd = new Random();

                do
                {
                    code = "S" + rnd.Next(10000, 99999); // Example: S12345
                }
                while (db.Students.Any(s => s.StudentCode == code)); // Ensure uniqueness

                return code;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GenerateStudentCode error: {ex.Message}");
                // Fallback to timestamp-based code
                return "S" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }
        }

        private void PopulateDepartmentsDropDownList(object selectedDepartment = null)
        {
            try
            {
                var departmentsQuery = db.Departments
                    .Where(d => !d.IsDeleted)
                    .Include(d => d.Administrator)
                    .OrderBy(d => d.Name);
                ViewBag.DepartmentID = new SelectList(departmentsQuery, "DepartmentID", "Name", selectedDepartment);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PopulateDepartmentsDropDownList error: {ex.Message}");
                ViewBag.DepartmentID = new SelectList(new List<Department>(), "DepartmentID", "Name", selectedDepartment);
            }
        }

        private IEnumerable<Instructor> GetFilteredInstructors(string searchTerm, int? departmentFilter)
        {
            try
            {
                var instructors = db.Instructors
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.OfficeAssignment)
                    .Include(i => i.Courses.Select(c => c.Department))
                    .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student)))
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    instructors = instructors.Where(i =>
                        i.LastName.Contains(searchTerm) ||
                        i.FirstMidName.Contains(searchTerm) ||
                        i.UserName.Contains(searchTerm));
                }

                if (departmentFilter.HasValue)
                {
                    instructors = instructors.Where(i =>
                        i.Courses.Any(c => c.DepartmentID == departmentFilter.Value && !c.IsDeleted));
                }

                return instructors.OrderBy(i => i.LastName).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetFilteredInstructors error: {ex.Message}");
                return new List<Instructor>();
            }
        }

        private IEnumerable<Course> GetInstructorCourses(int instructorID)
        {
            try
            {
                return db.Instructors
                    .Where(i => i.ID == instructorID && !i.IsDeleted)
                    .SelectMany(i => i.Courses)
                    .Where(c => !c.IsDeleted)
                    .Include(c => c.Department)
                    .Include(c => c.Enrollments.Select(e => e.Student))
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment))
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetInstructorCourses error: {ex.Message}");
                return new List<Course>();
            }
        }

        private IEnumerable<Enrollment> GetCourseEnrollments(int courseID)
        {
            try
            {
                return db.Courses
                    .Where(c => c.CourseID == courseID && !c.IsDeleted)
                    .SelectMany(c => c.Enrollments)
                    .Where(e => !e.Student.IsDeleted)
                    .Include(e => e.Student)
                    .Include(e => e.Course.Department)
                    .Include(e => e.Course.Instructors.Select(i => i.OfficeAssignment))
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCourseEnrollments error: {ex.Message}");
                return new List<Enrollment>();
            }
        }

        #endregion
    }
}