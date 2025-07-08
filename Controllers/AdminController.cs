using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; 

namespace GorevTakipSistemi.Controllers
{
    
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        // GET: /Admin/Index
        public IActionResult Index()
        {
            
            ViewData["Username"] = User.Identity.Name;
            return View();
        }

        
    }
}