using Microsoft.EntityFrameworkCore;
using GorevTakipSistemi.Models; 
using System.Threading.Tasks; 

namespace GorevTakipSistemi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<GorevTakipSistemi.Models.Task> Tasks { get; set; }

       

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasDefaultValue(UserRole.User);

            modelBuilder.Entity<GorevTakipSistemi.Models.Task>() 
                .Property(t => t.Status)
                .HasDefaultValue("Beklemede");

            modelBuilder.Entity<GorevTakipSistemi.Models.Task>()
                .HasOne(t => t.AssignedUser)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}

