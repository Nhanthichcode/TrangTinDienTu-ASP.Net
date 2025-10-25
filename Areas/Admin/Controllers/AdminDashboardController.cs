using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trang_tin_điện_tử_mvc.Data;
using Trang_tin_điện_tử_mvc.Models;

[Area("Admin")]
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        // 1. Thống kê tổng quan
        ViewBag.TotalUsers = await _context.Users.CountAsync();
        ViewBag.TotalArticles = await _context.Articles.CountAsync();
        ViewBag.PendingArticles = await _context.Articles.CountAsync(a => !a.IsApproved);
        ViewBag.TotalComments = await _context.Comments.CountAsync();

        // 2. Dữ liệu cho biểu đồ cột (Bài viết theo tháng)
        var monthlyArticles = await _context.Articles
            .Where(a => a.CreatedAt.Year == DateTime.Now.Year)
            .GroupBy(a => a.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToListAsync();

        var monthLabels = new string[12];
        var monthData = new int[12];
        for (int i = 1; i <= 12; i++)
        {
            monthLabels[i - 1] = $"Thg {i}";
            var monthStat = monthlyArticles.FirstOrDefault(m => m.Month == i);
            monthData[i - 1] = monthStat?.Count ?? 0;
        }      

        ViewBag.MonthLabels = monthLabels;
        ViewBag.MonthData = monthData;

        // 3. Dữ liệu cho biểu đồ tròn (Bài viết theo danh mục)
        var articlesByCategory = await _context.Articles
            .GroupBy(a => a.Category.Name)
            .Select(g => new { CategoryName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        ViewBag.CategoryLabels = articlesByCategory.Select(x => x.CategoryName).ToList();
        ViewBag.CategoryData = articlesByCategory.Select(x => x.Count).ToList();

        // 4. Danh sách các bài viết chờ duyệt gần đây
        ViewBag.RecentPendingArticles = await _context.Articles
            .Include(a => a.Author)
            .Where(a => !a.IsApproved)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();


        const int topN = 10; // Số lượng top muốn hiển thị

        // 5. Top Tác giả theo Số bài viết
        var topAuthors = await _context.Articles
            .Include(a => a.Author) // Cần Include Author để lấy UserName
            .GroupBy(a => new { a.AuthorId, AuthorName = a.Author.UserName ?? "N/A" }) // Nhóm theo ID và Tên Tác giả
            .Select(g => new { AuthorName = g.Key.AuthorName, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(topN)
            .ToListAsync();

        ViewBag.TopAuthorLabels = topAuthors.Select(x => x.AuthorName).ToList();
        ViewBag.TopAuthorData = topAuthors.Select(x => x.Count).ToList();


        // 6. Top Bài viết theo Số bình luận
        var topCommentArticlesQuery = _context.Comments
            .GroupBy(c => c.ArticleId) // Nhóm theo bài viết
            .Select(g => new { ArticleId = g.Key, CommentCount = g.Count() }) // Đếm số bình luận
            .OrderByDescending(x => x.CommentCount) // Sắp xếp
            .Take(topN); // Lấy top N

        // Lấy Title của các bài viết top
        var topCommentArticlesData = await topCommentArticlesQuery
            .Join(_context.Articles, // Join với bảng Articles
                  commentStat => commentStat.ArticleId, // Khóa từ Comments
                  article => article.Id, // Khóa từ Articles
                  (commentStat, article) => new // Kết quả join
                  {
                      ArticleTitle = article.Title ?? $"Bài viết ID: {article.Id}", // Lấy Title
                      commentStat.CommentCount
                  })
            .ToListAsync();

        ViewBag.TopCommentArticleLabels = topCommentArticlesData.Select(x => x.ArticleTitle).ToList();
        ViewBag.TopCommentArticleData = topCommentArticlesData.Select(x => x.CommentCount).ToList();
        return View();
    }
}
