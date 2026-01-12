namespace EatUp.Models
{
    public class CartItem
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = "";
        public string RestaurantName { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public string ImageUrl { get; set; } = "/images/default-image.png";
        public decimal DeliveryFee { get; set; }

        public decimal TotalPrice => UnitPrice * Quantity;
    }

}
