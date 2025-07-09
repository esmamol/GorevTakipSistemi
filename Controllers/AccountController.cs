using Microsoft.AspNetCore.Mvc;
using GorevTakipSistemi.Models; // User ve UserRole enum'u için
using GorevTakipSistemi.ViewModels;
using GorevTakipSistemi.Data;
using BCrypt.Net; // BCrypt kullanımı için
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // [Authorize] ve [AllowAnonymous] için
using System.Linq; // Rol yönlendirmesi için User.IsInRole() kullanımı

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
        [AllowAnonymous] // Giriş yapmadan erişilebilir
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous] // Giriş yapmadan erişilebilir
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kullanıcıyı doğrudan veritabanından çek (UserRole navigasyon özelliğine ihtiyaç yok)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

                // Şifre doğrulamasını BCrypt ile yap
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
                        // ExpireUtc sadece IsPersistent true ise anlamlıdır.
                        // Eğer RememberMe false ise, tarayıcı kapanınca çerez silinir.
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : (DateTimeOffset?)null
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Rolüne göre yönlendirme (User.IsInRole kullanımı)
                    if (User.IsInRole(UserRole.Admin.ToString())) // Enum değerini string'e çevirerek kontrol et
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (User.IsInRole(UserRole.User.ToString())) // Enum değerini string'e çevirerek kontrol et
                    {
                        return RedirectToAction("Index", "User");
                    }
                    // Eğer tanımsız bir rol varsa veya eşleşmezse Home'a yönlendirilebilir
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous] // Giriş yapmadan erişilebilir
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous] // Giriş yapmadan erişilebilir
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kullanıcı adı (Username) zaten kullanımda mı kontrolü
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanımda.");
                    return View(model);
                }

                // E-posta adresi (Email) boş değilse ve zaten kullanımda mı kontrolü
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
                    Role = UserRole.User // Enum değeri doğrudan atanır
                };

                _context.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kaydınız başarıyla tamamlandı. Şimdi giriş yapabilirsiniz.";
                return RedirectToAction("Login"); // Kayıt sonrası Login sayfasına yönlendir
            }
            return View(model);
        }

        [HttpGet]
        [Authorize] // Çıkış yapmak için giriş yapmış olmak gerekir
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account"); // Çıkış sonrası Login sayfasına yönlendir
        }

        [AllowAnonymous] // Yetkisiz erişim sayfasını görmek için giriş yapmış olmak gerekmez
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}