using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Trang_tin_điện_tử_mvc.Data;
using Trang_tin_điện_tử_mvc.Models;

namespace Trang_tin_điện_tử_mvc.Controllers
{
    public class MediaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MediaController>
    _logger; // 🎯 Sửa thành MediaController

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
            Index(string? search, int? articleId, int page = 1)
        {
            int pageSize = 12;

            var query = _context.Media
            .Include(m => m.Article)
            .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(m => m.FileName.Contains(search));

            if (articleId.HasValue)
                query = query.Where(m => m.ArticleId == articleId.Value);

            int totalItems = await query.CountAsync();
            var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

            ViewBag.Search = search;
            ViewBag.ArticleId = articleId;
            ViewBag.Articles = new SelectList(_context.Articles, "Id", "Title");
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(items);
        }

        // -----------------------------------------------
        // UPLOAD (Summernote & Multiple files)
        // -----------------------------------------------
        [HttpPost]
        public async Task<IActionResult>
            Upload(IFormFile file, int? articleId)
        {
            _logger.LogInformation("=======<> bắt đầu upload ảnh <>=======");

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest("Định dạng file không được hỗ trợ");

            // Kiểm tra kích thước file (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File quá lớn (tối đa 5MB)");

            string uploadPath = Path.Combine(_env.WebRootPath, "uploads", "medias");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(uploadPath, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string fileUrl = $"/uploads/medias/{fileName}";

                // Lưu vào DB
                var media = new Media
                {
                    FileName = file.FileName,
                    FileUrl = fileUrl,
                    FileType = file.ContentType,
                    FileSizeKB = (int)(file.Length / 1024),
                    CreatedAt = DateTime.Now,
                    ArticleId = articleId == 0 ? null : articleId
                };

                _context.Media.Add(media);
                await _context.SaveChangesAsync();

                _logger.LogInformation("=======<> upload thành công: {FileName} <>=======", fileName);
                return Json(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload file");
                return StatusCode(500, "Lỗi server khi upload file");
            }
        }

        // 🎯 THÊM ACTION UPLOAD NHIỀU ẢNH
        [HttpPost]
        public async Task<IActionResult>
            UploadMultiple(List<IFormFile>
                files, int? articleId)
        {
            if (files == null || !files.Any())
                return BadRequest("No files uploaded");

            var results = new List
            <object>
                ();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    results.Add(new { fileName = file.FileName, success = false, error = "Định dạng không hỗ trợ" });
                    continue;
                }

                if (file.Length > 5 * 1024 * 1024)
                {
                    results.Add(new { fileName = file.FileName, success = false, error = "File quá lớn" });
                    continue;
                }

                try
                {
                    string uploadPath = Path.Combine(_env.WebRootPath, "uploads", "medias");
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    string fileName = $"{Guid.NewGuid()}{fileExtension}";
                    string filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    string fileUrl = $"/uploads/medias/{fileName}";

                    var media = new Media
                    {
                        FileName = file.FileName,
                        FileUrl = fileUrl,
                        FileType = file.ContentType,
                        FileSizeKB = (int)(file.Length / 1024),
                        CreatedAt = DateTime.Now,
                        ArticleId = articleId == 0 ? null : articleId
                    };

                    _context.Media.Add(media);
                    results.Add(new { fileName = file.FileName, success = true, url = fileUrl });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi upload file: {FileName}", file.FileName);
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
        public async Task<IActionResult>
            DeleteFile(int id)
        {
            var media = await _context.Media
            .Include(m => m.ArticleImagePositions)
            .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null) return NotFound();

            try
            {
                // Xóa file vật lý
                string filePath = Path.Combine(_env.WebRootPath, media.FileUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Xóa các ArticleImagePositions liên quan
                if (media.ArticleImagePositions.Any())
                {
                    _context.ArticleImagePositions.RemoveRange(media.ArticleImagePositions);
                }

                // Xóa Media
                _context.Media.Remove(media);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã xóa media: {FileName}", media.FileName);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa media: {MediaId}", id);
                return StatusCode(500, "Lỗi khi xóa file");
            }
        }

        // 🎯 THÊM ACTION LẤY THỐNG KÊ
        [HttpGet]
        public async Task<IActionResult>
            GetStats()
        {
            var totalMedia = await _context.Media.CountAsync();
            var usedMedia = await _context.Media.CountAsync(m => m.ArticleId != null);
            var unusedMedia = await _context.Media.CountAsync(m => m.ArticleId == null);
            var totalSize = await _context.Media.SumAsync(m => m.FileSizeKB) / 1024;

            return Json(new
            {
                totalMedia,
                usedMedia,
                unusedMedia,
                totalSize
            });
        }
    }
}
