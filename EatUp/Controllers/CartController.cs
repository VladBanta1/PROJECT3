using EatUp.Data;
using EatUp.Helpers;
using EatUp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



namespace EatUp.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // GET: /Cart/Add?menuItemId=5
        public async Task<IActionResult> Add(int menuItemId)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == menuItemId);

            if (menuItem == null)
            {
                return NotFound();
            }

            var cart = GetCart();

            var existingItem = cart.FirstOrDefault(c => c.MenuItemId == menuItemId);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    MenuItemId = menuItem.Id,
                    MenuItemName = menuItem.Name,
                    RestaurantName = menuItem.Restaurant?.Name ?? "",
                    UnitPrice = menuItem.Price,
                    Quantity = 1
                });
            }

            SaveCart(cart);

            // după adăugare, mergem la coș
            return RedirectToAction("Index");
        }

        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string customerName, string customerAddress, string customerPhone)
        {
            var cart = GetCart();
            if (cart == null || !cart.Any())
            {
                TempData["CartError"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(customerName) ||
                string.IsNullOrWhiteSpace(customerAddress) ||
                string.IsNullOrWhiteSpace(customerPhone))
            {
                TempData["CartError"] = "Please fill in all the customer details.";
                return RedirectToAction("Index");
            }

            var total = cart.Sum(c => c.TotalPrice);

            var order = new Order
            {
                CustomerName = customerName,
                CustomerAddress = customerAddress,
                CustomerPhone = customerPhone,
                TotalPrice = total,
                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // ca să avem order.Id

            foreach (var item in cart)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
                _context.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();

            // goliți coșul
            SaveCart(new List<CartItem>());

            return RedirectToAction("Confirmation", new { id = order.Id });
        }

        // GET: /Cart/Confirmation/5
        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }


        // GET: /Cart/Remove?menuItemId=5
        public IActionResult Remove(int menuItemId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.MenuItemId == menuItemId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        private List<CartItem> GetCart()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey);
            return cart ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObject(CartSessionKey, cart);
        }
    }
}
