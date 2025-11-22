using Microsoft.CodeAnalysis.Differencing;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Trang_tin_điện_tử_mvc.Models
{
    public class Article
    {
        [DisplayName("Mã bài viết")]
        public int Id { get; set; }
        [DisplayName("Tiêu đề")]
        public string Title { get; set; } = null!;
        [DisplayName("Tóm tắt")]
        public string? Summary { get; set; }

        [DisplayName("Nội dung")]
        [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết")]
        public string Content { get; set; } = string.Empty;
        [DisplayName("Ảnh đại diện")]
        public string? ThumbnailUrl { get; set; }
        [DisplayName("Ngày tạo")]
        public DateTime CreatedAt { get; set; }
        [DisplayName("Ngày cập nhật")]
        public DateTime? UpdatedAt { get; set; }
        [DisplayName("Trạng thái")]
        public bool IsApproved { get; set; } = false;
        [DisplayName("Lượt xem")]
        public int ViewCount { get; set; } = 0;

        // Khóa ngoại
        [DisplayName("Tác giả")]
        public string AuthorId { get; set; } = null!;
        public ApplicationUser? Author { get; set; }

        [DisplayName("Danh mục")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Quan hệ
        public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Media> Media { get; set; } = new List<Media>();
        public virtual ICollection<ArticleImagePosition> ArticleImagePositions { get; set; } = new List<ArticleImagePosition>();
    }
}
