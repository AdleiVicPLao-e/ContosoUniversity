using ContosoUniversity.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.ViewModels
{
    public class InstructorManagementViewModel
    {
        public InstructorIndexData InstructorData { get; set; }
        public AssignCoursesViewModel AssignCoursesModel { get; set; }

        // For creating new instructors
        public Instructor NewInstructor { get; set; }

        // For filtering
        public string SearchTerm { get; set; }
        public int? DepartmentFilter { get; set; }
    }

    public class AssignCoursesViewModel
    {
        public Instructor Instructor { get; set; }
        public List<AssignedCourseData> Courses { get; set; }

        public AssignCoursesViewModel()
        {
            Courses = new List<AssignedCourseData>();
        }
    }

    public class AssignedCourseData
    {
        public int CourseID { get; set; }
        public string Title { get; set; }
        public string Department { get; set; }
        public int EnrolledStudents { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
        public bool Assigned { get; set; }

        [Display(Name = "Status")]
        public string Status => !IsActive ? "Inactive" :
                              EnrolledStudents >= Capacity ? "Full" :
                              "Available";
    }

    public class InstructorStatsViewModel
    {
        public Instructor Instructor { get; set; }

        [Display(Name = "Total Courses")]
        public int TotalCourses { get; set; }

        [Display(Name = "Total Students")]
        public int TotalStudents { get; set; }

        [Display(Name = "Average Students Per Course")]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        public decimal AverageStudentsPerCourse { get; set; }

        [Display(Name = "Courses Needing Grades")]
        public int CoursesNeedingGrades { get; set; }

        [Display(Name = "Workload Level")]
        public string WorkloadLevel { get; set; }
    }
}