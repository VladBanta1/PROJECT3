using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EatUp.Data;
using EatUp.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace EatUp.Controllers
{
    public class RestaurantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RestaurantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =======================
        // PUBLIC (CLIENT)
        // =======================

        // GET: Restaurants
        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants
                .Where(r => r.IsApproved)
                .ToListAsync();

            return View(restaurants);
        }

        // GET: Restaurants/Details/5
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

        // GET: Restaurants/Create
        [Authorize(Roles = "Restaurant")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Restaurants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Create(Restaurant restaurant)
        {
            if (ModelState.IsValid)
            {
                restaurant.OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                restaurant.IsApproved = false;

                _context.Add(restaurant);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(restaurant);
        }

        // GET: Restaurants/Edit/5
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound();

            // doar owner-ul poate edita
            if (restaurant.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            return View(restaurant);
        }

        // POST: Restaurants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> Edit(int id, Restaurant restaurant)
        {
            if (id != restaurant.Id) return NotFound();

            var existing = await _context.Restaurants.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existing == null) return NotFound();

            if (existing.OwnerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return Forbid();

            // orice edit necesită re-aprobare
            restaurant.OwnerId = existing.OwnerId;
            restaurant.IsApproved = false;

            _context.Update(restaurant);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =======================
        // ADMIN ROLE
        // =======================

        // GET: Restaurants/Pending
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pending()
        {
            var pending = await _context.Restaurants
                .Where(r => !r.IsApproved)
                .ToListAsync();

            return View(pending);
        }

        // POST: Restaurants/Approve/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound();

            restaurant.IsApproved = true;
            _context.Update(restaurant);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Pending));
        }

        // GET: Restaurants/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null) return NotFound();

            return View(restaurant);
        }

        // POST: Restaurants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant != null)
            {
                _context.Restaurants.Remove(restaurant);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
