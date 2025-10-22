using ContosoUniversity.Helpers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web.Mvc;

namespace ContosoUniversity.Models
{
    public abstract class Person
    {
        // Define user roles as bit flags (outside the class if you prefer global use)
        public enum UserRole
        {
            Student = 1,
            Instructor = 2,
            Administrator = 4
        }

        public int ID { get; set; }

        private string username;
        private string password;
        private bool deleted;

        [Required]
        [StringLength(50)]
        public string UserName
        {
            get => username;
            set => username = value;
        }

        // 🔐 Encrypted value stored in DB
        [Required]
        [Column("EncryptedPassword")]
        [ScaffoldColumn(false)] // Not displayed in auto forms
        public string EncryptedPassword
        {
            get => password;
            set => password = value;
        }

        // 🔒 Computed password property (NotMapped, decrypted on get)
        [NotMapped]
        [ScaffoldColumn(false)] // Don’t show this in any auto form
        public string Password
        {
            get => string.IsNullOrEmpty(password)
                ? null
                : CryptoHelper.Decrypt(password);

            set => password = string.IsNullOrEmpty(value)
                ? null
                : CryptoHelper.Encrypt(value);
        }

        [Required]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters.")]
        [Column("FirstName")]
        [Display(Name = "First Name")]
        public string FirstMidName { get; set; }

        [Display(Name = "Full Name")]
        public string FullName => $"{LastName}, {FirstMidName}";

        // 🧩 Roles with bitwise flags
        [Required]
        public UserRole Roles { get; set; }

        [Required]
        [Display(Name = "Is Logged In")]
        public bool IsLoggedIn { get; set; } = false;

        // 🧱 Soft delete flag — protected so only controller/service can modify
        [ScaffoldColumn(false)]
        public bool IsDeleted
        {
            get => deleted;
            set => deleted = value; // only controller or derived class can change
        }

        // ✅ Role accessors
        [NotMapped] public bool IsStudent => HasRole(UserRole.Student);
        [NotMapped] public bool IsInstructor => HasRole(UserRole.Instructor);
        [NotMapped] public bool IsAdministrator => HasRole(UserRole.Administrator);

        [NotMapped]
        public UserRole PrimaryRole =>
            HasRole(UserRole.Administrator) ? UserRole.Administrator :
            HasRole(UserRole.Instructor) ? UserRole.Instructor :
            HasRole(UserRole.Student) ? UserRole.Student :
            UserRole.Student;

        // Role helpers
        public bool HasRole(UserRole role) => (Roles & role) == role;
        public void AddRole(UserRole role) => Roles |= role;
        public void RemoveRole(UserRole role) => Roles &= ~role;
        public IEnumerable<UserRole> GetRoles() =>
            System.Enum.GetValues(typeof(UserRole))
                .Cast<UserRole>()
                .Where(HasRole);

        // Login logic
        public bool TryLogin(string enteredPassword)
        {
            if (IsLoggedIn)
                return false; // already logged in

            if (Password == enteredPassword)
            {
                IsLoggedIn = true;
                return true;
            }

            return false;
        }

        public void Logout() => IsLoggedIn = false;
        public void ForceLogout() => IsLoggedIn = false;

        // Validation
        protected virtual bool ValidateRoles() => Roles != 0;
    }
}
