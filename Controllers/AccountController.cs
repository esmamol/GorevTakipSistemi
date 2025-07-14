using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.Models;
using GorevTakipSistemi.ViewModels;
using GorevTakipSistemi.Data;
using System.Security.Claims; 
using Microsoft.AspNetCore.Authentication; 
using Microsoft.AspNetCore.Authentication.Cookies; 
using Microsoft.EntityFrameworkCore; 
using Microsoft.AspNetCore.Authorization;


namespace GorevTakipSistemi.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous] 
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole(UserRole.Admin.ToString()))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (User.IsInRole(UserRole.User.ToString()))
                {
                    return RedirectToAction("Index", "User");
                }
                
                return RedirectToAction("AccessDenied", "Account");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid) 
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

              
                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Role, user.Role.ToString()) // user.Role enum'unu string'e çevir
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                   
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe, // "Beni Hatırla" işaretliyse kalıcı çerez, değilse session çerezi
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : (DateTimeOffset?)null
                    };

                    await HttpContext.SignInAsync( 
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                    // ClaimsPrincipal nesnesini kullanarak kimlik doğrulama çerezini oluşturur ve kullanıcının tarayıcısına gönderir.

                 
                    if (user.Role == UserRole.Admin)
                    {
                        return RedirectToAction("Index", "Admin"); 
                    }
                    else if (user.Role == UserRole.User)
                    {
                        return RedirectToAction("Index", "User"); 
                    }
                  
                    return RedirectToAction("AccessDenied", "Account");
                }
                ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous] 
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

                
                if (!string.IsNullOrEmpty(model.Email) && await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanımda.");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username, 
                                               
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = UserRole.User // Varsayılan olarak 'User' rolü atandı.
                };

                _context.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kaydınız başarıyla tamamlandı. Şimdi giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            return View(model);
        }

        [HttpGet]
        [Authorize] 
        public async Task<IActionResult> Logout()
        {
           
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account"); 
        }

        [AllowAnonymous] 
        public IActionResult AccessDenied()
        {
            return View(); // Views/Account/AccessDenied.cshtml yolundaki View'ı render ederek kullanıcıya yetkisiz erişim mesajını gösterir.
        }
    }
}