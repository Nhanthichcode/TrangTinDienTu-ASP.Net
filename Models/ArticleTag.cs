using System.ComponentModel;

namespace Trang_tin_điện_tử_mvc.Models
{
    public class ArticleTag
    {
        [DisplayName("Mã bài viết")]
        public int ArticleId { get; set; }
        [DisplayName("Bài viết")]
        public Article? Article { get; set; }
        [DisplayName("Mã thẻ")]
        public int TagId { get; set; }
        [DisplayName("Thẻ")]
        public Tag? Tag { get; set; }
    }
}
