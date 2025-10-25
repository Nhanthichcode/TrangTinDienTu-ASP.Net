using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trang_tin_điện_tử_mvc.Models;

namespace Trang_tin_điện_tử_mvc.Data
{
    public class DataSeeder
    {
        public static async Task SeedAsync(
           IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Đảm bảo DB đã tồn tại
            await context.Database.MigrateAsync();

            // Seed Roles
            string[] roles = { "Admin", "Author", "User" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            var adminEmail = "admin1@news.com";
            var adminEmail1 = "sa@news.com";
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
                        AvatarUrl = "/uploads/avatars/default-images.png"
                    };
                    await userManager.CreateAsync(user, password);
                    await userManager.AddToRoleAsync(user, role);
                }
                return user;
            }

            var admin = await EnsureUser(adminEmail, "Admin@123", "Admin", "Quản trị viên");
            var admin1 = await EnsureUser(adminEmail1, "Admin@123", "Admin", "Quản trị viên");
            var author = await EnsureUser(authorEmail, "Author@123", "Author", "Tác giả Tin tức");
            var normalUser = await EnsureUser(userEmail, "User@123", "User", "Người dùng thường");

            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Công nghệ", Slug = "cong-nghe", Description = "Tin tức công nghệ mới nhất" },
                    new Category { Name = "Thể thao", Slug = "the-thao", Description = "Tin tức thể thao trong và ngoài nước" },
                    new Category { Name = "Giải trí", Slug = "giai-tri", Description = "Showbiz, phim ảnh, âm nhạc" },
                    new Category { Name = "Kinh tế", Slug = "kinh-te", Description = "Tin tức kinh tế, thị trường, tài chính" },
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();

                if (!context.Tags.Any())
                {
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
                }
            }
            if (!context.Articles.Any())
            {
                author = await userManager.FindByNameAsync("some_author_username");
                var catTech = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Công nghệ");
                var catTravel = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Du lịch");
                var catFood = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Ẩm thực");
                var catLife = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Đời sống");
                //Nếu chưa có, bạn cần thay thế author.Id và catTech.Id bằng ID chuỗi/ số hợp lệ.

                // --- THAY THẾ ID THỰC TẾ VÀO ĐÂY ---
                string authorIdPlaceholder = author?.Id ?? "50352235-f22c-4295-9f43-83351e77e91e"; // Thay "default-author-id" bằng ID hợp lệ nếu author là null
                int catTechIdPlaceholder = catTech?.Id ?? 1; // Thay 1 bằng ID Category hợp lệ nếu catTech là null
                int catTravelIdPlaceholder = catTravel?.Id ?? 2;
                int catFoodIdPlaceholder = catFood?.Id ?? 3;
                int catLifeIdPlaceholder = catLife?.Id ?? 4;

                var random = new Random();
                var articlesToSeed = new List<Article>();
                var startDate = DateTime.Now;

                for (int i = 1; i <= 50; i++)
                {
                    string title = $"Bài viết Mẫu số {i}: Chủ đề ngẫu nhiên";
                    string summary = $"Đây là tóm tắt ngắn gọn cho bài viết mẫu số {i}.";
                    string content = $"Đây là nội dung chi tiết được tạo tự động cho bài viết mẫu số {i}. Phần nội dung này cần đủ dài để kiểm tra hiển thị. ";
                    string thumbnailUrl = $"/uploads/articles/sample{i % 10 + 1}.jpg"; // Dùng lặp lại 10 ảnh mẫu
                    DateTime createdAt = startDate.AddDays(-i).AddHours(random.Next(-12, 12)); // Ngày tạo lùi dần và giờ ngẫu nhiên
                    bool isApproved = random.Next(0, 5) > 0; // 80% được duyệt (4/5)
                    int viewCount = random.Next(50, 2000);
                    int categoryId = catTechIdPlaceholder; // Mặc định

                    // Phân loại chủ đề và nội dung ngẫu nhiên
                    int topic = i % 4; // Chia đều cho 4 chủ đề
                    switch (topic)
                    {
                        case 0: // Công nghệ
                            title = $"Tin tức Công nghệ {i}: Đột phá mới";
                            summary = $"Khám phá những xu hướng công nghệ mới nhất định hình tương lai, bài viết số {i}.";
                            content += "Lĩnh vực công nghệ thông tin đang chứng kiến những bước tiến vượt bậc. Trí tuệ nhân tạo (AI) không còn là khái niệm xa vời mà đã len lỏi vào mọi khía cạnh đời sống, từ trợ lý ảo thông minh đến xe tự lái. Bên cạnh đó, công nghệ blockchain đang cách mạng hóa cách chúng ta thực hiện giao dịch và lưu trữ dữ liệu một cách an toàn, minh bạch. Điện toán đám mây tiếp tục là nền tảng vững chắc cho sự phát triển của các ứng dụng và dịch vụ trực tuyến. Đừng quên Internet of Things (IoT) đang kết nối hàng tỷ thiết bị, tạo ra một thế giới thông minh hơn. Bài viết này sẽ đi sâu phân tích những tác động và tiềm năng của các công nghệ này.";
                            categoryId = catTechIdPlaceholder;
                            break;
                        case 1: // Du lịch
                            title = $"Khám phá Điểm đến {i}: Kinh nghiệm du lịch";
                            summary = $"Chia sẻ những bí kíp và trải nghiệm thực tế tại điểm đến hấp dẫn số {i}.";
                            content += $"Lên kế hoạch cho một chuyến đi luôn là điều thú vị. Bài viết {i} sẽ cung cấp cho bạn những thông tin hữu ích về một địa điểm du lịch tuyệt vời. Từ việc lựa chọn thời điểm lý tưởng, săn vé máy bay giá rẻ, tìm kiếm chỗ ở phù hợp với túi tiền, đến việc khám phá những món ăn địa phương độc đáo và các hoạt động không thể bỏ lỡ. Chúng tôi cũng chia sẻ những lưu ý về văn hóa, an toàn và cách chuẩn bị hành lý gọn nhẹ mà hiệu quả. Hãy chuẩn bị sẵn sàng cho hành trình khám phá những vùng đất mới đầy màu sắc và thú vị!";
                            categoryId = catTravelIdPlaceholder;
                            break;
                        case 2: // Ẩm thực
                            title = $"Công thức Nấu ăn {i}: Món ngon tại nhà";
                            summary = $"Hướng dẫn chi tiết cách chế biến món ăn hấp dẫn số {i} ngay tại căn bếp của bạn.";
                            content += $"Bạn yêu thích nấu nướng và muốn tự tay chuẩn bị những bữa ăn ngon cho gia đình? Công thức số {i} này sẽ là gợi ý tuyệt vời. Chúng tôi sẽ hướng dẫn bạn từng bước, từ khâu chuẩn bị nguyên liệu tươi ngon, các kỹ thuật sơ chế, đến bí quyết nêm nếm gia vị sao cho vừa miệng và cách trình bày món ăn đẹp mắt. Dù bạn là người mới bắt đầu hay đã có kinh nghiệm, công thức này đều dễ dàng thực hiện. Hãy vào bếp và trổ tài ngay thôi!";
                            categoryId = catFoodIdPlaceholder;
                            break;
                        case 3: // Đời sống
                            title = $"Câu chuyện Đời sống {i}: Góc nhìn & Chia sẻ";
                            summary = $"Những câu chuyện, bài học và góc nhìn đa chiều về cuộc sống, bài viết số {i}.";
                            content += $"Cuộc sống muôn màu muôn vẻ với vô vàn câu chuyện và cảm xúc. Bài viết {i} là nơi chia sẻ những góc nhìn, trải nghiệm cá nhân về các vấn đề thường nhật, từ mối quan hệ gia đình, bạn bè, tình yêu, đến công việc, sự nghiệp và những trăn trở trong hành trình phát triển bản thân. Chúng ta cùng suy ngẫm về những giá trị cốt lõi, học cách đối mặt với khó khăn, tìm kiếm niềm vui và ý nghĩa trong từng khoảnh khắc. Hy vọng những chia sẻ này sẽ mang đến sự đồng cảm và nguồn cảm hứng tích cực cho bạn.";
                            categoryId = catLifeIdPlaceholder;
                            break;
                    }

                    // Thêm nội dung lặp lại để đủ dài
                    content += " " + content + " Lặp lại để tăng độ dài.";
                    content += " " + content; // Lặp lại lần nữa

                    articlesToSeed.Add(new Article
                    {
                        Title = title,
                        Summary = summary,
                        Content = content,
                        ThumbnailUrl = thumbnailUrl,
                        CreatedAt = createdAt,
                        UpdatedAt = null, // Có thể để null ban đầu
                        IsApproved = isApproved,
                        ViewCount = viewCount,
                        AuthorId = authorIdPlaceholder,
                        CategoryId = categoryId
                    });
                }
                context.Articles.AddRange(articlesToSeed);
                await context.SaveChangesAsync();
            }
        }
    }    
}
