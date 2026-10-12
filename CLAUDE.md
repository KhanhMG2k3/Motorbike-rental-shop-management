# MotoRent – quy ước dự án (dành cho người và AI)

## Công nghệ
- .NET 8, ASP.NET Core Web API + MVC, EF Core 8 **Database-First**, SQL Server, OData 9.
- Schema gốc: `MotorbikeRentalDB.sql`. Luồng nghiệp vụ: `LuongNghiepVu.md`. Giải thích DB: `GiaiThich_Database.md`.

## Kiến trúc
- `MotoRent.BusinessObjects`: Entities (scaffold – KHÔNG sửa tay), DTOs, Constants, Exceptions, Validation.
- `MotoRent.DataAccess`: `MotorbikeRentalContext` (scaffold), Repositories (`IGenericRepository<T>`, repository riêng khi cần query đặc thù).
- `MotoRent.Services`: business logic. Đăng ký DI trong `ServiceRegistration.AddMotoRentServices()`.
- `MotoRent.API`: Controller chỉ gọi Service, không gọi DbContext. OData controller nằm trong `Controllers/OData`.
- `MotoRent.WebApp`: MVC, gọi API qua typed `HttpClient` trong `Services/`. Không tham chiếu DataAccess.

## Quy ước code
- Không trả Entity từ API, luôn dùng DTO: `{Entity}Dto`, `Create{Entity}Dto`, `Update{Entity}Dto`.
- Map Entity ↔ DTO **thủ công** trong `MotoRent.Services/Mappings` (không dùng AutoMapper). Dùng `Expression<Func<Entity, Dto>>` trong `.Select()` để EF dịch ra SQL.
- Validation bằng DataAnnotations trên DTO, thông báo lỗi **tiếng Việt**. Quy tắc cần truy vấn DB thì kiểm tra trong Service.
- Mọi response bọc `ApiResponse<T>` (trừ OData). Lỗi nghiệp vụ throw `BusinessException` (400), `NotFoundException` (404), `ConflictException` (409) → `ExceptionMiddleware` xử lý.
- Trạng thái lưu dạng string, dùng hằng số trong `BusinessObjects/Constants` (`MotorbikeStatus`, `RentalStatus`, `AppRoles`).
- Async/await toàn bộ, tên method kết thúc bằng `Async`.
- Xoá = xoá mềm (`IsActive = false` hoặc `Status = Inactive`) để giữ lịch sử.
- Thời gian dùng `DateTime.Now` (DB lưu giờ địa phương).
- Không tự ý thêm NuGet package mới khi chưa thống nhất với nhóm.

## Phân công
- Dev 1: Auth/JWT, Users, Branches, Payments, Dashboard, ApiClient + Cookie Auth WebApp.
- Dev 2 (Đạt): Brands, Categories, Motorbikes, OData, Maintenance, DTO/Validation, Postman.
- Dev 3: Rentals (tạo/duyệt/giao/trả/huỷ), Reviews, Layout/Partial/TagHelper, báo cáo.
- Kiểm tra trùng lịch xe: dùng `IMotorbikeRepository.IsAvailableAsync(...)` (đã có sẵn).

## Chạy
- API: `dotnet run --project MotoRent.API --launch-profile http` → http://localhost:5001/swagger
- WebApp: `dotnet run --project MotoRent.WebApp --launch-profile http` → http://localhost:5002
- Connection string riêng từng máy: `MotoRent.API/appsettings.Local.json` (gitignored).
