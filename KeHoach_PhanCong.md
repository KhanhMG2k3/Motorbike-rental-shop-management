# Kế hoạch dự án PRN232 – Quản lý cửa hàng cho thuê xe máy (MotoRent)

> Nhóm 3 dev · Bắt đầu: **Tuần 4** · Hoàn thành: **Tuần 9** (6 tuần) · Cập nhật: 04/10/2026

---

## 0. Tóm tắt

| Hạng mục | Lựa chọn |
|---|---|
| Backend | ASP.NET Core **Web API** (.NET 8), EF Core 8 (Database-First), SQL Server |
| Bảo mật API | **JWT Bearer** + `[Authorize(Roles = "...")]` (Admin / Staff / Customer) |
| Tìm kiếm | **OData** (`Microsoft.AspNetCore.OData`) cho Motorbikes, Rentals |
| Validation | DataAnnotations hoặc FluentValidation trên **DTO**; AutoMapper Entity ↔ DTO |
| Frontend | ASP.NET Core **MVC** + Bootstrap 5 (responsive), **TagHelper, PartialView, ViewComponent** |
| Gọi API | `IHttpClientFactory` + typed client `ApiClient` tự gắn JWT |
| Phân quyền WebApp | **Cookie Authentication + ClaimsIdentity** (đọc role từ JWT) → `[Authorize(Roles=...)]` |
| Tài liệu API | Swagger (có nút Authorize JWT) + Postman collection |
| Quản lý mã nguồn | GitHub, nhánh `main` / `develop` / `feature/*`, Pull Request có review |

> ❓ **Tiêu chí 2.7 "Identity"**: phần lớn giảng viên chấp nhận Cookie Auth + ClaimsIdentity (`HttpContext.SignInAsync`). Nếu GV yêu cầu bắt buộc **ASP.NET Core Identity** (bảng AspNetUsers), hỏi lại GV ngay **tuần 4** để điều chỉnh DB.

---

## 1. Tác nhân (Actors) – tiêu chí 1.1, 2.1

| Tác nhân | Mô tả | Chức năng chính |
|---|---|---|
| **Guest** | Khách chưa đăng nhập | Xem danh sách xe, tìm kiếm/lọc, xem chi tiết & đánh giá, đăng ký, đăng nhập |
| **Customer** | Khách hàng | Cập nhật hồ sơ (CCCD, GPLX), đặt thuê xe, xem/huỷ đơn của mình, xem lịch sử thanh toán, đánh giá xe sau khi thuê |
| **Staff** | Nhân viên chi nhánh | Duyệt/huỷ đơn, giao xe, nhận xe & tính phí, ghi nhận thanh toán, quản lý bảo dưỡng, cập nhật trạng thái xe |
| **Admin** | Quản trị | Quản lý chi nhánh, hãng, loại xe, xe, tài khoản (khoá/mở, tạo Staff), xem Dashboard doanh thu |

---

## 2. Kiến trúc Solution

```
MotoRent.sln
├── MotoRent.BusinessObjects   // Entities (scaffold), DTOs, Enums, Constants
├── MotoRent.DataAccess        // MotorbikeRentalContext, Repositories (IRepository<T>, UnitOfWork tuỳ chọn)
├── MotoRent.Services          // Business logic: AuthService, RentalService, ... + Validators + AutoMapper Profiles
├── MotoRent.API               // Controllers, OData, JWT, Swagger, Middleware xử lý lỗi
└── MotoRent.WebApp            // MVC: Controllers, Views, Partial, ViewComponents, ApiClient
```

Quy ước:
- Controller **không** gọi DbContext trực tiếp → gọi Service.
- API trả về chuẩn chung: `ApiResponse<T> { success, message, data, errors }`.
- Lỗi được bắt bởi `ExceptionMiddleware` → 400/404/409/500 rõ ràng.

---

## 3. Danh sách Endpoint – tiêu chí 1.2, 1.8, 1.9, 1.10

| Module | Method & Route | Quyền | Mô tả |
|---|---|---|---|
| **Auth** | POST `/api/auth/register` | Public | Đăng ký Customer |
| | POST `/api/auth/login` | Public | Trả JWT (+ refresh token) |
| | POST `/api/auth/refresh` | Public | Làm mới token *(tuỳ chọn)* |
| | GET/PUT `/api/auth/me` | Đã đăng nhập | Xem/sửa hồ sơ |
| | PUT `/api/auth/change-password` | Đã đăng nhập | Đổi mật khẩu |
| **Users** | GET `/api/users` | Admin | Danh sách, lọc theo role |
| | POST `/api/users/staff` | Admin | Tạo tài khoản Staff |
| | PATCH `/api/users/{id}/status` | Admin | Khoá / mở |
| **Branches** | GET `/api/branches` | Public | |
| | POST/PUT/DELETE `/api/branches/{id}` | Admin | Xoá mềm |
| **Brands / Categories** | GET `/api/brands`, `/api/categories` | Public | |
| | POST/PUT/DELETE | Admin | |
| **Motorbikes** | GET `/odata/Motorbikes` | Public | **OData**: `$filter`, `$orderby`, `$top`, `$skip`, `$expand`, `$count` |
| | GET `/api/motorbikes/{id}` | Public | Chi tiết + rating |
| | GET `/api/motorbikes/available?start=&end=` | Public | Xe trống trong khoảng thời gian |
| | POST/PUT/DELETE `/api/motorbikes/{id}` | Admin | |
| | PATCH `/api/motorbikes/{id}/status` | Admin, Staff | |
| **Rentals** | GET `/odata/Rentals` | Admin, Staff | **OData** tìm theo mã, trạng thái, ngày |
| | GET `/api/rentals/my` | Customer | Đơn của tôi |
| | GET `/api/rentals/{id}` | Chủ đơn, Staff, Admin | |
| | POST `/api/rentals` | Customer, Staff | Tạo đơn (check trùng lịch) |
| | PUT `/api/rentals/{id}/confirm` | Staff | Duyệt |
| | PUT `/api/rentals/{id}/pickup` | Staff | Giao xe |
| | PUT `/api/rentals/{id}/return` | Staff | Nhận xe, tính phí |
| | PUT `/api/rentals/{id}/cancel` | Customer (đơn mình), Staff | Huỷ |
| **Payments** | GET `/api/rentals/{id}/payments` | Chủ đơn, Staff, Admin | |
| | POST `/api/payments` | Staff | Ghi nhận thanh toán |
| **Maintenance** | GET/POST/PUT `/api/maintenances` | Admin, Staff | |
| **Reviews** | GET `/api/motorbikes/{id}/reviews` | Public | |
| | POST `/api/reviews` | Customer | Chỉ khi đơn Completed |
| **Dashboard** | GET `/api/dashboard/summary`, `/revenue?year=` | Admin | Dùng view thống kê |

---

## 4. Phân công vai trò

Nguyên tắc: **mỗi người sở hữu module từ đầu đến cuối (API + DTO + Validation + giao diện MVC)** → ai cũng hiểu toàn bộ luồng, dễ trả lời vấn đáp. Ngoài ra mỗi người giữ thêm **1 trách nhiệm chung**.

### 👤 Dev 1 – Leader / Backend Core & Security
**Trách nhiệm chung:** kiến trúc solution, Git (review & merge PR), DB, JWT, middleware, deploy/demo.

| Module | Việc cụ thể |
|---|---|
| Setup | Tạo solution 5 project, scaffold DbContext, DI, AutoMapper, Swagger + JWT, `ApiResponse`, `ExceptionMiddleware`, `CLAUDE.md` |
| Auth & Users | Register/Login/Me/ChangePassword, JWT + role claims, Admin quản lý tài khoản |
| WebApp Auth | Trang Login/Register/Profile, Cookie Auth lưu JWT, `ApiClient` (DelegatingHandler gắn Bearer token), xử lý 401 → về Login |
| Branches | CRUD API + trang Admin |
| Payments | API ghi nhận thanh toán + trang lịch sử thanh toán |
| Dashboard | API thống kê + trang Dashboard (Chart.js) |

**Kỹ năng cần nắm:** JWT, Claims, Middleware, DI, Git flow.

### 👤 Dev 2 – Backend Catalog & OData
**Trách nhiệm chung:** chuẩn hoá DTO + Validation, OData, Postman collection & test API.

| Module | Việc cụ thể |
|---|---|
| Brands, Categories | CRUD API + DTO + Validation + trang Admin |
| Motorbikes | CRUD, đổi trạng thái, upload ảnh (`wwwroot/images`), API xe trống theo ngày |
| OData | Cấu hình EDM model, `/odata/Motorbikes`, `/odata/Rentals`, giới hạn `MaxTop`, `$count` |
| WebApp Motorbikes | Trang danh sách xe (lọc: hãng, loại, giá, chi nhánh → gọi OData), phân trang, trang chi tiết xe |
| Maintenance | API + trang quản lý bảo dưỡng (Staff/Admin) |

**Kỹ năng cần nắm:** OData query options, FluentValidation/DataAnnotations, AutoMapper, LINQ.

### 👤 Dev 3 – Rental Workflow & Frontend Lead
**Trách nhiệm chung:** giao diện chung (Layout, Bootstrap responsive, TagHelper, Partial/ViewComponent), báo cáo & chụp màn hình.

| Module | Việc cụ thể |
|---|---|
| UI Shell | `_Layout`, navbar theo role, `_Alert` partial, `_Pagination` partial, `_MotorbikeCard` partial, custom TagHelper (vd `<status-badge>`, `<money>`), ViewComponent giỏ thuê |
| Rentals API | Tạo đơn (check trùng lịch, snapshot giá, sinh RentalCode), Confirm/Pickup/Return/Cancel (transaction), tính LateFee |
| WebApp Rentals | Customer: đặt xe, "Đơn của tôi", huỷ. Staff: danh sách đơn, duyệt, giao xe, nhận xe |
| Reviews | API + form đánh giá + hiển thị trên trang chi tiết xe |

**Kỹ năng cần nắm:** state machine nghiệp vụ, transaction EF, Razor/TagHelper, Bootstrap grid.

> Rentals là module nặng nhất → Dev 1 hỗ trợ phần Payment khi Return; Dev 2 hỗ trợ API xe trống.

---

## 5. Timeline chi tiết (Tuần 4 → Tuần 9)

### Tuần 4 – Phân tích & nền móng *(đang ở đây)*
| Ai | Việc | Output |
|---|---|---|
| Cả nhóm | Chốt đề tài, actors, use case, endpoint (mục 1, 3), hỏi GV về Identity | Use case diagram |
| Dev 1 | Chạy `MotorbikeRentalDB.sql`, tạo repo + solution, scaffold, Swagger, `CLAUDE.md` | Repo chạy được |
| Dev 2 | ERD (mermaid trong file giải thích DB), viết DTO cho Brand/Category/Motorbike | DTO folder |
| Dev 3 | Wireframe các trang (Figma/giấy), `_Layout` + Bootstrap | Giao diện khung |

**Mốc:** Cuối tuần 4 – mọi người clone về, chạy API (Swagger) & WebApp được.

### Tuần 5 – Auth + CRUD danh mục
| Ai | Việc |
|---|---|
| Dev 1 | Auth API (JWT, BCrypt, role), ExceptionMiddleware, `ApiResponse` |
| Dev 2 | CRUD Brands, Categories, Motorbikes (API + Validation) |
| Dev 3 | Partial/TagHelper dùng chung, trang Login/Register (giao diện), trang danh sách xe tĩnh |

**Mốc:** Login bằng Swagger lấy token, gọi API có phân quyền.

### Tuần 6 – Nghiệp vụ chính
| Ai | Việc |
|---|---|
| Dev 1 | WebApp: Cookie Auth + ApiClient, Users admin, Branches |
| Dev 2 | OData Motorbikes & Rentals, API xe trống, WebApp danh sách/chi tiết xe gọi OData |
| Dev 3 | Rentals API: tạo đơn, confirm, pickup, return, cancel |

**Mốc (giữa kỳ nội bộ):** khách đặt được 1 đơn trên WebApp, Staff duyệt được.

### Tuần 7 – Hoàn thiện module
| Ai | Việc |
|---|---|
| Dev 1 | Payments API + UI, Dashboard (view thống kê + Chart.js) |
| Dev 2 | Maintenance API + UI, Admin quản lý xe (upload ảnh) |
| Dev 3 | WebApp Rentals cho Customer & Staff, Reviews |

**Mốc:** **Feature complete** – tất cả chức năng chạy end-to-end.

### Tuần 8 – Test, sửa lỗi, đánh bóng
| Ai | Việc |
|---|---|
| Dev 1 | Kiểm thử phân quyền (mỗi role thử gọi API không được phép), refresh token (nếu kịp), review code toàn bộ |
| Dev 2 | Postman collection đầy đủ + test validate (dữ liệu sai → 400), test OData |
| Dev 3 | Responsive (mobile), thông báo lỗi thân thiện, bắt đầu chụp màn hình & viết báo cáo |

**Mốc:** Code freeze cuối tuần 8 (chỉ sửa bug).

### Tuần 9 – Báo cáo & bảo vệ
| Ai | Việc |
|---|---|
| Cả nhóm | Hoàn thành báo cáo (mục 7), slide, quay video demo dự phòng |
| Cả nhóm | **Demo thử 2 lần**, mỗi người tự giải thích code module mình **và** 1 module người khác |

### Họp nhóm
- **Daily 10 phút** (chat): Hôm qua làm gì / hôm nay / vướng gì.
- **Họp tuần** (Chủ nhật, 45 phút): demo phần đã làm, merge `develop`, chia việc tuần sau.
- Theo dõi task bằng **GitHub Projects** (Kanban: Todo / Doing / Review / Done).

---

## 6. Dùng AI để hỗ trợ code hiệu quả

### 6.1 Nguyên tắc chung
1. **AI viết – người hiểu.** Mỗi đoạn AI sinh ra phải đọc được và giải thích được; GV sẽ hỏi vấn đáp.
2. **Một nguồn quy ước chung:** đặt file `CLAUDE.md` (hoặc `.github/copilot-instructions.md`) ở gốc repo → AI của cả 3 người sinh code cùng phong cách.
3. **Đưa ngữ cảnh thật:** dán schema SQL, entity, DTO mẫu vào prompt thay vì mô tả bằng lời.
4. **Làm từng bước nhỏ:** 1 prompt = 1 module/1 lớp, chạy thử ngay, commit, rồi mới làm tiếp.
5. **Không đưa secret** (connection string thật, JWT key) vào prompt.
6. **Review chéo bằng AI trước khi tạo PR**, nhưng người review thật vẫn là thành viên nhóm.

### 6.2 Công cụ gợi ý
| Công cụ | Dùng cho |
|---|---|
| **Claude Code** (CLI / Desktop / VS Code) | Làm việc trên cả repo: sinh module, sửa lỗi nhiều file, review diff, viết test |
| GitHub Copilot (Visual Studio / VS Code) | Gợi ý code dòng-theo-dòng khi gõ |
| Claude / ChatGPT (chat) | Giải thích khái niệm, viết use case, nội dung báo cáo |

### 6.3 Mẫu `CLAUDE.md` đặt ở gốc repo
```markdown
# MotoRent – quy ước dự án
- .NET 8, EF Core 8 Database-First, SQL Server. Schema: /docs/MotorbikeRentalDB.sql
- Kiến trúc: BusinessObjects / DataAccess / Services / API / WebApp. Controller chỉ gọi Service.
- Không trả Entity từ API, luôn dùng DTO: {Entity}Dto, Create{Entity}Dto, Update{Entity}Dto.
- Validation bằng DataAnnotations trên DTO, thông báo lỗi tiếng Việt.
- Mọi response bọc trong ApiResponse<T>. Lỗi nghiệp vụ throw BusinessException -> middleware trả 400.
- Status lưu dạng string, dùng hằng số trong BusinessObjects/Constants.
- Async/await toàn bộ, tên method kết thúc bằng Async.
- Roles: "Admin", "Staff", "Customer".
- WebApp gọi API qua ApiClient (IHttpClientFactory), không gọi DbContext.
- Không tự ý thêm package NuGet mới mà không hỏi.
```

### 6.4 Prompt mẫu theo vai trò

**Dev 1 – Auth/JWT**
> Đọc CLAUDE.md và bảng Users, Roles trong docs/MotorbikeRentalDB.sql. Tạo AuthService + AuthController: register (Customer), login trả JWT chứa claims NameIdentifier, Email, Name, Role; mật khẩu dùng BCrypt.Net-Next; cấu hình JwtBearer trong Program.cs đọc key từ appsettings "Jwt". Giải thích từng bước sau khi viết.

**Dev 2 – OData**
> Cấu hình OData v8 cho entity Motorbike (expand Brand, Category, Branch) tại route /odata/Motorbikes, cho phép Filter/OrderBy/Select/Expand/Count, MaxTop 50. Cho 5 ví dụ URL tìm kiếm theo nghiệp vụ: xe tay ga Honda giá ≤ 200k còn trống, sắp xếp theo giá.

**Dev 3 – Nghiệp vụ thuê xe**
> Viết RentalService.CreateAsync(CreateRentalDto, customerId) theo quy tắc trong docs/GiaiThich_Database.md mục 3.7: kiểm tra xe Available, không trùng lịch, snapshot PricePerDay, tính NumberOfDays (làm tròn lên), SubTotal, DepositAmount, sinh RentalCode RNT-yyyyMMdd-xxxx, dùng transaction. Liệt kê các test case biên trước khi code.

**Ai cũng dùng được**
- *Debug:* "Lỗi sau xảy ra khi gọi POST /api/rentals: <dán stack trace + code liên quan>. Tìm nguyên nhân gốc, đừng chỉ che lỗi."
- *Review:* "Review diff nhánh này so với develop: lỗi logic, lỗ hổng phân quyền, N+1 query, chỗ trả Entity thay vì DTO." (Claude Code: `/code-review`)
- *Test:* "Sinh xUnit test cho RentalService.ReturnAsync với các case: trả đúng hạn, trễ 30 phút, trễ 3 giờ, trễ 8 giờ, đơn không ở trạng thái Renting."
- *Giao diện:* "Tạo view Razor danh sách xe dạng card Bootstrap 5 responsive (4 cột desktop, 2 tablet, 1 mobile) dùng partial _MotorbikeCard và TagHelper asp-*."
- *Báo cáo:* "Từ danh sách endpoint này, viết đặc tả use case 'Thuê xe' theo mẫu: Actor, Tiền điều kiện, Luồng chính, Luồng thay thế, Hậu điều kiện."
- *Vấn đáp:* "Đóng vai giảng viên PRN232, hỏi tôi 10 câu khó về phần JWT/OData/Rentals trong code này và chấm câu trả lời."

### 6.5 Những thứ KHÔNG nên giao hết cho AI
- Thiết kế nghiệp vụ (quy tắc tính phí, trạng thái) → nhóm tự chốt, AI chỉ triển khai.
- Merge conflict lớn → tự đọc và giải quyết.
- Nội dung "Phân công công việc" trong báo cáo → phải trung thực.

---

## 7. Checklist đối chiếu tiêu chí chấm

| Tiêu chí | Ở đâu | Phụ trách |
|---|---|---|
| 1.1 / 2.1 Tác nhân | Mục 1 + báo cáo | Cả nhóm |
| 1.2 / 2.2 Chức năng endpoint | Mục 3 + Swagger | Cả nhóm |
| 1.3 ASP.NET Web API | MotoRent.API | Dev 1 |
| 1.4 SQL Server + EF Core | MotorbikeRentalDB.sql, DbContext | Dev 1 |
| 1.5 Ràng buộc dữ liệu CSDL | CHECK/UNIQUE/FK (file giải thích mục 5) | Dev 1 |
| 1.6 DTO + CRUD | Tất cả module | Dev 2 chuẩn hoá |
| 1.7 Validation | DTO + Service | Dev 2 |
| 1.8 Routing | Attribute routing theo mục 3 | Cả nhóm |
| 1.9 OData | /odata/Motorbikes, /odata/Rentals | Dev 2 |
| 1.10 JWT + Role | Auth + `[Authorize(Roles)]` | Dev 1 |
| 2.3 MVC | MotoRent.WebApp | Dev 3 |
| 2.4 HTML5/CSS3/Bootstrap responsive | Layout, views | Dev 3 |
| 2.5 TagHelper, PartialView | `_MotorbikeCard`, `_Pagination`, `<status-badge>` | Dev 3 |
| 2.6 Gọi Web API | ApiClient | Dev 1 |
| 2.7 Identity / phân quyền | Cookie Auth + Claims | Dev 1 |
| 2.8 Kiểm tra dữ liệu phía client | jQuery Validation + ModelState | Dev 3 |
| 3. Báo cáo | Phân tích thiết kế, phân công, ảnh giao diện | Dev 3 chủ trì, cả nhóm viết |

### Cấu trúc báo cáo đề xuất
1. Giới thiệu đề tài & mục tiêu
2. Phân tích: actors, use case diagram, đặc tả use case chính (Đặt xe, Duyệt đơn, Giao/Nhận xe, Thanh toán)
3. Thiết kế: kiến trúc solution, ERD, mô tả bảng, danh sách API, sơ đồ trạng thái đơn thuê
4. Kết quả: ảnh chụp giao diện từng chức năng theo role + ảnh Swagger/Postman
5. Phân công công việc (bảng mục 4 + % đóng góp)
6. Kết luận, hạn chế, hướng phát triển (thanh toán VNPay thật, gửi email, bản đồ chi nhánh)

---

## 8. Rủi ro & phương án

| Rủi ro | Phương án |
|---|---|
| Rentals trễ tiến độ (module khó nhất) | Làm luồng chính trước (Create → Confirm → Pickup → Return); Cancel/LateFee sau. Dev 1 hỗ trợ từ tuần 7 |
| Conflict khi merge | Chia theo module/thư mục, PR nhỏ, merge `develop` ít nhất 2 lần/tuần |
| DB khác nhau giữa các máy | Ai đổi schema phải cập nhật file SQL + báo nhóm, mọi người chạy lại script + scaffold lại |
| Code AI không đồng nhất | Bắt buộc dùng `CLAUDE.md`, review PR |
| Thành viên không hiểu code người khác | Tuần 9 mỗi người trình bày lại 1 module của người khác |
