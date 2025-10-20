using ContosoUniversity.Models;
using System;
using System.ComponentModel.DataAnnotations;
using static ContosoUniversity.Models.Person;

public class Administrator : Person
{
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Administrator Since")]
    public DateTime AdministratorSince { get; set; }

    [StringLength(50)]
    [Display(Name = "Administrative Level")]
    public string AdministrativeLevel { get; set; }

    // Admin-specific permissions only
    public bool CanManageUsers { get; set; }
    public bool CanManageSystem { get; set; }
    public bool CanViewReports { get; set; }
    public bool CanManageAllDepartments { get; set; }

    public Administrator()
    {
        AddRole(UserRole.Administrator);
        AdministratorSince = DateTime.Now;
    }
}