namespace EatUp.Models.ViewModels
{
    public class RestaurantDistanceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }

        public double DistanceKm { get; set; }
        public decimal DeliveryFee { get; set; }

        public int DeliveryTimeMinutes { get; set; }
    }
}
