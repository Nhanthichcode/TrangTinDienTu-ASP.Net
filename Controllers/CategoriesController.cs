using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // Thêm nếu cần phân quyền
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trang_tin_điện_tử_mvc.Data; // Namespace DbContext của bạn
using Trang_tin_điện_tử_mvc.Models; // Namespace Model của bạn

namespace Trang_tin_điện_tử_mvc.Controllers
{
    [Authorize(Policy = "RequireAdminRole")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories (Xem danh sách)
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                                        .OrderBy(c => c.Name) // Sắp xếp theo tên
                                        .ToListAsync();
            return View(categories);
        }

        // GET: Categories/Details/5 (Xem chi tiết)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Categories/Create (Hiển thị form tạo mới)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create (Xử lý tạo mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Slug,Description")] Category category)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra Slug trùng lặp (QUAN TRỌNG)
                bool slugExists = await _context.Categories.AnyAsync(c => c.Slug == category.Slug);
                if (slugExists)
                {
                    ModelState.AddModelError("Slug", "Slug này đã tồn tại. Vui lòng chọn slug khác.");
                    return View(category); // Trả về form với lỗi
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo danh mục '{category.Name}' thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5 (Hiển thị form sửa)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5 (Xử lý sửa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Slug,Description")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra Slug trùng lặp (ngoại trừ chính nó)
                bool slugExists = await _context.Categories.AnyAsync(c => c.Slug == category.Slug && c.Id != category.Id);
                if (slugExists)
                {
                    ModelState.AddModelError("Slug", "Slug này đã tồn tại. Vui lòng chọn slug khác.");
                    return View(category);
                }

                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật danh mục '{category.Name}' thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
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
            return View(category);
        }

        // GET: Categories/Delete/5 (Hiển thị trang xác nhận xóa)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            // Kiểm tra xem danh mục có đang được sử dụng bởi bài viết nào không
            bool isInUse = await _context.Articles.AnyAsync(a => a.CategoryId == id);
            if (isInUse)
            {
                ViewData["ErrorMessage"] = $"Không thể xóa danh mục '{category.Name}' vì đang có bài viết thuộc danh mục này.";
            }


            return View(category);
        }

        // POST: Categories/Delete/5 (Xác nhận và xử lý xóa)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Kiểm tra lại xem có đang được sử dụng không (phòng trường hợp dữ liệu thay đổi)
            bool isInUse = await _context.Articles.AnyAsync(a => a.CategoryId == id);
            if (isInUse)
            {
                // Nếu đang dùng, không cho xóa và báo lỗi
                TempData["ErrorMessage"] = "Không thể xóa danh mục này vì đang được sử dụng bởi bài viết.";
                // Có thể redirect về trang Delete để hiển thị lỗi, hoặc về Index
                // return RedirectToAction(nameof(Delete), new { id = id });
                return RedirectToAction(nameof(Index));
            }

            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa danh mục '{category.Name}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục để xóa.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Hàm kiểm tra sự tồn tại (dùng trong Edit)
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}