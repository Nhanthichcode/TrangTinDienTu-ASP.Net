using Microsoft.AspNetCore.Identity;
using System.ComponentModel;

namespace Trang_tin_điện_tử_mvc.Models
{
    public class ApplicationUser : IdentityUser
    {
        [DisplayName("Họ và tên")]
        public string? FullName { get; set; }
        [DisplayName("Đã kích hoạt")]
        public bool IsApproved { get; set; } = true;
        [DisplayName("Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }
        [DisplayName("Ảnh đại diện")]
        public string? AvatarUrl { get; set; }

        // Quan hệ
        public ICollection<Article> Articles { get; set; } = new List<Article>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}