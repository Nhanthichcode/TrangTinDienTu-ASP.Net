using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Trang_tin_điện_tử_mvc.Data;
using Trang_tin_điện_tử_mvc.Models;
using X.PagedList.EF;
using X.PagedList;
using System.IO;
using System.Text.RegularExpressions;

namespace Trang_tin_điện_tử_mvc.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ArticlesController> _logger;
        private const int DefaultPageSize = 6;

        public ArticlesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, ILogger<ArticlesController> logger)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Articles (Index - Search - Filter)
        public async Task<IActionResult> Index(
            string searchString,
            int pageNumber = 1,
            string filterAuthor = "",
            string filterStatus = "",
            int? filterCategory = null,
            string filterTag = ""
        )
        {
            int pageSize = 6;
            var articlesQuery = _context.Articles
                .AsNoTracking()
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
                .AsSplitQuery()
                .AsQueryable();

            // --- 1. LOGIC PHÂN QUYỀN ---
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Author"))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    articlesQuery = articlesQuery.Where(a => a.AuthorId == currentUserId);
                }
                else if (!User.IsInRole("Admin"))
                {
                    articlesQuery = articlesQuery.Where(a => a.IsApproved);
                }
            }
            else
            {
                articlesQuery = articlesQuery.Where(a => a.IsApproved);
            }

            // --- 2. LOGIC TÌM KIẾM ---
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                articlesQuery = articlesQuery.Where(a => a.Title.Contains(searchString) || a.Summary.Contains(searchString));
                ViewData["CurrentFilter"] = searchString;
            }

            // --- 3. LOGIC BỘ LỌC ---
            if (!string.IsNullOrEmpty(filterAuthor))
            {
                articlesQuery = articlesQuery.Where(a => a.Author.UserName == filterAuthor);
                ViewData["FilterAuthor"] = filterAuthor;
            }
            if (!string.IsNullOrEmpty(filterStatus))
            {
                if (filterStatus == "approved") articlesQuery = articlesQuery.Where(a => a.IsApproved);
                else if (filterStatus == "pending") articlesQuery = articlesQuery.Where(a => !a.IsApproved);
                ViewData["FilterStatus"] = filterStatus;
            }
            if (filterCategory.HasValue)
            {
                articlesQuery = articlesQuery.Where(a => a.CategoryId == filterCategory);
                ViewData["FilterCategory"] = filterCategory;
            }
            if (!string.IsNullOrEmpty(filterTag))
            {
                articlesQuery = articlesQuery.Where(a => a.ArticleTags.Any(at => at.Tag.Name == filterTag));
                ViewData["FilterTag"] = filterTag;
            }

            // --- 4. KẾT THÚC & PHÂN TRANG ---
            articlesQuery = articlesQuery.OrderByDescending(a => a.CreatedAt);

            var pagedArticles = await X.PagedList.EF.PagedListExtensions.ToPagedListAsync(articlesQuery.AsNoTracking(), pageNumber, pageSize);

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Authors = await _context.Users
                .Where(u => _context.Articles.Any(a => a.AuthorId == u.Id))
                .Select(u => u.UserName)
                .Distinct()
                .ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ArticleTablePartial", pagedArticles);
            }

            return View(pagedArticles);
        }

        // GET: Articles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var isAdminOrAuthor = User.Identity.IsAuthenticated && (User.IsInRole("Admin") || User.IsInRole("Author"));

            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (article == null) return NotFound();

            // 🎯 TÁI TẠO CONTENT CÓ ẢNH Ở ĐÚNG VỊ TRÍ
            ViewBag.ContentWithImages = await ReconstructContentWithImages(article.Content, article.Id);

            // 🎯 LẤY DANH SÁCH ẢNH TỪ MEDIA CHO CAROUSEL
            var articleImages = await _context.Media
                .Where(m => m.ArticleId == id && m.FileType.StartsWith("image/"))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.ArticleImages = articleImages;

            if (!isAdminOrAuthor)
            {
                article.ViewCount++;
                await _context.SaveChangesAsync();
            }

            // Tải bình luận (giữ nguyên)
            var allComments = await _context.Comments
                .Where(c => c.ArticleId == id)
                .Include(c => c.User)
                .ToListAsync();

            var commentLookup = allComments.ToDictionary(c => c.Id);
            var topLevelComments = new List<Comment>();

            foreach (var comment in allComments)
            {
                comment.Replies = new List<Comment>();
                if (comment.ParentCommentId.HasValue)
                {
                    if (commentLookup.TryGetValue(comment.ParentCommentId.Value, out var parentComment))
                    {
                        parentComment.Replies.Add(comment);
                    }
                }
                else
                {
                    topLevelComments.Add(comment);
                }
            }

            article.Comments = topLevelComments.OrderByDescending(c => c.CreatedAt).ToList();

            // Lấy bài viết liên quan
            var relatedArticles = await _context.Articles
                .Where(a => a.CategoryId == article.CategoryId && a.Id != article.Id && a.IsApproved)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Include(a => a.Author)
                .ToListAsync();
            ViewBag.RelatedArticles = relatedArticles;

            return View(article);
        }
        // GET: Articles/Create
        [Authorize(Policy = "ElevatedRights")]
        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new ArticleCreateViewModel();
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.Tags = _context.Tags.ToList();
            return View(viewModel);
        }

        [Authorize(Policy = "ElevatedRights")]
        public async Task<IActionResult> Pending()
        {
            var articlesQuery = _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
                .Where(a => a.IsApproved == false)
                .AsQueryable();

            var articles = await articlesQuery.OrderByDescending(a => a.CreatedAt).ToListAsync();
            return View(articles);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return NotFound();

            if (!article.IsApproved)
            {
                article.IsApproved = true;
                article.UpdatedAt = DateTime.Now;
                _context.Update(article);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Đã duyệt thành công bài viết: \"{article.Title}\"";
            }
            else { TempData["Warning"] = $"Bài viết \"{article.Title}\" đã được duyệt trước đó."; }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unapprove(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return NotFound();

            if (article.IsApproved)
            {
                article.IsApproved = false;
                article.UpdatedAt = DateTime.Now;
                _context.Update(article);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Đã bỏ duyệt (ẩn) bài viết: \"{article.Title}\"";
            }
            else { TempData["Warning"] = $"Bài viết \"{article.Title}\" vốn chưa được duyệt."; }
            return RedirectToAction(nameof(Index));
        }

        // POST: Articles/Create
        [HttpPost]
        [Authorize(Policy = "ElevatedRights")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleCreateViewModel viewModel, IFormFile? thumbnailFile, List<int>? SelectedTagIds)
        {
            _logger.LogInformation("--- BẮT ĐẦU TẠO BÀI VIẾT: {Title} ---", viewModel.Title);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (ModelState.IsValid)
            {
                // 🎯 TRÍCH XUẤT ẢNH VÀ LƯU VỊ TRÍ
                var (cleanContent, positions) = await ExtractImagesAndStorePositions(viewModel.Content, 0); // Tạm thời chưa có articleId

                var article = new Article
                {
                    Title = viewModel.Title,
                    Summary = viewModel.Summary,
                    Content = cleanContent, // 🎯 LƯU CONTENT KHÔNG CÓ ẢNH (CHỈ CÓ PLACEHOLDER)
                    CategoryId = viewModel.CategoryId,
                    AuthorId = userId,
                    CreatedAt = DateTime.Now,
                    IsApproved = false,
                    ViewCount = 0
                };

                // Xử lý thumbnail (giữ nguyên)
                if (thumbnailFile != null && thumbnailFile.Length > 0)
                {
                    _logger.LogInformation("Đang xử lý ảnh Thumbnail: {FileName} ({Size} bytes)", thumbnailFile.FileName, thumbnailFile.Length);
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string uploadsFolder = Path.Combine(wwwRootPath, "uploads/articles");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(thumbnailFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await thumbnailFile.CopyToAsync(fileStream);
                    }
                    article.ThumbnailUrl = "/uploads/articles/" + uniqueFileName;
                }
                else
                {
                    article.ThumbnailUrl = "/uploads/articles/default-thumbnail.jpg";
                }

                if (SelectedTagIds != null && SelectedTagIds.Any())
                {
                    article.ArticleTags = SelectedTagIds.Select(tagId => new ArticleTag { TagId = tagId }).ToList();
                }

                _context.Add(article);
                await _context.SaveChangesAsync(); // 🎯 Lưu để có ID

                // 🎯 CẬP NHẬT LẠI ARTICLEID CHO CÁC MEDIA VÀ ARTICLEIMAGEPOSITION
                foreach (var position in positions)
                {
                    position.ArticleId = article.Id;
                    var media = await _context.Media.FindAsync(position.MediaId);
                    if (media != null)
                    {
                        media.ArticleId = article.Id;
                    }
                }
                _context.ArticleImagePositions.AddRange(positions);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã tạo bài viết ID: {ArticleId} và xử lý ảnh", article.Id);
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", viewModel.CategoryId);
            ViewBag.Tags = await _context.Tags.ToListAsync();
            return View(viewModel);
        }

        private async Task LinkMediaToArticle(string content, int articleId)
        {
            _logger.LogInformation("vào hàm gán ảnh mồ côi");

            if (string.IsNullOrEmpty(content)) return;

            // 1. Dùng Regex để trích xuất tất cả các đường dẫn ảnh trong nội dung
            // Tìm chuỗi: src="/uploads/medias/..."
            var matches = Regex.Matches(content, @"src=""(/uploads/medias/[^""]+)""");

            if (matches.Count > 0)
            {
                _logger.LogInformation("Tìm thấy ảnh mồ côi");
                // Tạo danh sách các URL ảnh tìm được
                var fileUrls = matches.Select(m => m.Groups[1].Value).Distinct().ToList();

                // 2. Truy vấn trực tiếp các Media có URL nằm trong danh sách này VÀ đang mồ côi
                // (Truy vấn này chạy trên SQL Server, nhanh hơn nhiều so với tải về RAM)
                var mediasToLink = await _context.Media
                    .Where(m => m.ArticleId == null && fileUrls.Contains(m.FileUrl))
                    .ToListAsync();

                // 3. Cập nhật ID bài viết
                if (mediasToLink.Any())
                {
                    foreach (var media in mediasToLink)
                    {
                        media.ArticleId = articleId;
                    }
                    _context.UpdateRange(mediasToLink);
                    await _context.SaveChangesAsync();
                }
            }
            _logger.LogInformation("Hoàn tất gắn id");
        }

        //GET: Articles/Edit/5
        [Authorize(Policy = "ElevatedRights")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                .Include(a => a.ArticleImagePositions) // 🎯 THÊM INCLUDE NÀY
                    .ThenInclude(aip => aip.Media)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (article == null) return NotFound();

            // 🎯 TÁI TẠO CONTENT CÓ ẢNH ĐỂ HIỂN THỊ TRONG EDITOR
            ViewBag.ContentWithImages = await ReconstructContentWithImages(article.Content, article.Id);

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", article.CategoryId);
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "UserName", article.AuthorId);
            ViewBag.Tags = await _context.Tags.ToListAsync();
            return View(article);
        }

        //POST: Articles/Edit/5
        [HttpPost]
        [Authorize(Policy = "ElevatedRights")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormFile? thumbnailFile, List<int>? SelectedTagIds)
        {
            var articleToUpdate = await _context.Articles
                .Include(a => a.ArticleTags)
                .Include(a => a.ArticleImagePositions) // 🎯 THÊM INCLUDE NÀY
                    .ThenInclude(aip => aip.Media)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (articleToUpdate == null) return NotFound();

            // 🎯 LƯU CONTENT HIỆN TẠI (CÓ ẢNH) ĐỂ SO SÁNH
            string currentContentWithImages = await ReconstructContentWithImages(articleToUpdate.Content, articleToUpdate.Id);

            if (await TryUpdateModelAsync<Article>(articleToUpdate, "",
                a => a.Title, a => a.Summary, a => a.Content, a => a.CategoryId, a => a.IsApproved, a => a.ViewCount, a => a.AuthorId))
            {
                // 🎯 KIỂM TRA XEM CONTENT CÓ THAY ĐỔI KHÔNG
                bool contentChanged = articleToUpdate.Content != currentContentWithImages;

                if (contentChanged)
                {
                    // 🎯 TRÍCH XUẤT ẢNH VÀ LƯU VỊ TRÍ TỪ CONTENT MỚI
                    var (cleanContent, imagePositions) = await ExtractImagesAndStorePositions(articleToUpdate.Content, articleToUpdate.Id);

                    // Cập nhật content với placeholder
                    articleToUpdate.Content = cleanContent;

                    // 🎯 XÓA VỊ TRÍ ẢNH CŨ VÀ THÊM MỚI
                    var oldPositions = _context.ArticleImagePositions.Where(p => p.ArticleId == articleToUpdate.Id);
                    _context.ArticleImagePositions.RemoveRange(oldPositions);

                    if (imagePositions.Any())
                    {
                        _context.ArticleImagePositions.AddRange(imagePositions);
                    }
                }

                articleToUpdate.UpdatedAt = DateTime.Now;

                // Xử lý thumbnail (giữ nguyên)
                if (thumbnailFile != null && thumbnailFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(articleToUpdate.ThumbnailUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, articleToUpdate.ThumbnailUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
                    }
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/articles");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(thumbnailFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await thumbnailFile.CopyToAsync(fileStream);
                    }
                    articleToUpdate.ThumbnailUrl = "/uploads/articles/" + uniqueFileName;
                }

                UpdateArticleTags(SelectedTagIds, articleToUpdate);

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArticleExists(articleToUpdate.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // 🎯 NẾU MODEL STATE INVALID, TÁI TẠO LẠI CONTENT CÓ ẢNH ĐỂ HIỂN THỊ
            ViewBag.ContentWithImages = await ReconstructContentWithImages(articleToUpdate.Content, articleToUpdate.Id);

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", articleToUpdate.CategoryId);
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "UserName", articleToUpdate.AuthorId);
            ViewBag.Tags = await _context.Tags.ToListAsync();
            return View(articleToUpdate);
        }
        // cập nhật danh sách thẻ tag
       
        private void UpdateArticleTags(List<int>? selectedTagIds, Article articleToUpdate)
        {
            if (selectedTagIds == null) { articleToUpdate.ArticleTags = new List<ArticleTag>(); return; }
            var selectedTagsHS = new HashSet<int>(selectedTagIds);
            var articleTagsHS = new HashSet<int>(articleToUpdate.ArticleTags.Select(at => at.TagId));
            foreach (var tag in _context.Tags)
            {
                if (selectedTagsHS.Contains(tag.Id))
                {
                    if (!articleTagsHS.Contains(tag.Id)) articleToUpdate.ArticleTags.Add(new ArticleTag { TagId = tag.Id });
                }
                else
                {
                    if (articleTagsHS.Contains(tag.Id))
                    {
                        var tagToRemove = articleToUpdate.ArticleTags.FirstOrDefault(at => at.TagId == tag.Id);
                        if (tagToRemove != null) _context.Remove(tagToRemove);
                    }
                }
            }
        }

        // GET: Articles/Delete/5
        [Authorize(Policy = "ElevatedRights")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var article = await _context.Articles
                .Include(a => a.Author).Include(a => a.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (article == null) return NotFound();
            return View(article);
        }

        // POST: Articles/Delete/5 (ĐÃ SỬA ĐỂ XÓA RÁC)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return NotFound();

            // Kiểm tra quyền (Admin hoặc Tác giả)
            var currentUserId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && article.AuthorId != currentUserId)
            {
                return Forbid();
            }

            // 🎯 XÓA ARTICLE IMAGE POSITIONS TRƯỚC
            var articleImagePositions = await _context.ArticleImagePositions
                .Where(aip => aip.ArticleId == id)
                .ToListAsync();
            _context.ArticleImagePositions.RemoveRange(articleImagePositions);

            // --- XÓA ẢNH TRONG NỘI DUNG (MEDIA) ---
            var relatedMedia = await _context.Media.Where(m => m.ArticleId == id).ToListAsync();
            foreach (var media in relatedMedia)
            {
                if (!string.IsNullOrEmpty(media.FileUrl))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, media.FileUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        try { System.IO.File.Delete(filePath); } catch { }
                    }
                }
                _context.Media.Remove(media);
            }

            // --- XÓA ẢNH THUMBNAIL ---
            if (!string.IsNullOrEmpty(article.ThumbnailUrl) && !article.ThumbnailUrl.Contains("default-thumbnail"))
            {
                var thumbPath = Path.Combine(_webHostEnvironment.WebRootPath, article.ThumbnailUrl.TrimStart('/'));
                if (System.IO.File.Exists(thumbPath))
                {
                    try { System.IO.File.Delete(thumbPath); } catch { }
                }
            }

            // Xóa bài viết
            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private bool ArticleExists(int id)
        {
            return _context.Articles.Any(e => e.Id == id);
        }

        // --- CÁC ACTION BÌNH LUẬN (Giữ nguyên như đã sửa) ---
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int ArticleId, string Content, int? ParentCommentId)
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                TempData["CommentError"] = "Nội dung bình luận không được để trống.";
                return RedirectToAction("Details", new { id = ArticleId });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var comment = new Comment
            {
                ArticleId = ArticleId,
                Content = Content,
                UserId = userId,
                CreatedAt = DateTime.Now,
                IsApproved = true,
                ParentCommentId = ParentCommentId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            TempData["CommentSuccess"] = "Bình luận của bạn đã được gửi.";
            return RedirectToAction("Details", controllerName: "Articles", new { id = ArticleId }, "comment-" + comment.Id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditComment(int commentId, int articleId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["CommentError"] = "Nội dung bình luận không được để trống.";
                return RedirectToAction("Details", controllerName: "Articles", new { id = articleId }, "comment-" + commentId);
            }
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            if (comment.UserId != currentUserId && !isAdmin) return Forbid();

            comment.Content = content;
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();
            TempData["CommentSuccess"] = "Đã cập nhật bình luận.";
            return RedirectToAction("Details", controllerName: "Articles", new { id = articleId }, "comment-" + comment.Id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId, int articleId)
        {
            var commentToDelete = await _context.Comments.FindAsync(commentId);
            if (commentToDelete == null) return NotFound();
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            if (commentToDelete.UserId != currentUserId && !isAdmin) return Forbid();

            await DeleteCommentAndRepliesAsync(commentId);
            await _context.SaveChangesAsync();
            TempData["CommentSuccess"] = "Đã xóa bình luận.";
            return RedirectToAction("Details", controllerName: "Articles", new { id = articleId }, "comment-section");
        }

        private async Task DeleteCommentAndRepliesAsync(int commentId)
        {
            var commentToDelete = await _context.Comments
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (commentToDelete != null)
            {
                if (commentToDelete.Replies != null && commentToDelete.Replies.Any())
                {
                    var replyIds = commentToDelete.Replies.Select(r => r.Id).ToList();
                    foreach (var replyId in replyIds)
                    {
                        await DeleteCommentAndRepliesAsync(replyId);
                    }
                }
                _context.Comments.Remove(commentToDelete);
            }
        }
        // 🎯 PHƯƠNG THỨC TÁCH URL ẢNH TỪ CONTENT
        private List<string> ExtractImageUrls(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return new List<string>();

            var pattern = @"<img[^>]+src=""([^""]+)""[^>]*>";
            var matches = Regex.Matches(htmlContent, pattern);

            return matches
                .Where(m => m.Groups.Count > 1 && !string.IsNullOrEmpty(m.Groups[1].Value))
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }

        // 🎯 PHƯƠNG THỨC XÓA ẢNH KHỎI CONTENT
        private string RemoveImagesFromContent(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return htmlContent;

            var pattern = @"<img[^>]+>";
            return Regex.Replace(htmlContent, pattern, "");
        }

        // 🎯 PHƯƠNG THỨC CẬP NHẬT MEDIA VỚI ARTICLE ID
        private async Task UpdateMediaWithArticleId(List<string> imageUrls, int articleId)
        {
            _logger.LogInformation("Cập nhật Media với ArticleId: {ArticleId}", articleId);

            // Tìm các media có URL trùng với ảnh trong content
            var mediasToUpdate = await _context.Media
                .Where(m => imageUrls.Contains(m.FileUrl))
                .ToListAsync();

            foreach (var media in mediasToUpdate)
            {
                media.ArticleId = articleId;
                _logger.LogInformation("Đã cập nhật Media {MediaId} với ArticleId {ArticleId}", media.Id, articleId);
            }

            if (mediasToUpdate.Any())
            {
                _context.Media.UpdateRange(mediasToUpdate);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã cập nhật {Count} media với ArticleId {ArticleId}", mediasToUpdate.Count, articleId);
            }
        }
        //Xử lý ảnh dạng base64
        private async Task<Media> ProcessBase64Image(string base64String, int articleId)
        {
            try
            {
                // Regex để trích xuất loại ảnh và dữ liệu
                var match = Regex.Match(base64String, @"data:image/(?<type>[a-z]+);base64,(?<data>.+)");
                if (!match.Success) return null;

                string imageType = match.Groups["type"].Value;
                string base64Data = match.Groups["data"].Value;

                byte[] imageBytes = Convert.FromBase64String(base64Data);

                string fileName = Guid.NewGuid().ToString() + "." + imageType;
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "medias");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                string filePath = Path.Combine(uploadPath, fileName);
                string fileUrl = "/uploads/medias/" + fileName;

                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                var media = new Media
                {
                    FileName = fileName,
                    FileUrl = fileUrl,
                    FileType = "image/" + imageType,
                    FileSizeKB = (int)(imageBytes.Length / 1024),
                    CreatedAt = DateTime.Now,
                    ArticleId = articleId
                };

                _context.Media.Add(media);
                return media;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý ảnh base64");
                return null;
            }
        }

        //Khôi phục vị trí ảnh trong content và lưu vào bảng ArticleImagePosition
        private async Task<(string cleanContent, List<ArticleImagePosition> positions)> ExtractImagesAndStorePositions(string htmlContent, int articleId)
        {
            var positions = new List<ArticleImagePosition>();
            var cleanContent = htmlContent;
            int positionIndex = 0;

            // Tìm tất cả ảnh (cả base64 và URL)
            var imgPattern = @"<img[^>]+src=""([^""]+)""[^>]*>";
            var matches = Regex.Matches(htmlContent, imgPattern);

            foreach (Match match in matches)
            {
                string src = match.Groups[1].Value;
                string placeholder = $"{{image{positionIndex}}}";

                Media media = null;

                // Xử lý base64
                if (src.StartsWith("data:image"))
                {
                    media = await ProcessBase64Image(src, articleId);
                }
                // Xử lý URL thông thường (đã upload qua MediaController)
                else
                {
                    // Tìm media theo URL, nếu chưa có thì tạo mới (trường hợp ảnh được upload nhưng chưa có trong Media)
                    media = await _context.Media.FirstOrDefaultAsync(m => m.FileUrl == src);
                    if (media == null)
                    {
                        // Tạo mới media từ URL (ảnh từ nguồn khác, hoặc đã upload nhưng chưa lưu vào Media)
                        // Trong trường hợp này, chúng ta không lưu file vì file đã có trong wwwroot, chỉ cần lưu thông tin vào Media
                        media = new Media
                        {
                            FileName = Path.GetFileName(src),
                            FileUrl = src,
                            FileType = "image/" + Path.GetExtension(src).TrimStart('.'),
                            FileSizeKB = 0, // Không biết kích thước
                            CreatedAt = DateTime.Now,
                            ArticleId = articleId
                        };
                        _context.Media.Add(media);
                    }
                    else
                    {
                        // Cập nhật ArticleId nếu media đã tồn tại nhưng chưa có articleId
                        if (media.ArticleId == null || media.ArticleId == 0)
                        {
                            media.ArticleId = articleId;
                        }
                    }
                }

                if (media != null)
                {
                    // Lưu vào bảng ArticleImagePosition
                    var position = new ArticleImagePosition
                    {
                        ArticleId = articleId,
                        MediaId = media.Id,
                        PositionIndex = positionIndex,
                        Placeholder = placeholder
                    };
                    positions.Add(position);

                    // Thay thế ảnh bằng placeholder
                    cleanContent = cleanContent.Replace(match.Value, placeholder);
                    positionIndex++;
                }
            }

            return (cleanContent, positions);
        }
        //Tái tạo nội dung với ảnh từ bảng ArticleImagePosition
        // 🎯 PHƯƠNG THỨC TÁI TẠO CONTENT CÓ ẢNH TỪ PLACEHOLDER
        private async Task<string> ReconstructContentWithImages(string cleanContent, int articleId)
        {
            if (string.IsNullOrEmpty(cleanContent))
                return cleanContent;

            // Lấy tất cả vị trí ảnh của bài viết
            var imagePositions = await _context.ArticleImagePositions
                .Where(aip => aip.ArticleId == articleId)
                .Include(aip => aip.Media)
                .OrderBy(aip => aip.PositionIndex)
                .ToListAsync();

            var contentWithImages = cleanContent;

            // Thay thế placeholder bằng thẻ img thật
            foreach (var position in imagePositions)
            {
                if (!string.IsNullOrEmpty(position.Media?.FileUrl))
                {
                    string imgTag = $"<img src=\"{position.Media.FileUrl}\" class=\"article-image\" alt=\"\" />";
                    contentWithImages = contentWithImages.Replace(position.Placeholder, imgTag);
                }
            }

            return contentWithImages;
        }
    }
}