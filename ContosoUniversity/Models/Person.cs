using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ContosoUniversity.Helpers;

namespace ContosoUniversity.Models
{
    public abstract class Person
    {
        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; }

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
    }
}
