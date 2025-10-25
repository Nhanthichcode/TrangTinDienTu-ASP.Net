namespace Trang_tin_điện_tử_mvc.Models
{
    public class ArticleStatSummary
    {
        public int ArticleId { get; set; }
        public string ArticleTitle { get; set; } = string.Empty; // Tiêu đề bài viết
        public long TotalViews { get; set; } // Tổng lượt xem (dùng long)
        public long TotalLikes { get; set; } // Tổng lượt thích (dùng long)
    }
}