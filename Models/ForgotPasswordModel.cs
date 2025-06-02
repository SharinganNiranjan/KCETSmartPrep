using System.ComponentModel.DataAnnotations;

namespace KcetPrep1.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
