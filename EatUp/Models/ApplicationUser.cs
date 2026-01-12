using Microsoft.AspNetCore.Identity;

namespace EatUp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public string? Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }


        public Restaurant? Restaurant { get; set; }
    }
}
