using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using GorevTakipSistemi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding; // <-- Bu satırı eklediğinizden emin olun!

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
            // Kullanıcının kimliğini ve rolünü doğru bir şekilde alalım
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int currentUserId))
            {
                // Kullanıcı ID'si alınamazsa, bu bir yetkilendirme sorunudur veya oturum açmamıştır.
                TempData["ErrorMessage"] = "Kullanıcı bilgisi alınamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole(GorevTakipSistemi.Models.UserRole.Admin.ToString()))
            {
                var allTasks = await _context.Tasks.Include(t => t.AssignedUser).ToListAsync();
                return View(allTasks);
            }
            else // User rolündeki kullanıcılar
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
        public async Task<IActionResult> Create() // async olarak değiştirildi
        {
            var users = await _context.Users.ToListAsync(); // async olarak çek
            ViewData["AssignedUserId"] = new SelectList(users, "Id", "Username");
            return View();
        }

        // POST: Task/Create (Yeni Görev Ekleme İşlemi)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskViewModel viewModel)
        {
            // ModelState.Remove, AssignedUser/AssignedUserId için oluşabilecek gereksiz validasyon hatalarını bypass eder.
            // Özellikle CreateTaskViewModel'de [Required] olmamasına rağmen EF Core'dan gelen bir sıkıntı olabilir.
            ModelState.Remove("AssignedUser"); // Eğer modelinizde AssignedUser navigasyon özelliği varsa
            // ModelState.Remove("AssignedUserId"); // Eğer AssignedUserId için özel bir validasyon hatası gelirse açılabilir

            if (ModelState.IsValid)
            {
                // Seçilen AssignedUserId'nin geçerli olup olmadığını manuel kontrol edelim
                if (viewModel.AssignedUserId == 0) // Eğer dropdown'dan "-- Kullanıcı Seçin --" seçildiyse (value="0")
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

                if (ModelState.IsValid) // Tekrar kontrol et, manuel hata eklendiyse
                {
                    var task = new GorevTakipSistemi.Models.Task
                    {
                        Title = viewModel.Title,
                        Description = viewModel.Description,
                        AssignedUserId = viewModel.AssignedUserId,
                        Status = "Beklemede"
                    };

                    _context.Add(task);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Görev başarıyla eklendi.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // ModelState.IsValid false ise (ilk validasyonda veya manuel eklenen hata ile)
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

        // GET: Task/Details/5
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

            if (User.IsInRole(GorevTakipSistemi.Models.UserRole.User.ToString()))
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int currentUserId) || task.AssignedUserId != currentUserId)
                {
                    return Forbid();
                }
            }

            return View(task);
        }

        // GET: Task/Edit/5
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

            if (User.IsInRole(GorevTakipSistemi.Models.UserRole.User.ToString()))
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int currentUserId) || task.AssignedUserId != currentUserId)
                {
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
        public async Task<IActionResult> Edit(int id, GorevTakipSistemi.Models.Task task)
        {
            if (id != task.Id)
            {
                return NotFound();
            }

            // existingTask'i veritabanından çekelim, AsNoTracking ile takip edilmesini engelleyelim
            // Böylece manuel olarak Attach edip değişiklikleri kontrol edebiliriz.
            var existingTask = await _context.Tasks.Include(t => t.AssignedUser).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (existingTask == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            if (User.IsInRole(GorevTakipSistemi.Models.UserRole.User.ToString()))
            {
                var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdString, out int currentUserId) || existingTask.AssignedUserId != currentUserId)
                {
                    return Forbid();
                }

                // User rolündeyse, sadece Status'u güncelleyebilir.
                // Model state'i Status dışındaki alanlar için temizleyelim, gereksiz hataları engeller.
                ModelState.Remove("Title");
                ModelState.Remove("Description");
                ModelState.Remove("AssignedUserId");
                ModelState.Remove("AssignedUser");

                // Sadece Status alanının validasyonunu kontrol et
                if (!ModelState.ContainsKey("Status") || ModelState["Status"].Errors.Count == 0)
                {
                    // ExistingTask'i takip etmeye başla
                    _context.Attach(existingTask);
                    // Sadece Status özelliğini değiştirildi olarak işaretle
                    _context.Entry(existingTask).Property(t => t.Status).IsModified = true;

                    // Diğer alanların orijinal değerlerini korumak için IsModified = false yap
                    _context.Entry(existingTask).Property(t => t.Title).IsModified = false;
                    _context.Entry(existingTask).Property(t => t.Description).IsModified = false;
                    _context.Entry(existingTask).Property(t => t.AssignedUserId).IsModified = false;

                    // Formdan gelen Status değerini ata
                    existingTask.Status = task.Status;

                    try
                    {
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Görev başarıyla güncellendi.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!TaskExists(existingTask.Id))
                        {
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
                        TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                        var users = await _context.Users.ToListAsync();
                        ViewData["AssignedUserId"] = new SelectList(users, "Id", "Username", existingTask.AssignedUserId);
                        return View(existingTask);
                    }
                }
                else // Status validasyonu başarısız olursa
                {
                    StringBuilder errorMessages = new StringBuilder();
                    errorMessages.AppendLine("Güncelleme başarısız oldu. Lütfen hataları düzeltin:");
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
                    ViewData["AssignedUserId"] = new SelectList(users, "Id", "Username", existingTask.AssignedUserId);
                    return View(existingTask);
                }
            }
            else if (User.IsInRole(GorevTakipSistemi.Models.UserRole.Admin.ToString()))
            {
                // Admin rolündeyse, tüm alanları güncelleyebilir.
                // ModelState'i AssignedUser navigasyon özelliği hatasından temizleyelim.
                ModelState.Remove("AssignedUser");

                // Formdan gelen 'task' nesnesinin ID'sini mevcut görev ID'siyle eşleştir
                task.Id = existingTask.Id; // Güvenlik için ID'nin doğru olduğundan emin olalım

                // DbContext'e 'task' nesnesini Attach et
                // Bu nesne formdan geldiği için yeni bir nesne olarak algılanabilir.
                _context.Attach(task);

                // Entity'nin durumunu Modified olarak ayarla. Bu, EF'nin tüm alanlarda değişiklik aramasını sağlar.
                // Bu satır, task objesinin tüm propertylerinin güncellendiğini EF'e bildirir.
                _context.Entry(task).State = EntityState.Modified;

                // AssignedUserId için manuel doğrulama yapalım
                if (task.AssignedUserId == 0) // Formdan gelen task.AssignedUserId kontrol ediyoruz
                {
                    ModelState.AddModelError("AssignedUserId", "Atanacak kullanıcı seçimi zorunludur.");
                }
                else
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
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Görev başarıyla güncellendi.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!TaskExists(existingTask.Id))
                        {
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
                        TempData["ErrorMessage"] = $"Bir hata oluştu: {ex.Message}";
                        var users = await _context.Users.ToListAsync();
                        ViewData["AssignedUserId"] = new SelectList(users, "Id", "Username", existingTask.AssignedUserId);
                        return View(existingTask);
                    }
                }
                else // Admin rolünde ModelState.IsValid false ise
                {
                    StringBuilder errorMessages = new StringBuilder();
                    errorMessages.AppendLine("Güncelleme başarısız oldu. Lütfen hataları düzeltin:");
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
                    ViewData["AssignedUserId"] = new SelectList(users, "Id", "Username", existingTask.AssignedUserId);
                    return View(existingTask); // Hata durumunda existingTask'i gönderiyoruz, çünkü bu, veritabanındaki son durumdu.
                }
            }

            var allUsers = await _context.Users.ToListAsync();
            ViewData["AssignedUserId"] = new SelectList(allUsers, "Id", "Username", existingTask.AssignedUserId);
            return View(existingTask);
        }

        // GET: Task/Delete/5
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

        // POST: Task/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Görev başarıyla silindi.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}