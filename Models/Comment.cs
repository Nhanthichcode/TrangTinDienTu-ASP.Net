using System.ComponentModel;
using System.ComponentModel.DataAnnotations; 
using System.ComponentModel.DataAnnotations.Schema;
namespace Trang_tin_điện_tử_mvc.Models
{
    public class Comment
    {
        [DisplayName("Mã bình luận")]
        public int Id { get; set; }

        [DisplayName("Nội dung")]
        [Required(ErrorMessage = "Vui lòng nhập nội dung bình luận")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Bình luận phải từ 1 đến 1000 ký tự")]
        public string Content { get; set; } = string.Empty; // <-- Dùng string.Empty an toàn hơn

        [DisplayName("Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DisplayName("Được duyệt")]
        public bool IsApproved { get; set; } = true;

        [DisplayName("Bình luận cha")]
        public int? ParentCommentId { get; set; } // <-- (1)

        [ForeignKey("ParentCommentId")]
        public Comment? ParentComment { get; set; }

        // Khóa ngoại
        [Required] 
        public string UserId { get; set; } = null!;
        public ApplicationUser? User { get; set; }

        [Required] 
        public int ArticleId { get; set; }
        public Article? Article { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}