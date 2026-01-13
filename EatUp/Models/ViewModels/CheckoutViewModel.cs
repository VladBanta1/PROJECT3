namespace EatUp.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; } = new();

        public decimal Subtotal { get; set; }

        public double RestaurantLat { get; set; }
        public double RestaurantLng { get; set; }
    }
}