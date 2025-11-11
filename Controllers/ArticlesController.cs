    using System;
    using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
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


namespace Trang_tin_điện_tử_mvc.Controllers
    {
    public class ArticlesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ArticlesController> _logger;
        public ArticlesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, ILogger<ArticlesController> logger)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Articles
        public async Task<IActionResult> Index(int? categoryId, string tag, string query) // Thêm tham số từ lần trước
        {
            var articlesQuery = _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .AsQueryable();

            // --- LỌC ---
            if (categoryId.HasValue)
            {
                articlesQuery = articlesQuery.Where(a => a.CategoryId == categoryId.Value);
                // ... (ViewData cho tiêu đề lọc)
            }
            if (!string.IsNullOrEmpty(tag))
            {
                articlesQuery = articlesQuery.Where(a => a.ArticleTags.Any(at => at.Tag.Name == tag));
                // ... (ViewData cho tiêu đề lọc)
            }
            if (!string.IsNullOrEmpty(query)) // Lọc cho Search
            {
                articlesQuery = articlesQuery.Where(a => a.Title.Contains(query) || a.Content.Contains(query));
                ViewData["FilterTitle"] = $"Kết quả tìm kiếm cho: \"{query}\"";
                ViewData["CurrentFilter"] = query;
            }

            // --- PHÂN QUYỀN ---
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    // Admin xem được hết
                }
                else if (User.IsInRole("Author"))
                {
                    // Author CHỈ xem bài của mình
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    articlesQuery = articlesQuery.Where(a => a.AuthorId == currentUserId);
                }
                else // User thường
                {
                    articlesQuery = articlesQuery.Where(a => a.IsApproved);
                }
            }
            else // Khách
            {
                articlesQuery = articlesQuery.Where(a => a.IsApproved);
            }

            var articles = await articlesQuery
                                 .OrderByDescending(a => a.CreatedAt)
                                 .ToListAsync();

            // Tải lại sidebar (nếu View Index của Article cũng có sidebar)
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Tags = await _context.Tags.OrderBy(t => t.Name).Take(10).ToListAsync();

            return View(articles);
        }
        public async Task<IActionResult> Search(string query)
        {
            // Chuyển hướng đến Index với tham số query
            return RedirectToAction(nameof(Index), new { query = query });
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
            if (!isAdminOrAuthor) // Nếu không phải Admin/Author thì mới đếm
            {
                article.ViewCount++; 
                await _context.SaveChangesAsync(); // 3. PHẢI CÓ 'await'
            }
            // Tải và xây dựng cây bình luận (Logic này đã đúng)
            var allComments = await _context.Comments
                .Where(c => c.ArticleId == id)
                .Include(c => c.User)
                .ToListAsync();

            var commentLookup = allComments.ToDictionary(c => c.Id);
            var topLevelComments = new List<Comment>();

            foreach (var comment in allComments)
            {
                // Đảm bảo Replies được khởi tạo
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

            // Lấy bài viết liên quan (cho thanh cuộn ở cuối)
            var relatedArticles = await _context.Articles
                .Where(a => a.CategoryId == article.CategoryId && a.Id != article.Id && a.IsApproved)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Include(a => a.Author)
                .ToListAsync();
            ViewBag.RelatedArticles = relatedArticles;

            return View(article);
        }        
        
        //Get duyệt bài
        [Authorize(Policy = "ElevatedRights")]
        public async Task<IActionResult> Pending()
        {
            // Start base query
            var articlesQuery = _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                    .Where(a => a.IsApproved == false)
                .AsQueryable();

            // Execute query and order results
            var articles = await articlesQuery
                                 .OrderByDescending(a => a.CreatedAt)
                                 .ToListAsync();
            return View(articles);
        }

        //Post Duyệt bài
        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
            {
                return NotFound();
            }

            if (!article.IsApproved) // Chỉ cập nhật nếu bài chưa được duyệt
            {
                article.IsApproved = true;
                article.UpdatedAt = DateTime.Now; // Cập nhật thời gian sửa đổi (tùy chọn)
                _context.Update(article);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Đã duyệt thành công bài viết: \"{article.Title}\"";
            }
            else
            {
                TempData["Warning"] = $"Bài viết \"{article.Title}\" đã được duyệt trước đó."; // Thông báo nếu đã duyệt rồi
            }


            // Quay lại trang Index (hoặc trang danh sách bài chờ duyệt nếu có)
            return RedirectToAction(nameof(Index));
        }

        //Post ẩn bài 
        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unapprove(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
            {
                return NotFound();
            }

            if (article.IsApproved) // Chỉ cập nhật nếu bài đang được duyệt
            {
                article.IsApproved = false; // Đặt lại thành chưa duyệt
                article.UpdatedAt = DateTime.Now;
                _context.Update(article);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Đã bỏ duyệt (ẩn) bài viết: \"{article.Title}\"";
            }
            else
            {
                TempData["Warning"] = $"Bài viết \"{article.Title}\" vốn chưa được duyệt.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Articles/Create
        [Authorize(Policy = "ElevatedRights")]
        [HttpGet]
        public IActionResult Create() // Không cần async ở đây
        {
            // Tạo một viewModel rỗng để truyền cho View nếu cần
            var viewModel = new ArticleCreateViewModel();

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.Tags = _context.Tags.ToList();
            return View(viewModel); // Truyền viewModel sang View
        }

        // POST: Articles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.        
        [HttpPost]
        [Authorize(Policy = "ElevatedRights")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleCreateViewModel viewModel, IFormFile? thumbnailFile, List<int>? SelectedTagIds)
        {
            // Chỉ gán AuthorId từ người dùng đang đăng nhập để bảo mật
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Xử lý nếu người dùng chưa đăng nhập
                return Challenge(); // Hoặc RedirectToAction("Login", "Account");
            }


            if (ModelState.IsValid)
            {
                var article = new Article
                {
                    Title = viewModel.Title,
                    Summary = viewModel.Summary,
                    Content = viewModel.Content,
                    CategoryId = viewModel.CategoryId,
                    // 2. Gán các giá trị từ server
                    AuthorId = userId,
                    CreatedAt = DateTime.Now,
                    IsApproved = false,
                    ViewCount = 0
                };
                // 1. Xử lý lưu ảnh
                if (thumbnailFile != null && thumbnailFile.Length > 0)
                {
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
                    // Tùy chọn: Gán ảnh mặc định nếu không có ảnh tải lên
                    article.ThumbnailUrl = "/uploads/articles/default-thumbnail.png";
                }

                if (SelectedTagIds != null && SelectedTagIds.Any())
                {
                    article.ArticleTags = SelectedTagIds.Select(tagId => new ArticleTag { TagId = tagId }).ToList();
                }

                _context.Add(article);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu ModelState không hợp lệ, tải lại ViewBag và trả về View
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", viewModel.CategoryId);
            ViewBag.Tags = await _context.Tags.ToListAsync();
            return View(viewModel);
        }

        // GET: Articles/Edit/5
        [Authorize(Policy = "ElevatedRights")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Tải bài viết CÙNG VỚI tất cả các dữ liệu liên quan cần thiết
            var article = await _context.Articles
                .Include(a => a.Author)      // Tải kèm Author
                .Include(a => a.Category)    // Tải kèm Category
                .Include(a => a.ArticleTags) // Tải kèm các tag đã chọn
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", article.CategoryId);
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "UserName", article.AuthorId);
            ViewBag.Tags = await _context.Tags.ToListAsync();

            return View(article);
        }

        // POST: Articles/Edit/5
        [Authorize(Policy = "ElevatedRights")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormFile? thumbnailFile, List<int>? SelectedTagIds)
        {
            var articleToUpdate = await _context.Articles
                .Include(a => a.ArticleTags)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (articleToUpdate == null)
            {
                return NotFound();
            }

            // Cập nhật các thuộc tính từ form một cách an toàn
            if (await TryUpdateModelAsync<Article>(
                articleToUpdate,
                "", // Prefix để trống
                a => a.Title, a => a.Summary, a => a.Content, a => a.CategoryId,
                a => a.IsApproved, a => a.ViewCount, a => a.AuthorId))
            {
                articleToUpdate.UpdatedAt = DateTime.Now;

                // 1. Xử lý cập nhật ảnh (nếu có ảnh mới được tải lên)
                if (thumbnailFile != null && thumbnailFile.Length > 0)
                {
                    // (Tùy chọn) Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(articleToUpdate.ThumbnailUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, articleToUpdate.ThumbnailUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Lưu ảnh mới
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/articles");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(thumbnailFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await thumbnailFile.CopyToAsync(fileStream);
                    }
                    articleToUpdate.ThumbnailUrl = "/uploads/articles/" + uniqueFileName;
                }

                // 2. Cập nhật Tags
                UpdateArticleTags(SelectedTagIds, articleToUpdate);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArticleExists(articleToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Nếu ModelState không hợp lệ, tải lại dữ liệu cho View
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", articleToUpdate.CategoryId);
            ViewData["AuthorId"] = new SelectList(_context.Users, "Id", "UserName", articleToUpdate.AuthorId);
            ViewBag.Tags = await _context.Tags.ToListAsync();
            return View(articleToUpdate);
        }

        // Phương thức trợ giúp để cập nhật tags
        private void UpdateArticleTags(List<int>? selectedTagIds, Article articleToUpdate)
        {
            if (selectedTagIds == null)
            {
                articleToUpdate.ArticleTags = new List<ArticleTag>();
                return;
            }

            var selectedTagsHS = new HashSet<int>(selectedTagIds);
            var articleTagsHS = new HashSet<int>(articleToUpdate.ArticleTags.Select(at => at.TagId));

            foreach (var tag in _context.Tags)
            {
                if (selectedTagsHS.Contains(tag.Id))
                {
                    if (!articleTagsHS.Contains(tag.Id))
                    {
                        articleToUpdate.ArticleTags.Add(new ArticleTag { TagId = tag.Id });
                    }
                }
                else
                {
                    if (articleTagsHS.Contains(tag.Id))
                    {
                        var tagToRemove = articleToUpdate.ArticleTags.FirstOrDefault(at => at.TagId == tag.Id);
                        if (tagToRemove != null)
                        {
                            _context.Remove(tagToRemove);
                        }
                    }
                }
            }
        }
        
        // GET: Articles/Delete/5
        [Authorize(Policy = "ElevatedRights")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (article == null)
            {
                return NotFound();
            }

            return View(article);
        }

        // POST: Articles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article != null)
            {
                _context.Articles.Remove(article);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ArticleExists(int id)
        {
            return _context.Articles.Any(e => e.Id == id);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int ArticleId, string Content, int? ParentCommentId)
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                TempData["CommentError"] = "Nội dung bình luận không được để trống.";
                return RedirectToAction("Details", controllerName: "Articles", new { id = ArticleId }, "comment-section");
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge(); // Yêu cầu đăng nhập
            }

            var comment = new Comment
            {
                ArticleId = ArticleId,
                Content = Content,
                UserId = userId,
                CreatedAt = DateTime.Now,
                IsApproved = true, // Tự động duyệt
                ParentCommentId = ParentCommentId // Sẽ là null (gốc) hoặc ID (trả lời)
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["CommentSuccess"] = "Bình luận của bạn đã được gửi.";

            // Sửa lỗi Redirect: Dùng tham số có tên
            return RedirectToAction(
                actionName: "Details", controllerName: "Articles",
                routeValues: new { id = ArticleId },
                fragment: "comment-" + comment.Id
            );
        }

        // --- XÓA ACTION ReplyToComment (Đã gộp vào AddComment) ---
        // public async Task<IActionResult> ReplyToComment(...) {}

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditComment(int commentId, int articleId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["CommentError"] = "Nội dung bình luận không được để trống.";
                return RedirectToAction(
                     actionName: "Details", controllerName: "Articles",
                     routeValues: new { id = articleId },
                     fragment: "comment-" + commentId
                 );
            }

            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            if (comment.UserId != currentUserId && !isAdmin)
            {
                return Forbid();
            }

            comment.Content = content;
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            TempData["CommentSuccess"] = "Đã cập nhật bình luận."; // Đồng bộ TempData
            return RedirectToAction(
                 actionName: "Details", controllerName: "Articles",
                 routeValues: new { id = articleId },
                 fragment: "comment-" + comment.Id
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId, int articleId)
        {
            var commentToDelete = await _context.Comments.FindAsync(commentId);
            if (commentToDelete == null) return NotFound();

            // Kiểm tra bảo mật
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            if (commentToDelete.UserId != currentUserId && !isAdmin)
            {
                return Forbid();
            }

            // --- SỬA LỖI: DÙNG HÀM XÓA ĐỆ QUY ---
            // Hàm này sẽ xóa commentId và TẤT CẢ con cháu của nó
            await DeleteCommentAndRepliesAsync(commentId);

            // Lưu thay đổi 1 lần duy nhất
            await _context.SaveChangesAsync();
            // ------------------------------------

            TempData["CommentSuccess"] = "Đã xóa bình luận (và các trả lời).";

            return RedirectToAction(
                actionName: "Details", controllerName: "Articles",
                routeValues: new { id = articleId },
                fragment: "comment-section" // Quay về khu vực bình luận
            );
        }

        // --- HÀM TRỢ GIÚP XÓA ĐỆ QUY ---
        private async Task DeleteCommentAndRepliesAsync(int commentId)
        {
            // Tìm bình luận cần xóa VÀ các con trực tiếp của nó
            var commentToDelete = await _context.Comments
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (commentToDelete != null)
            {
                // Nếu có con, gọi đệ quy để xóa từng đứa con
                if (commentToDelete.Replies != null && commentToDelete.Replies.Any())
                {
                    // Chuyển sang List để tránh lỗi "Collection was modified"
                    var replyIds = commentToDelete.Replies.Select(r => r.Id).ToList();
                    _logger.LogInformation("Bình luận {CommentId} có {Count} con. Bắt đầu xóa con.", commentId, replyIds.Count);
                    foreach (var replyId in replyIds)
                    {
                        await DeleteCommentAndRepliesAsync(replyId); // Gọi đệ quy
                    }
                }

                // Sau khi tất cả con cháu đã bị xóa, xóa chính nó
                _context.Comments.Remove(commentToDelete);
                _logger.LogInformation("Đã đánh dấu xóa Comment ID: {CommentId}", commentId);
            }
        }
    }
}
