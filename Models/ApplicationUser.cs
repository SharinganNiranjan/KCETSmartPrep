using Microsoft.AspNetCore.Identity;

namespace KcetPrep1.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Add custom fields if needed
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}
