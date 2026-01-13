using EatUp.Data;
using EatUp.Helpers;
using EatUp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using EatUp.Helpers;
using EatUp.Models.ViewModels;

namespace EatUp.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "Cart";
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context,
                              UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class LocationDto
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
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
                    Quantity = 1,
                    ImageUrl = menuItem.ImageUrl ?? "/images/default-image.png",
                    DeliveryFee = menuItem.Restaurant?.DeliveryFee ?? 0
                });

            }

            SaveCart(cart);
            TempData["Success"] = "Item added to cart.";
            return RedirectToAction(nameof(Index));
        }

        // =======================
        // CHECKOUT
        // =======================

        // GET: /Cart/Checkout
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (cart == null || !cart.Any())
                return RedirectToAction("Index");

            var restaurant = GetRestaurantFromCart();
            if (restaurant == null)
                return RedirectToAction("Index");

            var model = new CheckoutViewModel
            {
                CartItems = cart,
                Subtotal = cart.Sum(x => x.TotalPrice),
                RestaurantLat = restaurant.Latitude,
                RestaurantLng = restaurant.Longitude
            };

            return View(model);
        }


        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // ⬅️ OBLIGATORIU LOGAT
        public async Task<IActionResult> CheckoutConfirm()
        {
            var cart = GetCart();

            if (cart == null || !cart.Any())
            {
                TempData["CartError"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var order = new Order
            {
                CustomerName = user.FullName,
                CustomerAddress = user.Address,
                CustomerPhone = user.PhoneNumber,
                TotalPrice = cart.Sum(c => c.TotalPrice),
                CreatedAt = DateTime.Now,
                UserId = user.Id
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            await _context.SaveChangesAsync();

            SaveCart(new List<CartItem>());
            TempData["Success"] = "Your order has been placed successfully.";
            return RedirectToAction("Confirmation", new { id = order.Id });
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
            TempData["Warning"] = "Item removed from cart.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CalculateDelivery([FromBody] LocationDto dto)
        {
            var user = _context.Users.First(u => u.UserName == User.Identity.Name);

            var restaurant = GetRestaurantFromCart(); // metoda ta existentă
            if (restaurant == null) return BadRequest();

            double distanceKm = GeoHelper.DistanceKm(
                dto.Latitude, dto.Longitude,
                restaurant.Latitude, restaurant.Longitude
            );

            decimal deliveryFee = (decimal)(5 + distanceKm * 2);
            deliveryFee = Math.Min(deliveryFee, 25);

            decimal subtotal = GetCartSubtotal(); // deja o ai
            decimal total = subtotal + deliveryFee;

            return Json(new
            {
                distanceKm,
                deliveryFee,
                total
            });
        }

        private decimal GetCartSubtotal()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart");

            if (cart == null || !cart.Any())
                return 0;

            return cart.Sum(i => i.UnitPrice * i.Quantity);
        }


        // =======================
        // SESSION HELPERS
        // =======================
        private Restaurant? GetRestaurantFromCart()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");

            if (cart == null || !cart.Any())
                return null;

            // luăm primul produs din coș
            int menuItemId = cart.First().MenuItemId;

            // găsim restaurantul prin MenuItem
            return _context.MenuItems
                .Include(m => m.Restaurant)
                .Where(m => m.Id == menuItemId)
                .Select(m => m.Restaurant)
                .FirstOrDefault();
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
