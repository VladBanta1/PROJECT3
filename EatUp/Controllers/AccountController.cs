using EatUp.Data;
using EatUp.Models;
using EatUp.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EatUp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // =======================
        // LOGIN
        // =======================

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(
                email, password, false, false);

            if (result.Succeeded)
            {
                TempData["Success"] = "You have logged in successfully.";
                return RedirectToAction("Index", "Home");
                
            }
            else
            {


                ModelState.AddModelError("", "Invalid login");
                TempData["Error"] = "Invalid email or password.";

                return View();
            }
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
    }
}
