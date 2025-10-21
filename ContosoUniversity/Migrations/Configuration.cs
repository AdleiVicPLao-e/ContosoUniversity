namespace ContosoUniversity.Migrations
{
    using ContosoUniversity.Models;
    using ContosoUniversity.DAL;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using DotNetEnv;


    internal sealed class Configuration : DbMigrationsConfiguration<SchoolContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(SchoolContext context)
        {
            Env.Load();

            // -----------------------------
            // 1. Administrators
            // -----------------------------
            var admins = new List<Administrator>
            {
                new Administrator
                {
                    FirstMidName = "Admin",
                    LastName = "User",
                    UserName = "admin",
                    Password = "password123",
                    Roles = Person.UserRole.Administrator,
                    AdministratorSince = DateTime.Parse("2010-01-01"),
                    AdministrativeLevel = "SuperAdmin",
                    CanManageUsers = true,
                    CanManageSystem = true,
                    CanViewReports = true,
                    CanManageAllDepartments = true,
                    IsLoggedIn = false
                },
                new Administrator
                {
                    FirstMidName = "Department",
                    LastName = "Head",
                    UserName = "deptadmin",
                    Password = "password123",
                    Roles = Person.UserRole.Administrator,
                    AdministratorSince = DateTime.Parse("2015-06-01"),
                    AdministrativeLevel = "DepartmentAdmin",
                    CanManageUsers = false,
                    CanManageSystem = false,
                    CanViewReports = true,
                    CanManageAllDepartments = false,
                    IsLoggedIn = false
                },
                new Administrator
                {
                    FirstMidName = "Engineering",
                    LastName = "Admin",
                    UserName = "engadmin",
                    Password = "password123",
                    Roles = Person.UserRole.Administrator,
                    AdministratorSince = DateTime.Parse("2018-03-15"),
                    AdministrativeLevel = "DepartmentAdmin",
                    CanManageUsers = false,
                    CanManageSystem = false,
                    CanViewReports = true,
                    CanManageAllDepartments = false,
                    IsLoggedIn = false
                },
                new Administrator
                {
                    FirstMidName = "Science",
                    LastName = "Admin",
                    UserName = "sciadmin",
                    Password = "password123",
                    Roles = Person.UserRole.Administrator,
                    AdministratorSince = DateTime.Parse("2020-01-10"),
                    AdministrativeLevel = "DepartmentAdmin",
                    CanManageUsers = false,
                    CanManageSystem = false,
                    CanViewReports = true,
                    CanManageAllDepartments = false,
                    IsLoggedIn = false
                },
                new Administrator
                {
                    FirstMidName = "Arts",
                    LastName = "Admin",
                    UserName = "artsadmin",
                    Password = "password123",
                    Roles = Person.UserRole.Administrator,
                    AdministratorSince = DateTime.Parse("2019-08-20"),
                    AdministrativeLevel = "DepartmentAdmin",
                    CanManageUsers = false,
                    CanManageSystem = false,
                    CanViewReports = true,
                    CanManageAllDepartments = false,
                    IsLoggedIn = false
                }
            };
            admins.ForEach(a => context.Administrators.AddOrUpdate(p => p.UserName, a));
            context.SaveChanges();

            // -----------------------------
            // 2. Instructors
            // -----------------------------
            var instructors = new List<Instructor>
            {
                new Instructor { FirstMidName="Kim", LastName="Abercrombie", UserName="kim.abercrombie", Password="password123", Roles=Person.UserRole.Instructor, HireDate=DateTime.Parse("1995-03-11"), Specialization="Computer Science", Salary=75000m, IsLoggedIn=false },
                new Instructor { FirstMidName="Fadi", LastName="Fakhouri", UserName="fadi.fakhouri", Password="password123", Roles=Person.UserRole.Instructor, HireDate=DateTime.Parse("2002-07-06"), Specialization="Software Engineering", Salary=80000m, IsLoggedIn=false },
                new Instructor { FirstMidName="Roger", LastName="Harui", UserName="roger.harui", Password="password123", Roles=Person.UserRole.Instructor, HireDate=DateTime.Parse("1998-07-01"), Specialization="Economics", Salary=70000m, IsLoggedIn=false },
                new Instructor { FirstMidName="Candace", LastName="Kapoor", UserName="candace.kapoor", Password="password123", Roles=Person.UserRole.Instructor, HireDate=DateTime.Parse("2001-01-15"), Specialization="Mathematics", Salary=72000m, IsLoggedIn=false },
                new Instructor { FirstMidName="Roger", LastName="Zheng", UserName="roger.zheng", Password="password123", Roles=Person.UserRole.Instructor, HireDate=DateTime.Parse("2004-02-12"), Specialization="Chemistry", Salary=68000m, IsLoggedIn=false },
                new Instructor { FirstMidName="Test", LastName="Instructor", UserName="instructor", Password="password123", Roles=Person.UserRole.Instructor, HireDate=DateTime.Parse("2010-01-01"), Specialization="Computer Science", Salary=65000m, IsLoggedIn=false }
            };
            instructors.ForEach(i => context.Instructors.AddOrUpdate(p => p.UserName, i));
            context.SaveChanges();

            // -----------------------------
            // 3. Students
            // -----------------------------
            var students = new List<Student>
            {
                new Student { FirstMidName="Carson", LastName="Alexander", UserName="carson.alexander", Password="password123", Roles=Person.UserRole.Student, EnrollmentDate=DateTime.Parse("2010-09-01"), StudentCode="S10001", IsLoggedIn=false },
                new Student { FirstMidName="Meredith", LastName="Alonso", UserName="meredith.alonso", Password="password123", Roles=Person.UserRole.Student, EnrollmentDate=DateTime.Parse("2012-09-01"), StudentCode="S10002", IsLoggedIn=false },
                new Student { FirstMidName="Arturo", LastName="Anand", UserName="arturo.anand", Password="password123", Roles=Person.UserRole.Student, EnrollmentDate=DateTime.Parse("2013-09-01"), StudentCode="S10003", IsLoggedIn=false },
                new Student { FirstMidName="Gytis", LastName="Barzdukas", UserName="gytis.barzdukas", Password="password123", Roles=Person.UserRole.Student, EnrollmentDate=DateTime.Parse("2012-09-01"), StudentCode="S10004", IsLoggedIn=false },
                new Student { FirstMidName="Yan", LastName="Li", UserName="yan.li", Password="password123", Roles=Person.UserRole.Student, EnrollmentDate=DateTime.Parse("2012-09-01"), StudentCode="S10005", IsLoggedIn=false },
                new Student { FirstMidName="Peggy", LastName="Justice", UserName="peggy.justice", Password="password123", Roles=Person.UserRole.Student, EnrollmentDate=DateTime.Parse("2011-09-01"), StudentCode="S10006", IsLoggedIn=false },
                new Student { FirstMidName="Laura", LastName="Norman", UserName="laura.norman", Password="password123", Roles=Person.UserRole.Student, EnrollmentDate=DateTime.Parse("2013-09-01"), StudentCode="S10007", IsLoggedIn=false },
                new Student { FirstMidName="Nino", LastName="Olivetto", UserName="nino.olivetto", Password="password123", Roles=Person.UserRole.Student, EnrollmentDate=DateTime.Parse("2005-09-01"), StudentCode="S10008", IsLoggedIn=false },
                new Student { FirstMidName="Test", LastName="Student", UserName="student", Password="password123", Roles=Person.UserRole.Student, EnrollmentDate=DateTime.Parse("2023-09-01"), StudentCode="S99999", IsLoggedIn=false }
            };
            students.ForEach(s => context.Students.AddOrUpdate(p => p.UserName, s));
            context.SaveChanges();

            // -----------------------------
            // 4. Departments (UPDATED with AdministratorID)
            // -----------------------------
            var adminUser = admins.First(a => a.LastName == "User");
            var deptHead = admins.First(a => a.LastName == "Head");
            var engAdmin = admins.First(a => a.LastName == "Admin" && a.FirstMidName == "Engineering");
            var sciAdmin = admins.First(a => a.LastName == "Admin" && a.FirstMidName == "Science");
            var artsAdmin = admins.First(a => a.LastName == "Admin" && a.FirstMidName == "Arts");

            var departments = new List<Department>
            {
                new Department {
                    Name="English",
                    Budget=350000,
                    StartDate=DateTime.Parse("2007-09-01"),
                    AdministratorID = artsAdmin.ID
                },
                new Department {
                    Name="Mathematics",
                    Budget=100000,
                    StartDate=DateTime.Parse("2007-09-01"),
                    AdministratorID = adminUser.ID
                },
                new Department {
                    Name="Engineering",
                    Budget=350000,
                    StartDate=DateTime.Parse("2007-09-01"),
                    AdministratorID = engAdmin.ID
                },
                new Department {
                    Name="Economics",
                    Budget=100000,
                    StartDate=DateTime.Parse("2007-09-01"),
                    AdministratorID = deptHead.ID
                },
                new Department {
                    Name="Chemistry",
                    Budget=180000,
                    StartDate=DateTime.Parse("2008-09-01"),
                    AdministratorID = sciAdmin.ID
                }
            };
            departments.ForEach(d => context.Departments.AddOrUpdate(p => p.Name, d));
            context.SaveChanges();

            // -----------------------------
            // 5. Courses
            // -----------------------------
            var chemistryDept = departments.First(d => d.Name == "Chemistry");
            var economicsDept = departments.First(d => d.Name == "Economics");
            var mathDept = departments.First(d => d.Name == "Mathematics");
            var englishDept = departments.First(d => d.Name == "English");
            var engineeringDept = departments.First(d => d.Name == "Engineering");

            var courses = new List<Course>
            {
                new Course{CourseID=1050, Title="Chemistry", Credits=3, Capacity=30, Description="Intro to chemical principles", DepartmentID=chemistryDept.DepartmentID},
                new Course{CourseID=4022, Title="Microeconomics", Credits=3, Capacity=25, Description="Individual economic behavior", DepartmentID=economicsDept.DepartmentID},
                new Course{CourseID=4041, Title="Macroeconomics", Credits=3, Capacity=25, Description="Aggregate economic activity", DepartmentID=economicsDept.DepartmentID},
                new Course{CourseID=1045, Title="Calculus", Credits=4, Capacity=35, Description="Differential & integral calculus", DepartmentID=mathDept.DepartmentID},
                new Course{CourseID=3141, Title="Trigonometry", Credits=4, Capacity=30, Description="Trigonometric functions", DepartmentID=mathDept.DepartmentID},
                new Course{CourseID=2021, Title="Composition", Credits=3, Capacity=20, Description="Effective writing fundamentals", DepartmentID=englishDept.DepartmentID},
                new Course{CourseID=2042, Title="Literature", Credits=4, Capacity=20, Description="Survey of major literary works", DepartmentID=englishDept.DepartmentID},
                new Course{CourseID=5010, Title="Computer Science", Credits=4, Capacity=40, Description="Intro to programming", DepartmentID=engineeringDept.DepartmentID},
                new Course{CourseID=5020, Title="Database Systems", Credits=3, Capacity=25, Description="Database system design", DepartmentID=engineeringDept.DepartmentID}
            };
            courses.ForEach(c => context.Courses.AddOrUpdate(p => p.CourseID, c));
            context.SaveChanges();

            // -----------------------------
            // 6. OfficeAssignments
            // -----------------------------
            var abercrombie = instructors.First(i => i.LastName == "Abercrombie");
            var fakhouri = instructors.First(i => i.LastName == "Fakhouri");
            var harui = instructors.First(i => i.LastName == "Harui");
            var kapoor = instructors.First(i => i.LastName == "Kapoor");
            var zheng = instructors.First(i => i.LastName == "Zheng");

            var offices = new List<OfficeAssignment>
            {
                new OfficeAssignment{InstructorID=abercrombie.ID, Location="Smith 17"},
                new OfficeAssignment{InstructorID=fakhouri.ID, Location="Gowan 27"},
                new OfficeAssignment{InstructorID=harui.ID, Location="Thompson 304"},
                new OfficeAssignment{InstructorID=kapoor.ID, Location="Math Tower 101"},
                new OfficeAssignment{InstructorID=zheng.ID, Location="Science Hall 205"}
            };
            offices.ForEach(o => context.OfficeAssignments.AddOrUpdate(p => p.InstructorID, o));
            context.SaveChanges();

            // -----------------------------
            // 7. Instructor-Course assignments
            // -----------------------------

            // First ensure all courses have their Instructors collection initialized
            foreach (var course in courses)
            {
                if (course.Instructors == null)
                {
                    course.Instructors = new List<Instructor>();
                }
            }

            // Create assignment list using anonymous objects instead of tuples
            var courseAssignments = new[]
            {
                new { CourseTitle = "Chemistry", InstructorLastName = "Kapoor" },
                new { CourseTitle = "Chemistry", InstructorLastName = "Harui" },
                new { CourseTitle = "Microeconomics", InstructorLastName = "Zheng" },
                new { CourseTitle = "Macroeconomics", InstructorLastName = "Zheng" },
                new { CourseTitle = "Calculus", InstructorLastName = "Fakhouri" },
                new { CourseTitle = "Trigonometry", InstructorLastName = "Harui" },
                new { CourseTitle = "Composition", InstructorLastName = "Abercrombie" },
                new { CourseTitle = "Literature", InstructorLastName = "Abercrombie" },
                new { CourseTitle = "Computer Science", InstructorLastName = "Abercrombie" },
                new { CourseTitle = "Database Systems", InstructorLastName = "Abercrombie" },
                new { CourseTitle = "Computer Science", InstructorLastName = "Instructor" }
            };

            foreach (var assignment in courseAssignments)
            {
                var course = courses.First(c => c.Title == assignment.CourseTitle);
                var instructor = instructors.First(i => i.LastName == assignment.InstructorLastName);

                if (!course.Instructors.Any(i => i.ID == instructor.ID))
                {
                    course.Instructors.Add(instructor);
                }
            }

            context.SaveChanges();

            // -----------------------------
            // 8. Enroll students
            // -----------------------------
            var alexander = students.First(s => s.LastName == "Alexander");
            var alonso = students.First(s => s.LastName == "Alonso");
            var anand = students.First(s => s.LastName == "Anand");
            var barzdukas = students.First(s => s.LastName == "Barzdukas");
            var li = students.First(s => s.LastName == "Li");
            var justice = students.First(s => s.LastName == "Justice");
            var olivetto = students.First(s => s.LastName == "Olivetto");
            var norman = students.First(s => s.LastName == "Norman");
            var testStudent = students.First(s => s.LastName == "Student");

            var chemistryCourse = courses.First(c => c.Title == "Chemistry");
            var microeconomicsCourse = courses.First(c => c.Title == "Microeconomics");
            var macroeconomicsCourse = courses.First(c => c.Title == "Macroeconomics");
            var calculusCourse = courses.First(c => c.Title == "Calculus");
            var trigonometryCourse = courses.First(c => c.Title == "Trigonometry");
            var compositionCourse = courses.First(c => c.Title == "Composition");
            var literatureCourse = courses.First(c => c.Title == "Literature");
            var computerScienceCourse = courses.First(c => c.Title == "Computer Science");
            var databaseSystemsCourse = courses.First(c => c.Title == "Database Systems");

            var enrollments = new List<Enrollment>
            {
                new Enrollment{StudentID=alexander.ID, CourseID=chemistryCourse.CourseID, Grade=Grade.A},
                new Enrollment{StudentID=alexander.ID, CourseID=microeconomicsCourse.CourseID, Grade=Grade.C},
                new Enrollment{StudentID=alexander.ID, CourseID=macroeconomicsCourse.CourseID, Grade=Grade.B},
                new Enrollment{StudentID=alonso.ID, CourseID=calculusCourse.CourseID, Grade=Grade.B},
                new Enrollment{StudentID=alonso.ID, CourseID=trigonometryCourse.CourseID, Grade=Grade.B},
                new Enrollment{StudentID=alonso.ID, CourseID=compositionCourse.CourseID, Grade=Grade.B},
                new Enrollment{StudentID=anand.ID, CourseID=chemistryCourse.CourseID},
                new Enrollment{StudentID=anand.ID, CourseID=microeconomicsCourse.CourseID, Grade=Grade.B},
                new Enrollment{StudentID=barzdukas.ID, CourseID=chemistryCourse.CourseID, Grade=Grade.B},
                new Enrollment{StudentID=li.ID, CourseID=compositionCourse.CourseID, Grade=Grade.B},
                new Enrollment{StudentID=justice.ID, CourseID=literatureCourse.CourseID, Grade=Grade.B},
                new Enrollment{StudentID=alexander.ID, CourseID=computerScienceCourse.CourseID, Grade=Grade.A},
                new Enrollment{StudentID=olivetto.ID, CourseID=computerScienceCourse.CourseID, Grade=Grade.B},
                new Enrollment{StudentID=anand.ID, CourseID=databaseSystemsCourse.CourseID},
                new Enrollment{StudentID=li.ID, CourseID=literatureCourse.CourseID, Grade=Grade.B},
                new Enrollment{StudentID=norman.ID, CourseID=compositionCourse.CourseID, Grade=Grade.C},
                new Enrollment{StudentID=testStudent.ID, CourseID=computerScienceCourse.CourseID, Grade=Grade.B}
            };

            foreach (var e in enrollments)
            {
                if (!context.Enrollments.Any(en => en.StudentID == e.StudentID && en.CourseID == e.CourseID))
                    context.Enrollments.Add(e);
            }
            context.SaveChanges();
        }
    }
}