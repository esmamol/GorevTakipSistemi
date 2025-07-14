using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models; 
using GorevTakipSistemi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Claims;
using System.IO; // Path ve FileStream için eklendi 

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

        // GET: Task
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi alınamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole(UserRole.Admin.ToString()))
            {
                var allTasks = await _context.Tasks.Include(t => t.AssignedUser).ToListAsync();
                return View(allTasks);
            }
            else 
            {
                var userTasks = await _context.Tasks
                                                    .Where(t => t.AssignedUserId == currentUserId)
                                                    .Include(t => t.AssignedUser)
                                                    .ToListAsync();
                return View(userTasks);
            }
        }

        // GET: Task/Create (Yeni Görev Ekleme Formu)
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Create()
        {
            var users = await _context.Users.ToListAsync();
            ViewData["AssignedUserId"] = new SelectList(users, "Id", "Username");
            return View();
        }

        // POST: Task/Create (Yeni Görev Ekleme İşlemi)
        [HttpPost]
        [Authorize(Roles = "Admin")] 
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı koruma
        public async Task<IActionResult> Create(CreateTaskViewModel viewModel, IFormFile imageFile)
        {
            ModelState.Remove("AssignedUser");
            ModelState.Remove("Messages"); 

            if (ModelState.IsValid)
            {
                if (viewModel.AssignedUserId == 0)
                {
                    ModelState.AddModelError("AssignedUserId", "Atanacak kullanıcı seçimi zorunludur.");
                }
                else
                {
                    var userExists = await _context.Users.AnyAsync(u => u.Id == viewModel.AssignedUserId);
                    if (!userExists)
                    {
                        ModelState.AddModelError("AssignedUserId", "Seçilen kullanıcı bulunamadı.");
                    }
                }

                if (ModelState.IsValid)
                {
                    var task = new GorevTakipSistemi.Models.Task
                    {
                        Title = viewModel.Title,
                        Description = viewModel.Description,
                        AssignedUserId = viewModel.AssignedUserId,
                        Status = "Beklemede", 
                        CreatedDate = DateTime.Now,
                        LastUpdatedDate = DateTime.Now 
                    };

                    
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                        var extension = Path.GetExtension(imageFile.FileName);
                        task.ImagePath = fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }
                    }

                    _context.Add(task);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Görev başarıyla eklendi.";
                    return RedirectToAction(nameof(Index));
                }
            }

            StringBuilder errorMessages = new StringBuilder();
            errorMessages.AppendLine("Görev eklenemedi. Lütfen hataları düzeltin:");
            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    if (!string.IsNullOrEmpty(error.ErrorMessage))
                    {
                        errorMessages.AppendLine($"- {error.ErrorMessage}");
                    }
                    else if (error.Exception != null)
                    {
                        errorMessages.AppendLine($"- Bir hata oluştu: {error.Exception.Message}");
                    }
                }
            }
            TempData["ErrorMessage"] = errorMessages.ToString();

            var users = await _context.Users.ToListAsync();
            ViewData["AssignedUserId"] = new SelectList(users, "Id", "Username", viewModel.AssignedUserId);
            return View(viewModel);
        }

        // GET: Task/Details/5 (Görev Detayları)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            if (User.IsInRole(UserRole.User.ToString()))
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int currentUserId) || task.AssignedUserId != currentUserId)
                {
                    TempData["ErrorMessage"] = "Bu görevin detaylarını görüntülemeye yetkiniz yok.";
                    return Forbid();
                }
            }

            return View(task);
        }

        // GET: Task/Edit/5 (Görev Düzenleme Formu)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.Include(t => t.AssignedUser).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            if (User.IsInRole(UserRole.User.ToString()))
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int currentUserId) || task.AssignedUserId != currentUserId)
                {
                    TempData["ErrorMessage"] = "Bu görevi düzenlemeye yetkiniz yok.";
                    return Forbid();
                }
            }

            var users = await _context.Users.ToListAsync();
            ViewData["AssignedUserId"] = new SelectList(users, "Id", "Username", task.AssignedUserId);
            return View(task);
        }

        // POST: Task/Edit/5 (Görev Düzenleme İşlemi)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Status,AssignedUserId,CreatedDate,LastUpdatedDate,CompletionDate,ImagePath")] GorevTakipSistemi.Models.Task task, IFormFile imageFile) // IFormFile eklendi
        {
            if (id != task.Id)
            {
                TempData["ErrorMessage"] = "Geçersiz görev kimliği.";
                return NotFound();
            }

            var existingTask = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (existingTask == null)
            {
                TempData["ErrorMessage"] = "Güncellenmek istenen görev bulunamadı.";
                return NotFound();
            }

            // Yetki kontrolü için kullanıcı rolü ve atanan kullanıcı bilgileri
            bool isAdmin = User.IsInRole(UserRole.Admin.ToString());
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi alınamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }
            bool isAssignedUser = (existingTask.AssignedUserId == currentUserId);

           
            if (!isAdmin && !isAssignedUser)
            {
                TempData["ErrorMessage"] = "Bu görevi düzenlemeye yetkiniz yok.";
                return Forbid();
            }
            
            
            ModelState.Clear();

            var taskToUpdate = new GorevTakipSistemi.Models.Task();
            _context.Entry(taskToUpdate).CurrentValues.SetValues(existingTask); 

           
            if (isAssignedUser && !isAdmin)
            {
              
                task.ImagePath = existingTask.ImagePath; 
        
            }
            
            else if (isAdmin)
            {
               
                task.Status = existingTask.Status;
                task.CreatedDate = existingTask.CreatedDate; 
            }

          
            if (string.IsNullOrEmpty(task.Title) && isAdmin)
            {
                ModelState.AddModelError("Title", "Başlık alanı boş olamaz.");
            }
            if (string.IsNullOrEmpty(task.Description) && isAdmin)
            {
                ModelState.AddModelError("Description", "Açıklama alanı boş olamaz.");
            }
            if (task.AssignedUserId == 0 && isAdmin)
            {
                 ModelState.AddModelError("AssignedUserId", "Atanacak kullanıcı seçimi zorunludur.");
            }
            else if (isAdmin) 
            {
                var selectedUserExists = await _context.Users.AnyAsync(u => u.Id == task.AssignedUserId);
                if (!selectedUserExists)
                {
                    ModelState.AddModelError("AssignedUserId", "Seçilen kullanıcı bulunamadı.");
                }
            }


            if (ModelState.IsValid)
            {
                try
                {
                    
                    _context.Entry(existingTask).CurrentValues.SetValues(task); 

                   
                    existingTask.LastUpdatedDate = DateTime.Now;

             
                    if (isAssignedUser && !isAdmin) 
                    {
                        
                        _context.Entry(existingTask).Property(t => t.Status).IsModified = true;
                        _context.Entry(existingTask).Property(t => t.CompletionDate).IsModified = true;
                     
                        _context.Entry(existingTask).Property(t => t.Title).IsModified = false;
                        _context.Entry(existingTask).Property(t => t.Description).IsModified = false;
                        _context.Entry(existingTask).Property(t => t.AssignedUserId).IsModified = false;
                        _context.Entry(existingTask).Property(t => t.CreatedDate).IsModified = false; 
                        _context.Entry(existingTask).Property(t => t.ImagePath).IsModified = false;
                    }
                    else if (isAdmin) 
                    {
                       
                        _context.Entry(existingTask).Property(t => t.Title).IsModified = true;
                        _context.Entry(existingTask).Property(t => t.Description).IsModified = true;
                        _context.Entry(existingTask).Property(t => t.AssignedUserId).IsModified = true;
                        _context.Entry(existingTask).Property(t => t.Status).IsModified = false; 
                        _context.Entry(existingTask).Property(t => t.CompletionDate).IsModified = true;
                        _context.Entry(existingTask).Property(t => t.CreatedDate).IsModified = false; 
                        
                      
                        if (imageFile != null && imageFile.Length > 0)
                        {
                          
                            if (!string.IsNullOrEmpty(existingTask.ImagePath))
                            {
                                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", existingTask.ImagePath);
                                if (System.IO.File.Exists(oldImagePath))
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                            }

                            var fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                            var extension = Path.GetExtension(imageFile.FileName);
                            existingTask.ImagePath = fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(stream);
                            }
                            _context.Entry(existingTask).Property(t => t.ImagePath).IsModified = true;
                        }
                        else
                        {
                           
                             _context.Entry(existingTask).Property(t => t.ImagePath).IsModified = false;
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Görev başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(existingTask.Id))
                    {
                        TempData["ErrorMessage"] = "Görev veritabanında bulunamadı. Başka bir kullanıcı tarafından silinmiş olabilir.";
                        return NotFound();
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Görev güncellenirken bir çakışma oluştu. Lütfen tekrar deneyin.";
                        var users = await _context.Users.ToListAsync();
                        ViewData["AssignedUserId"] = new SelectList(users, "Id", "Username", existingTask.AssignedUserId);
                        return View(existingTask);
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Görev güncellenirken bir hata oluştu: {ex.Message}";
                    var users = await _context.Users.ToListAsync();
                    ViewData["AssignedUserId"] = new SelectList(users, "Id", "Username", existingTask.AssignedUserId);
                    return View(existingTask);
                }
            }

           
            StringBuilder errorMessages = new StringBuilder();
            errorMessages.AppendLine("Görev güncellenemedi. Lütfen hataları düzeltin:");
            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    if (!string.IsNullOrEmpty(error.ErrorMessage))
                    {
                        errorMessages.AppendLine($"- {error.ErrorMessage}");
                    }
                    else if (error.Exception != null)
                    {
                        errorMessages.AppendLine($"- {error.Exception.Message}");
                    }
                }
            }
            TempData["ErrorMessage"] = errorMessages.ToString();
            var usersList = await _context.Users.ToListAsync();
            ViewData["AssignedUserId"] = new SelectList(usersList, "Id", "Username", task.AssignedUserId);
           
            task.Status = existingTask.Status;
            task.AssignedUserId = existingTask.AssignedUserId;
            task.ImagePath = existingTask.ImagePath; 
            return View(task); 
        }


        // GET: Task/Delete/5 (Görev Silme Onay Sayfası)
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Task/Delete/5 (Görev Silme İşlemi)
        [HttpPost, ActionName("Delete")] 
        [ValidateAntiForgeryToken] 
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
               
                var messages = await _context.Messages.Where(m => m.TaskId == id).ToListAsync();
                _context.Messages.RemoveRange(messages);

                if (!string.IsNullOrEmpty(task.ImagePath))
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", task.ImagePath);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Tasks.Remove(task);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Görev başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        // Görevin veritabanında olup olmadığını kontrol eder
        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}