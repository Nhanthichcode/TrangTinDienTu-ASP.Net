// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Trang_tin_điện_tử_mvc.Models;

namespace Trang_tin_điện_tử_mvc.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
         SignInManager<ApplicationUser> signInManager,
         UserManager<ApplicationUser> userManager,
         ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]            
            [Display(Name = "Email hoặc Tên đăng nhập")]
            public string UserNameOrEmail { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // 1. Tìm người dùng theo UserName hoặc Email
                var user = await _userManager.FindByNameAsync(Input.UserNameOrEmail)
                    ?? await _userManager.FindByEmailAsync(Input.UserNameOrEmail);

                // Nếu tìm thấy người dùng thì mới thực hiện các kiểm tra tiếp theo
                if (user != null)
                {
                    // 2. Kiểm tra xem họ có bị khóa không
                    if (await _userManager.IsLockedOutAsync(user))
                    {
                        _logger.LogWarning($"Tài khoản người dùng '{user.UserName}' đang bị khóa.");
                        return RedirectToPage("./Lockout");
                    }

                    // 3. Kiểm tra xem tài khoản có được duyệt không (IsApproved)
                    // Đã an toàn để truy cập user.IsApproved vì user != null
                    if (user.IsApproved == false)
                    {
                        _logger.LogWarning($"Người dùng '{user.UserName}' cố gắng đăng nhập nhưng tài khoản chưa được kích hoạt/bị khóa (IsApproved = false).");
                        ModelState.AddModelError(string.Empty, "Tài khoản của bạn chưa được kích hoạt hoặc đã bị khóa. Vui lòng liên hệ quản trị viên.");
                        return Page();
                    }
                }

                // 4. Thử đăng nhập (PasswordSignInAsync tự xử lý việc user null hoặc sai password)
                // Lưu ý: PasswordSignInAsync sử dụng UserName để đăng nhập, không phải Email.
                // Nếu Input.UserNameOrEmail là Email, bạn cần dùng user.UserName (nếu user != null) hoặc để SignInManager tự xử lý.
                // Cách tốt nhất khi cho phép đăng nhập bằng cả hai là:
                var userNameToSignIn = user?.UserName ?? Input.UserNameOrEmail;

                var result = await _signInManager.PasswordSignInAsync(userNameToSignIn, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // User đã đăng nhập thành công, chắc chắn user != null.
                    // Không cần tìm lại user nữa.

                    // Kiểm tra vai trò để chuyển hướng
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });

                    if (await _userManager.IsInRoleAsync(user, "Author"))
                        return RedirectToAction("Index", "AuthorDashboard", new { area = "Admin" });

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    // Đăng nhập thất bại (sai username/password hoặc user không tồn tại)
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
