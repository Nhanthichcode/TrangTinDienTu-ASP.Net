using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Trang_tin_điện_tử_mvc.Data;
using Trang_tin_điện_tử_mvc.Models;
using Microsoft.AspNetCore.Authorization;

namespace Trang_tin_điện_tử_mvc.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment webHostEnvironment, ApplicationDbContext context)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "Chưa có vai trò";
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // GET: Users/Create
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> Create()
        {
            // Pass available roles to the view for selection
            ViewBag.RolesList = new SelectList(await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(), "Name", "Name");
            var model = new UserCreateViewModel(); // Pass an empty model
            return View(model);
        }

        // POST: Users/Create
        [HttpPost]
        [Authorize(Policy = "RequireAdminRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Map ViewModel to ApplicationUser
                var user = new ApplicationUser
                {
                    UserName = model.Email, // Use Email as UserName by default
                    Email = model.Email,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth,
                    IsApproved = model.IsApproved,
                    EmailConfirmed = true 
                };

                string? avatarUrl = null; // Biến tạm để lưu đường dẫn
                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    // 1. Xác định thư mục lưu trữ
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                    Directory.CreateDirectory(uploadsFolder); // Tạo thư mục nếu chưa có

                    // 2. Tạo tên file duy nhất (để tránh trùng lặp)
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AvatarFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // 3. Lưu file ảnh vào thư mục
                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.AvatarFile.CopyToAsync(fileStream);
                        }
                        // 4. Lưu đường dẫn tương đối vào biến
                        avatarUrl = "/uploads/avatars/" + uniqueFileName;
                    }
                    catch (Exception ex)
                    {
                        // Ghi log lỗi hoặc xử lý lỗi lưu file
                        ModelState.AddModelError("AvatarFile", $"Lỗi khi lưu ảnh: {ex.Message}");
                        // Tải lại RolesList và trả về View
                        ViewBag.RolesList = new SelectList(await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(), "Name", "Name", model.SelectedRole);
                        return View(model);
                    }
                }
                user.AvatarUrl = avatarUrl; // Gán đường dẫn đã lưu (hoặc null nếu không có ảnh)
                // Attempt to create the user with the provided password
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // If a role was selected, assign it
                    if (!string.IsNullOrEmpty(model.SelectedRole))
                    {
                        // Double-check the role exists before assigning
                        if (await _roleManager.RoleExistsAsync(model.SelectedRole))
                        {
                            await _userManager.AddToRoleAsync(user, model.SelectedRole);
                        }
                        else
                        {
                            // Optional: Handle if the selected role somehow doesn't exist
                            ModelState.AddModelError("SelectedRole", $"Vai trò '{model.SelectedRole}' không tồn tại.");
                            // Need to reload roles before returning the view
                            ViewBag.RolesList = new SelectList(await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(), "Name", "Name", model.SelectedRole);
                            return View(model);
                        }
                    }

                    TempData["Message"] = "Tạo người dùng mới thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else // If user creation failed (e.g., duplicate email/username, password complexity)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            // If ModelState is invalid or creation failed, redisplay the form with errors
            ViewBag.RolesList = new SelectList(await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(), "Name", "Name", model.SelectedRole);
            return View(model);
        }

        // GET: Users/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;

            return View(user);
        }

        [Authorize(Policy = "RequireAdminRole")]
        // GET: Users/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound($"Không tìm thấy người dùng với ID '{id}'.");

            var userRoles = await _userManager.GetRolesAsync(user);
            var currentUserRole = userRoles.FirstOrDefault();
            var allRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

            // Tạo ViewModel và đổ dữ liệu từ user
            var viewModel = new UserEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth,
                ExistingAvatarUrl = user.AvatarUrl, // Lấy URL ảnh hiện tại
                IsApproved = user.IsApproved,
                SelectedRole = currentUserRole, // Gán vai trò hiện tại
                                                // Tạo SelectList ngay trong ViewModel (hoặc vẫn dùng ViewBag)
                RolesList = new SelectList(allRoles, "Name", "Name", currentUserRole)
            };

            return View(viewModel); // Trả về View với ViewModel        }
        }
         
        [Authorize(Policy = "RequireAdminRole")]
        // POST: Users/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel viewModel)
        {
            var user = await _userManager.FindByIdAsync(viewModel.Id);
            if (user == null)
            {
                return NotFound($"Không tìm thấy người dùng với ID '{viewModel.Id}'.");
            }

            // Cập nhật RolesList để hiển thị lại form nếu có lỗi validation
            var allRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            viewModel.RolesList = new SelectList(allRoles, "Name", "Name", viewModel.SelectedRole);
            // Giữ lại ảnh hiện tại để hiển thị lại nếu lỗi
            viewModel.ExistingAvatarUrl = user.AvatarUrl;


            if (ModelState.IsValid)
            {
                bool hasChanges = false; // Cờ kiểm tra xem có thay đổi gì không

                // Cập nhật thông tin cơ bản
                if (user.FullName != viewModel.FullName) { user.FullName = viewModel.FullName; hasChanges = true; }
                if (user.DateOfBirth != viewModel.DateOfBirth) { user.DateOfBirth = viewModel.DateOfBirth; hasChanges = true; }
                if (user.IsApproved != viewModel.IsApproved) { user.IsApproved = viewModel.IsApproved; hasChanges = true; }
                // Cập nhật Email (nếu cho phép và có thay đổi) - Cần xử lý xác thực email mới nếu cần
                if (user.Email != viewModel.Email)
                {
                    var setEmailResult = await _userManager.SetEmailAsync(user, viewModel.Email);
                    if (!setEmailResult.Succeeded) { /* Xử lý lỗi */ ModelState.AddModelError("", "Lỗi khi cập nhật Email."); return View(viewModel); }
                    // Có thể cần SetuserName trùng Email nếu bạn dùng Email làm username
                    var setuserNameResult = await _userManager.SetUserNameAsync(user, viewModel.Email);
                    if (!setuserNameResult.Succeeded) { /* Xử lý lỗi */ ModelState.AddModelError("", "Lỗi khi cập nhật userName."); return View(viewModel); }
                    hasChanges = true;
                }


                // Xử lý Upload Ảnh đại diện mới
                if (viewModel.AvatarFile != null && viewModel.AvatarFile.Length > 0)
                {
                    // Xóa ảnh cũ (nếu có và không phải ảnh mặc định)
                    if (!string.IsNullOrEmpty(user.AvatarUrl) && !user.AvatarUrl.EndsWith("default-images.png")) // Thay tên ảnh mặc định nếu khác
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            try { System.IO.File.Delete(oldImagePath); }
                            catch (IOException ex) { } // Cần inject ILogger
                        }
                    }

                    // Lưu ảnh mới
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string uploadsFolder = Path.Combine(wwwRootPath, "uploads", "avatars"); // Thư mục lưu avatars
                    Directory.CreateDirectory(uploadsFolder); // Tạo thư mục nếu chưa có
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(viewModel.AvatarFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await viewModel.AvatarFile.CopyToAsync(fileStream);
                        }
                        user.AvatarUrl = "/uploads/avatars/" + uniqueFileName; // Lưu đường dẫn tương đối
                        hasChanges = true;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("AvatarFile", "Không thể lưu ảnh đại diện.");
                        return View(viewModel);
                    }
                }

                // Xử lý thay đổi Vai trò
                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentRole = currentRoles.FirstOrDefault();
                if (currentRole != viewModel.SelectedRole)
                {
                    if (!string.IsNullOrEmpty(currentRole)) // Xóa vai trò cũ nếu có
                    {
                        var removeResult = await _userManager.RemoveFromRoleAsync(user, currentRole);
                        if (!removeResult.Succeeded) { /* Xử lý lỗi */ ModelState.AddModelError("", "Lỗi khi xóa vai trò cũ."); return View(viewModel); }
                    }
                    if (!string.IsNullOrEmpty(viewModel.SelectedRole)) // Thêm vai trò mới nếu có chọn
                    {
                        var addResult = await _userManager.AddToRoleAsync(user, viewModel.SelectedRole);
                        if (!addResult.Succeeded) { /* Xử lý lỗi */ ModelState.AddModelError("", $"Lỗi khi thêm vai trò '{viewModel.SelectedRole}'."); return View(viewModel); }
                    }
                    hasChanges = true;
                }


                // Chỉ gọi UpdateAsync nếu thực sự có thay đổi
                if (hasChanges)
                {
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (updateResult.Succeeded)
                    {
                        TempData["SuccessMessage"] = $"Đã cập nhật thông tin người dùng '{user.UserName}' thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        // Thêm lỗi vào ModelState nếu Update thất bại
                        foreach (var error in updateResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    TempData["InfoMessage"] = "Không có thay đổi nào được thực hiện.";
                    return RedirectToAction(nameof(Index));
                }

            } // Kết thúc if (ModelState.IsValid)

            // Nếu ModelState không hợp lệ, hiển thị lại form với lỗi và dữ liệu đã nhập
            return View(viewModel);
        }
        [Authorize(Policy = "RequireAdminRole")]
        // POST: Users/Approve/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsApproved = true;
            await _userManager.UpdateAsync(user);

            TempData["Message"] = $"Đã mở khóa tài khoản: {user.Email}";
            return RedirectToAction(nameof(Index));
        }

        // POST: Users/Unapprove/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unapprove(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsApproved = false;
            await _userManager.UpdateAsync(user);

            TempData["Message"] = $"Đã khóa tài khoản: {user.Email}";
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [Authorize(Policy = "RequireAdminRole")]
        // POST: Users/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                TempData["Message"] = "Đã xóa người dùng thành công.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
