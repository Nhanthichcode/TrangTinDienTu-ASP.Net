using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trang_tin_điện_tử_mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Trang_tin_điện_tử_mvc.Data
{
    public class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILogger<DataSeeder>>();

            try
            {
                logger.LogInformation("Bắt đầu Seed dữ liệu...");

                // Vô hiệu hóa MigrateAsync vì đã chạy Update-Database thủ công
                await context.Database.MigrateAsync();

                // --- 1. SEED ROLES ---
                string[] roles = { "Admin", "Author", "User" };
                foreach (var roleName in roles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                        logger.LogInformation($"Đã tạo vai trò: {roleName}");
                    }
                }
                logger.LogInformation("Seed Roles hoàn tất.");

                // --- 2. SEED USERS ---
                var adminEmail = "admin@news.com";
                var admin1Email1 = "admin1@news.com";
                var authorEmail = "author@news.com";
                var userEmail = "user@news.com";

                async Task<ApplicationUser> EnsureUser(string email, string password, string role, string fullName)
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            EmailConfirmed = true,
                            FullName = fullName,
                            AvatarUrl = "/uploads/avatars/default-images.png",
                            IsApproved = true
                        };
                        var createResult = await userManager.CreateAsync(user, password);
                        if (createResult.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, role);
                            logger.LogInformation($"Đã tạo người dùng: {email} với vai trò: {role}");
                        }
                        else
                        {
                            foreach (var error in createResult.Errors) { logger.LogError($"Lỗi khi tạo user {email}: {error.Description}"); }
                        }
                    }
                    return user;
                }

                var admin = await EnsureUser(adminEmail, "Admin@123", "Admin", "Quản trị viên");
                var admin1 = await EnsureUser(admin1Email1, "Admin@123", "Admin", "Super Admin");
                var author = await EnsureUser(authorEmail, "Author@123", "Author", "Tác giả Tin tức");
                var normalUser = await EnsureUser(userEmail, "User@123", "User", "Người dùng thường");
                logger.LogInformation("Seed Users hoàn tất.");

                // --- 3. SEED CATEGORIES & TAGS ---
                if (!context.Categories.Any())
                {
                    logger.LogInformation("Bắt đầu Seed Categories...");
                    var categories = new List<Category>
                    {
                        new Category { Name = "Công nghệ", Slug = "cong-nghe", Description = "Tin tức công nghệ mới nhất" },
                        new Category { Name = "Thể thao", Slug = "the-thao", Description = "Tin tức thể thao trong và ngoài nước" },
                        new Category { Name = "Giải trí", Slug = "giai-tri", Description = "Showbiz, phim ảnh, âm nhạc" },
                        new Category { Name = "Đời sống", Slug = "doi-song", Description = "Câu chuyện, góc nhìn về cuộc sống" },
                        new Category { Name = "Ẩm thực", Slug = "am-thuc", Description = "Công thức, nhà hàng, món ngon" }
                    };
                    await context.Categories.AddRangeAsync(categories);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seed Categories hoàn tất.");
                }

                if (!context.Tags.Any())
                {
                    logger.LogInformation("Bắt đầu Seed Tags...");
                    var tags = new List<Tag>
                    {
                        new Tag { Name = "AI", Slug = "ai" },
                        new Tag { Name = "Công nghệ", Slug = "cong-nghe" },
                        new Tag { Name = "Bóng đá", Slug = "bong-da" },
                        new Tag { Name = "Phim ảnh", Slug = "phim-anh" },
                        new Tag { Name = "Kinh doanh", Slug = "kinh-doanh" }
                    };
                    await context.Tags.AddRangeAsync(tags);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seed Tags hoàn tất.");
                }

                // --- 4. SEED ARTICLES (SAU KHI ĐÃ CÓ CATEGORIES VÀ USERS) ---
                if (!context.Articles.Any())
                {
                    logger.LogInformation("Bắt đầu Seed Articles...");

                    var articleAuthor = await userManager.FindByEmailAsync(authorEmail);
                    var catTech = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "cong-nghe");
                    var catSports = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "the-thao");
                    var catFood = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "am-thuc");
                    var catLife = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "doi-song");

                    if (articleAuthor == null)
                    {
                        logger.LogError("Không tìm thấy tác giả 'author@news.com' để gán bài viết.");
                        return; // Dừng lại nếu thiếu tác giả
                    }
                    if (catTech == null || catSports == null || catFood == null || catLife == null)
                    {
                        logger.LogError("Một hoặc nhiều danh mục không tồn tại. Seed Articles thất bại.");
                        return; // Dừng lại nếu thiếu danh mục
                    }

                    string authorIdPlaceholder = articleAuthor.Id;
                    int catTechIdPlaceholder = catTech.Id;
                    int catSportsIdPlaceholder = catSports.Id;
                    int catFoodIdPlaceholder = catFood.Id;
                    int catLifeIdPlaceholder = catLife.Id;

                    var random = new Random();
                    var articlesToSeed = new List<Article>();
                    var startDate = DateTime.Now;

                    for (int i = 1; i <= 50; i++)
                    {
                        string title = $"Bài viết Mẫu số {i}: Chủ đề ngẫu nhiên";
                        string summary = $"Đây là tóm tắt ngắn gọn cho bài viết mẫu số {i}.";
                        string content = $"Đây là nội dung chi tiết được tạo tự động cho bài viết mẫu số {i}. Phần nội dung này cần đủ dài để kiểm tra hiển thị. ";
                        string thumbnailUrl = $"/uploads/articles/sample{i % 10 + 1}.jpg";
                        DateTime createdAt = startDate.AddDays(-i).AddHours(random.Next(-12, 12));
                        bool isApproved = random.Next(0, 5) > 0;
                        int viewCount = random.Next(50, 2000);
                        int categoryId = catTechIdPlaceholder;

                        int topic = i % 4;
                        switch (topic)
                        {
                            case 0:
                                title = $"Tin tức Công nghệ {i}: Đột phá mới";
                                summary = $"Khám phá những xu hướng công nghệ mới nhất định hình tương lai, bài viết số {i}.";
                                content += "Lĩnh vực công nghệ thông tin đang chứng kiến những bước tiến vượt bậc. Trí tuệ nhân tạo (AI) không còn là khái niệm xa vời mà đã len lỏi vào mọi khía cạnh đời sống, từ trợ lý ảo thông minh đến xe tự lái. Bên cạnh đó, công nghệ blockchain đang cách mạng hóa cách chúng ta thực hiện giao dịch và lưu trữ dữ liệu một cách an toàn, minh bạch. Điện toán đám mây tiếp tục là nền tảng vững chắc cho sự phát triển của các ứng dụng và dịch vụ trực tuyến. Đừng quên Internet of Things (IoT) đang kết nối hàng tỷ thiết bị, tạo ra một thế giới thông minh hơn. Bài viết này sẽ đi sâu phân tích những tác động và tiềm năng của các công nghệ này.";
                                categoryId = catTechIdPlaceholder;
                                break;
                            case 1:
                                title = $"Tin Thể thao {i}: Tổng hợp";
                                summary = $"Cập nhật tin tức thể thao nóng hổi, bình luận chuyên sâu, bài viết số {i}.";
                                content += $"Kết quả các trận đấu bóng đá đỉnh cao vừa kết thúc. Phân tích chiến thuật của các đội bóng hàng đầu. Câu chuyện hậu trường của các vận động viên. Thị trường chuyển nhượng đang nóng lên từng ngày với những bản hợp đồng bom tấn. Bên cạnh bóng đá, các môn thể thao khác như tennis, bóng rổ, đua xe F1 cũng liên tục có những diễn biến hấp dẫn. Hãy cùng chúng tôi theo dõi và cập nhật những thông tin thể thao mới nhất.";
                                categoryId = catSportsIdPlaceholder;
                                break;
                            case 2:
                                title = $"Công thức Nấu ăn {i}: Món ngon tại nhà";
                                summary = $"Hướng dẫn chi tiết cách chế biến món ăn hấp dẫn số {i} ngay tại căn bếp của bạn.";
                                content += $"Bạn yêu thích nấu nướng và muốn tự tay chuẩn bị những bữa ăn ngon cho gia đình? Công thức số {i} này sẽ là gợi ý tuyệt vời. Chúng tôi sẽ hướng dẫn bạn từng bước, từ khâu chuẩn bị nguyên liệu tươi ngon, các kỹ thuật sơ chế, đến bí quyết nêm nếm gia vị sao cho vừa miệng và cách trình bày món ăn đẹp mắt. Dù bạn là người mới bắt đầu hay đã có kinh nghiệm, công thức này đều dễ dàng thực hiện. Hãy vào bếp và trổ tài ngay thôi!";
                                categoryId = catFoodIdPlaceholder;
                                break;
                            case 3:
                                title = $"Câu chuyện Đời sống {i}: Góc nhìn & Chia sẻ";
                                summary = $"Những câu chuyện, bài học và góc nhìn đa chiều về cuộc sống, bài viết số {i}.";
                                content += $"Cuộc sống muôn màu muôn vẻ với vô vàn câu chuyện và cảm xúc. Bài viết {i} là nơi chia sẻ những góc nhìn, trải nghiệm cá nhân về các vấn đề thường nhật, từ mối quan hệ gia đình, bạn bè, tình yêu, đến công việc, sự nghiệp và những trăn trở trong hành trình phát triển bản thân. Chúng ta cùng suy ngẫm về những giá trị cốt lõi, học cách đối mặt với khó khăn, tìm kiếm niềm vui và ý nghĩa trong từng khoảnh khắc. Hy vọng những chia sẻ này sẽ mang đến sự đồng cảm và nguồn cảm hứng tích cực cho bạn.";
                                categoryId = catLifeIdPlaceholder;
                                break;
                        }

                        content += " " + content + " Lặp lại để tăng độ dài.";
                        content += " " + content;

                        articlesToSeed.Add(new Article
                        {
                            Title = title,
                            Summary = summary,
                            Content = content,
                            ThumbnailUrl = thumbnailUrl,
                            CreatedAt = createdAt,
                            UpdatedAt = null,
                            IsApproved = isApproved,
                            ViewCount = viewCount,
                            AuthorId = authorIdPlaceholder,
                            CategoryId = categoryId
                        });
                    }
                    await context.Articles.AddRangeAsync(articlesToSeed);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seed 50 Articles hoàn tất.");
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Đã xảy ra lỗi nghiêm trọng trong khi chạy DataSeeder.");
                // throw; // <-- SỬA LỖI: Vô hiệu hóa dòng này để không làm sập app khi deploy
            }
        }
    }
}