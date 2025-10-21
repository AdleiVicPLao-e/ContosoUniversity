using ContosoUniversity.Models;
using System.Collections.Generic;

namespace ContosoUniversity.ViewModels
{
    public class StudentProgressViewModel
    {
        public List<Enrollment> Enrollments { get; set; }
        public int TotalCreditsAttempted { get; set; }
        public int TotalCreditsEarned { get; set; }
        public decimal GPA { get; set; }
    }
}