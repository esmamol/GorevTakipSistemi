using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using GorevTakipSistemi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Linq;
using System.IO;
using System.Security.Claims; 

namespace GorevTakipSistemi.Controllers
{
    public class TaskController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public TaskController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Task (Görev Listeleme)
        public async Task<IActionResult> Index()
        {
            var isAdmin = User.IsInRole(GorevTakipSistemi.Models.UserRole.Admin.ToString());
            IQueryable<GorevTakipSistemi.Models.Task> applicationDbContext = _context.Tasks.Include(t => t.AssignedUser);

            if (!isAdmin)
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdString == null || !int.TryParse(userIdString, out int currentUserId))
                {
                    TempData["ErrorMessage"] = "Kullanıcı bilgisi alınamadı. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Account"); // Ya da boş liste döndür
                }
                applicationDbContext = applicationDbContext.Where(t => t.AssignedUserId == currentUserId);
            }
            return View(await applicationDbContext.ToListAsync());
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

            return View(task);
        }

        // GET: Task/Create (Yeni Görev Oluşturma Formu)
        public IActionResult Create()
        {
            
            if (!User.IsInRole(UserRole.Admin.ToString()))
            {
                TempData["ErrorMessage"] = "Görev oluşturma yetkiniz yok.";
                return Forbid();
            }

            ViewData["AssignedUserId"] = new SelectList(_context.Users, "Id", "Username");
            return View();
        }


        // POST: Task/Create (Yeni Görev Ekleme İşlemi)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken] // CSRF saldırılarına karşı koruma
        public async Task<IActionResult> Create(CreateTaskViewModel viewModel, IFormFile? imageFile) 
        {
            
            ModelState.Remove("AssignedUser");
            ModelState.Remove("Messages");


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

           
            if (!ModelState.IsValid)
            {
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

            
            var task = new GorevTakipSistemi.Models.Task
            {
                Title = viewModel.Title,
                Description = viewModel.Description,
                AssignedUserId = viewModel.AssignedUserId,
                Status = GorevTakipSistemi.Models.TaskStatus.Beklemede.ToString(), 
                CreatedDate = DateTime.Now,
                StartDate = null
            };

            // Görsel yükleme
            if (imageFile != null && imageFile.Length > 0)
            {
                
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
              
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Dosyayı kaydet
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                
                task.ImagePath = "images/" + uniqueFileName;
            }

            _context.Add(task);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Görev başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }



        // GET: Task/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            bool isAdmin = User.IsInRole(UserRole.Admin.ToString());
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi alınamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }
            bool isAssignedUser = (task.AssignedUserId == currentUserId);

            if (!isAdmin && !isAssignedUser)
            {
                TempData["ErrorMessage"] = "Bu görevi düzenlemeye yetkiniz yok.";
                return Forbid();
            }

            ViewData["AssignedUserId"] = new SelectList(_context.Users, "Id", "Username", task.AssignedUserId);
            return View(task);
        }


        // POST: Task/Edit/5 (Görev Düzenleme İşlemi)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Status,AssignedUserId,CreatedDate,StartDate,CompletionDate")] GorevTakipSistemi.Models.Task task, IFormFile? imageFile, bool removeImage)
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

            task.CreatedDate = existingTask.CreatedDate;
            task.StartDate = existingTask.StartDate;
            task.CompletionDate = existingTask.CompletionDate;
            task.ImagePath = existingTask.ImagePath;


            bool isAdmin = User.IsInRole(UserRole.Admin.ToString());
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi alınamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }
            bool isAssignedUser = (existingTask.AssignedUserId == currentUserId);

            // Yetkilendirme kontrolü
            if (!isAdmin && !isAssignedUser)
            {
                TempData["ErrorMessage"] = "Bu görevi düzenlemeye yetkiniz yok.";
                return Forbid();
            }

            string oldStatus = existingTask.Status;
            var updatedTaskData = task; 


            
            if (isAdmin)
            {
                
                existingTask.Title = updatedTaskData.Title;
                existingTask.Description = updatedTaskData.Description;
                existingTask.AssignedUserId = updatedTaskData.AssignedUserId;
               
            }
            else if (isAssignedUser) 
            {
               
                existingTask.Status = updatedTaskData.Status;

              
                if (existingTask.Status == GorevTakipSistemi.Models.TaskStatus.İşlemeAlındı.ToString())
                {
                    existingTask.StartDate = DateTime.Now;
                }
               
                else if (existingTask.Status == GorevTakipSistemi.Models.TaskStatus.Beklemede.ToString())
                {
                    existingTask.StartDate = null;
                }
               
            }


          
            if (isAdmin)
            {
                existingTask.Title = task.Title;
                existingTask.Description = task.Description;
                existingTask.AssignedUserId = task.AssignedUserId;

                string currentExistingImagePath = existingTask.ImagePath;
                if (imageFile != null && imageFile.Length > 0)
                {
                    const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
                    if (imageFile.Length > MaxFileSize)
                    {
                        ModelState.AddModelError("imageFile", "Görsel boyutu 5 MB'ı aşamaz.");
                       
                        TempData["ErrorMessage"] = "Görsel boyutu 5 MB'ı aşıyor.";
                        
                        return RedirectToAction(nameof(Edit), new { id = id });
                    }

                    var AllowedImageMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                    if (!AllowedImageMimeTypes.Contains(imageFile.ContentType))
                    {
                        ModelState.AddModelError("imageFile", "Sadece JPG, PNG veya GIF formatında görseller yüklenebilir.");
                        TempData["ErrorMessage"] = "Geçersiz görsel formatı. Sadece JPG, PNG, GIF desteklenir.";
                        return RedirectToAction(nameof(Edit), new { id = id });
                    }
                }

                if (removeImage && !string.IsNullOrEmpty(existingTask.ImagePath))
                {
                    var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingTask.ImagePath);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                    existingTask.ImagePath = null;
                }
                if (imageFile != null)
                {
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    existingTask.ImagePath = "images/" + uniqueFileName;
                }
                existingTask.StartDate = DateTime.Now;

                _context.Entry(existingTask).State = EntityState.Modified;
            }


            
            if (isAssignedUser && existingTask.Status != oldStatus)
            {
                if (existingTask.Status == GorevTakipSistemi.Models.TaskStatus.Tamamlandı.ToString())
                {
                    if (!existingTask.CompletionDate.HasValue)
                    {
                        existingTask.CompletionDate = DateTime.Now;
                    }
                }
                else if (oldStatus == GorevTakipSistemi.Models.TaskStatus.Tamamlandı.ToString() && existingTask.Status != GorevTakipSistemi.Models.TaskStatus.Tamamlandı.ToString())
                {
                    existingTask.CompletionDate = null;
                }
            }

            try
            {
                _context.Update(existingTask);
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

        // GET: Task/Delete/5
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

            
            if (!User.IsInRole(UserRole.Admin.ToString()))
            {
                TempData["ErrorMessage"] = "Görev silme yetkiniz yok.";
                return Forbid();
            }

            return View(task);
        }

        // POST: Task/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            
            if (!User.IsInRole(UserRole.Admin.ToString()))
            {
                TempData["ErrorMessage"] = "Görev silme yetkiniz yok.";
                return Forbid();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                
                if (!string.IsNullOrEmpty(task.ImagePath))
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", task.ImagePath);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                _context.Tasks.Remove(task);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Görev başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}