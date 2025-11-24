using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Trang_tin_điện_tử_mvc.Data;
using Trang_tin_điện_tử_mvc.Models;

namespace Trang_tin_điện_tử_mvc.Controllers
{
    public class MediaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MediaController> _logger; 
        int pageSize = 12;

        public MediaController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<MediaController>
            logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // -----------------------------------------------
        // INDEX (Gallery + Search + Filter + Pagination)
        // -----------------------------------------------
        public async Task<IActionResult>
            Index(string? search, string? category, int? articleId, string? status, int page = 1)
        {
            var query = _context.Media
        .Include(m => m.Article)
        .AsNoTracking()
        .Include(m => m.UploadedByUser)
        .AsQueryable();

            // 2. Áp dụng các bộ lọc
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => m.FileName.Contains(search));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(m => m.Category == category);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "used")
                {
                    // Đang dùng: Có ArticleId HOẶC là Avatar
                    query = query.Where(m => m.ArticleId != null || m.Category == "UserAvatar");
                }
                else if (status == "unused")
                {
                    // Chưa dùng: Không có ArticleId VÀ Không phải Avatar
                    query = query.Where(m => m.ArticleId == null && m.Category != "UserAvatar");
                }
            }

            if (articleId.HasValue)
            {
                query = query.Where(m => m.ArticleId == articleId.Value);
            }

            int totalItems = await query.CountAsync();
            var allMediaBaseQuery = _context.Media.AsNoTracking();
            int statsTotalCount = await allMediaBaseQuery.CountAsync();
            int statsUsedCount = await allMediaBaseQuery.CountAsync(m => m.ArticleId != null || m.Category == "UserAvatar");
            int statsUnusedCount = statsTotalCount - statsUsedCount;
            long statsTotalSizeKB = await allMediaBaseQuery.SumAsync(m => (long?)m.FileSizeKB) ?? 0;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

            var pagedItems = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.Media.Select(m => m.Category).Distinct().ToListAsync();
            var vietnameseCategories = new List<SelectListItem>
                {
                    new SelectListItem { Value = "ArticleContent", Text = "Nội dung bài viết" },
                    new SelectListItem { Value = "ArticleThumbnail", Text = "Ảnh đại diện (Thumbnail)" },
                    new SelectListItem { Value = "UserAvatar", Text = "Avatar người dùng" }
                };

            var statusList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "used", Text = "Đang sử dụng" },
                    new SelectListItem { Value = "unused", Text = "Chưa sử dụng (Rác)" }
                };
            
            var articlesQuery = _context.Articles.OrderByDescending(a => a.CreatedAt).Select(a => new { a.Id, a.Title });

            var viewModel = new MediaIndexViewModel
            {
                PagedMediaList = pagedItems,
                TotalMediaCount = statsTotalCount,
                UsedMediaCount = statsUsedCount,
                UnusedMediaCount = statsUnusedCount,
                TotalSizeKB = statsTotalSizeKB,
               
                ArticlesDropdown = new SelectList(articlesQuery, "Id", "Title", articleId),
                CategoriesDropdown = new SelectList(vietnameseCategories, "Value", "Text", category), // Dùng list mới tạo
                StatusDropdown = new SelectList(statusList, "Value", "Text", status), // Dropdown trạng thái
              
                CurrentPage = page,
                TotalPages = totalPages,
                CurrentSearch = search,
                CurrentArticleId = articleId,
                CurrentCategory = category,
                CurrentStatus = status
            };

            return View(viewModel);
        }

        // -----------------------------------------------
        // UPLOAD (Summernote & Multiple files)
        // -----------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, int? articleId, string category = "ArticleContent")
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            // Validate file ... (giữ nguyên logic check đuôi file và size của bạn)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension)) return BadRequest("Định dạng file không hỗ trợ");
            if (file.Length > 5 * 1024 * 1024) return BadRequest("File quá lớn (>5MB)");

            // 1. Xác định thư mục dựa trên Category
            string folderName = "medias"; // Mặc định
            if (category == "UserAvatar") folderName = "avatars";
            else if (category == "ArticleThumbnail") folderName = "thumbnails";
            else if (category == "ArticleContent") folderName = "content";

            string uploadPath = Path.Combine(_env.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            // 2. Lưu file
            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(uploadPath, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string fileUrl = $"/uploads/{folderName}/{fileName}";
                string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // 3. Lưu DB với thông tin đầy đủ
                var media = new Media
                {
                    FileName = file.FileName,
                    FileUrl = fileUrl,
                    FileType = file.ContentType,
                    FileSizeKB = (int)(file.Length / 1024),
                    CreatedAt = DateTime.Now,
                    ArticleId = articleId == 0 ? null : articleId,
                    Category = category, // Lưu loại ảnh
                    UploadedByUserId = currentUserId // Lưu người upload
                };

                _context.Media.Add(media);
                await _context.SaveChangesAsync();

                return Json(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi upload");
                return StatusCode(500, "Lỗi server");
            }
        }

        // 🎯 THÊM ACTION UPLOAD NHIỀU ẢNH
        [HttpPost]
        public async Task<IActionResult> UploadMultiple(List<IFormFile> files, int? articleId, string category = "ArticleContent")
        {
            if (files == null || !files.Any()) return BadRequest("No files uploaded");

            var results = new List<object>();
            string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Xác định thư mục (như hàm trên)
            string folderName = "medias";
            if (category == "ArticleThumbnail") folderName = "thumbnails";
            else if (category == "ArticleContent") folderName = "content";

            string uploadPath = Path.Combine(_env.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                try
                {
                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    string fileUrl = $"/uploads/{folderName}/{fileName}";
                    var media = new Media
                    {
                        FileName = file.FileName,
                        FileUrl = fileUrl,
                        FileType = file.ContentType,
                        FileSizeKB = (int)(file.Length / 1024),
                        CreatedAt = DateTime.Now,
                        ArticleId = articleId == 0 ? null : articleId,
                        Category = category,
                        UploadedByUserId = currentUserId
                    };

                    _context.Media.Add(media);
                    results.Add(new { fileName = file.FileName, success = true, url = fileUrl });
                }
                catch (Exception ex)
                {
                    results.Add(new { fileName = file.FileName, success = false, error = "Lỗi server" });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { results });
        }
      
        // -----------------------------------------------
        // BROWSER PICKER (Popup chọn ảnh)
        // -----------------------------------------------
        public async Task
        <IActionResult>
            Browser()
        {
            var files = await _context.Media
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

            return PartialView("_MediaBrowser", files);
        }

        // -----------------------------------------------
        // DELETE (xóa file vật lý + DB)
        // -----------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var media = await _context.Media
                .Include(m => m.ArticleImagePositions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null) return NotFound();

            if (media.Category == "UserAvatar")
            {
                var isUsed = await _context.Users.AnyAsync(u => u.AvatarUrl == media.FileUrl);
                if (isUsed) return BadRequest("Ảnh này đang được dùng làm Avatar, không thể xóa!");
            }


            try
            {
                // Xóa file vật lý
                string filePath = Path.Combine(_env.WebRootPath, media.FileUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

                // Xóa ArticleImagePositions
                if (media.ArticleImagePositions.Any()) _context.ArticleImagePositions.RemoveRange(media.ArticleImagePositions);

                _context.Media.Remove(media);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa media {Id}", id);
                return StatusCode(500, "Lỗi hệ thống");
            }
        }


        // -----------------------------------------------
        // DELETE ALL UNUSED (QUAN TRỌNG: ĐÃ SỬA LOGIC)
        // -----------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DeleteAllUnused()
        {
            _logger.LogInformation("Bắt đầu xóa ảnh rác...");

            // QUAN TRỌNG: Chỉ xóa ảnh CONTENT hoặc THUMBNAIL mà không có ArticleId.
            // TUYỆT ĐỐI KHÔNG XÓA "UserAvatar" vì Avatar luôn có ArticleId = null.

            var unusedMediaList = await _context.Media
                .Where(m => m.ArticleId == null && m.Category != "UserAvatar") // <--- ĐIỀU KIỆN QUAN TRỌNG
                .ToListAsync();

            if (!unusedMediaList.Any())
                return Json(new { success = false, message = "Không tìm thấy ảnh rác nào (đã bỏ qua Avatar người dùng)." });

            int successCount = 0;
            int failCount = 0;
            var mediaToDeleteFromDb = new List<Media>();

            foreach (var media in unusedMediaList)
            {
                try
                {
                    string filePath = Path.Combine(_env.WebRootPath, media.FileUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

                    mediaToDeleteFromDb.Add(media);
                    successCount++;
                }
                catch (Exception)
                {
                    failCount++;
                }
            }

            if (mediaToDeleteFromDb.Any())
            {
                _context.Media.RemoveRange(mediaToDeleteFromDb);
                await _context.SaveChangesAsync();

                return Json(new { success = true, count = successCount, message = $"Đã xóa {successCount} ảnh rác. (Bỏ qua {failCount} file lỗi)." });
            }

            return Json(new { success = false, message = "Không xóa được file nào." });
        }

        //Gắn ảnh
        [HttpPost]
        public async Task<IActionResult> AttachToArticle(int mediaId, int articleId, string attachmentType)
        {
            var media = await _context.Media.FindAsync(mediaId);
            if (media == null) return NotFound("Không tìm thấy file media.");

            var article = await _context.Articles.FindAsync(articleId);
            if (article == null) return NotFound("Không tìm thấy bài viết.");

            // Cập nhật media hiện tại
            media.ArticleId = articleId;
            media.Category = attachmentType;
            if (attachmentType == "ArticleThumbnail")
            {
                article.ThumbnailUrl = media.FileUrl;
                var oldThumbnailMedia = await _context.Media
                    .Where(m => m.ArticleId == articleId && m.Category == "ArticleThumbnail" && m.Id != mediaId)
                    .FirstOrDefaultAsync();

                if (oldThumbnailMedia != null)
                {
                    bool isUsedInContent = !string.IsNullOrEmpty(article.Content) &&
                                           article.Content.Contains(oldThumbnailMedia.FileUrl);

                    if (isUsedInContent)
                    {
                        oldThumbnailMedia.Category = "ArticleContent";
                    }
                    else
                    {
                        string filePath = Path.Combine(_env.WebRootPath, oldThumbnailMedia.FileUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }

                        // 2. Xóa trong DB
                        _context.Media.Remove(oldThumbnailMedia);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
}
