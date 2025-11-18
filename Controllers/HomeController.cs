using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Trang_tin_điện_tử_mvc.Data;
using X.PagedList.EF;
using Trang_tin_điện_tử_mvc.Models;

namespace Trang_tin_điện_tử_mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private const int DefaultPageSize = 6;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IEmailSender emailSender)
        {
            _logger = logger;
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<IActionResult> GetSearchSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Json(new List<object>());
            var suggestions = await _context.Articles.AsNoTracking()
                .Where(a => a.IsApproved && (a.Title.Contains(query) || a.Summary.Contains(query)))
                .OrderByDescending(a => a.CreatedAt).Take(5)
                .Select(a => new { id = a.Id, title = a.Title, image = a.ThumbnailUrl ?? "/uploads/articles/default-thumbnail.jpg", date = a.CreatedAt.ToString("dd/MM") })
                .ToListAsync();
            return Json(suggestions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContactMessage(string contactName, string contactEmail, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(contactName) || string.IsNullOrWhiteSpace(contactEmail) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ContactError"] = "Vui lòng điền đầy đủ thông tin.";
                return RedirectToAction("Contact");
            }
            try
            {
                var adminEmail = "nhan_dth225710@student.agu.edu.vn";
                await _emailSender.SendEmailAsync(adminEmail, $"[E-News] {subject}", $"Từ: {contactName} ({contactEmail})<br/>Nội dung:<br/>{message.Replace("\n", "<br/>")}");
                TempData["ContactSuccess"] = "Gửi thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi mail");
                TempData["ContactError"] = "Lỗi khi gửi tin nhắn.";
            }
            return RedirectToAction("Contact");
        }

        public IActionResult Terms() => View();
        public IActionResult Contact()
        {
            ViewBag.ContactEmail = "nhan_dth225710@student.agu.edu.vn";
            return View();
        }

        public async Task<IActionResult> Index(string searchString, int? categoryId, string tag, int pageNumber = 1)
        {
            int pageSize = DefaultPageSize;

            // 1. Query cơ bản
            var baseQuery = _context.Articles
                .Include(a => a.Author).Include(a => a.Category).Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
                .Where(a => a.IsApproved)
                .OrderByDescending(a => a.CreatedAt)
                .AsQueryable();

            // 2. Xử lý Lọc
            bool isFiltered = false;

            if (!string.IsNullOrEmpty(tag))
            {
                baseQuery = baseQuery.Where(a => a.ArticleTags.Any(at => at.Tag.Name == tag));
                ViewData["CurrentTag"] = tag;
                ViewData["Title"] = $"Tag: {tag}";
                isFiltered = true;
            }
            else if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                baseQuery = baseQuery.Where(a => a.Title.Contains(searchString) || a.Summary.Contains(searchString));
                ViewData["CurrentFilter"] = searchString;
                ViewData["Title"] = $"Tìm kiếm: {searchString}";
                isFiltered = true;
            }
            else if (categoryId.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.CategoryId == categoryId);
                var catName = await _context.Categories.Where(c => c.Id == categoryId).Select(c => c.Name).FirstOrDefaultAsync();
                ViewData["CurrentCategoryId"] = categoryId;
                ViewData["Title"] = catName ?? "Chuyên mục";
                isFiltered = true;
            }
            else
            {
                ViewData["Title"] = "Trang chủ";
            }

            // 3. Phân trang
            var pagedArticles = await baseQuery.AsNoTracking().ToPagedListAsync(pageNumber, pageSize);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ArticleListPartial", pagedArticles);
            }

            var viewModel = new HomeIndexViewModel { LatestArticles = pagedArticles };

            // 4. Chỉ load Hero/Box chuyên mục khi KHÔNG lọc (Trang chủ thuần túy)
            if (!isFiltered)
            {
                viewModel.TopStory = await baseQuery.AsNoTracking().FirstOrDefaultAsync();
                viewModel.BusinessStories = await _context.Articles.AsNoTracking().Where(a => a.IsApproved && a.Category.Name == "Kinh Tế").OrderByDescending(a => a.CreatedAt).Take(4).ToListAsync();
                viewModel.TechStories = await _context.Articles.AsNoTracking().Where(a => a.IsApproved && a.Category.Name == "Công Nghệ").OrderByDescending(a => a.CreatedAt).Take(4).ToListAsync();
            }

            // Sidebar (Luôn hiện)
            viewModel.FeaturedArticles = await _context.Articles.Where(a => a.IsApproved).OrderByDescending(a => a.CreatedAt).Take(6).AsNoTracking().ToListAsync();
            viewModel.Categories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            viewModel.CategoryCounts = await _context.Articles.Where(a => a.IsApproved).GroupBy(a => a.CategoryId).Select(g => new { CategoryId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.CategoryId, x => x.Count);
            viewModel.Tags = await _context.Tags.AsNoTracking().OrderBy(t => t.Name).Take(15).ToListAsync();

            return View(viewModel);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}