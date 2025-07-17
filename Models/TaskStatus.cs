using System.ComponentModel.DataAnnotations; // Display attribute'ı için gerekli

namespace GorevTakipSistemi.Models 
{
    public enum TaskStatus
    {
        Beklemede,
        [Display(Name = "İşleme Alındı")]
        İşlemeAlındı,
        Tamamlandı
    }
}