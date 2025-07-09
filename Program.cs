using Microsoft.EntityFrameworkCore;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder; // WebApplication için

namespace GorevTakipSistemi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // DbContext kaydý
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Kimlik Doðrulama Servislerinin Ayarlanmasý
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login"; // Giriþ sayfasý yolu
                    options.LogoutPath = "/Account/Logout"; // Çýkýþ sayfasý yolu
                    options.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisiz eriþim sayfasý yolu
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Oturum süresi
                    options.SlidingExpiration = true; // Oturumun otomatik uzamasý (eðer çerez yenilemesi isteniyorsa)

                    // options.IsPersistent = false; // BU SATIR YANLIÞTI VE SÝLÝNMELÝDÝR.
                    // Kalýcýlýk AccountController'daki SignInAsync metoduyla yönetilir.
                });

            // Rol Yetkilendirme Servisi (AddAuthorization'dan önce olmalý)
            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Kimlik doðrulama ve yetkilendirme middleware'lerini doðru sýrada ekleyin
            app.UseAuthentication(); // Bu önce gelmeli
            app.UseAuthorization();  // Bu sonra gelmeli

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}"); // Varsayýlan rotayý Login sayfasýna çevrildi

            app.Run();
        }
    }
}