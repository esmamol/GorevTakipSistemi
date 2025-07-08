// ViewModels/LoginViewModel.cs
using System.ComponentModel.DataAnnotations; // DataAnnotations için gerekli

namespace GorevTakipSistemi.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı boş bırakılamaz.")] // Gerekli alan olduğunu belirtir
        [Display(Name = "Kullanıcı Adı")] // Formda gösterilecek etiket
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre boş bırakılamaz.")]
        [DataType(DataType.Password)] // Şifre alanı olarak gizlenmesini sağlar
        [Display(Name = "Şifre")]
        public string Password { get; set; }
    }
}