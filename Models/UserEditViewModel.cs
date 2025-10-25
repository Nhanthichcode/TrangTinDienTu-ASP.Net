using Microsoft.AspNetCore.Http; // Cho IFormFile
using Microsoft.AspNetCore.Mvc.Rendering; // Cho SelectList (tùy chọn)
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace Trang_tin_điện_tử_mvc.Models
{
    public class UserEditViewModel
    {
        // ID của user cần sửa (ẩn đi trên form)
        public string Id { get; set; }

        // Hiển thị, không cho sửa
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; } // Email có thể cho phép sửa hoặc không

        [StringLength(100)]
        [Display(Name = "Họ và Tên")]
        public string FullName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; } // Cho phép null

        [Display(Name = "Ảnh đại diện hiện tại")]
        public string? ExistingAvatarUrl { get; set; } // Để hiển thị ảnh cũ

        [Display(Name = "Chọn ảnh đại diện mới (để trống nếu không đổi)")]
        public IFormFile? AvatarFile { get; set; } // Dùng IFormFile để tải ảnh mới

        [Required] // Vai trò là bắt buộc
        [Display(Name = "Vai trò")]
        public string SelectedRole { get; set; }

        [Display(Name = "Đã duyệt/Kích hoạt")]
        public bool IsApproved { get; set; }

        // Dùng để tạo dropdown trong View (Controller sẽ gán giá trị)
        public SelectList? RolesList { get; set; }
    }
}
