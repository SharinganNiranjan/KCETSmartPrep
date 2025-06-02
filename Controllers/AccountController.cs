using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KcetPrep1.Models;
using System.Threading.Tasks;

namespace KcetPrep1.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            var model = new LoginModel
            {
                ReturnUrl = returnUrl
            };
            return View(model);
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToAction("Index", "Home"); // Always redirect to Home/Index
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            var model = new RegisterModel
            {
                ReturnUrl = returnUrl
            };
            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            model.ReturnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Student");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User created a new account with password.");
                    return RedirectToAction("Index", "Home"); // Redirect to Home/Index after registration
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}