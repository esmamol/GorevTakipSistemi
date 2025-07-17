// ViewModels/ResetPasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace GorevTakipSistemi.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Token zorunludur.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı ID zorunludur.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Yeni şifre alanı zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı alanı zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre Tekrar")]
        [Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}