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

        // İlişkiler
        public int TaskId { get; set; }
        [ForeignKey("TaskId")]
        public Task Task { get; set; }

        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public User Sender { get; set; }
    }
}