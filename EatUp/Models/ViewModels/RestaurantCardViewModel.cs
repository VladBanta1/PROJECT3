namespace EatUp.Models.ViewModels
{
    public class RestaurantCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }
        public int DeliveryTimeMinutes { get; set; }
    }
}
