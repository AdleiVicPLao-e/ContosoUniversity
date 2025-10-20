using ContosoUniversity.Models;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace ContosoUniversity.DAL
{
    public class SchoolContext : DbContext
    {
        public SchoolContext() : base("SchoolContext")
        {
            // Configuration settings
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        // Main entity sets
        public DbSet<Person> People { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Administrator> Administrators { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<OfficeAssignment> OfficeAssignments { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // Configure TPH Inheritance for Person hierarchy
            modelBuilder.Entity<Person>()
                .Map<Student>(m => m.Requires("PersonType").HasValue("Student"))
                .Map<Instructor>(m => m.Requires("PersonType").HasValue("Instructor"))
                .Map<Administrator>(m => m.Requires("PersonType").HasValue("Administrator"));

            // Configure Course-Instructor many-to-many relationship
            modelBuilder.Entity<Course>()
                .HasMany(c => c.Instructors)
                .WithMany(i => i.Courses)
                .Map(t => t.MapLeftKey("CourseID")
                    .MapRightKey("InstructorID")
                    .ToTable("CourseInstructor"));

            // Configure OfficeAssignment one-to-one relationship
            modelBuilder.Entity<OfficeAssignment>()
                .HasRequired(o => o.Instructor)
                .WithOptional(i => i.OfficeAssignment);

            // Configure Department-Administrator relationship
            modelBuilder.Entity<Department>()
                .HasOptional(d => d.Administrator)
                .WithMany()
                .HasForeignKey(d => d.InstructorID);

            // Configure Course-Department relationship
            modelBuilder.Entity<Course>()
                .HasRequired(c => c.Department)
                .WithMany(d => d.Courses)
                .HasForeignKey(c => c.DepartmentID)
                .WillCascadeOnDelete(false);

            // Configure Enrollment relationships
            modelBuilder.Entity<Enrollment>()
                .HasRequired(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseID);

            modelBuilder.Entity<Enrollment>()
                .HasRequired(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentID);

            // Configure Person properties
            modelBuilder.Entity<Person>()
                .Property(p => p.UserName)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Person>()
                .Property(p => p.EncryptedPassword)
                .IsRequired()
                .HasColumnName("EncryptedPassword");

            modelBuilder.Entity<Person>()
                .Property(p => p.LastName)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Person>()
                .Property(p => p.FirstMidName)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("FirstName");

            modelBuilder.Entity<Person>()
                .Property(p => p.Roles)
                .IsRequired();

            // Configure Student properties
            modelBuilder.Entity<Student>()
                .Property(s => s.EnrollmentDate)
                .HasColumnType("date");

            modelBuilder.Entity<Student>()
                .Property(s => s.StudentCode)
                .HasMaxLength(20);

            // Configure Instructor properties
            modelBuilder.Entity<Instructor>()
                .Property(i => i.HireDate)
                .HasColumnType("date");

            modelBuilder.Entity<Instructor>()
                .Property(i => i.Specialization)
                .HasMaxLength(50);

            modelBuilder.Entity<Instructor>()
                .Property(i => i.Salary)
                .HasColumnType("money");

            // Configure Administrator properties
            modelBuilder.Entity<Administrator>()
                .Property(a => a.AdministratorSince)
                .HasColumnType("date");

            modelBuilder.Entity<Administrator>()
                .Property(a => a.AdministrativeLevel)
                .HasMaxLength(50);

            // Configure Course properties
            modelBuilder.Entity<Course>()
                .HasKey(c => c.CourseID)
                .Property(c => c.CourseID)
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            modelBuilder.Entity<Course>()
                .Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Course>()
                .Property(c => c.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<Course>()
                .Property(c => c.Credits)
                .IsRequired();

            modelBuilder.Entity<Course>()
                .Property(c => c.Capacity)
                .IsRequired();

            modelBuilder.Entity<Course>()
                .Property(c => c.IsActive)
                .IsRequired();

            // Configure Department properties
            modelBuilder.Entity<Department>()
                .Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Department>()
                .Property(d => d.Budget)
                .HasColumnType("money");

            modelBuilder.Entity<Department>()
                .Property(d => d.StartDate)
                .HasColumnType("date");

            modelBuilder.Entity<Department>()
                .Property(d => d.RowVersion)
                .IsRowVersion();

            // Configure OfficeAssignment properties
            modelBuilder.Entity<OfficeAssignment>()
                .Property(o => o.Location)
                .HasMaxLength(50);

            // Configure stored procedures for Department
            modelBuilder.Entity<Department>().MapToStoredProcedures();

            // Ignore calculated properties
            modelBuilder.Entity<Course>()
                .Ignore(c => c.EnrolledStudents)
                .Ignore(c => c.AvailableSpots)
                .Ignore(c => c.FillRate)
                .Ignore(c => c.Status);

            modelBuilder.Entity<Person>()
                .Ignore(p => p.IsStudent)
                .Ignore(p => p.IsInstructor)
                .Ignore(p => p.IsAdministrator)
                .Ignore(p => p.PrimaryRole)
                .Ignore(p => p.Password);

            base.OnModelCreating(modelBuilder);
        }
    }
}