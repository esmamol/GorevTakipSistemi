using Microsoft.EntityFrameworkCore;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using Microsoft.AspNetCore.Authentication.Cookies; 

namespace GorevTakipSistemi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            // KÝMLÝK DOÐRULAMA SERVÝSLERÝ
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login"; 
                    options.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisiz eriþim sayfasý 
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Oturum süresi
                    options.SlidingExpiration = true; // Oturumun otomatik uzamasý
                });

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


            var app = builder.Build();

           
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            
            app.UseAuthentication(); //Önce kimlik doðrulamasý
            app.UseAuthorization();  //Sonra Yetkilendirme

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}