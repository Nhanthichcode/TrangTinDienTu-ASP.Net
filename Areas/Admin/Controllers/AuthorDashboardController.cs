using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trang_tin_điện_tử_mvc.Data;
using Trang_tin_điện_tử_mvc.Models;

[Area("Admin")]
public class AuthorDashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthorDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();
        ViewBag.TotalArticles = await _context.Articles.CountAsync();        
        ViewBag.TotalComments = await _context.Comments.CountAsync();

        // Thống kê bài viết theo tháng (ví dụ 12 tháng gần nhất)
        var monthLabels = Enumerable.Range(1, 12).Select(m => $"Tháng {m}").ToArray();
        var monthData = new List<int>();
        var now = DateTime.Now;

        for (int i = 1; i <= 12; i++)
        {
            var count = await _context.Articles
                .CountAsync(a => a.CreatedAt.Month == i && a.CreatedAt.Year == now.Year);
            monthData.Add(count);
        }

        ViewBag.MonthLabels = monthLabels;
        ViewBag.MonthData = monthData;

        return View();
    }
}
