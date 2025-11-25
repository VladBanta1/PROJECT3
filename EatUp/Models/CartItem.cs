namespace EatUp.Models
{
    public class CartItem
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;

        public string RestaurantName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
