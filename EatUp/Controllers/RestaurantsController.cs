using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EatUp.Data;
using EatUp.Models;
using EatUp.Models.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace EatUp.Controllers
{
    public class RestaurantsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RestaurantsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =======================
        // PUBLIC (CLIENT)
        // =======================

        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants
                .Where(r => r.IsApproved)
                .ToListAsync();

            return View(restaurants);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsApproved);

            if (restaurant == null) return NotFound();

            return View(restaurant);
        }

        // =======================
        // RESTAURANT ROLE
        // =======================

        [Authorize(Roles = "Restaurant")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Create(RestaurantFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string imagePath = "/images/placeholder.jpg";

            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/restaurants");
                Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(stream);

                imagePath = "/uploads/restaurants/" + fileName;
            }

            var restaurant = new Restaurant
            {
                Name = model.Name,
                Address = model.Address,
                DeliveryTimeMinutes = model.DeliveryTimeMinutes,
                DeliveryFee = model.DeliveryFee,
                ImageUrl = imagePath,
                OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                IsApproved = false,
                IsSubmitted = false
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Restaurant details saved. Please add menu items and submit for approval.";

            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Manage()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems)
                .FirstOrDefaultAsync(r => r.OwnerId == userId);

            if (restaurant == null)
                return RedirectToAction(nameof(Create));

            return View(restaurant);
        }

        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound();

            if (restaurant.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            var model = new RestaurantFormViewModel
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Address = restaurant.Address,
                DeliveryTimeMinutes = restaurant.DeliveryTimeMinutes,
                DeliveryFee = restaurant.DeliveryFee,
                ExistingImageUrl = restaurant.ImageUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Edit(RestaurantFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var restaurant = await _context.Restaurants.FindAsync(model.Id);
            if (restaurant == null) return NotFound();

            if (restaurant.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/restaurants");
                Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(stream);

                restaurant.ImageUrl = "/uploads/restaurants/" + fileName;
            }

            restaurant.Name = model.Name;
            restaurant.Address = model.Address;
            restaurant.DeliveryTimeMinutes = model.DeliveryTimeMinutes;
            restaurant.DeliveryFee = model.DeliveryFee;
            restaurant.IsApproved = false;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Restaurant details updated successfully.";
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> SubmitForApproval()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems)
                .FirstOrDefaultAsync(r => r.OwnerId == userId);

            if (restaurant == null) return NotFound();

            if (!restaurant.MenuItems.Any())
            {
                TempData["Error"] =
                    "You must add at least one menu item before submitting.";
                return RedirectToAction(nameof(Manage));
            }

            restaurant.IsSubmitted = true;
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Restaurant submitted for approval. Please wait for admin review.";

            return RedirectToAction(nameof(Manage));
        }

        // =======================
        // ADMIN ROLE
        // =======================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pending()
        {
            var pendingRestaurants = await _context.Restaurants
                .Where(r => r.IsSubmitted && !r.IsApproved)
                .ToListAsync();

            return View(pendingRestaurants);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound();

            restaurant.IsApproved = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Restaurant approved successfully.";
            return RedirectToAction(nameof(Pending));
        }
        // =======================
        // ADMIN – DELETE RESTAURANT
        // =======================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
                return NotFound();

            return View(restaurant);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
                return NotFound();

           
            if (!string.IsNullOrEmpty(restaurant.ImageUrl) &&
                restaurant.ImageUrl.StartsWith("/uploads/"))
            {
                var imagePath = Path.Combine(
                    _env.WebRootPath,
                    restaurant.ImageUrl.TrimStart('/')
                );

                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            _context.Restaurants.Remove(restaurant);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Restaurant deleted successfully.";

            return RedirectToAction(nameof(Pending));
        }
        // =======================
        // ADMIN – VIEW RESTAURANT DETAILS
        // =======================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ViewRestaurant(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
                return NotFound();

            return View(restaurant);
        }


    }
}
