using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using GorevTakipSistemi.ViewModels; 
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering; 

namespace GorevTakipSistemi.Controllers
{
    
    [Authorize]
    public class TaskController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TaskController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                
                var allTasks = await _context.Tasks.Include(t => t.AssignedUser).ToListAsync();
                return View(allTasks);
            }
            else 
            {
                
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int userId))
                {
                   
                    return RedirectToAction("Login", "Account");
                }

                // Sadece kullanıcıya atanan görevleri ve atandıkları kullanıcı bilgileriyle birlikte getir
                var userTasks = await _context.Tasks
                                            .Where(t => t.AssignedUserId == userId)
                                            .Include(t => t.AssignedUser)
                                            .ToListAsync();
                return View(userTasks);
            }
        }

        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
           
            ViewBag.Users = new SelectList(await _context.Users.ToListAsync(), "Id", "Username");
            return View();
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            if (ModelState.IsValid)
            {
                var task = new Models.Task // Models.Task kullanın çünkü "Task" hem model adı hem de Task sınıfı adı
                {
                    Title = model.Title,
                    Description = model.Description,
                    AssignedUserId = model.AssignedUserId,
                    Status = "Beklemede" 
                };

                _context.Add(task);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Görev başarıyla eklendi.";
                return RedirectToAction(nameof(Index)); 
            }

            
            ViewBag.Users = new SelectList(await _context.Users.ToListAsync(), "Id", "Username", model.AssignedUserId);
            return View(model);
        }

       
    }
}