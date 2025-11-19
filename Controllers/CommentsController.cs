using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        [Authorize(Policy = "RequireAdminRole")]        
        // GET: Comments
        public async Task<IActionResult> Index(string statusFilter = "All", int pageNumber = 1, int pageSize = 15)
        {
            var query = _context.Comments
                           .Include(c => c.Article)
                           .Include(c => c.User)
                           .AsQueryable();


            var allCommentsSorted = await query
                                    .OrderBy(c => c.ArticleId)
                                    .ThenBy(c => c.ParentCommentId ?? c.Id)
                                    .ThenBy(c => c.CreatedAt)
                                    .ToListAsync();

            // Xử lý lại để gom nhóm cha con (Giữ nguyên logic gom nhóm)
            var commentLookup = query.ToDictionary(c => c.Id);
            var topLevelComments = new List<Comment>(); // Danh sách chỉ chứa bình luận gốc

            foreach (var comment in query)
            {
                // Reset Replies để tránh dữ liệu cũ nếu dùng lại object từ cache (an toàn hơn)
                comment.Replies = new List<Comment>();
                if (comment.ParentCommentId.HasValue)
                {
                    // Nếu là con, tìm cha và thêm vào Replies của cha
                    if (commentLookup.TryGetValue(comment.ParentCommentId.Value, out var parentComment))
                    {
                        parentComment.Replies.Add(comment);
                    }
                    // (Optionally log warning if parent not found in the filtered list)
                }
                else
                {
                    // Nếu là gốc, thêm vào danh sách gốc
                    topLevelComments.Add(comment);
                }
            }

            // --- BƯỚC 3: Tạo danh sách phẳng theo thứ tự đệ quy ---
            var displayComments = new List<Comment>();
            // Hàm đệ quy nội bộ để duyệt cây
            void FlattenComments(IEnumerable<Comment> comments)
            {
                // Sắp xếp các bình luận ở cấp hiện tại (ví dụ: theo thời gian tạo)
                foreach (var comment in comments.OrderBy(c => c.CreatedAt))
                {
                    displayComments.Add(comment); // Thêm bình luận hiện tại vào danh sách hiển thị
                    if (comment.Replies.Any())
                    {
                        FlattenComments(comment.Replies); // Gọi đệ quy cho các con của nó
                    }
                }
            }

            // Bắt đầu duyệt từ các bình luận gốc, sắp xếp gốc theo ý muốn (ví dụ: mới nhất)
            FlattenComments(topLevelComments.OrderByDescending(c => c.CreatedAt));
            // --- KẾT THÚC TẠO DANH SÁCH PHẲNG ---


            // Tạo Map ID -> STT dựa trên danh sách hiển thị cuối cùng
            var commentOrderMap = displayComments
                                    .Select((comment, index) => new { comment.Id, Order = index + 1 })
                                    .ToDictionary(x => x.Id, x => x.Order);
            ViewBag.CommentOrderMap = commentOrderMap;


            // (Tùy chọn) Áp dụng phân trang cho displayComments nếu cần
            // Lưu ý: Phân trang sau khi flatten có thể cắt ngang "gia đình" bình luận
            // var pagedDisplayComments = displayComments.ToPagedList(pageNumber, pageSize);
            // return View(pagedDisplayComments);

            return View(displayComments);
        }

        [Authorize(Policy = "Freedom")]
        // GET: Comments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                                .Include(c => c.Article)          
                                .Include(c => c.User)              
                                .Include(c => c.Replies)           
                                    .ThenInclude(r => r.User)   
                                .FirstOrDefaultAsync(m => m.Id == id);

            if (comment == null)
            {
                return NotFound();
            }
            comment.Replies = comment.Replies?.OrderBy(r => r.CreatedAt).ToList() ?? new List<Comment>();
            return View(comment);
        }

        [Authorize(Policy = "Freedom")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            if (!comment.IsApproved)
            {
                comment.IsApproved = true;
                _context.Update(comment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã duyệt bình luận.";
            }
            else { TempData["WarningMessage"] = "Bình luận này đã được duyệt trước đó."; }
            return RedirectToAction(nameof(Index)); // Quay lại trang Index Comments
        }

        [Authorize(Policy = "Freedom")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnapproveComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            if (comment.IsApproved)
            {
                comment.IsApproved = false;
                _context.Update(comment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã bỏ duyệt bình luận.";
            }
            else { TempData["WarningMessage"] = "Bình luận này vốn chưa được duyệt."; }
            return RedirectToAction(nameof(Index)); // Quay lại trang Index Comments
        }

        [Authorize(Policy = "Freedom")]
        // GET: Comments/Create
        public IActionResult Create()
        {
            ViewData["ArticleId"] = new SelectList(_context.Articles, "Id", "Id");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        [Authorize(Policy = "Freedom")]
        // POST: Comments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Content,CreatedAt,IsApproved,UserId,ArticleId")] Comment comment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(comment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ArticleId"] = new SelectList(_context.Articles, "Id", "Id", comment.ArticleId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", comment.UserId);
            return View(comment);
        }

        [Authorize(Policy = "Freedom")]
        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }
            ViewData["ArticleId"] = new SelectList(_context.Articles, "Id", "Id", comment.ArticleId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", comment.UserId);
            return View(comment);
        }

        [Authorize(Policy = "Freedom")]
        // POST: Comments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Content,CreatedAt,IsApproved,UserId,ArticleId")] Comment comment)
        {
            if (id != comment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(comment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.Id))
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
            ViewData["ArticleId"] = new SelectList(_context.Articles, "Id", "Id", comment.ArticleId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", comment.UserId);
            return View(comment);
        }

        [Authorize(Policy = "Freedom")]
        //GET: xóa bình luận
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Tải bình luận cùng thông tin User và Article để hiển thị xác nhận
            var comment = await _context.Comments
                .Include(c => c.Article)
                .Include(c => c.User)
                // (Tùy chọn) Include Replies để biết có con không
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(m => m.Id == id.Value);

            if (comment == null)
            {
                return NotFound();
            }

            // (Tùy chọn) Đếm số lượng Replies để cảnh báo
            ViewData["ReplyCount"] = comment.Replies?.Count ?? 0;

            return View(comment); // Trả về View Delete.cshtml
        }

        [Authorize(Policy = "Freedom")]
        // --- POST: Comments/Delete/5 (XÁC NHẬN VÀ XỬ LÝ XÓA) ---
        [HttpPost, ActionName("Delete")] // Đặt tên Action là Delete nhưng route vẫn là /Comments/Delete/{id} (POST)
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) // Action này nhận ID từ form
        {
            // Không cần kiểm tra quyền ở đây nữa vì đã có [Authorize(Roles = "Admin")] ở trên Controller

            // --- GỌI HÀM XÓA ĐỆ QUY ---
            await DeleteCommentAndRepliesAsync(id);
            // -------------------------

            // --- LƯU THAY ĐỔI MỘT LẦN ---
            var changes = await _context.SaveChangesAsync();
            // ---------------------------

            if (changes > 0) // Kiểm tra xem có thực sự xóa gì không
            {
                TempData["SuccessMessage"] = $"Đã xóa bình luận (và {changes - 1} trả lời) thành công.";
            }
            else
            {
                TempData["WarningMessage"] = "Không tìm thấy bình luận để xóa hoặc đã bị xóa trước đó.";
            }

            // Chuyển hướng về trang Index của CommentsController
            return RedirectToAction(nameof(Index));
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
            }
        }
        private bool CommentExists(int id)
        {
            return _context.Comments.Any(e => e.Id == id);
        }
    }
}
