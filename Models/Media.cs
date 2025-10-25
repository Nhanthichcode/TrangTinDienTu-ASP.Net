using System.ComponentModel;

namespace Trang_tin_điện_tử_mvc.Models
{
    public class Media
    {
        [DisplayName("Mã phương tiện")]
        public int Id { get; set; }
        [DisplayName("Đường dẫn tệp")]
        public string FilePath { get; set; } = null!;
        [DisplayName("Chú thích")]
        public string? Caption { get; set; }
        [DisplayName("Ngày tải lên")]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Khóa ngoại
        public int ArticleId { get; set; }
        public Article? Article { get; set; } 
    }
}
