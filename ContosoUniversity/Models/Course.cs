using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Models
{
    public class Course
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "Number")]
        public int CourseID { get; set; }

        [StringLength(50, MinimumLength = 3)]
        public string Title { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Range(0, 5)]
        public int Credits { get; set; }

        [Range(1, 100)]
        [Display(Name = "Capacity")]
        public int Capacity { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public int DepartmentID { get; set; }

        public virtual Department Department { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; }
        public virtual ICollection<Instructor> Instructors { get; set; }

        // Calculated properties
        [NotMapped]
        [Display(Name = "Enrolled Students")]
        public int EnrolledStudents => Enrollments?.Count ?? 0;

        [NotMapped]
        [Display(Name = "Available Spots")]
        public int AvailableSpots => Capacity - EnrolledStudents;

        [NotMapped]
        [Display(Name = "Fill Rate")]
        [DisplayFormat(DataFormatString = "{0:P0}")]
        public decimal FillRate => Capacity > 0 ? (decimal)EnrolledStudents / Capacity : 0;

        [NotMapped]
        [Display(Name = "Status")]
        public string Status
        {
            get
            {
                if (!IsActive) return "Inactive";
                if (FillRate >= 0.95m) return "Full";
                if (FillRate >= 0.5m) return "Open";
                return "Low Enrollment";
            }
        }
    }
}