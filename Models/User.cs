using GorevTakipSistemi.Models;

namespace GorevTakipSistemi.Models
{
    public class User
    {
        public int Id { get; set; } 
        public string Username { get; set; }
        public string PasswordHash { get; set; } 
        public UserRole Role { get; set; } // Kullanıcı rolü (Admin/User - artık enum)

       
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}