using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;

namespace GorevTakipSistemi.Controllers
{
    [Authorize(Roles = "Admin")] 
    public class AdminController : Controller
    {
        public IActionResult Index() // Views/Admin/Index.cshtml yolundaki View'ı arar ve tarayıcıya HTML olarak gönderir.
        {
            return View();
        }
    }
}