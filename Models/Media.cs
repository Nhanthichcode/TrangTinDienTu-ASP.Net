using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Trang_tin_điện_tử_mvc.Models
{
    public class Media
    {
        [DisplayName("Mã Media")]
        public int Id { get; set; }

        [DisplayName("Mã bài viết")]
        public int? ArticleId { get; set; }
        public Article? Article { get; set; }

        [DisplayName("Tên file")]
        [Required]
        public string FileName { get; set; } = null!;

        [DisplayName("Đường dẫn file")]
        [Required]
        public string FileUrl { get; set; } = null!; // /uploads/.../abc.jpg

        [DisplayName("Loại file")]
        public string FileType { get; set; } = "image";
        // image / video / audio / file

        [DisplayName("Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DisplayName("Kích thước (KB)")]
        public long FileSizeKB { get; set; }
        public virtual ICollection<ArticleImagePosition> ArticleImagePositions { get; set; } = new List<ArticleImagePosition>();
    }
}
