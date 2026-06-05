using EduChatbot.MVC.Models;
using EduChatbot.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EduChatbot.MVC.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Use Identity service, Controller does not touch DbContext directly.
        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return await RedirectByRoleAsync(user);
        }

        ModelState.AddModelError(string.Empty, "Incorrect email or password.");
        return View(model);
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize(Roles = ApplicationRoles.Student + "," + ApplicationRoles.Lecturer)]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        // Profile shared for Student and Lecturer, data is still retrieved from ASP.NET Identity.
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new AccountProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty
        });
    }

    [Authorize(Roles = ApplicationRoles.Student + "," + ApplicationRoles.Lecturer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(AccountProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        model.Email = user.Email ?? string.Empty;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Only allow users to update display name, not login email.
        user.FullName = model.FullName.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["ProfileMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize(Roles = ApplicationRoles.Student + "," + ApplicationRoles.Lecturer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(AccountChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Profile), new AccountProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            });
        }

        // Identity checks old password, hashes new password and saves to AspNetUsers.PasswordHash.
        var result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(nameof(Profile), new AccountProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            });
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["ProfileMessage"] = "Password changed successfully.";
        return RedirectToAction(nameof(Profile));
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<IActionResult> RedirectByRoleAsync(ApplicationUser? user)
    {
        if (user != null && await _userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
        {
            return RedirectToAction("Dashboard", "Admin");
        }

        if (user != null && await _userManager.IsInRoleAsync(user, ApplicationRoles.Lecturer))
        {
            return RedirectToAction("Dashboard", "Documents");
        }

        if (user != null && await _userManager.IsInRoleAsync(user, ApplicationRoles.Student))
        {
            return RedirectToAction("Index", "Chat");
        }

        return RedirectToAction("Index", "Home");
    }
}
