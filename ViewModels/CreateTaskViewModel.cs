using System.ComponentModel.DataAnnotations;


namespace GorevTakipSistemi.ViewModels
{
    public class CreateTaskViewModel
    {
        [Required(ErrorMessage = "Başlık boş bırakılamaz.")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir.")]
        [Display(Name = "Görev Başlığı")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Açıklama boş bırakılamaz.")]
        [Display(Name = "Görev Açıklaması")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Atanacak kullanıcı seçilmelidir.")]
        [Display(Name = "Atanan Kullanıcı")]
        public int AssignedUserId { get; set; }

     
    }
}