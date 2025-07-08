using System.ComponentModel.DataAnnotations;

namespace GorevTakipSistemi.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı (Ad Soyad) zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Kullanıcı adı en az 6, en fazla 100 karakter olmalıdır.")]
        [Display(Name = "Adınız ve Soyadınız")]
        public string Username { get; set; }

        
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(100)]
        [Display(Name = "E-posta Adresiniz (İsteğe Bağlı)")]
        public string? Email { get; set; } 

        
        [Required(ErrorMessage = "Telefon numarası zorunludur.")] 
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [StringLength(11)]
        [Display(Name = "Telefon Numaranız")] 
        public string PhoneNumber { get; set; } 

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Şifre Tekrar")]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string ConfirmPassword { get; set; }
    }
}