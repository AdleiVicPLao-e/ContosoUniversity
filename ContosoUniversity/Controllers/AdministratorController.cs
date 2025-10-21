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
                    TotalStudents = db.Students.Count(),
                    TotalInstructors = db.Instructors.Count(),
                    TotalCourses = db.Courses.Count(),
                    TotalDepartments = db.Departments.Count(),
                    NewEnrollmentsThisMonth = db.Enrollments.Count(e => e.EnrollmentID > 0),
                    ActiveCourses = db.Courses.Count(c => c.Enrollments.Count > 0),
                    RecentEnrollments = db.Enrollments
                        .Include(e => e.Student)
                        .Include(e => e.Course.Department) // Include Course Department
                        .Include(e => e.Course.Instructors) // Include Course Instructors
                        .OrderByDescending(e => e.EnrollmentID)
                        .Take(10)
                        .ToList(),
                    RecentCourses = db.Courses
                        .Include(c => c.Department)
                        .Include(c => c.Instructors) // Include Instructors
                        .Include(c => c.Enrollments) // Include Enrollments
                        .OrderByDescending(c => c.CourseID)
                        .Take(5)
                        .ToList(),
                    ShowQuickActions = true
                };

                // Fix DepartmentStatistics with null checks and eager loading
                var departments = db.Departments
                    .Include(d => d.Courses.Select(c => c.Enrollments)) // Include Courses and Enrollments
                    .Include(d => d.Administrator) // Include Administrator
                    .Include(d => d.Courses.Select(c => c.Instructors)) // Include Course Instructors
                    .ToList();

                viewModel.DepartmentStatistics = departments.Select(d => new DepartmentStats
                {
                    DepartmentName = d?.Name ?? "Unknown Department",
                    TotalCourses = d?.Courses?.Count ?? 0,
                    TotalStudents = d?.Courses?.Sum(c => c?.Enrollments?.Count ?? 0) ?? 0,
                    TotalInstructors = db.Instructors
                        .Include(i => i.Courses.Select(c => c.Department)) // Include Instructor Courses and Departments
                        .Count(i => i.Courses.Any(c => c.DepartmentID == d.DepartmentID)),
                    BudgetUtilization = (d?.Budget ?? 0) > 0 ?
                        ((d?.Courses?.Sum(c => (c?.Credits ?? 0) * 1000m) ?? 0) / (d?.Budget ?? 1m)) : 0m,
                    DepartmentHead = d?.Administrator != null ?
                        $"{d.Administrator.FirstMidName} {d.Administrator.LastName}".Trim() : "Not Assigned"
                }).ToList();

                // Fix PopularCourses with null checks and eager loading
                var courses = db.Courses
                    .Include(c => c.Department)
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment)) // Include Instructors and OfficeAssignment
                    .Include(c => c.Enrollments.Select(e => e.Student)) // Include Enrollments and Students
                    .ToList();

                viewModel.PopularCourses = courses.Select(c => new CourseEnrollmentStats
                {
                    CourseTitle = c?.Title ?? "Unknown Course",
                    CourseCode = c != null ? $"CS{c.CourseID}" : "CS0",
                    EnrolledStudents = c?.Enrollments?.Count ?? 0,
                    Capacity = c?.Capacity ?? 0,
                    InstructorName = c?.Instructors?.FirstOrDefault() != null ?
                        $"{c.Instructors.First().FirstMidName} {c.Instructors.First().LastName}".Trim() : "Not Assigned",
                    DepartmentName = c?.Department?.Name ?? "No Department"
                })
                .OrderByDescending(c => c.EnrolledStudents)
                .Take(5)
                .ToList();

                // Fix InstructorWorkload with null checks and eager loading
                var instructors = db.Instructors
                    .Include(i => i.OfficeAssignment)
                    .Include(i => i.Courses.Select(c => c.Department)) // Include Courses and Departments
                    .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student))) // Include Courses, Enrollments, and Students
                    .ToList();

                viewModel.InstructorWorkload = instructors.Select(i => new InstructorStats
                {
                    InstructorName = i != null ? $"{i.FirstMidName} {i.LastName}".Trim() : "Unknown Instructor",
                    DepartmentName = i?.Courses?.FirstOrDefault()?.Department?.Name ?? "Not Assigned",
                    CoursesTeaching = i?.Courses?.Count ?? 0,
                    TotalStudents = i?.Courses?.Sum(c => c?.Enrollments?.Count ?? 0) ?? 0,
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
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Dashboard error: {ex.Message}");

                // Return a basic view model without statistics
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

            var courses = db.Courses
                .Include(c => c.Department)
                .Include(c => c.Instructors.Select(i => i.OfficeAssignment)) // Include Instructors and OfficeAssignment
                .Include(c => c.Enrollments.Select(e => e.Student)) // Include Enrollments and Students
                .AsQueryable();

            if (departmentId.HasValue)
            {
                courses = courses.Where(c => c.DepartmentID == departmentId.Value);
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
                    courses = courses.Where(c => c.Enrollments.Count >= c.Capacity);
                }
                else if (status == "low")
                {
                    courses = courses.Where(c => c.Enrollments.Count < 5);
                }
            }

            ViewBag.Departments = new SelectList(db.Departments, "DepartmentID", "Name", departmentId);
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

        // GET: Administrator/CreateCourse
        public ActionResult CreateCourse()
        {
            RequireRole(Person.UserRole.Administrator);
            PopulateDepartmentsDropDownList();
            ViewBag.Instructors = new MultiSelectList(db.Instructors
                .Include(i => i.OfficeAssignment) // Include OfficeAssignment
                .ToList(), "ID", "FullName");
            return View();
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
                            .Include(i => i.OfficeAssignment) // Include OfficeAssignment
                            .Where(i => selectedInstructors.Contains(i.ID))
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

            PopulateDepartmentsDropDownList(course.DepartmentID);
            ViewBag.Instructors = new MultiSelectList(db.Instructors
                .Include(i => i.OfficeAssignment) // Include OfficeAssignment
                .ToList(), "ID", "FullName", selectedInstructors);
            return View(course);
        }

        // GET: Administrator/EditCourse/5
        public ActionResult EditCourse(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            Course course = db.Courses
                .Include(c => c.Instructors.Select(i => i.OfficeAssignment)) // Include Instructors and OfficeAssignment
                .Include(c => c.Department) // Include Department
                .Include(c => c.Enrollments.Select(e => e.Student)) // Include Enrollments and Students
                .FirstOrDefault(c => c.CourseID == id);

            if (course == null)
            {
                return HttpNotFound();
            }

            PopulateDepartmentsDropDownList(course.DepartmentID);
            ViewBag.Instructors = new MultiSelectList(db.Instructors
                .Include(i => i.OfficeAssignment) // Include OfficeAssignment
                .ToList(), "ID", "FullName",
                course.Instructors.Select(i => i.ID));
            return View(course);
        }

        // POST: Administrator/EditCourse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCourse(Course course, int[] selectedInstructors)
        {
            RequireRole(Person.UserRole.Administrator);

            if (ModelState.IsValid)
            {
                var courseToUpdate = db.Courses
                    .Include(c => c.Instructors.Select(i => i.OfficeAssignment)) // Include Instructors and OfficeAssignment
                    .Include(c => c.Department) // Include Department
                    .FirstOrDefault(c => c.CourseID == course.CourseID);

                if (courseToUpdate == null)
                {
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

            PopulateDepartmentsDropDownList(course.DepartmentID);
            ViewBag.Instructors = new MultiSelectList(db.Instructors
                .Include(i => i.OfficeAssignment) // Include OfficeAssignment
                .ToList(), "ID", "FullName", selectedInstructors);
            return View(course);
        }

        // GET: Administrator/CourseDetails/5
        public ActionResult CourseDetails(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            Course course = db.Courses
                .Include(c => c.Department)
                .Include(c => c.Instructors.Select(i => i.OfficeAssignment)) // Include Instructors and OfficeAssignment
                .Include(c => c.Enrollments.Select(e => e.Student)) // Include Enrollments and Students
                .FirstOrDefault(c => c.CourseID == id);

            if (course == null)
            {
                return HttpNotFound();
            }

            return View(course);
        }

        // GET: Administrator/DeleteCourse/5
        public ActionResult DeleteCourse(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            Course course = db.Courses
                .Include(c => c.Department) // Include Department
                .Include(c => c.Instructors.Select(i => i.OfficeAssignment)) // Include Instructors and OfficeAssignment
                .Include(c => c.Enrollments.Select(e => e.Student)) // Include Enrollments and Students
                .FirstOrDefault(c => c.CourseID == id);

            if (course == null)
            {
                return HttpNotFound();
            }

            return View(course);
        }

        // POST: Administrator/DeleteCourse/5
        [HttpPost, ActionName("DeleteCourse")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCourseConfirmed(int id)
        {
            RequireRole(Person.UserRole.Administrator);

            Course course = db.Courses
                .Include(c => c.Instructors) // Include Instructors
                .Include(c => c.Enrollments) // Include Enrollments
                .FirstOrDefault(c => c.CourseID == id);

            if (course != null)
            {
                db.Courses.Remove(course);
                db.SaveChanges();
                TempData["Success"] = "Course deleted successfully";
            }

            return RedirectToAction("ManageCourses");
        }

        // POST: Administrator/ToggleCourseStatus/5
        [HttpPost]
        public ActionResult ToggleCourseStatus(int id)
        {
            RequireRole(Person.UserRole.Administrator);

            var course = db.Courses
                .Include(c => c.Instructors) // Include Instructors
                .Include(c => c.Enrollments) // Include Enrollments
                .FirstOrDefault(c => c.CourseID == id);

            if (course != null)
            {
                course.IsActive = !course.IsActive;
                db.SaveChanges();
                TempData["Success"] = $"Course {(course.IsActive ? "activated" : "deactivated")} successfully";
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

            if (multiplier != null)
            {
                ViewBag.RowsAffected = db.Database.ExecuteSqlCommand("UPDATE Course SET Credits = Credits * {0}", multiplier);
                TempData["Success"] = $"{ViewBag.RowsAffected} courses updated";
            }

            return View();
        }

        private void PopulateDepartmentsDropDownList(object selectedDepartment = null)
        {
            var departmentsQuery = db.Departments
                .Include(d => d.Administrator) // Include Administrator
                .OrderBy(d => d.Name);
            ViewBag.DepartmentID = new SelectList(departmentsQuery, "DepartmentID", "Name", selectedDepartment);
        }

        #endregion

        #region Department Management

        // GET: Administrator/ManageDepartments
        public async Task<ActionResult> ManageDepartments()
        {
            RequireRole(Person.UserRole.Administrator);
            var departments = db.Departments
                .Include(d => d.Administrator) // Include Administrator
                .Include(d => d.Courses.Select(c => c.Instructors)) // Include Courses and Instructors
                .Include(d => d.Courses.Select(c => c.Enrollments)); // Include Courses and Enrollments
            return View(await departments.ToListAsync());
        }

        // GET: Administrator/DepartmentDetails/5
        public async Task<ActionResult> DepartmentDetails(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            Department department = await db.Departments
                .Include(d => d.Administrator) // Include Administrator
                .Include(d => d.Courses.Select(c => c.Instructors.Select(i => i.OfficeAssignment))) // Include Courses, Instructors, and OfficeAssignment
                .Include(d => d.Courses.Select(c => c.Enrollments.Select(e => e.Student))) // Include Courses, Enrollments, and Students
                .FirstOrDefaultAsync(d => d.DepartmentID == id);

            if (department == null)
            {
                return HttpNotFound();
            }
            return View(department);
        }

        // GET: Administrator/CreateDepartment
        public ActionResult CreateDepartment()
        {
            RequireRole(Person.UserRole.Administrator);
            // Updated to use AdministratorID and filter by Administrator role with eager loading
            ViewBag.AdministratorID = new SelectList(db.Administrators
                .ToList(), "ID", "FullName");
            return View();
        }

        // POST: Administrator/CreateDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateDepartment([Bind(Include = "DepartmentID,Name,Budget,StartDate,AdministratorID")] Department department)
        {
            RequireRole(Person.UserRole.Administrator);

            if (ModelState.IsValid)
            {
                db.Departments.Add(department);
                await db.SaveChangesAsync();
                TempData["Success"] = "Department created successfully";
                return RedirectToAction("ManageDepartments");
            }

            ViewBag.AdministratorID = new SelectList(db.Administrators
                .ToList(), "ID", "FullName", department.AdministratorID);
            return View(department);
        }

        // GET: Administrator/EditDepartment/5
        public async Task<ActionResult> EditDepartment(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            Department department = await db.Departments
                .Include(d => d.Administrator) // Include Administrator
                .Include(d => d.Courses) // Include Courses
                .FirstOrDefaultAsync(d => d.DepartmentID == id);

            if (department == null)
            {
                return HttpNotFound();
            }

            // Updated to use AdministratorID with eager loading
            ViewBag.AdministratorID = new SelectList(db.Administrators
                .ToList(), "ID", "FullName", department.AdministratorID);
            return View(department);
        }

        // POST: Administrator/EditDepartment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditDepartment(int? id, byte[] rowVersion)
        {
            RequireRole(Person.UserRole.Administrator);

            // Updated field list to use AdministratorID
            string[] fieldsToBind = new string[] { "Name", "Budget", "StartDate", "AdministratorID", "RowVersion" };

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var departmentToUpdate = await db.Departments
                .Include(d => d.Administrator) // Include Administrator
                .Include(d => d.Courses) // Include Courses
                .FirstOrDefaultAsync(d => d.DepartmentID == id);

            if (departmentToUpdate == null)
            {
                Department deletedDepartment = new Department();
                TryUpdateModel(deletedDepartment, fieldsToBind);
                ModelState.AddModelError(string.Empty,
                    "Unable to save changes. The department was deleted by another user.");
                ViewBag.AdministratorID = new SelectList(db.Administrators
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
            }
            ViewBag.AdministratorID = new SelectList(db.Administrators
                .ToList(), "ID", "FullName", departmentToUpdate.AdministratorID);
            return View(departmentToUpdate);
        }

        // GET: Administrator/DeleteDepartment/5
        public async Task<ActionResult> DeleteDepartment(int? id, bool? concurrencyError)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            Department department = await db.Departments
                .Include(d => d.Administrator) // Include Administrator
                .Include(d => d.Courses.Select(c => c.Instructors)) // Include Courses and Instructors
                .Include(d => d.Courses.Select(c => c.Enrollments)) // Include Courses and Enrollments
                .FirstOrDefaultAsync(d => d.DepartmentID == id);

            if (department == null)
            {
                if (concurrencyError.GetValueOrDefault())
                {
                    return RedirectToAction("ManageDepartments");
                }
                return HttpNotFound();
            }

            if (concurrencyError.GetValueOrDefault())
            {
                ViewBag.ConcurrencyErrorMessage = "The record you attempted to delete was modified by another user after you got the original values. The delete operation was canceled and the current values in the database have been displayed. If you still want to delete this record, click the Delete button again. Otherwise click the Back to List hyperlink.";
            }

            return View(department);
        }

        // POST: Administrator/DeleteDepartment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteDepartment(Department department)
        {
            RequireRole(Person.UserRole.Administrator);

            try
            {
                db.Entry(department).State = EntityState.Deleted;
                await db.SaveChangesAsync();
                TempData["Success"] = "Department deleted successfully";
                return RedirectToAction("ManageDepartments");
            }
            catch (DbUpdateConcurrencyException)
            {
                return RedirectToAction("DeleteDepartment", new { concurrencyError = true, id = department.DepartmentID });
            }
            catch (DataException)
            {
                ModelState.AddModelError(string.Empty, "Unable to delete. Try again, and if the problem persists contact your system administrator.");
                return View(department);
            }
        }

        [HttpPost]
        public ActionResult AssignAdministratorToDepartment(int administratorId, int departmentId)
        {
            RequireRole(Person.UserRole.Administrator);
            var department = db.Departments
                .Include(d => d.Administrator) // Include Administrator
                .FirstOrDefault(d => d.DepartmentID == departmentId);

            if (department != null)
            {
                department.AdministratorID = administratorId;
                db.SaveChanges();
                TempData["Success"] = "Administrator assigned to department successfully";
            }

            return RedirectToAction("ManageDepartments");
        }

        #endregion

        #region User Management

        public ActionResult ManageUsers()
        {
            RequireRole(Person.UserRole.Administrator);
            var users = db.People
                .ToList();
            return View(users);
        }

        public ActionResult ManageStudents(string sortOrder, string currentFilter, string searchString, int? page)
        {
            RequireRole(Person.UserRole.Administrator);

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
                .Include(s => s.Enrollments.Select(e => e.Course.Department)) // Include Enrollments, Courses, and Departments
                .Include(s => s.Enrollments.Select(e => e.Course.Instructors)) // Include Enrollments, Courses, and Instructors
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

            if (ModelState.IsValid)
            {
                db.Students.Add(student);
                db.SaveChanges();
                TempData["Success"] = "Student created successfully";
                return RedirectToAction("ManageStudents");
            }

            return View(student);
        }

        // GET: Administrator/StudentDetails/5
        public ActionResult StudentDetails(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            Student student = db.Students
                .Include(s => s.Enrollments.Select(e => e.Course.Department)) // Include Enrollments, Courses, and Departments
                .Include(s => s.Enrollments.Select(e => e.Course.Instructors.Select(i => i.OfficeAssignment))) // Include Enrollments, Courses, Instructors, and OfficeAssignment
                .FirstOrDefault(s => s.ID == id);

            if (student == null)
            {
                return HttpNotFound();
            }

            return View(student);
        }

        [HttpPost]
        public ActionResult AssignCourseToInstructor(int courseId, int instructorId)
        {
            RequireRole(Person.UserRole.Administrator);
            var course = db.Courses
                .Include(c => c.Instructors.Select(i => i.OfficeAssignment)) // Include Instructors and OfficeAssignment
                .First(c => c.CourseID == courseId);
            var instructor = db.Instructors
                .Include(i => i.OfficeAssignment) // Include OfficeAssignment
                .First(i => i.ID == instructorId);
            course.Instructors.Add(instructor);
            db.SaveChanges();
            return RedirectToAction("ManageCourses");
        }

        public ActionResult ManageInstructors(int? instructorID, int? courseID, string searchTerm, int? departmentFilter)
        {
            RequireRole(Person.UserRole.Administrator);

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
                .Include(d => d.Administrator) // Include Administrator
                .ToList(), "DepartmentID", "Name", departmentFilter);
            return View(viewModel);
        }

        private IEnumerable<Instructor> GetFilteredInstructors(string searchTerm, int? departmentFilter)
        {
            var instructors = db.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses.Select(c => c.Department)) // Include Courses and Departments
                .Include(i => i.Courses.Select(c => c.Enrollments.Select(e => e.Student))) // Include Courses, Enrollments, and Students
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
                    i.Courses.Any(c => c.DepartmentID == departmentFilter.Value));
            }

            return instructors.OrderBy(i => i.LastName).ToList();
        }

        private IEnumerable<Course> GetInstructorCourses(int instructorID)
        {
            return db.Instructors
                .Where(i => i.ID == instructorID)
                .SelectMany(i => i.Courses)
                .Include(c => c.Department)
                .Include(c => c.Enrollments.Select(e => e.Student)) // Include Enrollments and Students
                .Include(c => c.Instructors.Select(i => i.OfficeAssignment)) // Include Instructors and OfficeAssignment
                .ToList();
        }

        private IEnumerable<Enrollment> GetCourseEnrollments(int courseID)
        {
            return db.Courses
                .Where(c => c.CourseID == courseID)
                .SelectMany(c => c.Enrollments)
                .Include(e => e.Student)
                .Include(e => e.Course.Department) // Include Course and Department
                .Include(e => e.Course.Instructors.Select(i => i.OfficeAssignment)) // Include Course, Instructors, and OfficeAssignment
                .ToList();
        }

        // GET: Administrator/CreateInstructor
        public ActionResult CreateInstructor()
        {
            RequireRole(Person.UserRole.Administrator);
            ViewBag.Departments = new SelectList(db.Departments
                .Include(d => d.Administrator) // Include Administrator
                .ToList(), "DepartmentID", "Name");
            return View();
        }

        // POST: Administrator/CreateInstructor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateInstructor(Instructor instructor)
        {
            RequireRole(Person.UserRole.Administrator);

            if (ModelState.IsValid)
            {
                db.Instructors.Add(instructor);
                db.SaveChanges();
                TempData["Success"] = "Instructor created successfully";
                return RedirectToAction("ManageInstructors");
            }

            ViewBag.Departments = new SelectList(db.Departments
                .Include(d => d.Administrator) // Include Administrator
                .ToList(), "DepartmentID", "Name");
            return View(instructor);
        }

        // GET: Administrator/AssignCoursesToInstructor/5
        public ActionResult AssignCoursesToInstructor(int? id)
        {
            RequireRole(Person.UserRole.Administrator);

            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var instructor = db.Instructors
                .Include(i => i.Courses.Select(c => c.Department)) // Include Courses and Departments
                .Include(d => d.Courses.Select(c => c.Instructors.Select(ins => ins.OfficeAssignment)))// Include Courses, Instructors, and OfficeAssignment
                .FirstOrDefault(i => i.ID == id);

            if (instructor == null)
            {
                return HttpNotFound();
            }

            var instructorCourses = new HashSet<int>(instructor.Courses.Select(c => c.CourseID));
            var allCourses = db.Courses
                .Include(c => c.Department)
                .Include(c => c.Instructors.Select(i => i.OfficeAssignment)) // Include Instructors and OfficeAssignment
                .Include(c => c.Enrollments.Select(e => e.Student)) // Include Enrollments and Students
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

        // POST: Administrator/AssignCoursesToInstructor/5
        [HttpPost]
        public ActionResult AssignCoursesToInstructor(int id, int[] selectedCourses)
        {
            RequireRole(Person.UserRole.Administrator);

            var instructor = db.Instructors
                .Include(i => i.Courses.Select(c => c.Department)) // Include Courses and Departments
                .FirstOrDefault(i => i.ID == id);

            if (instructor == null)
            {
                return HttpNotFound();
            }

            UpdateInstructorCourses(selectedCourses, instructor);
            db.SaveChanges();

            TempData["Success"] = "Courses assigned successfully";
            return RedirectToAction("ManageInstructors", new { instructorID = id });
        }

        private void UpdateInstructorCourses(int[] selectedCourses, Instructor instructor)
        {
            if (selectedCourses == null)
            {
                instructor.Courses = new List<Course>();
                return;
            }

            var selectedCoursesHS = new HashSet<int>(selectedCourses);
            var instructorCourses = new HashSet<int>(instructor.Courses.Select(c => c.CourseID));

            foreach (var course in db.Courses
                .Include(c => c.Instructors) // Include Instructors
                .Include(c => c.Department)) // Include Department
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

        // POST: Administrator/RemoveCourseFromInstructor
        [HttpPost]
        public ActionResult RemoveCourseFromInstructor(int instructorId, int courseId)
        {
            RequireRole(Person.UserRole.Administrator);

            var instructor = db.Instructors
                .Include(i => i.Courses.Select(c => c.Department)) // Include Courses and Departments
                .FirstOrDefault(i => i.ID == instructorId);

            var course = db.Courses
                .Include(c => c.Instructors) // Include Instructors
                .FirstOrDefault(c => c.CourseID == courseId);

            if (instructor != null && course != null)
            {
                instructor.Courses.Remove(course);
                db.SaveChanges();
                TempData["Success"] = "Course removed from instructor";
            }

            return RedirectToAction("ManageInstructors", new { instructorID = instructorId });
        }
        #endregion

        #region Helper Methods

        private List<SystemAlert> GetSystemAlerts()
        {
            var alerts = new List<SystemAlert>();

            var lowEnrollmentCourses = db.Courses
                .Include(c => c.Department) // Include Department
                .Include(c => c.Instructors) // Include Instructors
                .Where(c => c.Enrollments.Count < 5).ToList();
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

            // Updated to check for AdministratorID instead of InstructorID
            var departmentsWithoutHeads = db.Departments
                .Include(d => d.Administrator) // Include Administrator
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
                .Include(i => i.OfficeAssignment) // Include OfficeAssignment
                .Include(i => i.Courses) // Include Courses
                .Where(i => i.Courses.Count == 0).ToList();
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
                .Include(i => i.OfficeAssignment) // Include OfficeAssignment
                .Include(i => i.Courses.Select(c => c.Enrollments)) // Include Courses and Enrollments
                .Where(i => i.Courses.Sum(c => c.Enrollments.Count) > 100)
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

            return alerts;
        }

        private void UpdateCourseInstructors(int[] selectedInstructors, Course courseToUpdate)
        {
            if (selectedInstructors == null)
            {
                courseToUpdate.Instructors = new List<Instructor>();
                return;
            }

            var selectedInstructorsHS = new HashSet<int>(selectedInstructors);
            var courseInstructors = new HashSet<int>(courseToUpdate.Instructors.Select(i => i.ID));

            foreach (var instructor in db.Instructors
                .Include(i => i.OfficeAssignment) // Include OfficeAssignment
                .Include(i => i.Courses)) // Include Courses
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

        #endregion
    }
}