using Microsoft.EntityFrameworkCore;
using GorevTakipSistemi.Data;
using Microsoft.AspNetCore.Authentication.Cookies;


namespace GorevTakipSistemi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();


            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Uygulamaya kimlik do�rulama hizmetlerini ekler ve varsay�lan kimlik do�rulama �emas�n�n �erez tabanl� kimlik do�rulama olaca��n� belirtir.
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                    options.SlidingExpiration = true;


                });

            // Uygulamaya yetkilendirme servislerini ekler. 
            builder.Services.AddAuthorization();

            var app = builder.Build();


            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");

                app.UseHsts();
                
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(); 

            app.UseRouting();

            app.UseAuthentication(); // Bu �nce gelmeli
            app.UseAuthorization();  // Bu sonra gelmeli

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}