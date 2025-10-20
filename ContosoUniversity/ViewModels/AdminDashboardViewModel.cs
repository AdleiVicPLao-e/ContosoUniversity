using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;

namespace ContosoUniversity.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Summary statistics
        [Display(Name = "Total Students")]
        public int TotalStudents { get; set; }

        [Display(Name = "Total Instructors")]
        public int TotalInstructors { get; set; }

        [Display(Name = "Total Courses")]
        public int TotalCourses { get; set; }

        [Display(Name = "Total Departments")]
        public int TotalDepartments { get; set; }

        [Display(Name = "New Enrollments (This Month)")]
        public int NewEnrollmentsThisMonth { get; set; }

        [Display(Name = "Active Courses")]
        public int ActiveCourses { get; set; }

        // Recent activities
        [Display(Name = "Recent Enrollments")]
        public List<Enrollment> RecentEnrollments { get; set; }

        [Display(Name = "Recently Added Courses")]
        public List<Course> RecentCourses { get; set; }

        [Display(Name = "System Alerts")]
        public List<SystemAlert> SystemAlerts { get; set; }

        // Department statistics
        [Display(Name = "Department Statistics")]
        public List<DepartmentStats> DepartmentStatistics { get; set; }

        // Course enrollment statistics (without grades)
        [Display(Name = "Most Popular Courses")]
        public List<CourseEnrollmentStats> PopularCourses { get; set; }

        // Instructor statistics
        [Display(Name = "Instructor Workload")]
        public List<InstructorStats> InstructorWorkload { get; set; }

        // Quick actions properties
        [Display(Name = "Quick Actions")]
        public bool ShowQuickActions { get; set; }

        // Constructor to initialize collections
        public AdminDashboardViewModel()
        {
            RecentEnrollments = new List<Enrollment>();
            RecentCourses = new List<Course>();
            SystemAlerts = new List<SystemAlert>();
            DepartmentStatistics = new List<DepartmentStats>();
            PopularCourses = new List<CourseEnrollmentStats>();
            InstructorWorkload = new List<InstructorStats>();
        }

        // Calculated properties for admin perspective
        [Display(Name = "Enrollment Growth")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal EnrollmentGrowthRate => TotalStudents > 0 ? (decimal)NewEnrollmentsThisMonth / TotalStudents : 0;

        [Display(Name = "Course Utilization")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal CourseUtilization => TotalCourses > 0 ? (decimal)ActiveCourses / TotalCourses : 0;

        [Display(Name = "System Health")]
        public string SystemHealth
        {
            get
            {
                if (SystemAlerts.Count == 0) return "Excellent";
                if (SystemAlerts.Count <= 2) return "Good";
                if (SystemAlerts.Count <= 5) return "Fair";
                return "Needs Attention";
            }
        }

        [Display(Name = "Average Courses Per Instructor")]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        public decimal AverageCoursesPerInstructor => TotalInstructors > 0 ? (decimal)TotalCourses / TotalInstructors : 0;
    }

    // Supporting view model classes
    public class SystemAlert
    {
        public int AlertID { get; set; }

        [Display(Name = "Alert Type")]
        public string AlertType { get; set; } // "Warning", "Info", "Error"

        [Display(Name = "Message")]
        public string Message { get; set; }

        [Display(Name = "Timestamp")]
        [DataType(DataType.DateTime)]
        public DateTime Timestamp { get; set; }

        [Display(Name = "Priority")]
        public string Priority { get; set; } // "High", "Medium", "Low"

        [Display(Name = "Resolved")]
        public bool IsResolved { get; set; }
    }

    public class DepartmentStats
    {
        [Display(Name = "Department")]
        public string DepartmentName { get; set; }

        [Display(Name = "Total Courses")]
        public int TotalCourses { get; set; }

        [Display(Name = "Total Students")]
        public int TotalStudents { get; set; }

        [Display(Name = "Total Instructors")]
        public int TotalInstructors { get; set; }

        [Display(Name = "Budget Utilization")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal BudgetUtilization { get; set; }

        [Display(Name = "Department Head")]
        public string DepartmentHead { get; set; }

        [Display(Name = "Student-to-Instructor Ratio")]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        public decimal StudentToInstructorRatio => TotalInstructors > 0 ? (decimal)TotalStudents / TotalInstructors : 0;
    }

    public class CourseEnrollmentStats
    {
        [Display(Name = "Course")]
        public string CourseTitle { get; set; }

        [Display(Name = "Course Code")]
        public string CourseCode { get; set; }

        [Display(Name = "Enrolled Students")]
        public int EnrolledStudents { get; set; }

        [Display(Name = "Capacity")]
        public int Capacity { get; set; }

        [Display(Name = "Instructor")]
        public string InstructorName { get; set; }

        [Display(Name = "Department")]
        public string DepartmentName { get; set; }

        [Display(Name = "Fill Rate")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal FillRate => Capacity > 0 ? (decimal)EnrolledStudents / Capacity : 0;

        [Display(Name = "Status")]
        public string Status => FillRate >= 0.9m ? "Full" : FillRate >= 0.5m ? "Healthy" : "Low Enrollment";
    }

    public class InstructorStats
    {
        [Display(Name = "Instructor")]
        public string InstructorName { get; set; }

        [Display(Name = "Department")]
        public string DepartmentName { get; set; }

        [Display(Name = "Courses Teaching")]
        public int CoursesTeaching { get; set; }

        [Display(Name = "Total Students")]
        public int TotalStudents { get; set; }

        [Display(Name = "Office Location")]
        public string OfficeLocation { get; set; }

        [Display(Name = "Hire Date")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        [Display(Name = "Workload Level")]
        public string WorkloadLevel => TotalStudents > 100 ? "High" : TotalStudents > 50 ? "Medium" : "Low";
    }

    // Additional view model for admin reports
    public class AdminReportViewModel
    {
        [Display(Name = "Report Type")]
        public string ReportType { get; set; } // "Enrollment", "Department", "Financial"

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Department Filter")]
        public int? DepartmentId { get; set; }

        [Display(Name = "Academic Term")]
        public string AcademicTerm { get; set; }
    }

    // View model for system configuration
    public class SystemConfigViewModel
    {
        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; }

        [Display(Name = "Current Term")]
        public string CurrentTerm { get; set; }

        [Display(Name = "Enrollment Period Open")]
        public bool EnrollmentPeriodOpen { get; set; }

        [Display(Name = "Max Courses Per Student")]
        public int MaxCoursesPerStudent { get; set; }

        [Display(Name = "Min Students Per Course")]
        public int MinStudentsPerCourse { get; set; }

        [Display(Name = "System Maintenance Mode")]
        public bool MaintenanceMode { get; set; }
    }
}