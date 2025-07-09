using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;

namespace GorevTakipSistemi.Controllers
{
    [Authorize(Roles = "Admin")] // Sadece Admin rolüne sahip kullanıcılar erişebilir
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}