# MotoRent – Hệ thống quản lý cửa hàng cho thuê xe máy (PRN232)

ASP.NET Core Web API + MVC WebApp · .NET 8 · EF Core 8 · SQL Server · OData

## Tài liệu
| File | Nội dung |
|---|---|
| [KeHoach_PhanCong.md](KeHoach_PhanCong.md) | Kế hoạch tuần 5–9, phân công 3 dev, dùng AI |
| [LuongNghiepVu.md](LuongNghiepVu.md) | Luồng nghiệp vụ shop thuê xe, sơ đồ trạng thái, quy tắc |
| [GiaiThich_Database.md](GiaiThich_Database.md) | Giải thích từng bảng, ràng buộc |
| [MotorbikeRentalDB.sql](MotorbikeRentalDB.sql) | Script tạo DB + dữ liệu mẫu |
| [CLAUDE.md](CLAUDE.md) | Quy ước code (cho người và AI) |

## Cấu trúc solution
```
MotoRent.sln
├── MotoRent.BusinessObjects   Entities (scaffold), DTOs, Constants, Exceptions, Validation
├── MotoRent.DataAccess        MotorbikeRentalContext, Repositories
├── MotoRent.Services          Business logic, Mappings (map thủ công)
├── MotoRent.API               Web API + OData + Swagger        → http://localhost:5001/swagger
└── MotoRent.WebApp            ASP.NET Core MVC (gọi API)        → http://localhost:5002
```

## Cài đặt lần đầu (mỗi thành viên)
1. Cài **.NET 8 SDK**, **SQL Server**, **Visual Studio 2022** (hoặc VS Code).
2. Mở SSMS → chạy `MotorbikeRentalDB.sql` (tạo DB + dữ liệu mẫu).
3. Tạo file `MotoRent.API/appsettings.Local.json` (file này **không** đưa lên Git) với tên SQL Server của máy bạn:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.\\SQLEXPRESS;Database=MotorbikeRentalDB;Trusted_Connection=True;TrustServerCertificate=True"
     }
   }
   ```
   > Dùng SQL Server mặc định (`localhost`) thì không cần tạo file này.
4. Chạy **cả 2 project**: trong Visual Studio → chuột phải Solution → *Configure Startup Projects* → *Multiple startup projects* → chọn `MotoRent.API` và `MotoRent.WebApp` = **Start** (profile **http**).
   Hoặc dùng terminal:
   ```bash
   dotnet run --project MotoRent.API --launch-profile http
   dotnet run --project MotoRent.WebApp --launch-profile http
   ```

Tài khoản mẫu: `admin@motorent.vn`, `staff1@motorent.vn`, `customer1@example.com` … mật khẩu `123456`.

## Scaffold lại khi đổi database
```bash
dotnet tool restore
dotnet ef dbcontext scaffold "Server=.\SQLEXPRESS;Database=MotorbikeRentalDB;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --project MotoRent.DataAccess --startup-project MotoRent.API --output-dir ../MotoRent.BusinessObjects/Entities --namespace MotoRent.BusinessObjects.Entities --context-dir . --context MotorbikeRentalContext --context-namespace MotoRent.DataAccess --no-onconfiguring --force
```
> Không sửa trực tiếp file trong `Entities/` và `MotorbikeRentalContext.cs` vì scaffold lại sẽ bị ghi đè. Cần cấu hình thêm thì viết `partial class` ở file khác.

## API đã có (Dev 2 – Catalog)
| Method | Route | Mô tả |
|---|---|---|
| GET | `/api/brands`, `/api/brands/{id}` | Hãng xe (`?includeInactive=true` xem cả hãng đã ngừng) |
| POST / PUT / DELETE | `/api/brands/{id}` | Thêm / sửa / ngừng hoạt động (xoá mềm) |
| GET | `/api/categories`, `/api/categories/{id}` | Loại xe |
| POST / PUT / DELETE | `/api/categories/{id}` | Thêm / sửa / ngừng hoạt động |
| GET | `/api/motorbikes/{id}` | Chi tiết xe |
| GET | `/api/motorbikes/available?startDate=&endDate=&branchId=&categoryId=&brandId=` | Xe còn trống theo thời gian |
| POST / PUT | `/api/motorbikes/{id}` | Thêm / sửa xe |
| PATCH | `/api/motorbikes/{id}/status` | Đổi trạng thái (Available / Maintenance / Inactive) |
| DELETE | `/api/motorbikes/{id}` | Ngừng cho thuê (xoá mềm) |
| GET | `/odata/Motorbikes` | **OData** tìm kiếm xe: `$filter`, `$orderby`, `$select`, `$top`, `$skip`, `$count` |
| GET | `/odata/Rentals` | **OData** tra cứu đơn thuê (Admin/Staff) |

Ví dụ: xem file [MotoRent.API/MotoRent.API.http](MotoRent.API/MotoRent.API.http) (bấm *Send Request* ngay trong Visual Studio / VS Code).

## WebApp đã có
- `/Motorbikes` – danh sách xe: lọc theo từ khoá, hãng, loại, khoảng giá, chỉ xe sẵn sàng; sắp xếp; phân trang (gọi OData).
- `/Motorbikes/Details/{id}` – chi tiết xe, kiểm tra lịch trống + tạm tính tiền, xe cùng loại.
- Partial dùng chung: `_MotorbikeCard`, `_Pagination`.

## Quy trình Git
```
main  ← bản ổn định (merge từ develop cuối tuần)
 └── develop  ← nhánh chung
       └── feature/<tên-việc>  ← mỗi người làm ở đây, xong tạo Pull Request vào develop
```
