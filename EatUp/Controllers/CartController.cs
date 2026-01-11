using EatUp.Data;
using EatUp.Helpers;
using EatUp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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

        // =======================
        // CART (PUBLIC)
        // =======================

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
                return NotFound();

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
            return RedirectToAction(nameof(Index));
        }

        // =======================
        // CHECKOUT
        // =======================

        // GET: /Cart/Checkout
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
                return RedirectToAction(nameof(Index));

            // dacă nu e logat → mesaj frumos
            if (!User.Identity.IsAuthenticated)
            {
                return View("LoginRequired");
            }

            return View(cart);
        }

        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Checkout(
            string customerName,
            string customerAddress,
            string customerPhone)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["CartError"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(customerName) ||
                string.IsNullOrWhiteSpace(customerAddress) ||
                string.IsNullOrWhiteSpace(customerPhone))
            {
                TempData["CartError"] = "Please fill in all the customer details.";
                return RedirectToAction(nameof(Index));
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
            await _context.SaveChangesAsync();

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

            // golește coșul
            SaveCart(new List<CartItem>());

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        // =======================
        // CONFIRMATION
        // =======================

        // GET: /Cart/Confirmation/5
        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // =======================
        // REMOVE ITEM
        // =======================

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

            return RedirectToAction(nameof(Index));
        }

        // =======================
        // SESSION HELPERS
        // =======================

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
