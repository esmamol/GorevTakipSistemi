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

        // --- Şifremi Unuttum Metotları ---

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
       
        public async Task<IActionResult> ForgotPassword(string EmailOrUsername) 
        {
            if (string.IsNullOrEmpty(EmailOrUsername))
            {
                ModelState.AddModelError("", "E-posta adresi veya kullanıcı adı boş olamaz.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == EmailOrUsername || u.Username == EmailOrUsername);

            if (user == null)
            {
                TempData["Message"] = "Şifre sıfırlama linki e-posta adresinize gönderilmiştir.";
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            // Şifre sıfırlama token'ı oluşturma simülasyonu
            var resetToken = Guid.NewGuid().ToString("N").Substring(0, 16); // Basit bir token

            
            TempData["ResetToken"] = resetToken;
            TempData["UserIdForReset"] = user.Id;
            TempData["UserEmailForReset"] = user.Email; 

            TempData["Message"] = "Şifre sıfırlama linki e-posta adresinize gönderilmiştir.";
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous] 
        public IActionResult ForgotPasswordConfirmation()
        {
            ViewData["Title"] = "Şifre Sıfırlama Onayı";
            return View();
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous] 
        public async Task<IActionResult> ResetPassword(string token, int userId)
        {
            // Gerçek uygulamada: Token'ın geçerliliği (varlığı, süresi) burada kontrol edilir.
            // Bu bir simülasyon olduğu için sadece token'ın varlığını ve userId'nin geçerliliğini kontrol ediyoruz.
            // ÖNEMLİ: Gerçek uygulamada token'ı veritabanında saklamalı ve kontrol etmelisiniz.
            if (string.IsNullOrEmpty(token) || userId == 0)
            {
                TempData["ErrorMessage"] = "Geçersiz şifre sıfırlama linki.";
                return RedirectToAction("Login");
            }

           
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı veya geçersiz link.";
                return RedirectToAction("Login");
            }

            ViewData["Title"] = "Şifreyi Sıfırla";
            var model = new ResetPasswordViewModel { Token = token, UserId = userId };
            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(model.UserId);

                if (user == null || string.IsNullOrEmpty(model.Token))
                {
                    ModelState.AddModelError("", "Geçersiz şifre sıfırlama linki veya kullanıcı bulunamadı.");
                    return View(model);
                }

                
                if (!IsPasswordComplex(model.NewPassword))
                {
                    ModelState.AddModelError("NewPassword", "Şifre en az 8 karakter, bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir.");
                    return View(model);
                }

                // Şifreyi güncelle
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword); 

                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Şifreniz başarıyla sıfırlandı. Lütfen yeni şifrenizle giriş yapın.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        private bool IsPasswordComplex(string password)
        {
            // En az 8 karakter, bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            // Özel karakter kontrolü: Harf, rakam veya boşluk olmayan herhangi bir karakter
            if (!password.Any(ch => !char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch))) return false;
            return true;
        }

        // Logout metodu:
        [HttpGet]
        [Authorize] 
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
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
                        new Claim(ClaimTypes.Role, user.Role.ToString()) 
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
                    Role = UserRole.User 
                };

                _context.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kaydınız başarıyla tamamlandı. Şimdi giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            return View(model);
        }



        [AllowAnonymous] 
        public IActionResult AccessDenied()
        {
            return View(); // Views/Account/AccessDenied.cshtml yolundaki View'ı render ederek kullanıcıya yetkisiz erişim mesajını gösterir.
        }
    }
}