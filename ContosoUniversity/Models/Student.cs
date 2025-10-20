using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Models
{
    public class Student : Person
    {
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Enrollment Date")]
        public DateTime EnrollmentDate { get; set; }

        // Student-specific properties
        [StringLength(20)]
        [Display(Name = "Student ID")]
        public string StudentCode { get; set; }

        public virtual ICollection<Enrollment> Enrollments { get; set; }

        // Constructor to automatically add Student role
        public Student()
        {
            AddRole(UserRole.Student);
        }
    }
}