using System.ComponentModel.DataAnnotations;

namespace EatUp.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string AccountType { get; set; } = "Client";

        // COMUNE
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        // CLIENT
        public string? Address { get; set; }

        // RESTAURANT
        public string? RestaurantName { get; set; }
        public string? RestaurantAddress { get; set; }
    }
}
