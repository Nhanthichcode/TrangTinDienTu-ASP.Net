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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment webHostEnvironment)
        {
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
                    EmailConfirmed = true // Usually set true for admin-created accounts
                                          // Or implement email confirmation flow if needed
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
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = roles;
            return View(user);
        }

        [Authorize(Policy = "RequireAdminRole")]
        // POST: Users/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string fullName, string role, bool isApproved)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Id không được để trống.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Người dùng không tồn tại.");
            }

            // Cập nhật thông tin cơ bản
            user.FullName = fullName;
            user.IsApproved = isApproved;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewBag.Roles = await _roleManager.Roles.ToListAsync();
                return View(user);
            }

            // Cập nhật role nếu có thay đổi
            var currentRoles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrEmpty(role))
            {
                // Nếu role mới khác với role hiện tại
                if (!currentRoles.Contains(role))
                {
                    // Xóa tất cả role cũ và thêm role mới
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(role));
                    }

                    await _userManager.AddToRoleAsync(user, role);
                }
            }
            else
            {
                // Nếu không chọn role, xóa hết role cũ
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            TempData["Message"] = "Cập nhật người dùng thành công!";
            return RedirectToAction(nameof(Index));
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

            TempData["Message"] = $"Đã mở khóa tài khoản tác giả: {user.Email}";
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

            TempData["Message"] = $"Đã khóa tài khoản tác giả: {user.Email}";
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
