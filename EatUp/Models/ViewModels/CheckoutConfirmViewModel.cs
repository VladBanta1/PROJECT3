public class CheckoutConfirmViewModel
{
    public decimal DeliveryFee { get; set; }
    public decimal Subtotal { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
}
