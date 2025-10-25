using System.ComponentModel;

namespace Trang_tin_điện_tử_mvc.Models
{
    public class Category
    {
        [DisplayName("Mã danh mục")]
        public int Id { get; set; }
        [DisplayName ("Tên danh mục")]    
        public string Name { get; set; } = null!;
        [DisplayName("Đường dẫn tĩnh")]
        public string? Slug { get; set; }
        [DisplayName("Mô tả")]
        public string? Description { get; set; }

        // Quan hệ
        public ICollection<Article>? Articles { get; set; }
    }
}
