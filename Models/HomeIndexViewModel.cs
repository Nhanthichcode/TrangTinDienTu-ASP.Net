using Trang_tin_điện_tử_mvc.Models;
using X.PagedList;

namespace Trang_tin_điện_tử_mvc.Models
{
    // ViewModel này sẽ chứa tất cả dữ liệu cần thiết cho trang chủ
    public class HomeIndexViewModel
    {
        // 1. Dành cho khu vực "Hero" (Tin nổi bật nhất)
        public Article? TopStory { get; set; }

        // 2. Dành cho các khu vực chuyên mục (Ví dụ)
        public IEnumerable<Article> BusinessStories { get; set; } = new List<Article>();
        public IEnumerable<Article> TechStories { get; set; } = new List<Article>();

        // 3. Danh sách "Tin Mới Nhất" (Phân trang)
        // Đây là danh sách gốc của bạn
        public IPagedList<Article> LatestArticles { get; set; }

        // 4. Dữ liệu cho Sidebar (Giữ nguyên từ code của bạn)
        public IEnumerable<Article> FeaturedArticles { get; set; } = new List<Article>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IDictionary<int, int> CategoryCounts { get; set; } = new Dictionary<int, int>();
        public IEnumerable<Tag> Tags { get; set; } = new List<Tag>();
    }

    // (Bạn cũng có thể tạo một ViewModel riêng cho Sidebar nếu muốn)
    public class SidebarViewModel
    {
        public IEnumerable<Article> Featured { get; set; } = new List<Article>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IDictionary<int, int> CategoryCounts { get; set; } = new Dictionary<int, int>();
        public IEnumerable<Tag> Tags { get; set; } = new List<Tag>();
    }
}