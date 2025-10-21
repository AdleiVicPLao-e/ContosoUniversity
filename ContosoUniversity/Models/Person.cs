using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContosoUniversity.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace ContosoUniversity.Models
{
    public abstract class Person
    {
        // Move enum outside the class for better accessibility
        public enum UserRole
        {
            Student = 1,
            Instructor = 2,
            Administrator = 4
        }

        public int ID { get; set; }

        private string username;

        [Required]
        [StringLength(50)]
        public string UserName
        {
            get { return username; }
            set { username = value; }
        }

        private string password;

        [Required]
        [Column("EncryptedPassword")]
        public string EncryptedPassword
        {
            get => password;
            set => password = value;
        }

        [NotMapped]
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

        // Multi-role support using bit flags
        [Required]
        public UserRole Roles { get; set; }

        // Track login status to prevent multiple logins
        [Required]
        [Display(Name = "Is Logged In")]
        public bool IsLoggedIn { get; set; } = false;

        // Individual role checkers
        [NotMapped]
        public bool IsStudent => HasRole(UserRole.Student);
        [NotMapped]
        public bool IsInstructor => HasRole(UserRole.Instructor);
        [NotMapped]
        public bool IsAdministrator => HasRole(UserRole.Administrator);

        // Primary role for display/UI purposes
        [NotMapped]
        public UserRole PrimaryRole
        {
            get
            {
                if (HasRole(UserRole.Administrator)) return UserRole.Administrator;
                if (HasRole(UserRole.Instructor)) return UserRole.Instructor;
                if (HasRole(UserRole.Student)) return UserRole.Student;
                return UserRole.Student; // default
            }
        }

        // Role management methods
        public bool HasRole(UserRole role)
        {
            return (Roles & role) == role;
        }

        public void AddRole(UserRole role)
        {
            Roles |= role;
        }

        public void RemoveRole(UserRole role)
        {
            Roles &= ~role;
        }

        public IEnumerable<UserRole> GetRoles()
        {
            return System.Enum.GetValues(typeof(UserRole))
                .Cast<UserRole>()
                .Where(role => HasRole(role));
        }

        // Login/Logout methods
        public bool TryLogin(string enteredPassword)
        {
            if (IsLoggedIn)
            {
                return false; // Already logged in elsewhere
            }

            if (Password == enteredPassword)
            {
                IsLoggedIn = true;
                return true;
            }

            return false;
        }

        public void Logout()
        {
            IsLoggedIn = false;
        }

        public void ForceLogout()
        {
            IsLoggedIn = false;
            // You might want to log this action for security purposes
        }

        // Validation to ensure at least one role is set
        protected virtual bool ValidateRoles()
        {
            return Roles != 0; // At least one role must be set
        }
    }
}