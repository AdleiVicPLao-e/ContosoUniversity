using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Models
{
    public class Instructor : Person
    {
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Hire Date")]
        public DateTime HireDate { get; set; }

        // Instructor-specific properties
        [StringLength(50)]
        public string Specialization { get; set; }

        [DataType(DataType.Currency)]
        [Column(TypeName = "money")]
        public decimal Salary { get; set; }

        public virtual ICollection<Course> Courses { get; set; }
        public virtual OfficeAssignment OfficeAssignment { get; set; }

        // Constructor to automatically add Instructor role
        public Instructor()
        {
            AddRole(UserRole.Instructor);
        }
    }
}