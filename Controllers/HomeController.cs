using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Trang_tin_điện_tử_mvc.Data;
using Trang_tin_điện_tử_mvc.Models;
using X.PagedList.Extensions;

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

        // --- ACTION XỬ LÝ GỬI FORM LIÊN HỆ ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContactMessage(string contactName, string contactEmail, string subject, string message)
        {
            // Kiểm tra dữ liệu cơ bản (có thể dùng ViewModel và DataAnnotations để tốt hơn)
            if (string.IsNullOrWhiteSpace(contactName) ||
                string.IsNullOrWhiteSpace(contactEmail) ||
                string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["ContactError"] = "Vui lòng điền đầy đủ thông tin vào form.";
                return RedirectToAction("Contact");
            }

            try
            {
                // --- Logic gửi email ---
                var adminEmail = "nhan_dth225710@student.agu.edu.vn";
                var emailSubject = $"[E-News Contact] - {subject}";
                var emailBody = $"Bạn nhận được tin nhắn mới từ trang Liên hệ:<br/><br/>" +
                                $"<strong>Từ:</strong> {contactName} ({contactEmail})<br/>" +
                                $"<strong>Tiêu đề:</strong> {subject}<br/>" +
                                $"<strong>Nội dung:</strong><br/>{message.Replace(Environment.NewLine, "<br/>")}"; // Thay xuống dòng bằng <br>

                await _emailSender.SendEmailAsync(adminEmail, emailSubject, emailBody);

                TempData["ContactSuccess"] = "Tin nhắn của bạn đã được gửi thành công! Chúng tôi sẽ phản hồi sớm nhất có thể.";
                _logger.LogInformation("Đã gửi email liên hệ thành công từ {ContactEmail}", contactEmail);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email liên hệ từ {ContactEmail}", contactEmail);
                TempData["ContactError"] = "Đã xảy ra lỗi khi gửi tin nhắn. Vui lòng thử lại sau hoặc liên hệ trực tiếp qua email.";
            }

            return RedirectToAction("Contact"); // Quay lại trang Contact
        }
        public IActionResult Terms()
        {
            // Trả về View có tên tương ứng (GioiThieu.cshtml)
            return View();
        }
        public IActionResult Contact()
        {
            // Bạn có thể truyền thêm dữ liệu vào View nếu cần, ví dụ thông tin liên hệ từ cấu hình
            ViewBag.ContactEmail = "nhan_dth225710@student.agu.edu.vn";
            ViewBag.ContactPhone = "0123 456 789";
            ViewBag.ContactAddress = "Phường Bình Đức, Tỉnh An Giang, Việt Nam";
            return View();
        }
        public async Task<IActionResult> Index(int pageNumber = 1) // Thêm tham số pageNumber
        {
            int pageSize = DefaultPageSize; // Lấy page size

            // Tạo query cơ sở (chỉ lấy bài đã duyệt)
            var articlesQuery = _context.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Where(a => a.IsApproved)
                .OrderByDescending(a => a.CreatedAt);

            // Thực hiện phân trang bằng PagedList.Core
            // Cần cài đặt NuGet package: X.PagedList.Mvc.Core
            var pagedArticles = articlesQuery.ToPagedList(pageNumber, pageSize);

            // --- Lấy dữ liệu cho Sidebar (giữ nguyên logic cũ) ---
            var featuredArticles = await articlesQuery.Take(3).ToListAsync(); // Lấy 3 bài nổi bật từ query đã sắp xếp
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            var categoryCounts = await _context.Articles
                .Where(a => a.IsApproved)
                .GroupBy(a => a.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
            var tags = await _context.Tags.OrderBy(t => t.Name).Take(15).ToListAsync(); // Giới hạn số tag

            ViewBag.Featured = featuredArticles;
            ViewBag.Categories = categories;
            ViewBag.CategoryCounts = categoryCounts;
            ViewBag.Tags = tags;
            // --- Kết thúc lấy dữ liệu Sidebar ---

            // --- KIỂM TRA REQUEST AJAX ---
            // Nếu header "X-Requested-With" là "XMLHttpRequest", đây là yêu cầu AJAX
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Trả về Partial View chỉ chứa các card bài viết mới
                return PartialView("_ArticleListPartial", pagedArticles);
            }

            // Nếu là request thường, trả về View Index với dữ liệu ban đầu và thông tin phân trang
            return View(pagedArticles); // Truyền đối tượng PagedList làm Model
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
