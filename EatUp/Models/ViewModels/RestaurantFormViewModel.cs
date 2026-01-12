using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EatUp.Models.ViewModels
{
    public class RestaurantFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Address { get; set; }

        public int DeliveryTimeMinutes { get; set; }

        public decimal DeliveryFee { get; set; }

        public string? ExistingImageUrl { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
