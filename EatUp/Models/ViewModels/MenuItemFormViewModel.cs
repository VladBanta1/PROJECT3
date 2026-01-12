using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EatUp.Models.ViewModels
{
    public class MenuItemFormViewModel
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Range(0.01, 10000)]
        public decimal Price { get; set; }

        public string? ExistingImageUrl { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
