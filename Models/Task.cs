using System;
using System.Collections.Generic;  
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipSistemi.Models
{
    public class Task
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Başlık alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Durum alanı zorunludur.")]
        public string Status { get; set; } // Beklemede, İşleme Alındı, Tamamlandı 

        [Display(Name = "Atanan Kullanıcı")]
        public int AssignedUserId { get; set; }
        [ForeignKey("AssignedUserId")]
        public User AssignedUser { get; set; }

        
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now; 

        [Display(Name = "Başlanma Tarihi")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Tamamlanma Tarihi")]
        public DateTime? CompletionDate { get; set; }

        [Display(Name = "Görsel Yolu")]
        public string? ImagePath { get; set; }


        
       
    }
}