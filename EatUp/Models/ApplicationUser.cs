using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace EatUp.Models
{
    public class ApplicationUser : IdentityUser
    {
      
        public string? DisplayName { get; set; }

        public ICollection<Restaurant>? Restaurants { get; set; }
    }
}
