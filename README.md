# 📰 E-News - Trang Tin Tức Hiện Đại

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/your-username/your-repo) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) ![.NET Version](https://img.shields.io/badge/.NET-8.0-blueviolet) ![Bootstrap Version](https://img.shields.io/badge/Bootstrap-5.3-purple)

**E-News** là một ứng dụng web tin tức được xây dựng bằng ASP.NET Core MVC, cung cấp nền tảng để xuất bản, quản lý và đọc các bài viết một cách hiệu quả và thân thiện. Dự án này là [Mục đích dự án, ví dụ: "báo cáo môn học", "dự án cá nhân để học hỏi"...].

**Trang web demo:**
[https://e-news-asp-net-eye4bcg0befvejgc.southeastasia-01.azurewebsites.net/](https://e-news-asp-net-eye4bcg0befvejgc.southeastasia-01.azurewebsites.net/)

> ⚠️ **LƯU Ý KHI TRUY CẬP DEMO:**
>
> Do đây là dự án môn học chạy trên gói **Azure App Service Miễn phí (Free Tier)**, ứng dụng sẽ tự động **Dừng (Stopped)** nếu không có ai truy cập.
>
> Nếu bạn truy cập link trên và thấy thông báo lỗi (ví dụ: "Error 500" hoặc "Service Unavailable"), vui lòng nhấn vào link dưới đây để gửi email yêu cầu tôi khởi động lại máy chủ:
>
> **[Nhấn vào đây để yêu cầu mở lại trang web](mailto:nhanlx151@gmail.com?subject=Y%C3%AAu%20c%E1%BA%A7u%20m%E1%BB%9F%20demo%20E-News)**
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
    git clone [https://github.com/your-username/your-repo.git](https://github.com/your-username/your-repo.git)
    cd your-repo-folder
    ```
    *(Thay `your-username/your-repo` bằng URL repo của bạn)*

2.  **Cấu hình Chuỗi Kết nối (Connection String):**
    * Mở file `appsettings.Development.json` (hoặc `appsettings.json`).
    * Tìm đến phần `"ConnectionStrings"`.
    * Chỉnh sửa chuỗi `"DefaultConnection"` để trỏ đến CSDL SQL Server của bạn. Ví dụ:
        ```json
        "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EnewsDb;Trusted_Connection=True;MultipleActiveResultSets=true"
        ```
        *(Đảm bảo tên Server và Database đúng)*

3.  **Cập nhật Database (Entity Framework Migrations):**
    * Mở **Package Manager Console** trong Visual Studio (Tools > NuGet Package Manager > Package Manager Console) hoặc dùng Terminal/Command Prompt trong thư mục dự án.
    * Chạy lệnh để áp dụng các migrations và tạo database (nếu chưa có):
        ```powershell
        dotnet ef database update
        ```
        *(Nếu bạn chưa có thư mục Migrations, chạy `dotnet ef migrations add InitialCreate` trước)*

4.  **(Tùy chọn) Cấu hình Khác:**
    * Nếu bạn dùng dịch vụ ngoài (như SendGrid API Key), cấu hình chúng trong **User Secrets** (Chuột phải project > Manage User Secrets) hoặc `appsettings.Development.json` (nhớ không commit key lên repo công khai).

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

### 👤 Tài khoản Mặc định (Nếu có Seed Data)

* **Admin:** `admin@enews.com` / `Password123!` * **Author:** `author@enews.com` / `Password123!` *(Bạn nên tạo tài khoản Admin/Author bằng chức năng Seed Data trong `Program.cs`)*

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

---
