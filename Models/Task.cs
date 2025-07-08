namespace GorevTakipSistemi.Models
{
    public class Task
    {
        public int Id { get; set; } 
        public string Title { get; set; }
        public string Description { get; set; } // Görev açıklaması
        public string Status { get; set; } // Görev durumu (Beklemede, İşleme Alındı, Tamamlandı)

        public int AssignedUserId { get; set; } // Görevin atandığı kullanıcının ID'si


        public User AssignedUser { get; set; }
    }
}