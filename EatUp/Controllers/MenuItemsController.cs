using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EatUp.Data;
using EatUp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EatUp.Controllers
{
    public class MenuItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =======================
        // PUBLIC (CLIENT)
        // =======================

        // GET: MenuItems (doar din restaurante aprobate)
        public async Task<IActionResult> Index()
        {
            var menuItems = await _context.MenuItems
                .Include(m => m.Restaurant)
                .Where(m => m.Restaurant.IsApproved)
                .ToListAsync();

            return View(menuItems);
        }

        // GET: MenuItems/Details/5
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

        // GET: MenuItems/Create
        [Authorize(Roles = "Restaurant")]
        public IActionResult Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // restaurantul vede doar restaurantele LUI
            ViewData["RestaurantId"] = new SelectList(
                _context.Restaurants.Where(r => r.OwnerId == userId),
                "Id",
                "Name"
            );

            return View();
        }

        // POST: MenuItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Create(MenuItem menuItem)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == menuItem.RestaurantId);

            // securitate: doar owner-ul poate adăuga
            if (restaurant == null || restaurant.OwnerId != userId)
                return Forbid();

            if (ModelState.IsValid)
            {
                _context.Add(menuItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Menu item added successfully.";

                return RedirectToAction("Manage", "Restaurants");
            }

            ViewData["RestaurantId"] = new SelectList(
                _context.Restaurants.Where(r => r.OwnerId == userId),
                "Id",
                "Name",
                menuItem.RestaurantId
            );

            return View(menuItem);
        }

        // GET: MenuItems/Edit/5
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null) return NotFound();

            // doar owner-ul restaurantului poate edita
            if (menuItem.Restaurant.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            ViewData["RestaurantId"] = new SelectList(
                _context.Restaurants.Where(r => r.OwnerId == menuItem.Restaurant.OwnerId),
                "Id",
                "Name",
                menuItem.RestaurantId
            );

            return View(menuItem);
        }

        // POST: MenuItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Edit(int id, MenuItem menuItem)
        {
            if (id != menuItem.Id)
                return NotFound();

            var existingItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingItem == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (existingItem.Restaurant.OwnerId != userId)
                return Forbid();

            if (!ModelState.IsValid)
                return View(menuItem);

            // ✅ ACTUALIZĂM DOAR CÂMPURILE EDITABILE
            existingItem.Name = menuItem.Name;
            existingItem.Description = menuItem.Description;
            existingItem.Price = menuItem.Price;
            existingItem.ImageUrl = menuItem.ImageUrl;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Menu item updated successfully.";

            return RedirectToAction("Manage", "Restaurants");
        }
        // =======================
        // ADMIN / RESTAURANT DELETE
        // =======================

        // GET: MenuItems/Delete/5
        [Authorize(Roles = "Restaurant,Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var menuItem = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null) return NotFound();

            // restaurant poate șterge DOAR propriile item-uri
            if (User.IsInRole("Restaurant") &&
                menuItem.Restaurant.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            return View(menuItem);
        }

        // POST: MenuItems/Delete/5
        [HttpPost, ActionName("Delete")]
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

            _context.MenuItems.Remove(menuItem);
            await _context.SaveChangesAsync();
            TempData["Warning"] = "Menu item deleted.";

            return RedirectToAction("Manage", "Restaurants");
        }

        private bool MenuItemExists(int id)
        {
            return _context.MenuItems.Any(e => e.Id == id);
        }
    }
}
