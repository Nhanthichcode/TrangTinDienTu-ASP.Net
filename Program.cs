using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trang_tin_điện_tử_mvc.Data;
using Trang_tin_điện_tử_mvc.Models;

namespace Trang_tin_điện_tử_mvc
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            builder.Services.AddAuthorization(options =>
            {
                // Chính sách yêu cầu người dùng phải có vai trò "Admin"
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));

                // Chính sách yêu cầu người dùng phải có vai trò "Admin" HOẶC "Editor"
                options.AddPolicy("ElevatedRights", policy => policy.RequireRole("Admin", "Author"));
            });

            // Cấu hình Google login            
            builder.Services.AddAuthentication(options =>
            {
                // giữ default theo Identity
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            })
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                googleOptions.CallbackPath = "/signin-google"; // mặc định; thay nếu cần
                googleOptions.SaveTokens = true;
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None; // nếu cần gửi cookie cross-site
            });

            // Add Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
        .AddDefaultUI();
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages(); 

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=AdminDashboard}/{action=Index}/{id?}");
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=AuthorDashboard}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");            
            // Gọi hàm seed dữ liệu
            await DataSeeder.SeedAsync(app.Services);

            app.MapRazorPages();
            app.MapDefaultControllerRoute();
            app.Run();
        }
    }
}
