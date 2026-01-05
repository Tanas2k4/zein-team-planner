# ZEIN Team Planner

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**ZEIN Team Planner** là ứng dụng web quản lý công việc nhóm, được xây dựng bằng ASP.NET Core MVC.  
Ứng dụng giúp các team theo dõi, phân công và giám sát tiến độ nhiệm vụ một cách trực quan và dễ sử dụng.

![Demo Screenshot](https://github.com/user-attachments/assets/ada9cf90-1814-4169-8c59-224a726330b2)

## Tính năng hiện có

- Quản lý Tasks và Events
- Phân công người thực hiện cho từng Task
- Trạng thái Task: To Do → In Progress → Done
- Quản lý deadline và ngày tạo
- Lịch (Calendar) hiển thị Tasks & Events
- Sidebar điều hướng
- Giao diện responsive cơ bản

## Tính năng đang phát triển

- Thông báo nhắc việc
- Báo cáo tiến độ và thống kê chi tiết
- Comment & attachment cho Task
- Tích hợp thêm biểu đồ Chart.js

## Cách chạy dự án

1. Clone repository
```bash
git clone https://github.com/YOUR_USERNAME/zein-team-planner.git
cd zein-team-planner
```
Cập nhật connection string trong appsettings.json (nếu cần)
Áp dụng migrations
```bash
dotnet ef migration add DatabaseName
dotnet ef database update
```

2. Chạy ứng dụng
```bash
dotnet run
```

Mở trình duyệt tại địa chỉ được hiển thị - thường là https://localhost:5187.

Project vẫn đang được phát triển tích cực.
Mọi góp ý, báo lỗi hoặc pull request đều được chào đón!
Made with love by ZEIN DEV TEAM
