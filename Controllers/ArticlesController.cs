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
        public async Task<IActionResult> Index(int? categoryId, string tag) // Thêm cả tham số tag nếu bạn có lọc theo tag
        {
            var articlesQuery = _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .AsQueryable();

            // BƯỚC 2: LỌC THEO categoryId NẾU CÓ
            if (categoryId.HasValue)
            {
                articlesQuery = articlesQuery.Where(a => a.CategoryId == categoryId.Value);

                // (Tùy chọn) Lấy tên Category để hiển thị tiêu đề lọc
                var categoryName = await _context.Categories
                                          .Where(c => c.Id == categoryId.Value)
                                          .Select(c => c.Name)
                                          .FirstOrDefaultAsync();
                ViewData["FilterTitle"] = $"Bài viết thuộc danh mục: {categoryName ?? "Không rõ"}";
                ViewData["CurrentCategoryId"] = categoryId.Value; // Để highlight menu danh mục (nếu cần)
            }

            // (Tùy chọn) Thêm lọc theo tag nếu có
            if (!string.IsNullOrEmpty(tag))
            {
                articlesQuery = articlesQuery.Where(a => a.ArticleTags.Any(at => at.Tag.Name == tag));
                ViewData["FilterTitle"] = $"Bài viết có thẻ: {tag}";
                ViewData["CurrentTag"] = tag;
            }


            // Kiểm tra quyền xem bài viết (Logic cũ của bạn)
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (!User.IsInRole("Admin")) // Admin xem được hết
                {
                    if (User.IsInRole("Author"))
                    {
                        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        // Author xem bài của mình (đã lọc category/tag ở trên nếu có) HOẶC bài đã duyệt của người khác
 
                        articlesQuery = articlesQuery.Where(a => a.AuthorId == currentUserId);
 
                    }
                    else // User thường chỉ xem bài đã duyệt
                    {
                        articlesQuery = articlesQuery.Where(a => a.IsApproved);
                    }
                }
            }
            else // Khách chỉ xem bài đã duyệt
            {
                articlesQuery = articlesQuery.Where(a => a.IsApproved);
            }

            // Sắp xếp và lấy dữ liệu
            var articles = await articlesQuery
                                 .OrderByDescending(a => a.CreatedAt)
                                 .ToListAsync();

            // (Tùy chọn) Load lại danh sách Categories/Tags cho sidebar nếu cần
            ViewBag.Categories = await _context.Categories.ToListAsync(); // Ví dụ
                                                                          // ViewBag.Tags = await _context.Tags.ToListAsync(); // Ví dụ

            return View(articles);
        }

        // GET: Articles/Search?query=abc
        public async Task<IActionResult> Search(string query)
        {
            ViewData["CurrentFilter"] = query; // Lưu lại query để hiển thị trên ô tìm kiếm
            ViewData["FilterTitle"] = $"Kết quả tìm kiếm cho: \"{query}\""; // Tiêu đề trang kết quả

            var articlesQuery = _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .AsQueryable(); // Bắt đầu query

            // 1. Lọc theo Query (nếu query không rỗng)
            if (!String.IsNullOrEmpty(query))
            {
                // Tìm kiếm không phân biệt hoa thường trong Title HOẶC Content
                articlesQuery = articlesQuery.Where(a => a.Title.Contains(query)
                                                      || a.Content.Contains(query));
            }
            else
            {
                // Nếu query rỗng, không trả về kết quả nào hoặc trả về trang Index thông thường
                ViewData["FilterTitle"] = "Vui lòng nhập từ khóa tìm kiếm";
                // return View("Index", new List<Article>()); // Trả về danh sách rỗng
                return RedirectToAction(nameof(Index)); // Hoặc quay về trang Index
            }

            // 2. Áp dụng Logic Quyền (Tương tự như trong Index)
            // Chỉ hiển thị bài đã duyệt cho khách và user thường
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (!User.IsInRole("Admin") && !User.IsInRole("Author")) // User thường
                {
                    articlesQuery = articlesQuery.Where(a => a.IsApproved);
                }
                // Admin và Author có thể thấy bài chưa duyệt (nếu muốn)
                // Author chỉ thấy bài của mình? Logic này phức tạp hơn khi search, cần cân nhắc
            }
            else // Khách
            {
                articlesQuery = articlesQuery.Where(a => a.IsApproved);
            }

            // 3. Sắp xếp và lấy kết quả
            var searchResults = await articlesQuery
                                    .OrderByDescending(a => a.CreatedAt) // Sắp xếp theo ngày mới nhất
                                    .ToListAsync();

            // 4. Trả về View "Index" với dữ liệu đã lọc
            // View Index.cshtml sẽ được tái sử dụng để hiển thị kết quả tìm kiếm
            return View("Index", searchResults);
        }

        // GET: Articles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // BƯỚC 1: Tải bài viết (Article) và các thuộc tính liên quan (Author, Category, Tags)
            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (article == null)
            {
                return NotFound();
            } 
            var allComments = await _context.Comments
                .Where(c => c.ArticleId == id)
                .Include(c => c.User) // Tải thông tin người dùng (cho Avatar, Tên)
                .ToListAsync();
             
            var relatedArticles = await _context.Articles
                .Where(a => a.CategoryId == article.CategoryId // Cùng chuyên mục
                         && a.Id != article.Id              // Loại trừ bài hiện tại
                         && a.IsApproved)                   // Chỉ lấy bài đã duyệt
                .OrderByDescending(a => a.CreatedAt)        // Sắp xếp mới nhất
                .Take(5)                                    // Lấy 5 bài
                .Include(a => a.Author) // Include Author nếu cần hiển thị
                .ToListAsync();

            ViewBag.RelatedArticles = relatedArticles; 
            _logger.LogInformation($"--- Bắt đầu xây dựng cây bình luận cho Article ID: {id}"); // Log bắt đầu

            var commentLookup = allComments.ToDictionary(c => c.Id);
            var topLevelComments = new List<Comment>();

            foreach (var comment in allComments)
            {
                if (comment.ParentCommentId.HasValue)
                {
                    // Đây là bình luận con (Reply)
                    if (commentLookup.TryGetValue(comment.ParentCommentId.Value, out var parentComment))
                    {
                        // === LOG CHI TIẾT ===
                        _logger.LogDebug($"--- [Kiểm tra] Reply ID: {comment.Id} cho Parent ID: {parentComment.Id}. Số Replies hiện tại của Parent: {parentComment.Replies.Count}");

                        // (Thêm bước kiểm tra này để chắc chắn)
                        if (parentComment.Replies.Any(r => r.Id == comment.Id))
                        {
                            _logger.LogWarning($"--- !!! [CẢNH BÁO] Reply ID: {comment.Id} ĐÃ TỒN TẠI trong Parent ID: {parentComment.Id}. Bỏ qua việc thêm lại.");
                        }
                        else
                        {
                            // Thêm bình luận con này vào danh sách Replies của cha nó
                            parentComment.Replies.Add(comment);
                            _logger.LogDebug($"--- [Thêm thành công] Reply ID: {comment.Id} vào Parent ID: {parentComment.Id}. Số Replies MỚI của Parent: {parentComment.Replies.Count}");
                        }
                        // === KẾT THÚC LOG ===
                    }
                    else
                    {
                        // Log lỗi nếu không tìm thấy cha (như cũ)
                        _logger.LogError($"--- Lỗi logic: Comment ID {comment.Id} có ParentCommentId={comment.ParentCommentId} không tồn tại!");
                    }
                }
                else
                {
                    // Đây là bình luận gốc (không có cha)
                    topLevelComments.Add(comment);
                }
            }

            _logger.LogInformation($"--- Xây dựng cây bình luận xong. Số bình luận gốc: {topLevelComments.Count}"); // Log kết thúc

            // --- Phần còn lại của action Details ---
            article.Comments = topLevelComments.OrderByDescending(c => c.CreatedAt).ToList();
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
        // BƯỚC 1: XÓA THAM SỐ ParentCommentId
        public async Task<IActionResult> AddComment(int ArticleId, string Content)
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                TempData["CommentError"] = "Nội dung bình luận không được để trống.";
                // SỬA REDIRECT CHO ĐÚNG
                return RedirectToAction(
                    actionName: "Details",
                    routeValues: new { id = ArticleId }
                );
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account"); // Hoặc return Challenge();
            }

            var comment = new Comment
            {
                ArticleId = ArticleId,
                Content = Content,
                UserId = userId,
                CreatedAt = DateTime.Now,
                IsApproved = true,
                // BƯỚC 2: BỎ GÁN ParentCommentId (luôn là null cho bình luận gốc)
                ParentCommentId = null
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(
   actionName: "Details",
   controllerName: "Articles",
   routeValues: new { id = ArticleId }, 
   fragment: "comment-" + comment.Id
);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm để chống tấn công CSRF
        [Authorize] // Bắt buộc người dùng phải đăng nhập
        public async Task<IActionResult> EditComment(int commentId, int articleId, string content)
        {
            // Kiểm tra nội dung
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["CommentError"] = "Nội dung bình luận không được để trống."; // Đồng bộ TempData
                return RedirectToAction(
     actionName: "Details",
     controllerName: "Articles",
     routeValues: new { id = articleId },
     fragment: "comment-" + commentId
 );
            }

            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return NotFound();
            }

            // --- KIỂM TRA BẢO MẬT PHÍA SERVER ---
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (comment.UserId != currentUserId && !isAdmin)
            {
                return Forbid(); // Trả về lỗi 403 Forbidden nếu không có quyền
            }
            // --- Kết thúc kiểm tra bảo mật ---

            // Cập nhật nội dung và lưu
            comment.Content = content;
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            TempData["CommentError"] = "Nội dung bình luận không được để trống."; // Đồng bộ TempData
            return RedirectToAction(
             actionName: "Details",
             controllerName: "Articles",
             routeValues: new { id = articleId },
             fragment: "comment-" + commentId
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        // BƯỚC 1: THÊM THAM SỐ ArticleId
        public async Task<IActionResult> ReplyToComment(int ParentCommentId, int ArticleId, string Content)
        {
            // Kiểm tra nội dung bình luận
            if (string.IsNullOrWhiteSpace(Content))
            {
                TempData["CommentError"] = "Nội dung bình luận không được để trống."; // Đồng bộ TempData
                                                                                      // SỬA REDIRECT CHO ĐÚNG
                return RedirectToAction(
                    actionName: "Details",
                    controllerName: "Articles",
                    routeValues: new { id = ArticleId }, // Dùng ArticleId từ form
                    fragment: "comment-" + ParentCommentId // Trỏ về bình luận cha nếu lỗi
                );
            }

            // BƯỚC 2: KIỂM TRA BÀI VIẾT TỒN TẠI (Tùy chọn nhưng nên có)
            var articleExists = await _context.Articles.AnyAsync(a => a.Id == ArticleId);
            if (!articleExists)
            {
                return NotFound("Bài viết không tồn tại.");
            }

            // BƯỚC 3: KIỂM TRA BÌNH LUẬN CHA TỒN TẠI
            var parentCommentExists = await _context.Comments.AnyAsync(c => c.Id == ParentCommentId && c.ArticleId == ArticleId);
            if (!parentCommentExists)
            {
                TempData["CommentError"] = "Không thể trả lời bình luận không tồn tại.";
                return RedirectToAction("Details", new { id = ArticleId });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge(); // Người dùng phải đăng nhập
            }

            // Tạo bình luận con
            var newComment = new Comment
            {
                Content = Content,
                ParentCommentId = ParentCommentId,
                CreatedAt = DateTime.Now,
                UserId = userId, // Lấy ID người dùng hiện tại
                ArticleId = ArticleId, // Lấy từ form
                IsApproved = true // Giả định trả lời được duyệt ngay
            };

            // Lưu bình luận vào cơ sở dữ liệu
            _context.Comments.Add(newComment);
            await _context.SaveChangesAsync();

            TempData["CommentSuccess"] = "Câu trả lời của bạn đã được gửi."; // Đồng bộ TempData

            // BƯỚC 4: SỬA REDIRECT CHO ĐÚNG VÀ THÊM FRAGMENT
            return RedirectToAction(
                actionName: "Details",
                controllerName: "Articles",
                routeValues: new { id = ArticleId },
                fragment: "comment-" + newComment.Id // Trỏ đến trả lời vừa tạo
            );
        }

        // =================================================================
        // HÀM XÓA BÌNH LUẬN (ĐÃ CHUYỂN SANG MVC)
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId, int articleId)
        {
            var commentToCheck = await _context.Comments.FindAsync(commentId);
            if (commentToCheck == null)
            {
                return NotFound();
            }
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (commentToCheck.UserId != currentUserId && !isAdmin)
            {
                return Forbid();
            } 
            await DeleteCommentAndRepliesAsync(commentId);
 
            // --- Kết thúc kiểm tra bảo mật ---

            // --- GỌI HÀM XÓA ĐỆ QUY ---
            // Hàm này sẽ xử lý việc xóa commentId và tất cả con cháu của nó
            await DeleteCommentAndRepliesAsync(commentId);
            // -------------------------

            // --- LƯU THAY ĐỔI MỘT LẦN DUY NHẤT ---
            // SaveChangesAsync sẽ áp dụng tất cả các lệnh Remove đã được gọi
            // bên trong DeleteCommentAndRepliesAsync 
            await _context.SaveChangesAsync();
            // ------------------------------------

            TempData["CommentSuccess"] = "Đã xóa bình luận và các trả lời."; // Đồng bộ TempData
            return RedirectToAction(
                actionName: "Details",
                controllerName: "Articles",
                routeValues: new { id = articleId },
               fragment: "comment-" + commentId // Chuyển đến khu vực bình luận chung
            );
        }
         
        private async Task DeleteCommentAndRepliesAsync(int commentId)
        {
            // Tìm bình luận cần xóa và các con trực tiếp của nó
            var commentToDelete = await _context.Comments
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (commentToDelete != null)
            {
                // Nếu có con, gọi đệ quy để xóa từng đứa con (và cháu chắt của nó)
                if (commentToDelete.Replies != null && commentToDelete.Replies.Any())
                {
                    var replyIds = commentToDelete.Replies.Select(r => r.Id).ToList();
                    foreach (var replyId in replyIds)
                    {
                        await DeleteCommentAndRepliesAsync(replyId); // Gọi đệ quy
                    }
                }

                // Sau khi tất cả con cháu đã bị xóa (hoặc không có con), xóa chính nó
                _context.Comments.Remove(commentToDelete);
                // LƯU Ý: Không gọi SaveChangesAsync() ở đây, để hàm DeleteComment gọi 1 lần duy nhất.
            }
        }
    }
}
