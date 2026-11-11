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
  - API client mới kế thừa `ApiClientBase` (trả `ApiResult<T>`, tự bắt lỗi mất kết nối); lỗi API đưa vào form bằng `result.AddErrorsTo(ModelState, "Form")`.
  - Trang quản trị nằm trong Area `Admin` (`Areas/Admin`), thông báo dùng `TempData["Success"]` / `TempData["Error"]` + partial `_StatusMessage`.
  - Badge trạng thái: `<status-badge value="..." kind="motorbike|maintenance" />` (thêm kind mới trong `DisplayHelper`).
  - Form POST luôn có `[ValidateAntiForgeryToken]`; `returnUrl` phải kiểm tra `Url.IsLocalUrl`.

## Quy ước code
- Không trả Entity từ API, luôn dùng DTO: `{Entity}Dto`, `Create{Entity}Dto`, `Update{Entity}Dto`.
- Map Entity ↔ DTO **thủ công** trong `MotoRent.Services/Mappings` (không dùng AutoMapper). Dùng `Expression<Func<Entity, Dto>>` trong `.Select()` để EF dịch ra SQL.
- Validation bằng DataAnnotations trên DTO, thông báo lỗi **tiếng Việt**. Quy tắc cần truy vấn DB thì kiểm tra trong Service.
- Mọi response bọc `ApiResponse<T>` (trừ OData). Lỗi nghiệp vụ throw `BusinessException` (400), `NotFoundException` (404), `ConflictException` (409), `ForbiddenException` (403 – đã đăng nhập nhưng không có quyền với dữ liệu đó, VD khách xem đơn của người khác) → `ExceptionMiddleware` xử lý. Chưa đăng nhập: `[Authorize]` trả 401.
- Controller cần đăng nhập thì gắn `[Authorize]` / `[Authorize(Roles = ...)]`; **không** viết "fallback tạm" khi thiếu token. Lấy user bằng `User.GetUserId()` rồi ném `UnauthorizedAccessException` nếu null.
- Giờ mở cửa chi nhánh: dùng `BranchHours` (`MotoRent.BusinessObjects/Helpers`) khi kiểm tra giờ nhận / trả xe (quy tắc BR12).
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
- Người đang đăng nhập ở API: `User.GetUserId()` (`MotoRent.API/Extensions`) – trả null khi chưa có JWT.
- Tất cả quy tắc nghiệp vụ + validate đã ghi ở `LuongNghiepVu.md` mục 6 (BR1…BR76) — sửa logic thì cập nhật luôn tài liệu này.

## Chạy
- API: `dotnet run --project MotoRent.API --launch-profile http` → http://localhost:5001/swagger
- WebApp: `dotnet run --project MotoRent.WebApp --launch-profile http` → http://localhost:5002
- Connection string riêng từng máy: `MotoRent.API/appsettings.Local.json` (gitignored).
- Test API: `npx newman run docs/postman/MotoRent_Dev1.postman_collection.json` và `..._Dev2...` (API phải đang chạy).
- Test luồng thuê xe + thanh toán + phân quyền: `node docs/e2e/rental-flow.e2e.js` (nên chạy lại `MotorbikeRentalDB.sql` trước). **Mỗi lần gộp code lên `develop`/`main` nên chạy lại cả 3 bộ test này.**
