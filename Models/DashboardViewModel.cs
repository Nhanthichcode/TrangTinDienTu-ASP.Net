namespace Trang_tin_điện_tử_mvc.Models 
{
    public class DashboardViewModel
    {
        public long TotalViewsToday { get; set; }
        public long TotalLikesToday { get; set; }
        public long TotalViewsThisWeek { get; set; }
        public long TotalLikesThisWeek { get; set; }
        public long TotalViewsThisMonth { get; set; }
        public long TotalLikesThisMonth { get; set; }
        public long TotalViewsAllTime { get; set; }
        public long TotalLikesAllTime { get; set; }

        public List<ArticleStatSummary> TopViewedArticlesWeek { get; set; } = new List<ArticleStatSummary>();
        public List<ArticleStatSummary> TopLikedArticlesWeek { get; set; } = new List<ArticleStatSummary>();
        public List<ArticleStatSummary> TopViewedArticlesMonth { get; set; } = new List<ArticleStatSummary>();
        public List<ArticleStatSummary> TopLikedArticlesMonth { get; set; } = new List<ArticleStatSummary>();

        // Thêm các dữ liệu khác nếu cần (ví dụ: dữ liệu cho biểu đồ nhỏ)
    }
}