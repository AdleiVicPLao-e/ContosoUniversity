using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using ContosoUniversity.Models;
using static ContosoUniversity.Models.Person;

namespace ContosoUniversity.DAL
{
    public class SchoolInitializer : System.Data.Entity.DropCreateDatabaseIfModelChanges<SchoolContext>
    {
        protected override void Seed(SchoolContext context)
        {
            // Create departments first
            var departments = new List<Department>
            {
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01") },
                new Department { Name = "English", Budget = 120000, StartDate = DateTime.Parse("2007-09-01") },
                new Department { Name = "Economics", Budget = 200000, StartDate = DateTime.Parse("2007-09-01") },
                new Department { Name = "Mathematics", Budget = 250000, StartDate = DateTime.Parse("2007-09-01") },
                new Department { Name = "Chemistry", Budget = 180000, StartDate = DateTime.Parse("2008-09-01") }
            };
            departments.ForEach(d => context.Departments.Add(d));
            context.SaveChanges();

            // Create instructors
            var instructors = new List<Instructor>
            {
                new Instructor { FirstMidName = "Kim", LastName = "Abercrombie",
                    HireDate = DateTime.Parse("1995-03-11"), UserName = "kim.abercrombie", Password = "password123",
                    Specialization = "Computer Science", Salary = 75000m },
                new Instructor { FirstMidName = "Fadi", LastName = "Fakhouri",
                    HireDate = DateTime.Parse("2002-07-06"), UserName = "fadi.fakhouri", Password = "password123",
                    Specialization = "Software Engineering", Salary = 80000m },
                new Instructor { FirstMidName = "Roger", LastName = "Harui",
                    HireDate = DateTime.Parse("1998-07-01"), UserName = "roger.harui", Password = "password123",
                    Specialization = "Economics", Salary = 70000m },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",
                    HireDate = DateTime.Parse("2001-01-15"), UserName = "candace.kapoor", Password = "password123",
                    Specialization = "Mathematics", Salary = 72000m },
                new Instructor { FirstMidName = "Roger", LastName = "Zheng",
                    HireDate = DateTime.Parse("2004-02-12"), UserName = "roger.zheng", Password = "password123",
                    Specialization = "Chemistry", Salary = 68000m }
            };
            instructors.ForEach(i => context.Instructors.Add(i));
            context.SaveChanges();

            // Create administrators
            var administrators = new List<Administrator>
            {
                new Administrator { FirstMidName = "Admin", LastName = "User",
                    UserName = "admin", Password = "password123",
                    AdministratorSince = DateTime.Parse("2010-01-01"),
                    AdministrativeLevel = "SuperAdmin",
                    CanManageUsers = true, CanManageSystem = true,
                    CanViewReports = true, CanManageAllDepartments = true },
                new Administrator { FirstMidName = "Department", LastName = "Head",
                    UserName = "deptadmin", Password = "password123",
                    AdministratorSince = DateTime.Parse("2015-06-01"),
                    AdministrativeLevel = "DepartmentAdmin",
                    CanManageUsers = false, CanManageSystem = false,
                    CanViewReports = true, CanManageAllDepartments = false }
            };
            administrators.ForEach(a => context.Administrators.Add(a));
            context.SaveChanges();

            // Assign department administrators
            departments[0].InstructorID = instructors[0].ID; // Engineering -> Kim
            departments[1].InstructorID = instructors[1].ID; // English -> Fadi  
            departments[2].InstructorID = instructors[2].ID; // Economics -> Roger H
            departments[3].InstructorID = instructors[3].ID; // Mathematics -> Candace
            departments[4].InstructorID = instructors[4].ID; // Chemistry -> Roger Z
            context.SaveChanges();

            // Create students with usernames and passwords
            var students = new List<Student>
            {
                new Student{FirstMidName="Carson", LastName="Alexander", UserName="carson.alexander", Password="password123",
                    EnrollmentDate=DateTime.Parse("2005-09-01"), StudentCode="S10001"},
                new Student{FirstMidName="Meredith", LastName="Alonso", UserName="meredith.alonso", Password="password123",
                    EnrollmentDate=DateTime.Parse("2002-09-01"), StudentCode="S10002"},
                new Student{FirstMidName="Arturo", LastName="Anand", UserName="arturo.anand", Password="password123",
                    EnrollmentDate=DateTime.Parse("2003-09-01"), StudentCode="S10003"},
                new Student{FirstMidName="Gytis", LastName="Barzdukas", UserName="gytis.barzdukas", Password="password123",
                    EnrollmentDate=DateTime.Parse("2002-09-01"), StudentCode="S10004"},
                new Student{FirstMidName="Yan", LastName="Li", UserName="yan.li", Password="password123",
                    EnrollmentDate=DateTime.Parse("2002-09-01"), StudentCode="S10005"},
                new Student{FirstMidName="Peggy", LastName="Justice", UserName="peggy.justice", Password="password123",
                    EnrollmentDate=DateTime.Parse("2001-09-01"), StudentCode="S10006"},
                new Student{FirstMidName="Laura", LastName="Norman", UserName="laura.norman", Password="password123",
                    EnrollmentDate=DateTime.Parse("2003-09-01"), StudentCode="S10007"},
                new Student{FirstMidName="Nino", LastName="Olivetto", UserName="nino.olivetto", Password="password123",
                    EnrollmentDate=DateTime.Parse("2005-09-01"), StudentCode="S10008"}
            };
            students.ForEach(s => context.Students.Add(s));
            context.SaveChanges();

            // Create courses with enhanced properties
            var courses = new List<Course>
            {
                new Course{CourseID=1050, Title="Chemistry", Credits=3, Capacity=30, IsActive=true,
                    Description="Introduction to chemical principles and laboratory techniques", DepartmentID=departments[4].DepartmentID},
                new Course{CourseID=4022, Title="Microeconomics", Credits=3, Capacity=25, IsActive=true,
                    Description="Study of individual economic behavior and market structures", DepartmentID=departments[2].DepartmentID},
                new Course{CourseID=4041, Title="Macroeconomics", Credits=3, Capacity=25, IsActive=true,
                    Description="Analysis of aggregate economic activity and policy", DepartmentID=departments[2].DepartmentID},
                new Course{CourseID=1045, Title="Calculus", Credits=4, Capacity=35, IsActive=true,
                    Description="Differential and integral calculus with applications", DepartmentID=departments[3].DepartmentID},
                new Course{CourseID=3141, Title="Trigonometry", Credits=4, Capacity=30, IsActive=true,
                    Description="Trigonometric functions and their applications", DepartmentID=departments[3].DepartmentID},
                new Course{CourseID=2021, Title="Composition", Credits=3, Capacity=20, IsActive=true,
                    Description="Fundamentals of effective writing and communication", DepartmentID=departments[1].DepartmentID},
                new Course{CourseID=2042, Title="Literature", Credits=4, Capacity=20, IsActive=true,
                    Description="Survey of major literary works and critical analysis", DepartmentID=departments[1].DepartmentID},
                new Course{CourseID=5010, Title="Computer Science", Credits=4, Capacity=40, IsActive=true,
                    Description="Introduction to programming and algorithms", DepartmentID=departments[0].DepartmentID},
                new Course{CourseID=5020, Title="Database Systems", Credits=3, Capacity=25, IsActive=true,
                    Description="Design and implementation of database systems", DepartmentID=departments[0].DepartmentID}
            };
            courses.ForEach(c => context.Courses.Add(c));
            context.SaveChanges();

            // Create office assignments
            var officeAssignments = new List<OfficeAssignment>
            {
                new OfficeAssignment { InstructorID = instructors[0].ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = instructors[1].ID, Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = instructors[2].ID, Location = "Thompson 304" },
                new OfficeAssignment { InstructorID = instructors[3].ID, Location = "Math Tower 101" },
                new OfficeAssignment { InstructorID = instructors[4].ID, Location = "Science Hall 205" }
            };
            officeAssignments.ForEach(o => context.OfficeAssignments.Add(o));
            context.SaveChanges();

            // Assign instructors to courses
            instructors[0].Courses.Add(courses[7]); // Kim -> Computer Science
            instructors[0].Courses.Add(courses[8]); // Kim -> Database Systems
            instructors[1].Courses.Add(courses[5]); // Fadi -> Composition
            instructors[1].Courses.Add(courses[6]); // Fadi -> Literature
            instructors[2].Courses.Add(courses[1]); // Roger H -> Microeconomics
            instructors[2].Courses.Add(courses[2]); // Roger H -> Macroeconomics
            instructors[3].Courses.Add(courses[3]); // Candace -> Calculus
            instructors[3].Courses.Add(courses[4]); // Candace -> Trigonometry
            instructors[4].Courses.Add(courses[0]); // Roger Z -> Chemistry
            context.SaveChanges();

            // Create enrollments
            var enrollments = new List<Enrollment>
            {
                new Enrollment{StudentID=1, CourseID=1050, Grade=Grade.A},
                new Enrollment{StudentID=1, CourseID=4022, Grade=Grade.C},
                new Enrollment{StudentID=1, CourseID=4041, Grade=Grade.B},
                new Enrollment{StudentID=2, CourseID=1045, Grade=Grade.B},
                new Enrollment{StudentID=2, CourseID=3141, Grade=Grade.F},
                new Enrollment{StudentID=2, CourseID=2021, Grade=Grade.F},
                new Enrollment{StudentID=3, CourseID=1050},
                new Enrollment{StudentID=4, CourseID=1050},
                new Enrollment{StudentID=4, CourseID=4022, Grade=Grade.F},
                new Enrollment{StudentID=5, CourseID=4041, Grade=Grade.C},
                new Enrollment{StudentID=6, CourseID=1045},
                new Enrollment{StudentID=7, CourseID=3141, Grade=Grade.A},
                new Enrollment{StudentID=8, CourseID=5010, Grade=Grade.B},
                new Enrollment{StudentID=1, CourseID=5010, Grade=Grade.A},
                new Enrollment{StudentID=3, CourseID=5020},
                new Enrollment{StudentID=5, CourseID=2042, Grade=Grade.B},
                new Enrollment{StudentID=7, CourseID=2021, Grade=Grade.C}
            };
            enrollments.ForEach(e => context.Enrollments.Add(e));
            context.SaveChanges();

            // Create additional test users for each role
            var testInstructor = new Instructor
            {
                FirstMidName = "Test",
                LastName = "Instructor",
                UserName = "instructor",
                Password = "password123",
                HireDate = DateTime.Parse("2010-01-01"),
                Specialization = "Computer Science",
                Salary = 65000m
            };
            context.Instructors.Add(testInstructor);
            context.SaveChanges();

            var testStudent = new Student
            {
                FirstMidName = "Test",
                LastName = "Student",
                UserName = "student",
                Password = "password123",
                EnrollmentDate = DateTime.Parse("2023-09-01"),
                StudentCode = "S99999"
            };
            context.Students.Add(testStudent);
            context.SaveChanges();

            // Assign test instructor to a course
            testInstructor.Courses.Add(courses[7]); // Computer Science
            context.SaveChanges();

            // Create enrollment for test student
            var testEnrollment = new Enrollment
            {
                StudentID = testStudent.ID,
                CourseID = courses[7].CourseID,
                Grade = Grade.B
            };
            context.Enrollments.Add(testEnrollment);
            context.SaveChanges();
        }
    }
}