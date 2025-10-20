using System.Collections.Generic;
using ContosoUniversity.Models;

namespace ContosoUniversity.ViewModels
{
    public class InstructorIndexData
    {
        public IEnumerable<Instructor> Instructors { get; set; }
        public IEnumerable<Course> Courses { get; set; }
        public IEnumerable<Enrollment> Enrollments { get; set; }

        // For tracking selections
        public int? SelectedInstructorID { get; set; }
        public int? SelectedCourseID { get; set; }

        // Constructor to initialize collections
        public InstructorIndexData()
        {
            Instructors = new List<Instructor>();
            Courses = new List<Course>();
            Enrollments = new List<Enrollment>();
        }
    }
}