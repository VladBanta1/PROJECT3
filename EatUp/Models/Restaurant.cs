namespace EatUp.Models
{
    public class Restaurant
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        public string CuisineType { get; set; } = string.Empty;
        public int DeliveryTimeMinutes { get; set; }
        public decimal DeliveryFee { get; set; }

        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }
        public bool IsApproved { get; set; } = false;

        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
