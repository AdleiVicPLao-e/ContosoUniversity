using ContosoUniversity.Models;
using System.Collections.Generic;

public class StudentTranscriptViewModel
{
    public string StudentName { get; set; }
    public string StudentID { get; set; }
    public List<Enrollment> Enrollments { get; set; }
    public decimal CumulativeGPA { get; set; }
    public int TotalCreditsEarned { get; set; }
}