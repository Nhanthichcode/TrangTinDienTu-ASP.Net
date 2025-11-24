using X.PagedList;

namespace Trang_tin_điện_tử_mvc.Models
{
    // ViewModel này sẽ chứa tất cả dữ liệu cần thiết cho trang chủ
    public class HomeIndexViewModel
    {
            // Các properties hiện tại
            public IPagedList<Article> LatestArticles { get; set; }
            public List<Article> FeaturedArticles { get; set; } // 3 bài cho 3 cột đầu
            public List<Article> SidebarArticles { get; set; } // 5 bài cho sidebar
            public Article TopStory { get; set; }
            public List<Article> BusinessStories { get; set; }
            public List<Article> TechStories { get; set; }
            public List<Article> WorldStories { get; set; }
            public List<Category> Categories { get; set; }
            public Dictionary<int, int> CategoryCounts { get; set; }
            public List<Tag> Tags { get; set; }
        
    }
}