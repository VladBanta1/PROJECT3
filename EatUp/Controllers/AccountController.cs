using EatUp.Data;
using EatUp.Models;
using EatUp.Models.ViewModels;
using EatUp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EatUp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly GeocodingService _geocodingService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            GeocodingService geocodingService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _geocodingService = geocodingService;
        }

        // =======================
        // LOGIN
        // =======================

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                TempData["Error"] = "Invalid email or password.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, password, false, false);

            if (result.Succeeded)
            {
                // 🔥 FALLBACK GEOLOCATION – dacă browserul nu a trimis locația
                if (user.Latitude == 0 && user.Longitude == 0 && !string.IsNullOrEmpty(user.Address))
                {
                    var coords = await _geocodingService.GetCoordinatesAsync(user.Address);

                    if (coords != null)
                    {
                        user.Latitude = coords.Value.lat;
                        user.Longitude = coords.Value.lon;
                        await _userManager.UpdateAsync(user);
                    }
                }

                TempData["Success"] = "You have logged in successfully.";
                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = "Invalid email or password.";
            return View();
        }


        // =======================
        // REGISTER
        // =======================

        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Address = model.AccountType == "Client" ? model.Address : null,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

               

                return View(model);
            }


            if (model.AccountType == "Restaurant")
            {
                await _userManager.AddToRoleAsync(user, "Restaurant");

                var restaurant = new Restaurant
                {
                    Name = model.RestaurantName!,
                    Address = model.RestaurantAddress!,
                    OwnerId = user.Id,
                    IsApproved = false
                };

                _context.Restaurants.Add(restaurant);
                await _context.SaveChangesAsync();
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Client");
            }
            TempData["Success"] = "Account created successfully.";
            await _signInManager.SignInAsync(user, false);
            return RedirectToAction("Index", "Home");
            

        }

        // =======================
        // LOGOUT
        // =======================

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            TempData["Info"] = "You have been logged out.";
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
            

        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveLocation(double latitude, double longitude)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            user.Latitude = latitude;
            user.Longitude = longitude;

            await _userManager.UpdateAsync(user);

            return Ok();
        }

    }
}
