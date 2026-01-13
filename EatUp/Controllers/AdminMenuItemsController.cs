using EatUp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatUp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminMenuItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminMenuItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTA PENDING
        public async Task<IActionResult> Pending()
        {
            var items = await _context.MenuItems
                .Include(m => m.Restaurant)
                .Where(m => !m.IsApproved)
                .ToListAsync();

            return View(items);
        }

        // APPROVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null)
                return NotFound();

            item.IsApproved = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu item approved.";
            return RedirectToAction(nameof(Pending));
        }
    }
}
