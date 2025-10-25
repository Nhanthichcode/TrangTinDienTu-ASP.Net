using System.ComponentModel;

namespace Trang_tin_điện_tử_mvc.Models
{
    public class Tag
    {
        [DisplayName("Mã thẻ")]
        public int Id { get; set; }
        [DisplayName("Tên thẻ")]
        public string Name { get; set; } = null!;
        [DisplayName("Đường dẫn tĩnh")]
        public string? Slug { get; set; }

        // Quan hệ
        public ICollection<ArticleTag>? ArticleTags { get; set; }
    }
}
