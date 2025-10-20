using ContosoUniversity.Models;
using System.Collections.Generic;

public class AssignCoursesViewModel
{
    public Instructor Instructor { get; set; }
    public List<AssignedCourseData> Courses { get; set; }
}

public class AssignedCourseData
{
    public int CourseID { get; set; }
    public string Title { get; set; }
    public string Department { get; set; }
    public bool Assigned { get; set; }
}