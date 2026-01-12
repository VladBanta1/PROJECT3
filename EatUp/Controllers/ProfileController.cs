using EatUp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }

    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ApplicationUser model)
    {
        var user = await _userManager.GetUserAsync(User);

        user.FullName = model.FullName;
        user.Address = model.Address;
        user.PhoneNumber = model.PhoneNumber;

        await _userManager.UpdateAsync(user);
        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
