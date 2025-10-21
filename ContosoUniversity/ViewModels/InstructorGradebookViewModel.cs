using ContosoUniversity.Models;
using System.Collections.Generic;

public class InstructorGradebookViewModel
{
    public List<Course> Courses { get; set; }
    public Course SelectedCourse { get; set; }
    public List<Enrollment> Enrollments { get; set; }
}