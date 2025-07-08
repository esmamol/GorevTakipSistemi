// ViewModels/RegisterViewModel.cs
using System.ComponentModel.DataAnnotations; // DataAnnotations için gerekli

namespace GorevTakipSistemi.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı boş bırakılamaz.")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre boş bırakılamaz.")]
        [StringLength(100, ErrorMessage = "{0} en az {2} ve en fazla {1} karakter uzunluğunda olmalıdır.", MinimumLength = 6)] // Şifre uzunluğu kısıtlaması
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Şifreyi Onayla")]
        [Compare("Password", ErrorMessage = "Şifre ve onay şifresi eşleşmiyor.")] // Şifrelerin eşleştiğini kontrol eder
        public string ConfirmPassword { get; set; }
    }
}