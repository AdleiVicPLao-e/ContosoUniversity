using ContosoUniversity.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.ViewModels
{
    public class InstructorDashboardViewModel
    {
        public Instructor Instructor { get; set; }

        [Display(Name = "Total Students")]
        public int TotalStudents { get; set; }

        [Display(Name = "Active Courses")]
        public int ActiveCourses { get; set; }

        [Display(Name = "Upcoming Deadlines")]
        public List<string> UpcomingDeadlines { get; set; }

        [Display(Name = "Recent Enrollments")]
        public List<Enrollment> RecentEnrollments { get; set; }

        [Display(Name = "Courses Needing Grades")]
        public int CoursesNeedingGrades { get; set; }

        public InstructorDashboardViewModel()
        {
            UpcomingDeadlines = new List<string>();
            RecentEnrollments = new List<Enrollment>();
        }

        [Display(Name = "Average Students Per Course")]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        public decimal AverageStudentsPerCourse => ActiveCourses > 0 ? (decimal)TotalStudents / ActiveCourses : 0;
    }
}