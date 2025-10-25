using System.ComponentModel.DataAnnotations;

namespace Trang_tin_điện_tử_mvc.Models // Adjust namespace if needed
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
        [Display(Name = "Ảnh đại diện")]
        public IFormFile? AvatarFile { get; set; }
        [Display(Name = "Họ và tên")]
        [StringLength(100)]
        public string? FullName { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Vai trò")]
        public string? SelectedRole { get; set; } // To capture the selected role name

        [Display(Name = "Kích hoạt")]
        public bool IsApproved { get; set; } = true; // Default to approved/active
    }
}