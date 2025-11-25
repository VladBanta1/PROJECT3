namespace EatUp.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        public int RestaurantId { get; set; }

        // navigațională, o facem nullable ca să nu fie required în model binding
        public Restaurant? Restaurant { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }
    }
}
