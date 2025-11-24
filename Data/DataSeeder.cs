using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trang_tin_điện_tử_mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace Trang_tin_điện_tử_mvc.Data
{
    public class DataSeeder
    {
        // Mật khẩu mặc định mạnh để vượt qua chính sách của Identity
        private const string DefaultPassword = "Password@123";
        public static async Task Initialize(IServiceProvider serviceProvider)
        {

                using var scope = serviceProvider.CreateScope();
                var services = scope.ServiceProvider;

                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var logger = services.GetRequiredService<ILogger<DataSeeder>>(); try
                {
                    await context.Database.MigrateAsync();
                    logger.LogInformation("<>==========đã tạo thành công DB");
                    // Kiểm tra nếu đã có dữ liệu thì không seed lại
                    if (context.Articles.Any())
                        {
                        logger.LogInformation("<>==========đã có dữ liệu trong DB");
                        return; // DB has been seeded
                        }

                // 1. Seed Roles và Users
                await SeedUsersAndRoles(userManager, roleManager);

                // Lấy user Author để gán cho bài viết
                var authorUser = await userManager.FindByEmailAsync("author@agu.edu.vn");
                if (authorUser == null) return; // Should not happen

                // 2. Seed Categories (10 danh mục)
                await SeedCategories(context);
                var categories = await context.Categories.ToListAsync();

                // 3. Seed Tags (15 thẻ)
                await SeedTags(context);
                var tags = await context.Tags.ToListAsync();

                // 4. Seed Articles (100 bài viết)
                await SeedArticles(context, authorUser, categories, tags);
                    logger.LogInformation("<>==========đã tạo thành công bài viết");

                }
                catch (Exception ex)
                {
                    // Ghi log lỗi nếu có (Lấy logger từ services của scope cũng được)
                    logger.LogError(ex, "<>==========Đã xảy ra lỗi khi khởi tạo dữ liệu (Seeding DB).");
                }            
        }

        private static async Task SeedUsersAndRoles(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "Author", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Tạo Admin User
            if (await userManager.FindByEmailAsync("admin@agu.edu.vn") == null)
            {
                var adminUser = new ApplicationUser { UserName = "admin@agu.edu.vn", Email = "admin@agu.edu.vn", FullName = "Quản trị viên AGU", EmailConfirmed = true};
                var result = await userManager.CreateAsync(adminUser, DefaultPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Tạo Author User (Người viết bài chính)
            if (await userManager.FindByEmailAsync("author@agu.edu.vn") == null)
            {
                var authorUser = new ApplicationUser { UserName = "author@agu.edu.vn", Email = "author@agu.edu.vn", FullName = "Ban Biên Tập AGU", EmailConfirmed = true, AvatarUrl = "/uploads/avatars/default-iamges.png" };
                var result = await userManager.CreateAsync(authorUser, DefaultPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(authorUser, "Author");
                }
            }
        }

        private static async Task SeedCategories(ApplicationDbContext context)
        {
            var categories = new List<Category>
            {
                new Category { Name = "Giáo dục & Đào tạo", Description = "Tin tức về hoạt động dạy và học." },
                new Category { Name = "Nghiên cứu khoa học", Description = "Các công trình, hội thảo nghiên cứu." },
                new Category { Name = "Văn hóa - Văn nghệ", Description = "Hoạt động văn hóa phong trào." },
                new Category { Name = "Thể thao học đường", Description = "Các giải đấu và hoạt động thể chất." },
                new Category { Name = "Thành tích nổi bật", Description = "Gương mặt tiêu biểu và giải thưởng." },
                new Category { Name = "Định hướng & Việc làm", Description = "Hướng nghiệp và cơ hội thực tập." },
                new Category { Name = "Điểm mới Tuyển sinh", Description = "Thông tin tuyển sinh mới nhất." },
                new Category { Name = "Tin tức chung", Description = "Các tin tức hoạt động khác của trường." },
                new Category { Name = "Hoạt động Đoàn - Hội", Description = "Phong trào thanh niên sinh viên." },
                new Category { Name = "Hợp tác Quốc tế", Description = "Các chương trình liên kết, trao đổi." }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        private static async Task SeedTags(ApplicationDbContext context)
        {
            var tags = new List<Tag>
            {
                new Tag { Name = "AGU" }, new Tag { Name = "Sinh viên" }, new Tag { Name = "Giảng viên" },
                new Tag { Name = "ĐHQG-HCM" }, new Tag { Name = "Hội thảo" }, new Tag { Name = "Công nghệ 4.0" },
                new Tag { Name = "Nông nghiệp ứng dụng" }, new Tag { Name = "Khởi nghiệp" }, new Tag { Name = "Kỹ năng mềm" },
                new Tag { Name = "Học bổng" }, new Tag { Name = "Tình nguyện hè" }, new Tag { Name = "Chuyển đổi số" },
                new Tag { Name = "Tuyển sinh 2024" }, new Tag { Name = "Cựu sinh viên" }, new Tag { Name = "Phát triển bền vững" }
            };
            context.Tags.AddRange(tags);
            await context.SaveChangesAsync();
        }

        private static async Task SeedArticles(ApplicationDbContext context, ApplicationUser author, List<Category> categories, List<Tag> tags)
        {
            var articles = new List<Article>();
            var random = new Random();
            int totalArticles = 120;
            int articlesPerTopic = 15; // Khoảng 12-13 bài mỗi chủ đề

            // Định nghĩa dữ liệu mẫu cho 8 chủ đề chính
            var topicData = new Dictionary<string, (List<(string Title, string Summary)>, string ContentBase)> {
                { "Giáo dục", (
                    new List<(string, string)> {
                        ("AGU đổi mới phương pháp giảng dạy theo hướng năng lực", "Trường Đại học An Giang áp dụng các phương pháp dạy học tích cực, lấy người học làm trung tâm trong năm học mới."),
                        ("Triển khai chương trình đào tạo chất lượng cao ngành Sư phạm", "Chương trình mới nhằm nâng cao chất lượng đầu ra cho sinh viên sư phạm, đáp ứng yêu cầu đổi mới giáo dục phổ thông."),
                        ("Hội nghị tổng kết công tác đào tạo năm học 2023-2024", "Đánh giá những kết quả đạt được và đề ra phương hướng nhiệm vụ cho năm học tiếp theo."),
                        ("Tăng cường ứng dụng công nghệ số trong quản lý đào tạo tại AGU", "Hệ thống quản lý học tập trực tuyến được nâng cấp giúp sinh viên và giảng viên tương tác hiệu quả hơn."),
                        ("Sinh viên AGU hào hứng với tuần lễ sinh hoạt công dân đầu khóa", "Chuỗi hoạt động giúp tân sinh viên hòa nhập nhanh chóng với môi trường đại học."),
                         ("AGU chú trọng phát triển kỹ năng mềm cho sinh viên năm cuối", "Các khóa học ngắn hạn về giao tiếp, làm việc nhóm được tổ chức thường xuyên."),
                        ("Đánh giá ngoài chương trình đào tạo theo tiêu chuẩn AUN-QA", "Nỗ lực không ngừng của nhà trường trong việc cam kết chất lượng đào tạo khu vực."),
                         ("Mở rộng các mô hình học tập trải nghiệm thực tế tại doanh nghiệp", "Sinh viên có cơ hội tiếp cận môi trường làm việc thực tế ngay từ năm thứ 2."),
                        ("Thư viện AGU bổ sung hàng ngàn đầu sách điện tử mới", "Phục vụ nhu cầu tra cứu, học tập và nghiên cứu ngày càng cao của cán bộ, giảng viên và sinh viên."),
                        ("Hội thảo nâng cao năng lực ngoại ngữ cho giảng viên trẻ", "Tạo điều kiện cho giảng viên tiếp cận các phương pháp giảng dạy tiên tiến bằng tiếng Anh."),
                        ("AGU ký kết thỏa thuận hợp tác đào tạo với các trường THPT trong tỉnh", "Tăng cường mối liên kết giữa giáo dục đại học và phổ thông."),
                        ("Lễ tốt nghiệp và trao bằng cho hơn 1500 tân cử nhân, kỹ sư", "Ngày hội vinh danh những nỗ lực không ngừng nghỉ của sinh viên AGU.")
                    },
                    // Content Base dài (sẽ được lặp lại để đủ 400-500 từ)
                    "<p>Trong bối cảnh đổi mới căn bản và toàn diện giáo dục đại học, Trường Đại học An Giang (AGU), thành viên của Đại học Quốc gia TP.HCM, đã và đang không ngừng nỗ lực nâng cao chất lượng đào tạo. Nhà trường xác định việc đổi mới phương pháp giảng dạy, chuyển từ truyền thụ kiến thức sang phát triển phẩm chất và năng lực người học là nhiệm vụ trọng tâm.</p><p>Theo đó, các chương trình đào tạo được rà soát, điều chỉnh theo hướng hiện đại, tiệm cận với chuẩn mực quốc tế và đáp ứng nhu cầu thực tiễn của thị trường lao động tại khu vực Đồng bằng sông Cửu Long. Việc ứng dụng công nghệ thông tin và chuyển đổi số trong dạy và học được đẩy mạnh, tạo môi trường học tập linh hoạt, mọi lúc mọi nơi cho sinh viên.</p><p>Bên cạnh kiến thức chuyên môn, AGU đặc biệt chú trọng trang bị cho sinh viên các kỹ năng mềm thiết yếu như kỹ năng giao tiếp, làm việc nhóm, tư duy phản biện và năng lực ngoại ngữ. Các hoạt động ngoại khóa, câu lạc bộ học thuật và các chương trình thực tập, kiến tập tại doanh nghiệp được tổ chức thường xuyên, giúp sinh viên tích lũy kinh nghiệm thực tế và tự tin hơn khi gia nhập thị trường lao động sau khi tốt nghiệp.</p><p>Lãnh đạo nhà trường nhấn mạnh: 'Mục tiêu của chúng tôi là đào tạo ra những công dân toàn cầu, vừa có đức vừa có tài, sẵn sàng đóng góp cho sự phát triển kinh tế - xã hội của địa phương và đất nước'. Với sự đầu tư đồng bộ về cơ sở vật chất, đội ngũ giảng viên trình độ cao và chương trình đào tạo tiên tiến, AGU tiếp tục khẳng định vị thế là trung tâm đào tạo nguồn nhân lực chất lượng cao uy tín tại khu vực.</p>"
                )},
                { "Nghiên cứu", (
                    new List<(string, string)> {
                         ("Sinh viên AGU đạt giải cao tại Hội nghị NCKH cấp Bộ", "Đề tài về nông nghiệp công nghệ cao của nhóm sinh viên đã xuất sắc vượt qua nhiều đối thủ."),
                         ("Công bố các đề tài nghiên cứu trọng điểm cấp Đại học Quốc gia", "Các nghiên cứu tập trung vào giải quyết các vấn đề cấp bách về môi trường và biến đổi khí hậu tại ĐBSCL."),
                         ("Hội thảo quốc tế về phát triển bền vững vùng ĐBSCL tổ chức tại AGU", "Quy tụ nhiều chuyên gia, nhà khoa học hàng đầu trong và ngoài nước tham gia thảo luận."),
                         ("AGU tăng cường đầu tư cho các phòng thí nghiệm nghiên cứu chuyên sâu", "Trang thiết bị hiện đại được bổ sung phục vụ công tác nghiên cứu của giảng viên và sinh viên."),
                         ("Nhiều bài báo quốc tế của giảng viên AGU được công bố trên các tạp chí uy tín", "Khẳng định năng lực nghiên cứu khoa học ngày càng nâng cao của đội ngũ cán bộ giảng viên."),
                         ("Phát động phong trào sinh viên nghiên cứu khoa học năm 2024", "Khuyến khích tinh thần sáng tạo, tìm tòi khám phá tri thức mới trong sinh viên."),
                         ("Chuyển giao công nghệ xử lý phụ phẩm nông nghiệp cho nông dân địa phương", "Kết quả nghiên cứu khoa học được ứng dụng thực tiễn, mang lại hiệu quả kinh tế cao."),
                         ("Hợp tác nghiên cứu với các viện, trường quốc tế về bảo tồn đa dạng sinh học", "Mở rộng mạng lưới hợp tác quốc tế trong lĩnh vực nghiên cứu khoa học."),
                         ("Thành lập các nhóm nghiên cứu mạnh trong lĩnh vực công nghệ sinh học và thực phẩm", "Tập hợp các nhà khoa học đầu ngành để thực hiện các đề tài quy mô lớn."),
                         ("AGU tổ chức báo cáo chuyên đề về phương pháp nghiên cứu định lượng", "Nâng cao năng lực phương pháp luận nghiên cứu cho nghiên cứu sinh và học viên cao học."),
                         ("Kết quả nghiên cứu về văn hóa Óc Eo của giảng viên AGU được đánh giá cao", "Đóng góp quan trọng vào việc bảo tồn và phát huy giá trị văn hóa lịch sử địa phương."),
                         ("Triển lãm các sản phẩm khoa học công nghệ tiêu biểu của sinh viên và giảng viên", "Cơ hội để quảng bá và kết nối các ý tưởng sáng tạo với doanh nghiệp.")
                    },
                    "<p>Hoạt động nghiên cứu khoa học (NCKH) luôn được Trường Đại học An Giang xác định là một trong những nhiệm vụ chiến lược quan trọng, song hành cùng công tác đào tạo. Trong giai đoạn 2022-2025, nhà trường đã ban hành nhiều chính sách khuyến khích, tạo điều kiện thuận lợi cho giảng viên và sinh viên tham gia NCKH.</p><p>Trọng tâm nghiên cứu của AGU hướng đến giải quyết các vấn đề thực tiễn cấp bách của khu vực Đồng bằng sông Cửu Long như thích ứng với biến đổi khí hậu, nông nghiệp thông minh, quản lý tài nguyên nước, bảo tồn văn hóa và phát triển du lịch bền vững. Số lượng và chất lượng các đề tài nghiên cứu các cấp (Nhà nước, Bộ, ĐHQG, Tỉnh, Cơ sở) không ngừng tăng lên qua từng năm.</p><p>Đặc biệt, phong trào sinh viên NCKH phát triển mạnh mẽ. Nhiều ý tưởng sáng tạo, táo bạo của sinh viên đã được hiện thực hóa thành các đề tài, dự án có tính ứng dụng cao, đạt nhiều giải thưởng tại các cuộc thi NCKH cấp khu vực và toàn quốc. Đây là môi trường tốt để sinh viên rèn luyện tư duy độc lập, kỹ năng giải quyết vấn đề và niềm đam mê khoa học.</p><p>Bên cạnh đó, AGU cũng tích cực đẩy mạnh hợp tác quốc tế trong NCKH, tổ chức nhiều hội thảo khoa học tầm cỡ, thu hút sự tham gia của các chuyên gia hàng đầu. Các bài báo công bố quốc tế trên các tạp chí thuộc danh mục ISI/Scopus của giảng viên nhà trường tăng trưởng ấn tượng, góp phần nâng cao uy tín học thuật của AGU trên bản đồ giáo dục đại học.</p>"
                )},
                 { "Văn nghệ", (
                    new List<(string, string)> {
                        ("Đêm nhạc hội chào tân sinh viên AGU rực rỡ sắc màu", "Chương trình nghệ thuật hoành tráng với sự tham gia của các CLB văn nghệ trường."),
                        ("Cuộc thi 'Tiếng hát sinh viên AGU' tìm kiếm nhiều tài năng mới", "Sân chơi âm nhạc hấp dẫn thu hút đông đảo sinh viên đăng ký tham gia."),
                        ("CLB Kịch AGU công diễn vở kịch về đề tài lịch sử địa phương", "Vở kịch nhận được sự đánh giá cao về tính nghệ thuật và ý nghĩa giáo dục."),
                        ("Triển lãm ảnh nghệ thuật 'Góc nhìn sinh viên' tại khuôn viên trường", "Trưng bày những tác phẩm nhiếp ảnh xuất sắc về đời sống sinh viên và cảnh đẹp An Giang."),
                        ("Giao lưu văn hóa văn nghệ với sinh viên quốc tế Lào và Campuchia", "Thắt chặt tình đoàn kết hữu nghị thông qua các tiết mục văn nghệ truyền thống."),
                        ("Hội thi 'Nét đẹp sinh viên AGU' tôn vinh vẻ đẹp trí tuệ và tài năng", "Các thí sinh trải qua nhiều vòng thi gay cấn để thể hiện bản lĩnh của mình."),
                        ("Đội văn nghệ xung kích AGU tham gia biểu diễn phục vụ cộng đồng", "Mang lời ca tiếng hát đến với các vùng sâu vùng xa, lan tỏa tinh thần tình nguyện."),
                        ("Tổ chức Tuần lễ văn hóa đọc với nhiều hoạt động phong phú", "Tọa đàm giới thiệu sách, thi xếp sách nghệ thuật, quyên góp sách tặng trẻ em nghèo."),
                        ("CLB Guitar AGU tổ chức đêm nhạc Acoustic gây quỹ từ thiện", "Không gian âm nhạc ấm cúng, kết nối những tấm lòng nhân ái."),
                        ("Sinh viên AGU tham gia Liên hoan tiếng hát sinh viên toàn quốc", "Mang đến liên hoan những tiết mục đặc sắc, đậm đà bản sắc văn hóa miền Tây."),
                        ("Chương trình biểu diễn thời trang tái chế bảo vệ môi trường", "Những bộ trang phục độc đáo được sáng tạo từ vật liệu phế thải."),
                        ("Hội thi múa dân vũ, flashmob sôi động toàn trường", "Tạo không khí vui tươi, rèn luyện sức khỏe và tinh thần đồng đội cho sinh viên.")
                    },
                    "<p>Bên cạnh công tác chuyên môn, đời sống văn hóa tinh thần của sinh viên Trường Đại học An Giang luôn được quan tâm chăm lo với nhiều hoạt động văn hóa, văn nghệ sôi nổi, đa dạng. Các hoạt động này không chỉ tạo sân chơi lành mạnh, bổ ích sau những giờ học căng thẳng mà còn là môi trường để sinh viên phát hiện và phát triển năng khiếu nghệ thuật của mình.</p><p>Hàng năm, Đoàn trường và Hội Sinh viên tổ chức thường niên các chương trình lớn như: Hội diễn văn nghệ truyền thống, Cuộc thi Tiếng hát sinh viên, Hội thi Nét đẹp sinh viên, các đêm nhạc hội chào đón tân sinh viên hay chia tay sinh viên cuối khóa. Các câu lạc bộ, đội, nhóm sở thích về âm nhạc, kịch, múa, nhiếp ảnh... hoạt động rất tích cực, thường xuyên tổ chức các buổi biểu diễn, triển lãm thu hút đông đảo sinh viên tham gia.</p><p>Thông qua các hoạt động văn hóa văn nghệ, sinh viên AGU không chỉ được thể hiện cá tính, sự sáng tạo mà còn được giáo dục về truyền thống văn hóa, lịch sử của dân tộc, tình yêu quê hương đất nước. Nhiều tiết mục văn nghệ của sinh viên nhà trường đã đạt giải cao tại các hội thi, liên hoan cấp khu vực và toàn quốc, góp phần quảng bá hình ảnh sinh viên AGU năng động, tài năng và giàu bản sắc.</p>"
                )},
                 { "Thể thao", (
                    new List<(string, string)> {
                        ("Khai mạc Hội thao sinh viên AGU năm học 2023-2024", "Hàng ngàn vận động viên sinh viên tranh tài ở nhiều bộ môn hấp dẫn."),
                        ("Đội tuyển bóng đá nam AGU vô địch giải sinh viên khu vực ĐBSCL", "Chiến thắng thuyết phục khẳng định sức mạnh của bóng đá sinh viên nhà trường."),
                        ("Sôi nổi giải bóng chuyền hơi cán bộ viên chức, người lao động", "Hoạt động giao lưu, rèn luyện sức khỏe tăng cường tình đoàn kết trong nhà trường."),
                        ("Sinh viên AGU giành nhiều huy chương tại Đại hội thể thao sinh viên toàn quốc", "Thành tích xuất sắc ở các nội dung điền kinh, bơi lội và võ thuật."),
                        ("CLB Cầu lông AGU tổ chức giải mở rộng thu hút nhiều tay vợt mạnh", "Tạo cơ hội cọ xát, nâng cao trình độ cho các thành viên CLB."),
                        ("Phát động phong trào 'Mỗi sinh viên tập luyện một môn thể thao'", "Khuyến khích sinh viên tích cực rèn luyện thân thể, nâng cao sức khỏe."),
                        ("Tổ chức giải chạy việt dã 'AGU Run' gây quỹ học bổng", "Hàng ngàn sinh viên và giảng viên tham gia chạy vì mục đích cộng đồng."),
                        ("Đội tuyển bóng rổ AGU thi đấu giao hữu với các trường bạn", "Tăng cường mối quan hệ giao lưu học hỏi giữa các trường đại học trong khu vực."),
                        ("Nâng cấp cơ sở vật chất, sân bãi phục vụ hoạt động thể dục thể thao", "Sân bóng đá cỏ nhân tạo, nhà thi đấu đa năng được tu sửa khang trang."),
                        ("Hội thi các môn thể thao dân tộc mừng Đảng mừng Xuân", "Bảo tồn và phát huy các trò chơi dân gian, thể thao truyền thống."),
                        ("CLB Võ thuật AGU biểu diễn tại lễ khai giảng năm học mới", "Những màn trình diễn võ thuật đẹp mắt, thể hiện tinh thần thượng võ."),
                        ("Tổ chức lớp dạy bơi phòng chống đuối nước cho sinh viên", "Trang bị kỹ năng sống cần thiết cho sinh viên vùng sông nước.")
                    },
                    "<p>Phong trào thể dục thể thao (TDTT) trong sinh viên Trường Đại học An Giang luôn phát triển mạnh mẽ, trở thành một phần không thể thiếu trong đời sống học đường. Nhà trường luôn xác định giáo dục thể chất là yếu tố quan trọng góp phần giáo dục toàn diện cho sinh viên, với phương châm 'Khỏe để học tập, xây dựng và bảo vệ Tổ quốc'.</p><p>Hệ thống cơ sở vật chất phục vụ TDTT của trường ngày càng được đầu tư hoàn thiện với nhà thi đấu đa năng, sân bóng đá cỏ nhân tạo, sân bóng chuyền, bóng rổ, sân cầu lông, phòng tập gym... đáp ứng nhu cầu tập luyện đa dạng của sinh viên. Hội thao sinh viên cấp trường được tổ chức định kỳ hàng năm với quy mô lớn, thu hút sự tham gia nhiệt tình của sinh viên tất cả các khoa.</p><p>Bên cạnh các môn thể thao truyền thống, nhà trường cũng khuyến khích phát triển các CLB thể thao sở thích như võ thuật, khiêu vũ thể thao, yoga... Các đội tuyển thể thao của AGU thường xuyên tham gia và đạt thành tích cao tại các giải đấu sinh viên cấp khu vực, toàn quốc, khẳng định vị thế thể thao học đường của nhà trường. Thông qua hoạt động TDTT, sinh viên không chỉ được rèn luyện sức khỏe, phát triển thể lực mà còn được rèn luyện ý chí, tính kỷ luật và tinh thần đồng đội.</p>"
                )},
                  { "Thành tích", (
                    new List<(string, string)> {
                        ("Sinh viên Nguyễn Văn A đạt danh hiệu 'Sinh viên 5 tốt' cấp Trung ương", "Tấm gương sáng về học tập và rèn luyện toàn diện."),
                        ("Nhóm sinh viên AGU đạt giải Nhất cuộc thi 'Ý tưởng khởi nghiệp' quốc gia", "Dự án nông nghiệp thông minh của nhóm được ban giám khảo đánh giá rất cao."),
                        ("Giảng viên Trần Thị B được vinh danh nhà giáo tiêu biểu toàn quốc", "Ghi nhận những cống hiến không mệt mỏi cho sự nghiệp giáo dục đại học."),
                        ("Trường ĐH An Giang nhận Huân chương Lao động hạng Nhất", "Phần thưởng cao quý của Đảng và Nhà nước trao tặng cho tập thể nhà trường."),
                        ("Đội tuyển Olympic Toán học AGU đạt thành tích xuất sắc", "Mang về nhiều huy chương Vàng, Bạc trong kỳ thi Olympic Toán sinh viên toàn quốc."),
                        ("Sinh viên Khoa CNTT đạt giải cao trong kỳ thi Lập trình sinh viên quốc tế ICPC", "Khẳng định năng lực công nghệ thông tin của sinh viên AGU trên đấu trường quốc tế."),
                        ("Công bố quyết định công nhận đạt chuẩn kiểm định chất lượng giáo dục chu kỳ 2", "Khẳng định uy tín và chất lượng đào tạo của nhà trường."),
                        ("Nhiều sinh viên AGU được kết nạp Đảng tại trường", "Vinh dự lớn lao của những đoàn viên ưu tú có thành tích xuất sắc."),
                        ("Thư viện AGU được đánh giá là một trong những thư viện hiện đại nhất khu vực", "Hệ thống tài nguyên số phong phú phục vụ hiệu quả việc học tập và nghiên cứu."),
                        ("Cựu sinh viên AGU thành đạt quay về tài trợ học bổng cho đàn em", "Nghĩa cử cao đẹp thể hiện truyền thống uống nước nhớ nguồn."),
                        ("Sinh viên Lê Văn C đạt giải thưởng 'Sao Tháng Giêng'", "Giải thưởng cao quý của Hội Sinh viên Việt Nam dành cho cán bộ Hội xuất sắc."),
                        ("Trường AGU dẫn đầu khu vực về số lượng bài báo quốc tế trong năm", "Thành tựu nổi bật trong công tác nghiên cứu khoa học của nhà trường.")
                    },
                    "<p>Trong suốt chặng đường phát triển, đặc biệt là giai đoạn 2022-2025, thầy và trò Trường Đại học An Giang đã không ngừng nỗ lực phấn đấu và gặt hái được nhiều thành tích tự hào trên mọi mặt công tác. Những thành quả này là minh chứng rõ nét cho chất lượng đào tạo, năng lực nghiên cứu và tinh thần nhiệt huyết của tập thể nhà trường.</p><p>Về đào tạo, nhiều thế hệ sinh viên AGU đã tốt nghiệp, trở thành nguồn nhân lực chất lượng cao, đóng góp tích cực cho sự phát triển của địa phương và đất nước. Nhiều sinh viên đạt các giải thưởng cao quý trong các kỳ thi Olympic quốc gia, các cuộc thi học thuật, sáng tạo khởi nghiệp và các danh hiệu như 'Sinh viên 5 tốt', 'Sao Tháng Giêng'.</p><p>Về nghiên cứu khoa học, đội ngũ giảng viên của trường đã thực hiện thành công nhiều đề tài nghiên cứu cấp Nhà nước, cấp Bộ, có giá trị thực tiễn cao. Số lượng bài báo công bố trên các tạp chí quốc tế uy tín tăng mạnh. Về công tác xây dựng và phát triển nhà trường, AGU đã hoàn thành xuất sắc công tác kiểm định chất lượng giáo dục, khẳng định vị thế là một cơ sở giáo dục đại học uy tín. Những phần thưởng cao quý như Huân chương Lao động, Cờ thi đua của Chính phủ... là sự ghi nhận xứng đáng cho những đóng góp của nhà trường.</p>"
                )},
                   { "Định hướng", (
                    new List<(string, string)> {
                        ("Tọa đàm 'Hành trang lập nghiệp' cho sinh viên năm cuối", "Các chuyên gia, doanh nhân chia sẻ kinh nghiệm quý báu về thị trường lao động."),
                        ("Ngày hội việc làm AGU 2024 thu hút hàng trăm doanh nghiệp", "Hàng ngàn cơ hội việc làm và thực tập được mang đến cho sinh viên."),
                        ("Hội thảo 'Kỹ năng chinh phục nhà tuyển dụng' thu hút đông đảo sinh viên", "Hướng dẫn cách viết CV, kỹ năng phỏng vấn xin việc hiệu quả."),
                        ("AGU ký kết thỏa thuận hợp tác với các tập đoàn lớn về cung ứng nhân lực", "Mở rộng cánh cửa nghề nghiệp cho sinh viên sau khi tốt nghiệp."),
                        ("Chương trình tư vấn hướng nghiệp 'Chọn nghề đúng - Sáng tương lai' cho học sinh THPT", "Giúp học sinh phổ thông có định hướng rõ ràng hơn trong việc chọn ngành, chọn trường."),
                        ("Ra mắt Trung tâm Hỗ trợ sinh viên và Quan hệ doanh nghiệp", "Cầu nối quan trọng giữa nhà trường, sinh viên và đơn vị tuyển dụng."),
                        ("Tổ chức các chuyến tham quan thực tế doanh nghiệp (Company Tour)", "Giúp sinh viên có cái nhìn trực quan về môi trường làm việc chuyên nghiệp."),
                        ("Khóa đào tạo kỹ năng khởi nghiệp đổi mới sáng tạo cho sinh viên", "Trang bị kiến thức và kỹ năng cần thiết để sinh viên tự tin khởi nghiệp."),
                        ("Tư vấn du học và cơ hội việc làm tại nước ngoài cho sinh viên AGU", "Thông tin về các chương trình học bổng và thị trường lao động quốc tế."),
                        ("Diễn đàn 'Cựu sinh viên AGU - Kết nối và sẻ chia'", "Cơ hội để các thế hệ sinh viên giao lưu, học hỏi kinh nghiệm lẫn nhau."),
                        ("Khảo sát tình hình việc làm của sinh viên sau tốt nghiệp", "Cơ sở quan trọng để nhà trường điều chỉnh chương trình đào tạo sát với nhu cầu thực tế."),
                        ("Tổ chức thi thử chứng chỉ ngoại ngữ, tin học chuẩn đầu ra cho sinh viên", "Giúp sinh viên tự đánh giá năng lực và có kế hoạch ôn tập phù hợp.")
                    },
                    "<p>Công tác định hướng nghề nghiệp, tư vấn việc làm và hỗ trợ khởi nghiệp cho sinh viên luôn được Trường Đại học An Giang đặc biệt quan tâm. Nhà trường xác định đây là một trong những nhiệm vụ trọng tâm để nâng cao tỷ lệ sinh viên có việc làm sau khi tốt nghiệp và đáp ứng nhu cầu ngày càng cao của thị trường lao động.</p><p>Trung tâm Hỗ trợ sinh viên và Quan hệ doanh nghiệp của trường thường xuyên tổ chức các hoạt động đa dạng như: Ngày hội việc làm, các buổi tọa đàm, hội thảo chuyên đề về kỹ năng mềm, kỹ năng tìm việc, phỏng vấn tuyển dụng với sự tham gia của các diễn giả là chuyên gia nhân sự, lãnh đạo doanh nghiệp. Nhà trường cũng tích cực mở rộng mạng lưới hợp tác với hàng trăm doanh nghiệp, tập đoàn uy tín trong và ngoài tỉnh để tạo nguồn cơ hội thực tập và việc làm phong phú cho sinh viên.</p><p>Bên cạnh đó, các hoạt động thúc đẩy tinh thần khởi nghiệp trong sinh viên cũng được chú trọng thông qua các cuộc thi ý tưởng sáng tạo, các khóa đào tạo kỹ năng khởi nghiệp. Hệ thống tư vấn học tập, cố vấn học tập hoạt động hiệu quả, giúp sinh viên xác định rõ mục tiêu nghề nghiệp ngay từ những năm đầu đại học và xây dựng lộ trình học tập, rèn luyện phù hợp để đạt được mục tiêu đó.</p>"
                )},
                    { "Điểm mới", (
                    new List<(string, string)> {
                        ("AGU công bố đề án tuyển sinh đại học chính quy năm 2024", "Nhiều điểm mới trong phương thức xét tuyển và chỉ tiêu các ngành."),
                        ("Mở thêm 3 ngành đào tạo mới đáp ứng nhu cầu nhân lực thời đại 4.0", "Các ngành mới thuộc lĩnh vực công nghệ số và nông nghiệp công nghệ cao."),
                        ("Áp dụng phương thức xét tuyển kết hợp chứng chỉ ngoại ngữ quốc tế", "Tạo thêm cơ hội cho các thí sinh có năng lực ngoại ngữ tốt."),
                        ("Tư vấn tuyển sinh trực tuyến thu hút hàng chục ngàn lượt theo dõi", "Giải đáp thắc mắc của thí sinh và phụ huynh về kỳ thi tuyển sinh sắp tới."),
                        ("AGU dành nhiều suất học bổng giá trị cho tân sinh viên xuất sắc", "Chính sách thu hút nhân tài với tổng trị giá học bổng lên đến hàng tỷ đồng."),
                        ("Công bố điểm chuẩn trúng tuyển vào AGU năm 2023", "Điểm chuẩn các ngành nhìn chung ổn định, một số ngành 'hot' có điểm tăng nhẹ."),
                        ("Tổ chức ngày hội 'Open Day' chào đón học sinh THPT tham quan trường", "Học sinh được trải nghiệm môi trường đại học, tìm hiểu về các ngành nghề đào tạo."),
                        ("Điều chỉnh chương trình đào tạo theo hướng tăng thời lượng thực hành", "Giúp sinh viên nâng cao kỹ năng nghề nghiệp, đáp ứng yêu cầu của nhà tuyển dụng."),
                        ("Hợp tác với doanh nghiệp trong xây dựng và đánh giá chương trình đào tạo", "Đảm bảo chương trình đào tạo luôn cập nhật, bám sát thực tiễn."),
                        ("Triển khai hệ thống đăng ký xét tuyển trực tuyến hiện đại, thuận tiện", "Giúp thí sinh dễ dàng thực hiện các thủ tục đăng ký xét tuyển mọi lúc mọi nơi."),
                        ("Thông tin về các chương trình liên kết đào tạo quốc tế tại AGU", "Cơ hội nhận bằng cấp quốc tế ngay tại Việt Nam với chi phí hợp lý."),
                        ("Chính sách ưu tiên xét tuyển thẳng cho học sinh giỏi quốc gia", "Chào đón những tài năng trẻ đến học tập và phát triển tại AGU.")
                    },
                    "<p>Mỗi mùa tuyển sinh, Trường Đại học An Giang luôn nỗ lực đổi mới để mang đến những cơ hội học tập tốt nhất cho các bạn trẻ, đồng thời đáp ứng nhu cầu nguồn nhân lực chất lượng cao cho xã hội. Trong giai đoạn 2022-2025, nhà trường đã thực hiện nhiều cải tiến đột phá trong công tác tuyển sinh và đào tạo.</p><p>Điểm nổi bật là việc mở thêm các mã ngành đào tạo mới, đón đầu xu hướng phát triển của nền kinh tế số và Cuộc cách mạng công nghiệp 4.0, như Trí tuệ nhân tạo, Khoa học dữ liệu, Nông nghiệp công nghệ cao, Logistics... Các phương thức xét tuyển được đa dạng hóa, bên cạnh xét tuyển dựa trên kết quả thi tốt nghiệp THPT, nhà trường tăng cường xét tuyển dựa trên kết quả học tập THPT (học bạ), xét tuyển thẳng, xét tuyển kết hợp chứng chỉ quốc tế và tổ chức kỳ thi đánh giá năng lực riêng (phối hợp với ĐHQG-HCM).</p><p>Chương trình đào tạo cũng được rà soát, điều chỉnh mạnh mẽ theo hướng giảm lý thuyết hàn lâm, tăng thời lượng thực hành, thực tập tại doanh nghiệp và chú trọng phát triển kỹ năng mềm, năng lực ngoại ngữ, tin học cho sinh viên. Nhà trường cũng ban hành nhiều chính sách học bổng hấp dẫn để thu hút thí sinh giỏi và hỗ trợ sinh viên có hoàn cảnh khó khăn.</p>"
                )},
                     { "Tin tức", (
                    new List<(string, string)> {
                        ("Lễ khai giảng năm học mới 2023-2024 tại Trường Đại học An Giang", "Không khí hân hoan chào đón năm học mới của thầy và trò nhà trường."),
                        ("Hội nghị viên chức, người lao động AGU năm 2024", "Phát huy dân chủ, trí tuệ tập thể trong việc xây dựng và phát triển nhà trường."),
                        ("Công đoàn AGU tổ chức các hoạt động chào mừng ngày Nhà giáo Việt Nam 20/11", "Tri ân công lao của các thế hệ thầy cô giáo."),
                        ("Sinh viên AGU tích cực tham gia chiến dịch Mùa hè xanh tình nguyện", "Dấu ấn tuổi trẻ AGU trên các nẻo đường xây dựng nông thôn mới."),
                        ("Tổ chức khám sức khỏe định kỳ đầu năm cho toàn thể sinh viên", "Đảm bảo sức khỏe tốt nhất cho sinh viên trong quá trình học tập."),
                        ("Đoàn trường AGU phát động tháng thanh niên với nhiều công trình, phần việc ý nghĩa", "Thi đua lập thành tích chào mừng kỷ niệm ngày thành lập Đoàn TNCS Hồ Chí Minh."),
                        ("Hội nghị đối thoại giữa Lãnh đạo trường với sinh viên", "Lắng nghe và giải quyết kịp thời các tâm tư, nguyện vọng chính đáng của sinh viên."),
                        ("Tăng cường công tác đảm bảo an ninh trật tự, an toàn trong khuôn viên trường", "Tạo môi trường học tập an toàn, lành mạnh cho sinh viên."),
                        ("Đoàn viên thanh niên AGU tham gia hiến máu tình nguyện", "Nghĩa cử cao đẹp thể hiện tinh thần tương thân tương ái vì cộng đồng."),
                        ("Tổ chức tập huấn kỹ năng phòng cháy chữa cháy cho cán bộ, sinh viên", "Nâng cao ý thức và kỹ năng ứng phó với các tình huống khẩn cấp."),
                        ("AGU tiếp và làm việc với đoàn công tác của Đại học Quốc gia TP.HCM", "Thắt chặt mối quan hệ hợp tác, chia sẻ kinh nghiệm giữa các đơn vị thành viên."),
                        ("Thông báo về lịch nghỉ Tết Nguyên đán Giáp Thìn 2024", "Sinh viên được nghỉ Tết dài ngày để sum họp bên gia đình.")
                    },
                    "<p>Bản tin chung của Trường Đại học An Giang (AGU) luôn cập nhật nhanh chóng, chính xác và toàn diện các hoạt động diễn ra hàng ngày trong nhà trường, phản ánh bức tranh sinh động về đời sống, học tập và công tác của toàn thể cán bộ, giảng viên và sinh viên. Từ các sự kiện trọng đại như Lễ khai giảng, Lễ tốt nghiệp, các hội nghị, hội thảo quan trọng đến các hoạt động thường nhật của các phòng ban, khoa, trung tâm đều được đưa tin kịp thời.</p><p>Công tác Đảng, Đoàn thể luôn được chú trọng với nhiều hoạt động thiết thực, ý nghĩa như các đợt sinh hoạt chính trị, các phong trào thi đua yêu nước, các hoạt động tình nguyện vì cộng đồng như Mùa hè xanh, Tiếp sức mùa thi, Hiến máu nhân đạo... thể hiện tinh thần trách nhiệm xã hội cao của tuổi trẻ AGU. Nhà trường cũng thường xuyên tổ chức các diễn đàn đối thoại để lắng nghe tâm tư, nguyện vọng của sinh viên, cán bộ viên chức, từ đó kịp thời điều chỉnh công tác quản lý, nâng cao chất lượng phục vụ.</p><p>Công tác đảm bảo an ninh trật tự, an toàn vệ sinh thực phẩm, y tế học đường, phòng chống cháy nổ... cũng luôn được quan tâm thực hiện tốt, xây dựng AGU trở thành một môi trường giáo dục an toàn, lành mạnh, thân thiện, là ngôi nhà chung ấm áp của mọi thành viên.</p>"
                )}
            };

            // Vòng lặp tạo bài viết từ dữ liệu mẫu
            foreach (var topic in topicData)
            {
                string categoryName = topic.Key;
                // Tìm Category ID tương ứng (Map tên topic sang tên category đầy đủ nếu cần)
                string fullCategoryName = categoryName switch
                {
                    "Giáo dục" => "Giáo dục & Đào tạo",
                    "Nghiên cứu" => "Nghiên cứu khoa học",
                    "Văn nghệ" => "Văn hóa - Văn nghệ",
                    "Thể thao" => "Thể thao học đường",
                    "Thành tích" => "Thành tích nổi bật",
                    "Định hướng" => "Định hướng & Việc làm",
                    "Điểm mới" => "Điểm mới Tuyển sinh",
                    "Tin tức" => "Tin tức chung",
                    _ => "Tin tức chung"
                };

                var category = categories.FirstOrDefault(c => c.Name == fullCategoryName);
                if (category == null) continue;

                var (articlesList, contentBase) = topic.Value;

                foreach (var (title, summary) in articlesList)
                {
                    // Tạo nội dung dài bằng cách lặp lại contentBase
                    // Để đạt 400-500 từ, ta lặp lại khoảng 2-3 lần đoạn base
                    StringBuilder fullContent = new StringBuilder();
                    fullContent.Append(contentBase);
                    fullContent.Append("<p><em>(Tiếp theo)... </em></p>");
                    fullContent.Append(contentBase); // Lặp lại lần 2

                    // Thêm một đoạn kết ngẫu nhiên để tạo sự khác biệt nhỏ
                    string[] endings = {
                        "<p>Tóm lại, với những nỗ lực không ngừng nghỉ, Trường Đại học An Giang đang ngày càng khẳng định vị thế vững chắc của mình trong hệ thống giáo dục đại học Việt Nam, đóng góp tích cực vào sự phát triển kinh tế - xã hội của khu vực Đồng bằng sông Cửu Long và cả nước.</p>",
                        "<p>Trong thời gian tới, nhà trường sẽ tiếp tục đẩy mạnh các giải pháp đồng bộ để thực hiện thắng lợi các mục tiêu chiến lược đã đề ra, phấn đấu trở thành một trung tâm đào tạo và nghiên cứu khoa học uy tín, chất lượng cao.</p>",
                        "<p>Tin tưởng rằng, với truyền thống đoàn kết, năng động, sáng tạo, thầy và trò Trường Đại học An Giang sẽ tiếp tục gặt hái được nhiều thành công hơn nữa trong tương lai, xứng đáng với niềm tin yêu của xã hội.</p>"
                    };
                    fullContent.Append(endings[random.Next(endings.Length)]);

                    var article = new Article
                    {
                        Title = title,
                        Summary = summary,
                        Content = fullContent.ToString(),
                        // Ảnh thumbnail ngẫu nhiên (bạn cần có các ảnh này trong wwwroot/uploads/articles)
                        ThumbnailUrl = $"/uploads/articles/default-thumbnail.jpg",
                        // Ngày tạo ngẫu nhiên trong khoảng 2022-2025
                        CreatedAt = GetRandomDate(new DateTime(2022, 1, 1), new DateTime(2025, 12, 31)),
                        IsApproved = true, // Duyệt sẵn để hiện lên trang chủ
                        ViewCount = random.Next(50, 5000),
                        CategoryId = category.Id,
                        AuthorId = author.Id,
                        ArticleTags = new List<ArticleTag>()
                    };

                    // Gán ngẫu nhiên 2-4 thẻ cho mỗi bài viết
                    int numberOfTags = random.Next(2, 5);
                    var shuffledTags = tags.OrderBy(x => random.Next()).Take(numberOfTags).ToList();
                    foreach (var tag in shuffledTags)
                    {
                        article.ArticleTags.Add(new ArticleTag { TagId = tag.Id });
                    }

                    articles.Add(article);
                }
            }

            // Xáo trộn danh sách bài viết trước khi thêm vào DB để ngày tháng không bị tuần tự
            var shuffledArticles = articles.OrderBy(a => random.Next()).ToList();

            // Thêm vào context và lưu (Batch lưu để tăng tốc)
            int count = 0;
            foreach (var article in shuffledArticles)
            {
                context.Articles.Add(article);
                count++;
                // Lưu từng đợt 20 bài để tránh quá tải bộ nhớ nếu số lượng lớn
                if (count % 20 == 0)
                {
                    await context.SaveChangesAsync();
                    // Detach để giải phóng bộ nhớ
                    foreach (var entry in context.ChangeTracker.Entries())
                    {
                        entry.State = EntityState.Detached;
                    }
                    // Cần load lại user và categories/tags cho đợt kế tiếp vì đã detach
                    author = await context.Users.FirstAsync(u => u.Email == "author@agu.edu.vn");
                    categories = await context.Categories.ToListAsync();
                    tags = await context.Tags.ToListAsync();
                }
            }
            // Lưu số còn lại
            await context.SaveChangesAsync();
        }

        // Hàm hỗ trợ tạo ngày ngẫu nhiên
        private static DateTime GetRandomDate(DateTime startDate, DateTime endDate)
        {
            Random random = new Random();
            int range = (endDate - startDate).Days;
            return startDate.AddDays(random.Next(range)).AddHours(random.Next(0, 24)).AddMinutes(random.Next(0, 60));
        }
    }
}