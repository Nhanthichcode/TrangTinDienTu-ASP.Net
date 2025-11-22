using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Trang_tin_điện_tử_mvc.Models;

namespace Trang_tin_điện_tử_mvc.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        // Các bảng (DbSet)
        public DbSet<Article> Articles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ArticleTag> ArticleTags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Media> Media { get; set; }
        public DbSet<ArticleImagePosition> ArticleImagePositions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //  Thiết lập khóa chính tổng hợp cho bảng trung gian ArticleTag
            modelBuilder.Entity<ArticleTag>()
                .HasKey(at => new { at.ArticleId, at.TagId });

            //  Quan hệ 1-n: Category - Article
            modelBuilder.Entity<Article>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Articles)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            //  Quan hệ 1-n: User (Author) - Article
            modelBuilder.Entity<Article>()
                .HasOne(a => a.Author)
                .WithMany(u => u.Articles)
                .HasForeignKey(a => a.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            //  Quan hệ 1-n: Article - Comment
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Article)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);

            //  Quan hệ 1-n: User - Comment
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ 1-n: Comment - Reply (Nested Comments)
            modelBuilder.Entity<Comment>()
                .HasMany(c => c.Replies) // Một comment có nhiều Replies (con)
                .WithOne(c => c.ParentComment) // Mỗi Reply có một ParentComment (cha)
                .HasForeignKey(c => c.ParentCommentId) // Khóa ngoại là ParentCommentId
                .OnDelete(DeleteBehavior.NoAction);

            //  Quan hệ n-n: Article - Tag qua ArticleTag
            modelBuilder.Entity<ArticleTag>()
                .HasOne(at => at.Article)
                .WithMany(a => a.ArticleTags)
                .HasForeignKey(at => at.ArticleId);

            modelBuilder.Entity<ArticleTag>()
                .HasOne(at => at.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(at => at.TagId);

            //  Quan hệ 1-n: Article - Media
            modelBuilder.Entity<Media>()
                .HasOne(m => m.Article)
                .WithMany(a => a.Media)
                .HasForeignKey(m => m.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
           
            modelBuilder.Entity<ArticleImagePosition>()
              .HasOne(aip => aip.Article)
              .WithMany(a => a.ArticleImagePositions)
              .HasForeignKey(aip => aip.ArticleId)
              .OnDelete(DeleteBehavior.Cascade); // Cho phép cascade từ Article

            modelBuilder.Entity<ArticleImagePosition>()
                .HasOne(aip => aip.Media)
                .WithMany(m => m.ArticleImagePositions)
                .HasForeignKey(aip => aip.MediaId)
                .OnDelete(DeleteBehavior.Restrict);

            //  Đặt tên bảng (tuỳ chọn, giúp CSDL gọn gàng)
            modelBuilder.Entity<Article>().ToTable("Articles");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Tag>().ToTable("Tags");
            modelBuilder.Entity<ArticleTag>().ToTable("ArticleTags");
            modelBuilder.Entity<Comment>().ToTable("Comments");
            modelBuilder.Entity<Media>().ToTable("Media");
        }
    }
}
