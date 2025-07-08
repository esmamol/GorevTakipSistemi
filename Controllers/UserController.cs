using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; 
namespace GorevTakipSistemi.Controllers
{

    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        // GET: /User/Index
        public IActionResult Index()
        {
            
            ViewData["Username"] = User.Identity.Name;

            return View();
        }
    }
}