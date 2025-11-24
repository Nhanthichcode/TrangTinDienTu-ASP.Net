namespace Trang_tin_điện_tử_mvc.Models
{
    public class MediaIndexViewModel
    {
        // Danh sách media đã phân trang (Dùng IEnumerable hoặc IPagedList tùy bạn)
        public IEnumerable<Media> PagedMediaList { get; set; }

        // Các số liệu thống kê toàn cục
        public int TotalMediaCount { get; set; }
        public int UsedMediaCount { get; set; }
        public int UnusedMediaCount { get; set; }
        public long TotalSizeKB { get; set; } // Dùng long để tránh tràn số nếu dung lượng lớn

        // Các thông tin hỗ trợ cho View (dropdown, phân trang)
        public Microsoft.AspNetCore.Mvc.Rendering.SelectList ArticlesDropdown { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string CurrentSearch { get; set; }
        public int? CurrentArticleId { get; set; }
    }
}
