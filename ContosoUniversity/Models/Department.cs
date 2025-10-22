using ContosoUniversity.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ContosoUniversity.Models
{
    public class Department
    {
        public int DepartmentID { get; set; }

        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; }

        [DataType(DataType.Currency)]
        [Column(TypeName = "money")]
        public decimal Budget { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        // Changed to AdministratorID
        public int? AdministratorID { get; set; }

        [ForeignKey("AdministratorID")]
        public virtual Administrator Administrator { get; set; }

        public virtual ICollection<Course> Courses { get; set; }

        private bool deleted;
        // 🧱 Soft delete flag — protected so only controller/service can modify
        [ScaffoldColumn(false)]
        public bool IsDeleted
        {
            get => deleted;
            set => deleted = value; // only controller or derived class can change
        }
    }
}