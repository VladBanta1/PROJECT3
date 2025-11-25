namespace EatUp.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
