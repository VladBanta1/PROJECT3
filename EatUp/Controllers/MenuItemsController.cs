using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using EatUp.Data;
using EatUp.Models;
using EatUp.Models.ViewModels;

namespace EatUp.Controllers
{
    public class MenuItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MenuItemsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =======================
        // PUBLIC (CLIENT)
        // =======================

        public async Task<IActionResult> Index()
        {
            var menuItems = await _context.MenuItems
                .Include(m => m.Restaurant)
                .Where(m => m.Restaurant.IsApproved)
                .ToListAsync();

            return View(menuItems);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m =>
                    m.Id == id && m.Restaurant.IsApproved);

            if (menuItem == null) return NotFound();

            return View(menuItem);
        }

        // =======================
        // RESTAURANT ROLE
        // =======================

        [Authorize(Roles = "Restaurant")]
        public IActionResult Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ViewData["RestaurantId"] = new SelectList(
                _context.Restaurants.Where(r => r.OwnerId == userId),
                "Id",
                "Name"
            );

            return View(new MenuItemFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Create(MenuItemFormViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == model.RestaurantId);

            if (restaurant == null || restaurant.OwnerId != userId)
                return Forbid();

            if (!ModelState.IsValid)
            {
                ViewData["RestaurantId"] = new SelectList(
                    _context.Restaurants.Where(r => r.OwnerId == userId),
                    "Id",
                    "Name",
                    model.RestaurantId
                );

                return View(model);
            }

            string imagePath = "/images/placeholder-food.jpg";

            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/menu-items");
                Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(stream);

                imagePath = "/uploads/menu-items/" + fileName;
            }

            var menuItem = new MenuItem
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                RestaurantId = model.RestaurantId,
                ImageUrl = imagePath
            };

            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu item added successfully.";

            return RedirectToAction("Manage", "Restaurants");
        }

        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null) return NotFound();

            if (menuItem.Restaurant.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            var model = new MenuItemFormViewModel
            {
                Id = menuItem.Id,
                RestaurantId = menuItem.RestaurantId,
                Name = menuItem.Name,
                Description = menuItem.Description,
                Price = menuItem.Price,
                ExistingImageUrl = menuItem.ImageUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Edit(MenuItemFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == model.Id);

            if (menuItem == null) return NotFound();

            if (menuItem.Restaurant.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/menu-items");
                Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(stream);

                // șterge poza veche
                if (!string.IsNullOrEmpty(menuItem.ImageUrl) &&
                    menuItem.ImageUrl.StartsWith("/uploads/"))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, menuItem.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                menuItem.ImageUrl = "/uploads/menu-items/" + fileName;
            }

            menuItem.Name = model.Name;
            menuItem.Description = model.Description;
            menuItem.Price = model.Price;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu item updated successfully.";

            return RedirectToAction("Manage", "Restaurants");
        }

        // =======================
        // DELETE (ADMIN / RESTAURANT)
        // =======================

        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null) return NotFound();

            if (User.IsInRole("Restaurant") &&
                menuItem.Restaurant.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            return View(menuItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null) return NotFound();

            if (User.IsInRole("Restaurant") &&
                menuItem.Restaurant.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            if (!string.IsNullOrEmpty(menuItem.ImageUrl) &&
                menuItem.ImageUrl.StartsWith("/uploads/"))
            {
                var imagePath = Path.Combine(_env.WebRootPath, menuItem.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            _context.MenuItems.Remove(menuItem);
            await _context.SaveChangesAsync();

            TempData["Warning"] = "Menu item deleted.";

            return RedirectToAction("Manage", "Restaurants");
        }
    }
}
