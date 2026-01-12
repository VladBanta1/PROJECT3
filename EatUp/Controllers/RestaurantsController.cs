using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EatUp.Data;
using EatUp.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;

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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                TempData["Debug"] = string.Join(" | ", errors);
                return View(restaurant);
            }

            if (ModelState.IsValid)
            {
                restaurant.OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                restaurant.IsApproved = false;
                restaurant.IsSubmitted = false;

                _context.Restaurants.Add(restaurant);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] =
           "Restaurant details saved. Please add menu items and submit for approval.";

                return RedirectToAction(nameof(Manage));
            }
            return View(restaurant);
        }

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


        [Authorize(Roles = "Restaurant")]
        public async Task<IActionResult> SubmitForApproval()
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            var restaurant = await _context.Restaurants
                .Include(r => r.MenuItems)
                .FirstOrDefaultAsync(r => r.OwnerId == userId);

            if (restaurant == null)
                return NotFound();

            if (!restaurant.MenuItems.Any())
            {
                TempData["SuccessMessage"] =
                    "You must add at least one menu item before submitting.";
                return RedirectToAction("Manage");
            }

            restaurant.IsSubmitted = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Restaurant submitted for approval. Please wait for admin review.";

            return RedirectToAction("Manage");
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
            TempData["Success"] = "Restaurant details updated successfully.";

            return RedirectToAction(nameof(Manage));
        }

        // =======================
        // ADMIN ROLE
        // =======================

        // GET: Restaurants/Pending
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pending()
        {
            var pendingRestaurants = _context.Restaurants
                .Where(r => r.IsSubmitted && !r.IsApproved);

            return View(await pendingRestaurants.ToListAsync());
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
            TempData["Success"] = "Restaurant approved successfully.";
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
