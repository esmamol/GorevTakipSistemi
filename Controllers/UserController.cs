using Microsoft.AspNetCore.Authorization; // Bu using'i ekleyin
using Microsoft.AspNetCore.Mvc;

namespace GorevTakipSistemi.Controllers
{
    [Authorize(Roles = "User")] // Sadece User rolüne sahip kullanıcılar erişebilir
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}