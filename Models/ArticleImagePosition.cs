namespace Trang_tin_điện_tử_mvc.Models
{
    public class ArticleImagePosition
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public int MediaId { get; set; }
        public int PositionIndex { get; set; } // Thứ tự ảnh trong content
        public string Placeholder { get; set; } // Chuỗi thay thế, ví dụ: "{image0}"

        public Article Article { get; set; }
        public Media Media { get; set; }
    }
}
