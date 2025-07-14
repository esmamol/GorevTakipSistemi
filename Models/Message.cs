using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GorevTakipSistemi.Models
{
    public class Message
    {
        public int Id { get; set; }

        [StringLength(1000, ErrorMessage = "Mesaj içeriği en fazla 1000 karakter olabilir.")]
        public string Content { get; set; } 

        public DateTime SentDate { get; set; } = DateTime.Now;

        
        public int TaskId { get; set; }
        [ForeignKey("TaskId")]
        public Task Task { get; set; } // Mesajın ait olduğu görev

        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public User Sender { get; set; } // Mesajı gönderen kullanıcı
    }
}