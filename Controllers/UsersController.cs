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
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager, 
            IWebHostEnvironment webHostEnvironment, 
            ApplicationDbContext context, 
            SignInManager<ApplicationUser> signInManager,
            ILogger<UsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: Users
        [Authorize(Policy = "Freedom")]
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

        private async Task<string> SaveMediaAsync(IFormFile file, string folder, string category, string userId)
        {
            // 1. Upload file vật lý
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var filePath = Path.Combine(path, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/{folder}/{fileName}";

            // 2. Lưu vào bảng Media
            var media = new Media
            {
                FileName = fileName,
                FileUrl = fileUrl,
                FileType = "image",
                FileSizeKB = file.Length / 1024,
                CreatedAt = DateTime.Now,
                // QUAN TRỌNG: Phân loại
                Category = category, // Ví dụ: "UserAvatar"
                UploadedByUserId = userId,
                ArticleId = null // Avatar không thuộc bài viết nào
            };

            _context.Add(media);
            await _context.SaveChangesAsync(); // Lưu Media để lấy ID nếu cần

            return fileUrl; // Trả về đường dẫn để gán vào User.AvatarUrl
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
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth,
                    IsApproved = model.IsApproved,
                    EmailConfirmed = true,
                };

                // Biến lưu tạm đường dẫn file để xóa nếu tạo user thất bại
                string? uploadedFilePath = null;
                Media? avatarMedia = null;

                // 1. XỬ LÝ UPLOAD FILE VẬT LÝ (Chưa lưu vào DB Media vội)
                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    // Kiểm tra đuôi file
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("AvatarFile", "Định dạng ảnh không hợp lệ.");
                        await LoadRolesList(model.SelectedRole);
                        return View(model);
                    }

                    // Upload file lên ổ cứng
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                    Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AvatarFile.FileName);
                    uploadedFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(uploadedFilePath, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(fileStream);
                    }

                    // Gán đường dẫn string cho User ngay để hiển thị (nếu cần)
                    user.AvatarUrl = "/uploads/avatars/" + uniqueFileName;
                }

                // 2. TẠO USER TRONG DB
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // 3. NẾU TẠO USER THÀNH CÔNG -> MỚI TẠO MEDIA RECORD
                    // Lúc này User đã có trong DB, nên gán UploadedByUserId sẽ không bị lỗi FK
                    if (model.AvatarFile != null && uploadedFilePath != null)
                    {
                        avatarMedia = new Media
                        {
                            FileName = Path.GetFileName(uploadedFilePath),
                            FileUrl = user.AvatarUrl, // Lấy lại URL đã gán
                            FileType = model.AvatarFile.ContentType,
                            FileSizeKB = model.AvatarFile.Length / 1024,
                            CreatedAt = DateTime.Now,
                            Category = "UserAvatar", // Phân loại
                            UploadedByUserId = user.Id, // ID này giờ đã hợp lệ
                            ArticleId = null
                        };

                        _context.Media.Add(avatarMedia);
                        await _context.SaveChangesAsync();
                        
                    }

                    // Gán Role
                    if (!string.IsNullOrEmpty(model.SelectedRole))
                    {
                        if (await _roleManager.RoleExistsAsync(model.SelectedRole))
                        {
                            await _userManager.AddToRoleAsync(user, model.SelectedRole);
                        }
                    }

                    TempData["Message"] = "Tạo người dùng mới thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // 4. NẾU TẠO USER THẤT BẠI -> XÓA FILE VẬT LÝ
                    // Không cần xóa Media trong DB vì bước trên chưa chạy đến đoạn lưu DB Media
                    if (uploadedFilePath != null && System.IO.File.Exists(uploadedFilePath))
                    {
                        System.IO.File.Delete(uploadedFilePath);
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            await LoadRolesList(model.SelectedRole);
            return View(model);
        }
        private async Task LoadRolesList(string? selectedRole = null)
        {
            ViewBag.RolesList = new SelectList(await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(), "Name", "Name", selectedRole);
        }

        // GET: Users/Details/{id}        
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound($"Không tìm thấy người dùng với ID '{id}'.");

            var userRoles = await _userManager.GetRolesAsync(user);
            var roleName = userRoles.FirstOrDefault() ?? "Không có";
            ViewBag.UserRole = roleName;

            if (roleName == "Author")
            {
                var authorArticles = await _context.Articles
                    .Where(a => a.AuthorId == id)
                    //.Include( a=> a.IsApproved)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                ViewBag.AuthorArticles = authorArticles;
            }

            return View(user);
        }

        // GET: Users/Edit/{id}
        [Authorize(Policy = "Freedom")]       
        public async Task<IActionResult> Edit(string id)
        {

            if (string.IsNullOrEmpty(id)) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            // Chỉ Admin hoặc chính chủ sở hữu mới được xem Details
            if (!isAdmin && id != currentUserId)
            {
                return Forbid(); // Lỗi 403 Cấm
            }
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

        //POST: Users/Edit/{id}
        [Authorize(Policy = "Freedom")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel viewModel)
        {
            var user = await _userManager.FindByIdAsync(viewModel.Id);
            if (user == null)
            {
                return NotFound($"Không tìm thấy người dùng với ID '{viewModel.Id}'.");
            }

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && user.Id != currentUserId)
            {
                return Forbid();
            }

            // Load lại dữ liệu cần thiết cho View nếu bị lỗi
            var allRoles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            viewModel.RolesList = new SelectList(allRoles, "Name", "Name", viewModel.SelectedRole);
            viewModel.ExistingAvatarUrl = user.AvatarUrl;

            // --- LOGIC GÁN LẠI DỮ LIỆU BỊ ẨN KHI KHÔNG PHẢI ADMIN ---
            if (!isAdmin)
            {
                viewModel.Email = user.Email;
                viewModel.SelectedRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
                ModelState.Remove("SelectedRole");
                ModelState.Remove("IsApproved");
                viewModel.IsApproved = user.IsApproved;
            }

            if (ModelState.IsValid)
            {
                bool hasChanges = false;

                // Cập nhật thông tin cơ bản
                if (user.FullName != viewModel.FullName) { user.FullName = viewModel.FullName; hasChanges = true; }
                if (user.DateOfBirth != viewModel.DateOfBirth) { user.DateOfBirth = viewModel.DateOfBirth; hasChanges = true; }

                // --- XỬ LÝ UPLOAD ẢNH (REFACTOR CHO MEDIA) ---
                if (viewModel.AvatarFile != null && viewModel.AvatarFile.Length > 0)
                {
                    try
                    {
                        // A. TÌM VÀ XÓA AVATAR CŨ (Cả File và DB Media)
                        // Tìm record Media đang là Avatar của user này
                        var oldAvatarMedia = await _context.Media
                            .FirstOrDefaultAsync(m => m.UploadedByUserId == user.Id && m.Category == "UserAvatar");

                        if (oldAvatarMedia != null)
                        {
                            // Xóa file vật lý
                            var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, oldAvatarMedia.FileUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);

                            // Xóa record trong DB
                            _context.Media.Remove(oldAvatarMedia);
                        }
                        // (Backup) Xóa file cũ nếu user chưa có record trong Media (dữ liệu cũ)
                        else if (!string.IsNullOrEmpty(user.AvatarUrl) && !user.AvatarUrl.Contains("default"))
                        {
                            var oldPathLegacy = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldPathLegacy)) System.IO.File.Delete(oldPathLegacy);
                        }

                        // B. TẠO AVATAR MỚI
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = $"{Guid.NewGuid()}_{viewModel.AvatarFile.FileName}";
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await viewModel.AvatarFile.CopyToAsync(fileStream);
                        }

                        string newFileUrl = $"/uploads/avatars/{uniqueFileName}";

                        // Tạo record Media
                        var newMedia = new Media
                        {
                            FileName = uniqueFileName,
                            FileUrl = newFileUrl,
                            Category = "UserAvatar", // Đánh dấu loại
                            FileType = viewModel.AvatarFile.ContentType,
                            FileSizeKB = viewModel.AvatarFile.Length / 1024,
                            UploadedByUserId = user.Id, // Quan trọng: Gán chủ sở hữu
                            CreatedAt = DateTime.Now,
                            ArticleId = null
                        };

                        _context.Media.Add(newMedia); // Thêm vào Context

                        // Cập nhật User
                        user.AvatarUrl = newFileUrl;
                        // Nếu User có trường AvatarMediaId, bạn có thể gán sau khi SaveChanges hoặc để EF tự map nếu có quan hệ
                        // user.AvatarMedia = newMedia; 

                        hasChanges = true;
                    }
                    catch (Exception ex)
                    {
                        // Log error here
                        ModelState.AddModelError("AvatarFile", "Lỗi khi xử lý ảnh đại diện.");
                        return View(viewModel);
                    }
                }

                // --- KHỐI LOGIC ADMIN (Giữ nguyên) ---
                if (isAdmin)
                {
                    if (user.IsApproved != viewModel.IsApproved) { user.IsApproved = viewModel.IsApproved; hasChanges = true; }

                    if (user.Email != viewModel.Email)
                    {
                        var setEmailResult = await _userManager.SetEmailAsync(user, viewModel.Email);
                        if (!setEmailResult.Succeeded) { ModelState.AddModelError("", "Lỗi cập nhật Email."); return View(viewModel); }
                        await _userManager.SetUserNameAsync(user, viewModel.Email);
                        hasChanges = true;
                    }

                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var currentRole = currentRoles.FirstOrDefault();
                    if (currentRole != viewModel.SelectedRole)
                    {
                        if (!string.IsNullOrEmpty(currentRole)) await _userManager.RemoveFromRoleAsync(user, currentRole);
                        if (!string.IsNullOrEmpty(viewModel.SelectedRole)) await _userManager.AddToRoleAsync(user, viewModel.SelectedRole);
                        hasChanges = true;
                    }
                }

                // --- LƯU THAY ĐỔI ---
                if (hasChanges)
                {
                    // UpdateAsync chỉ cập nhật bảng User. 
                    // Cần SaveChangesAsync của _context để lưu bảng Media vừa Add/Remove ở trên.

                    var updateResult = await _userManager.UpdateAsync(user); // Lưu User
                    await _context.SaveChangesAsync(); // Lưu Media (quan trọng!)

                    if (updateResult.Succeeded)
                    {
                        TempData["SuccessMessage"] = $"Cập nhật thành công!";
                        if (!isAdmin) return RedirectToAction(nameof(Details), new { id = user.Id });
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        foreach (var error in updateResult.Errors) ModelState.AddModelError("", error.Description);
                    }
                }
                else
                {
                    TempData["InfoMessage"] = "Không có thay đổi nào.";
                    if (!isAdmin) return RedirectToAction(nameof(Details), new { id = user.Id });
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(viewModel);
        }
        // POST: Users/Approve/{id}
        [Authorize(Policy = "RequireAdminRole")]      
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
