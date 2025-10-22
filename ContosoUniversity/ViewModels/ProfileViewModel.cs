using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;

namespace ContosoUniversity.ViewModels
{
    public class ProfileViewModel
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot be longer than 50 characters")]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
        [Display(Name = "First Name")]
        public string FirstMidName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Full Name")]
        public string FullName => $"{LastName}, {FirstMidName}";

        public Person.UserRole Roles { get; set; }

        [Display(Name = "Primary Role")]
        public Person.UserRole PrimaryRole { get; set; }

        // Add these boolean properties for the view
        [Display(Name = "Is Administrator")]
        public bool IsAdministrator => (Roles & Person.UserRole.Administrator) == Person.UserRole.Administrator;

        [Display(Name = "Is Instructor")]
        public bool IsInstructor => (Roles & Person.UserRole.Instructor) == Person.UserRole.Instructor;

        [Display(Name = "Is Student")]
        public bool IsStudent => (Roles & Person.UserRole.Student) == Person.UserRole.Student;
    }
}