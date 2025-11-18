using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Trang_tin_điện_tử_mvc.Models
{
    public class ArticleCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200, MinimumLength = 10, ErrorMessage = "Tiêu đề phải dài từ 10 đến 200 ký tự")]
        [DisplayName("Tiêu đề")]
        public string Title { get; set; }

        [StringLength(500, ErrorMessage = "Tóm tắt không được vượt quá 500 ký tự")]
        [DisplayName("Tóm tắt")]
        public string? Summary { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [DisplayName("Nội dung")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [DisplayName("Danh mục")]
        public int CategoryId { get; set; }
    }
}
