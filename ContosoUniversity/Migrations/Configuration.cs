namespace ContosoUniversity.Migrations
{
    using ContosoUniversity.Models;
    using ContosoUniversity.DAL;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using static ContosoUniversity.Models.Person;

    internal sealed class Configuration : DbMigrationsConfiguration<SchoolContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(SchoolContext context)
        {
            // Create administrators first
            var administrators = new List<Administrator>
            {
                new Administrator {
                    FirstMidName = "Admin", LastName = "User",
                    UserName = "admin", Password = "password123",
                    AdministratorSince = DateTime.Parse("2010-01-01"),
                    AdministrativeLevel = "SuperAdmin",
                    CanManageUsers = true, CanManageSystem = true,
                    CanViewReports = true, CanManageAllDepartments = true
                },
                new Administrator {
                    FirstMidName = "Department", LastName = "Head",
                    UserName = "deptadmin", Password = "password123",
                    AdministratorSince = DateTime.Parse("2015-06-01"),
                    AdministrativeLevel = "DepartmentAdmin",
                    CanManageUsers = false, CanManageSystem = false,
                    CanViewReports = true, CanManageAllDepartments = false
                }
            };
            administrators.ForEach(a => context.Administrators.AddOrUpdate(p => p.UserName, a));
            context.SaveChanges();

            // Create instructors with enhanced properties
            var instructors = new List<Instructor>
            {
                new Instructor {
                    FirstMidName = "Kim", LastName = "Abercrombie", UserName = "kim.abercrombie", Password = "password123",
                    HireDate = DateTime.Parse("1995-03-11"), Specialization = "Computer Science", Salary = 75000m
                },
                new Instructor {
                    FirstMidName = "Fadi", LastName = "Fakhouri", UserName = "fadi.fakhouri", Password = "password123",
                    HireDate = DateTime.Parse("2002-07-06"), Specialization = "Software Engineering", Salary = 80000m
                },
                new Instructor {
                    FirstMidName = "Roger", LastName = "Harui", UserName = "roger.harui", Password = "password123",
                    HireDate = DateTime.Parse("1998-07-01"), Specialization = "Economics", Salary = 70000m
                },
                new Instructor {
                    FirstMidName = "Candace", LastName = "Kapoor", UserName = "candace.kapoor", Password = "password123",
                    HireDate = DateTime.Parse("2001-01-15"), Specialization = "Mathematics", Salary = 72000m
                },
                new Instructor {
                    FirstMidName = "Roger", LastName = "Zheng", UserName = "roger.zheng", Password = "password123",
                    HireDate = DateTime.Parse("2004-02-12"), Specialization = "Chemistry", Salary = 68000m
                },
                new Instructor {
                    FirstMidName = "Test", LastName = "Instructor", UserName = "instructor", Password = "password123",
                    HireDate = DateTime.Parse("2010-01-01"), Specialization = "Computer Science", Salary = 65000m
                }
            };
            instructors.ForEach(i => context.Instructors.AddOrUpdate(p => p.UserName, i));
            context.SaveChanges();

            // Create students with enhanced properties
            var students = new List<Student>
            {
                new Student {
                    FirstMidName = "Carson", LastName = "Alexander", UserName = "carson.alexander", Password = "password123",
                    EnrollmentDate = DateTime.Parse("2010-09-01"), StudentCode = "S10001"
                },
                new Student {
                    FirstMidName = "Meredith", LastName = "Alonso", UserName = "meredith.alonso", Password = "password123",
                    EnrollmentDate = DateTime.Parse("2012-09-01"), StudentCode = "S10002"
                },
                new Student {
                    FirstMidName = "Arturo", LastName = "Anand", UserName = "arturo.anand", Password = "password123",
                    EnrollmentDate = DateTime.Parse("2013-09-01"), StudentCode = "S10003"
                },
                new Student {
                    FirstMidName = "Gytis", LastName = "Barzdukas", UserName = "gytis.barzdukas", Password = "password123",
                    EnrollmentDate = DateTime.Parse("2012-09-01"), StudentCode = "S10004"
                },
                new Student {
                    FirstMidName = "Yan", LastName = "Li", UserName = "yan.li", Password = "password123",
                    EnrollmentDate = DateTime.Parse("2012-09-01"), StudentCode = "S10005"
                },
                new Student {
                    FirstMidName = "Peggy", LastName = "Justice", UserName = "peggy.justice", Password = "password123",
                    EnrollmentDate = DateTime.Parse("2011-09-01"), StudentCode = "S10006"
                },
                new Student {
                    FirstMidName = "Laura", LastName = "Norman", UserName = "laura.norman", Password = "password123",
                    EnrollmentDate = DateTime.Parse("2013-09-01"), StudentCode = "S10007"
                },
                new Student {
                    FirstMidName = "Nino", LastName = "Olivetto", UserName = "nino.olivetto", Password = "password123",
                    EnrollmentDate = DateTime.Parse("2005-09-01"), StudentCode = "S10008"
                },
                new Student {
                    FirstMidName = "Test", LastName = "Student", UserName = "student", Password = "password123",
                    EnrollmentDate = DateTime.Parse("2023-09-01"), StudentCode = "S99999"
                }
            };
            students.ForEach(s => context.Students.AddOrUpdate(p => p.UserName, s));
            context.SaveChanges();

            // Create departments
            var departments = new List<Department>
            {
                new Department {
                    Name = "English", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID = instructors.Single(i => i.LastName == "Abercrombie").ID
                },
                new Department {
                    Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID = instructors.Single(i => i.LastName == "Fakhouri").ID
                },
                new Department {
                    Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID = instructors.Single(i => i.LastName == "Harui").ID
                },
                new Department {
                    Name = "Economics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"),
                    InstructorID = instructors.Single(i => i.LastName == "Kapoor").ID
                },
                new Department {
                    Name = "Chemistry", Budget = 180000, StartDate = DateTime.Parse("2008-09-01"),
                    InstructorID = instructors.Single(i => i.LastName == "Zheng").ID
                }
            };
            departments.ForEach(d => context.Departments.AddOrUpdate(p => p.Name, d));
            context.SaveChanges();

            // Create courses with enhanced properties
            var courses = new List<Course>
            {
                new Course {
                    CourseID = 1050, Title = "Chemistry", Credits = 3, Capacity = 30, IsActive = true,
                    Description = "Introduction to chemical principles and laboratory techniques",
                    DepartmentID = departments.Single(s => s.Name == "Chemistry").DepartmentID,
                    Instructors = new List<Instructor>()
                },
                new Course {
                    CourseID = 4022, Title = "Microeconomics", Credits = 3, Capacity = 25, IsActive = true,
                    Description = "Study of individual economic behavior and market structures",
                    DepartmentID = departments.Single(s => s.Name == "Economics").DepartmentID,
                    Instructors = new List<Instructor>()
                },
                new Course {
                    CourseID = 4041, Title = "Macroeconomics", Credits = 3, Capacity = 25, IsActive = true,
                    Description = "Analysis of aggregate economic activity and policy",
                    DepartmentID = departments.Single(s => s.Name == "Economics").DepartmentID,
                    Instructors = new List<Instructor>()
                },
                new Course {
                    CourseID = 1045, Title = "Calculus", Credits = 4, Capacity = 35, IsActive = true,
                    Description = "Differential and integral calculus with applications",
                    DepartmentID = departments.Single(s => s.Name == "Mathematics").DepartmentID,
                    Instructors = new List<Instructor>()
                },
                new Course {
                    CourseID = 3141, Title = "Trigonometry", Credits = 4, Capacity = 30, IsActive = true,
                    Description = "Trigonometric functions and their applications",
                    DepartmentID = departments.Single(s => s.Name == "Mathematics").DepartmentID,
                    Instructors = new List<Instructor>()
                },
                new Course {
                    CourseID = 2021, Title = "Composition", Credits = 3, Capacity = 20, IsActive = true,
                    Description = "Fundamentals of effective writing and communication",
                    DepartmentID = departments.Single(s => s.Name == "English").DepartmentID,
                    Instructors = new List<Instructor>()
                },
                new Course {
                    CourseID = 2042, Title = "Literature", Credits = 4, Capacity = 20, IsActive = true,
                    Description = "Survey of major literary works and critical analysis",
                    DepartmentID = departments.Single(s => s.Name == "English").DepartmentID,
                    Instructors = new List<Instructor>()
                },
                new Course {
                    CourseID = 5010, Title = "Computer Science", Credits = 4, Capacity = 40, IsActive = true,
                    Description = "Introduction to programming and algorithms",
                    DepartmentID = departments.Single(s => s.Name == "Engineering").DepartmentID,
                    Instructors = new List<Instructor>()
                },
                new Course {
                    CourseID = 5020, Title = "Database Systems", Credits = 3, Capacity = 25, IsActive = true,
                    Description = "Design and implementation of database systems",
                    DepartmentID = departments.Single(s => s.Name == "Engineering").DepartmentID,
                    Instructors = new List<Instructor>()
                }
            };
            courses.ForEach(c => context.Courses.AddOrUpdate(p => p.CourseID, c));
            context.SaveChanges();

            // Create office assignments
            var officeAssignments = new List<OfficeAssignment>
            {
                new OfficeAssignment {
                    InstructorID = instructors.Single(i => i.LastName == "Abercrombie").ID,
                    Location = "Smith 17"
                },
                new OfficeAssignment {
                    InstructorID = instructors.Single(i => i.LastName == "Fakhouri").ID,
                    Location = "Gowan 27"
                },
                new OfficeAssignment {
                    InstructorID = instructors.Single(i => i.LastName == "Harui").ID,
                    Location = "Thompson 304"
                },
                new OfficeAssignment {
                    InstructorID = instructors.Single(i => i.LastName == "Kapoor").ID,
                    Location = "Math Tower 101"
                },
                new OfficeAssignment {
                    InstructorID = instructors.Single(i => i.LastName == "Zheng").ID,
                    Location = "Science Hall 205"
                }
            };
            officeAssignments.ForEach(o => context.OfficeAssignments.AddOrUpdate(p => p.InstructorID, o));
            context.SaveChanges();

            // Assign instructors to courses using updated method
            AddOrUpdateInstructor(context, "Chemistry", "Kapoor");
            AddOrUpdateInstructor(context, "Chemistry", "Harui");
            AddOrUpdateInstructor(context, "Microeconomics", "Zheng");
            AddOrUpdateInstructor(context, "Macroeconomics", "Zheng");
            AddOrUpdateInstructor(context, "Calculus", "Fakhouri");
            AddOrUpdateInstructor(context, "Trigonometry", "Harui");
            AddOrUpdateInstructor(context, "Composition", "Abercrombie");
            AddOrUpdateInstructor(context, "Literature", "Abercrombie");
            AddOrUpdateInstructor(context, "Computer Science", "Abercrombie");
            AddOrUpdateInstructor(context, "Database Systems", "Abercrombie");
            AddOrUpdateInstructor(context, "Computer Science", "Test Instructor");

            context.SaveChanges();

            // Create enrollments
            var enrollments = new List<Enrollment>
            {
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Alexander").ID,
                    CourseID = courses.Single(c => c.Title == "Chemistry").CourseID,
                    Grade = Grade.A
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Alexander").ID,
                    CourseID = courses.Single(c => c.Title == "Microeconomics").CourseID,
                    Grade = Grade.C
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Alexander").ID,
                    CourseID = courses.Single(c => c.Title == "Macroeconomics").CourseID,
                    Grade = Grade.B
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Alonso").ID,
                    CourseID = courses.Single(c => c.Title == "Calculus").CourseID,
                    Grade = Grade.B
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Alonso").ID,
                    CourseID = courses.Single(c => c.Title == "Trigonometry").CourseID,
                    Grade = Grade.B
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Alonso").ID,
                    CourseID = courses.Single(c => c.Title == "Composition").CourseID,
                    Grade = Grade.B
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Anand").ID,
                    CourseID = courses.Single(c => c.Title == "Chemistry").CourseID
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Anand").ID,
                    CourseID = courses.Single(c => c.Title == "Microeconomics").CourseID,
                    Grade = Grade.B
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Barzdukas").ID,
                    CourseID = courses.Single(c => c.Title == "Chemistry").CourseID,
                    Grade = Grade.B
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Li").ID,
                    CourseID = courses.Single(c => c.Title == "Composition").CourseID,
                    Grade = Grade.B
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Justice").ID,
                    CourseID = courses.Single(c => c.Title == "Literature").CourseID,
                    Grade = Grade.B
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Alexander").ID,
                    CourseID = courses.Single(c => c.Title == "Computer Science").CourseID,
                    Grade = Grade.A
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Olivetto").ID,
                    CourseID = courses.Single(c => c.Title == "Computer Science").CourseID,
                    Grade = Grade.B
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Anand").ID,
                    CourseID = courses.Single(c => c.Title == "Database Systems").CourseID
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Li").ID,
                    CourseID = courses.Single(c => c.Title == "Literature").CourseID,
                    Grade = Grade.B
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Norman").ID,
                    CourseID = courses.Single(c => c.Title == "Composition").CourseID,
                    Grade = Grade.C
                },
                new Enrollment {
                    StudentID = students.Single(s => s.LastName == "Test Student").ID,
                    CourseID = courses.Single(c => c.Title == "Computer Science").CourseID,
                    Grade = Grade.B
                }
            };

            foreach (Enrollment e in enrollments)
            {
                var enrollmentInDataBase = context.Enrollments.Where(
                    s => s.StudentID == e.StudentID && s.CourseID == e.CourseID).SingleOrDefault();
                if (enrollmentInDataBase == null)
                {
                    context.Enrollments.Add(e);
                }
            }
            context.SaveChanges();
        }

        void AddOrUpdateInstructor(SchoolContext context, string courseTitle, string instructorName)
        {
            var crs = context.Courses.SingleOrDefault(c => c.Title == courseTitle);
            var inst = crs.Instructors.SingleOrDefault(i => i.LastName == instructorName);
            if (inst == null)
                crs.Instructors.Add(context.Instructors.Single(i => i.LastName == instructorName));
        }
    }
}