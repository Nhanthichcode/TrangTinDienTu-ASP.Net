# 📰 E-News - Trang Tin Tức Hiện Đại

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/your-username/your-repo) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) ![.NET Version](https://img.shields.io/badge/.NET-8.0-blueviolet) ![Bootstrap Version](https://img.shields.io/badge/Bootstrap-5.3-purple)

**E-News** là một ứng dụng web tin tức được xây dựng bằng ASP.NET Core MVC, cung cấp nền tảng để xuất bản, quản lý và đọc các bài viết một cách hiệu quả và thân thiện. Dự án này là [Mục đích dự án, ví dụ: "báo cáo môn học", "dự án cá nhân để học hỏi"...].

**Trang web demo:**
[đi đến trang demo](https://e-news-asp-net-eye4bcg0befvejgc.southeastasia-01.azurewebsites.net/)
**(đã dừng do hết gói miễn phí của Microsoft_Azure)**
> ⚠️ **LƯU Ý KHI TRUY CẬP DEMO:**
>
> Do đây là dự án môn học chạy trên gói **Azure App Service Miễn phí (Free Tier)**, ứng dụng sẽ tự động **Dừng (Stopped)** nếu không có ai truy cập.
>
> Nếu bạn truy cập link trên và thấy thông báo lỗi (ví dụ: "Error 500" hoặc "Service Unavailable"), vui lòng nhấn vào link dưới đây để gửi email yêu cầu tôi khởi động lại máy chủ:
>
> **[Nhấn vào đây để yêu cầu mở lại trang web](mailto:nhanlx151@gmail.com?subject=Y%C3%AAu%20c%E1%BA%A7u%20m%E1%BB%9F%20demo%20E-News)**
> Hoặc nhanlx151@gmail.com
>
> *(Tôi sẽ khởi động lại máy chủ ngay khi nhận được email. Xin cảm ơn!)*

---

## 🔥 Tính Năng Nổi Bật

* **📰 Quản lý Bài viết (CRUD):** Thêm, xem, sửa, xóa bài viết với trình soạn thảo WYSIWYG (nếu có).
* **📂 Quản lý Danh mục & Thẻ Tag:** Phân loại bài viết khoa học, dễ dàng tìm kiếm.
* **👤 Hệ thống Người dùng & Phân quyền:**
    * Đăng ký, Đăng nhập, Quản lý tài khoản.
    * Phân quyền rõ ràng: Quản trị viên (Admin), Tác giả (Author), Người dùng (User).
    * Admin duyệt/khóa bài viết, quản lý người dùng.
    * Author tạo và quản lý bài viết của riêng mình.
* **💬 Hệ thống Bình luận:**
    * Người dùng đăng nhập có thể bình luận bài viết.
    * Hỗ trợ **bình luận lồng nhau** (trả lời bình luận).
    * Admin/Chủ bình luận có thể sửa/xóa bình luận.
* **🔍 Tìm kiếm:** Tìm kiếm bài viết theo từ khóa trong tiêu đề và nội dung.
* **📊 Thống kê:** (Nếu đã làm) Hiển thị lượt xem, lượt thích bài viết (có thể theo ngày/tuần/tháng).
* **🗺️ Bản đồ:** Tích hợp bản đồ (ví dụ: Google Maps).
* **🎨 Giao diện Responsive:** Hiển thị tốt trên mọi thiết bị (Desktop, Tablet, Mobile) nhờ Bootstrap 5.

---

## 🛠️ Công Nghệ Sử Dụng (Tech Stack)

* **Backend:**
    * ASP.NET Core MVC 8.0 * C#
    * Entity Framework Core 8.0 (Code-First)
* **Frontend:**
    * HTML5, CSS3
    * Bootstrap 5.3
    * JavaScript, jQuery
* **Database:**
    * Microsoft SQL Server * **Authentication & Authorization:**
    * ASP.NET Core Identity
* **Thư viện khác (Ví dụ):**
    * X.PagedList.Mvc.Core (Phân trang)
    * Hangfire (Tác vụ nền - nếu dùng cho thống kê)
    * Chart.js (Biểu đồ - nếu dùng)

---

## 🚀 Bắt Đầu Nào! (Getting Started)

Hướng dẫn cài đặt và chạy dự án trên máy cục bộ.

### ✅ Yêu cầu (Prerequisites)

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) hoặc mới hơn.
* [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (Express, Developer, hoặc bản khác) hoặc một CSDL tương thích khác.
* Một IDE hoặc Code Editor (khuyến nghị [Visual Studio](https://visualstudio.microsoft.com/) hoặc [VS Code](https://code.visualstudio.com/)).
* Git (để clone repo).

### ⚙️ Cài đặt (Installation)

1.  **Clone Repository:**
    ```bash
    https://github.com/Nhanthichcode/TrangTinDienTu-ASP.Net.git
    cd your-repo
    [ Ví dụ: cd TrangTinDienTu-ASP.Net ]
    ```
    *(Thay `your-repo` bằng URL repo của bạn)*

2.  Cấu hình Bí mật (User Secrets) - Rất Quan trọng:
   **Dự án này sử dụng User Secrets để lưu trữ các thông tin nhạy cảm (Chuỗi kết nối CSDL, API Keys) nhằm tránh đưa lên GitHub.
   * Mở dự án trong Visual Studio.
   * Trong Solution Explorer, chuột phải vào project Trang tin điện tử mvc > chọn Manage User Secrets.
   * Một file **secrets.json** sẽ mở ra. Dán nội dung sau vào file này:
```
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=aspnet-Trang_tin_điện_tử_mvc;Trusted_Connection=True;MultipleActiveResultSets=true"
     },
     "Authentication": {
       "Google": {
         "ClientId": "[ĐIỀN CLIENT ID CỦA BẠN VÀO ĐÂY]",
         "ClientSecret": "[ĐIỀN CLIENT SECRET CỦA BẠN VÀO ĐÂY]"
       }
     }
     // Thêm các khóa bí mật khác (ví dụ: SendGridApiKey) nếu có
   }
```
   * Lưu ý: Sửa lại **DefaultConnection** nếu bạn dùng tên Server hoặc Database khác cho máy local. Điền **ClientId** và **ClientSecret** của Google bạn đã tạo ( Nếu chưa đăng kí hãy đăng ký **Google Cloud** _tại đây: https://console.cloud.google.com_ ).
   * Nếu bạn không muốn **dùng Google** hãy mở file **Progams.cs** và comment đoạn mã sau:
````
   // builder.Services.AddAuthentication(options =>
   //            {
   //                // giữ default theo Identity
   //                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
   //                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
   //           })
   //         .AddGoogle(googleOptions =>
   //            {
   //              googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
   //            googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
   //          googleOptions.CallbackPath = "/signin-google"; // mặc định; thay nếu cần
   //        googleOptions.SaveTokens = true;
   //  });
```` 
3.  **Cập nhật Database (Entity Framework Migrations):**
    * Mở **Package Manager Console** trong Visual Studio (Tools > NuGet Package Manager > Package Manager Console) hoặc dùng Terminal/Command Prompt trong thư mục dự án.
    * Chạy lệnh để áp dụng các migrations và tạo database (nếu chưa có):
        ```powershell
       Update-Database
        ```
        *(Nếu bạn chưa có thư mục Migrations, chạy `dotnet ef migrations add InitialCreate` trước)*

4.  **Chạy Ứng Dụng:
   * Nhấn F5 hoặc nút "Run" trong Visual Studio. Ứng dụng sẽ tự động chạy DataSeeder (nếu CSDL trống) để tạo vai trò, tài khoản mẫu và bài viết mẫu.

### ▶️ Chạy Ứng Dụng (Running the Application)

1.  **Trong Visual Studio:**
    * Nhấn nút **F5** hoặc nút "Play" (với profile `https` hoặc `http`).
2.  **Sử dụng .NET CLI:**
    * Mở Terminal/Command Prompt trong thư mục gốc của dự án.
    * Chạy lệnh:
        ```bash
        dotnet run
        ```
    * Ứng dụng sẽ khởi chạy và lắng nghe trên các cổng được cấu hình (thường là `https://localhost:xxxx` và `http://localhost:yyyy`). Mở trình duyệt và truy cập URL đó.

### 👤 Tài khoản Mặc định:

* **Admin:** `admin@news.com` / `Admin@123` * **Author:** `author@news.com` / `Author@123`

---

## 📖 Cách Sử Dụng (Usage)

1.  Truy cập trang chủ để xem các bài viết mới nhất và nổi bật.
2.  Nhấp vào các **Chuyên mục** hoặc **Thẻ Tag** để lọc bài viết.
3.  Sử dụng thanh **Tìm kiếm** để tìm bài viết theo từ khóa.
4.  Nhấp vào tiêu đề hoặc ảnh bài viết để **Xem chi tiết**.
5.  **Đăng nhập/Đăng ký** để có thể **Bình luận** và **Trả lời bình luận**.
6.  Nếu là **Author**, truy cập khu vực **Quản lý bài viết** để tạo/sửa/xóa bài viết của bạn.
7.  Nếu là **Admin**, truy cập **Trang quản trị** để duyệt bài, quản lý người dùng, danh mục, thẻ tag...

---

## ☁️ Triển Khai (Deployment)

* Ứng dụng này có thể được triển khai lên các nền tảng hỗ trợ ASP.NET Core như:
    * **Azure App Service** (Có gói miễn phí F1 phù hợp cho demo/báo cáo).
    * Các nền tảng PaaS khác hỗ trợ Docker (Railway, Render, Fly.io...).
* Cần cấu hình chuỗi kết nối Production và các biến môi trường cần thiết (ví dụ: API Keys).

---

## 🤝 Đóng Góp (Contributing)

Hiện tại dự án hoan nghênh mọi đóng góp. Nếu bạn muốn đóng góp, vui lòng fork repo và tạo Pull Request.

---

## 📄 Giấy Phép (License)

Dự án này được cấp phép theo **Giấy phép MIT**. Xem file `LICENSE` để biết chi tiết.

---

## 🙏 Lời Cảm Ơn / Liên Hệ (Acknowledgements / Contact)

* Cảm ơn cô Vy đã hỗ trợ.
* Nếu bạn có câu hỏi hoặc góp ý, vui lòng liên hệ: nhan_dth225710@student.agu.edu.vn hoặc tạo Issue trên GitHub.
* Nguồn: Lê Trí Nhàn - Trust Me Bro?
---
