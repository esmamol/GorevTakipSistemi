using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
namespace GorevTakipSistemi.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı (Ad Soyad) boş bırakılamaz.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Kullanıcı adı en az 6, en fazla 100 karakter olmalıdır.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre alanı boş bırakılamaz.")]
        public string PasswordHash { get; set; }


        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(100)]
        public string? Email { get; set; } 

       
        [Required(ErrorMessage = "Telefon numarası boş bırakılamaz.")] 
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [StringLength(11)] 
        public string PhoneNumber { get; set; } 

        [Required(ErrorMessage = "Rol bilgisi boş bırakılamaz.")]
        public UserRole Role { get; set; }

        public ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}