using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.ViewModels; 
using GorevTakipSistemi.Data;     
using GorevTakipSistemi.Models;  
using Microsoft.EntityFrameworkCore; 
using System.Security.Claims; 
using Microsoft.AspNetCore.Authentication; 
using Microsoft.AspNetCore.Authentication.Cookies; 

namespace GorevTakipSistemi.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Dependency Injection ile DbContext'i alıyoruz
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        [HttpGet]
        public IActionResult Login()
        {
            return View(); 
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı koruma
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid) 
            {
               
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null)
                {
                   
                    if (user.PasswordHash == model.Password) 
                    {
                        
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Role, user.Role.ToString()),
                            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                        };

                        var claimsIdentity = new ClaimsIdentity(
                            claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties
                        {
                            // IsPersistent = true, // Beni Hatırla özelliği eklenebilir
                            // ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20), // Oturum süresi
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        
                        if (user.Role == UserRole.Admin)
                        {
                            return RedirectToAction("Index", "Admin"); 
                        }
                        else 
                        {
                            return RedirectToAction("Index", "User"); 
                        }
                    }
                }
                ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre."); 
            }
            return View(model); 
        }

       
        [HttpGet]
        public IActionResult Register()
        {
            return View(); 
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
               
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanımda.");
                    return View(model);
                }

                
                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = model.Password, 
                    Role = UserRole.User 
                };

                _context.Add(user);
                await _context.SaveChangesAsync(); 

               
                return RedirectToAction("Login", "Account");
            }
            return View(model);
        }

        
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account"); 
        }
    }
}