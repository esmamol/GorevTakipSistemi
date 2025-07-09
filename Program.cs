using Microsoft.EntityFrameworkCore;
using GorevTakipSistemi.Data;
using GorevTakipSistemi.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder; // WebApplication i�in

namespace GorevTakipSistemi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // DbContext kayd�
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Kimlik Do�rulama Servislerinin Ayarlanmas�
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login"; // Giri� sayfas� yolu
                    options.LogoutPath = "/Account/Logout"; // ��k�� sayfas� yolu
                    options.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisiz eri�im sayfas� yolu
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Oturum s�resi
                    options.SlidingExpiration = true; // Oturumun otomatik uzamas� (e�er �erez yenilemesi isteniyorsa)

                    // options.IsPersistent = false; // BU SATIR YANLI�TI VE S�L�NMEL�D�R.
                    // Kal�c�l�k AccountController'daki SignInAsync metoduyla y�netilir.
                });

            // Rol Yetkilendirme Servisi (AddAuthorization'dan �nce olmal�)
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

            // Kimlik do�rulama ve yetkilendirme middleware'lerini do�ru s�rada ekleyin
            app.UseAuthentication(); // Bu �nce gelmeli
            app.UseAuthorization();  // Bu sonra gelmeli

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}"); // Varsay�lan rotay� Login sayfas�na �evrildi

            app.Run();
        }
    }
}