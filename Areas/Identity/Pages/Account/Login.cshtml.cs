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
                var user = await _userManager.FindByNameAsync(Input.UserNameOrEmail)
                   ?? await _userManager.FindByEmailAsync(Input.UserNameOrEmail);

                // 2. Nếu tìm thấy người dùng, kiểm tra xem họ có bị khóa không
                if (user != null)
                {
                    if (await _userManager.IsLockedOutAsync(user))
                    {
                        _logger.LogWarning($"Tài khoản người dùng '{user.UserName}' đang bị khóa.");
                        // Chuyển hướng đến trang Lockout ngay lập tức
                        return RedirectToPage("./Lockout");
                    }
                }

                if (user.IsApproved == false)
                {
                    _logger.LogWarning($"Người dùng '{user.UserName}' cố gắng đăng nhập nhưng tài khoản chưa được kích hoạt/bị khóa (IsApproved = false).");
                    // Thêm lỗi vào ModelState để hiển thị cho người dùng
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn chưa được kích hoạt hoặc đã bị khóa. Vui lòng liên hệ quản trị viên.");
                    return Page(); // Hiển thị lại trang Login với thông báo lỗi
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.UserNameOrEmail, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // Lấy thông tin người dùng hiện tại
                    user = await _userManager.FindByNameAsync(Input.UserNameOrEmail)
                               ?? await _userManager.FindByEmailAsync(Input.UserNameOrEmail);

                    // Kiểm tra nếu là Admin thì vào Dashboard
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });

                    if (await _userManager.IsInRoleAsync(user, "Author"))
                        return RedirectToAction("Index", "AuthorDashboard", new { area = "Admin" });



                    // Người dùng thường thì về trang chủ
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
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
