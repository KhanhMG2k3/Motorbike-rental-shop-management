# KẾ HOẠCH DỰ ÁN NESTU – QUẢN LÝ NHÀ TRỌ SINH VIÊN

**Môn:** PRN232 – Building Cross-Platform Back-End Application With .NET
**Nhóm:** Đạt · Khánh · Lộc
**Thời gian:** Tuần 4 → Tuần 9 (hạn nộp cuối tuần 9)

---

## 1. Tổng quan kỹ thuật

### 1.1. Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Nền tảng | .NET 8 |
| Back-end | ASP.NET Core Web API |
| ORM / CSDL | Entity Framework Core (Code First) + SQL Server |
| Xác thực / phân quyền | ASP.NET Core Identity + JWT Bearer, Role-Based |
| Truy vấn nâng cao | OData (`Microsoft.AspNetCore.OData`) |
| Front-end (Client) | ASP.NET Core MVC, HTML5, CSS3, Bootstrap 5, TagHelper, PartialView |
| Tài liệu API | Swagger |
| Quản lý mã nguồn | GitHub |

### 1.2. Cấu trúc solution

```
Nestu.sln
├── Nestu.BusinessObjects   Entities, Enums, NestuDbContext, Fluent API
├── Nestu.DTOs              Request/Response DTO (dùng chung API + Client)
├── Nestu.Repositories      IRepository + Repository
├── Nestu.Services          Business logic, kiểm tra nghiệp vụ
├── Nestu.API               Controllers, OData, JWT, Swagger
└── Nestu.Client            MVC: Controllers, Views, PartialViews, ApiServices (HttpClient)
```

### 1.3. Cơ chế xác thực (đáp ứng 1.10 và 2.7)

1. **API** dùng ASP.NET Core Identity để lưu User/Role. Endpoint `POST /api/auth/login` kiểm tra mật khẩu bằng `UserManager` và trả về **JWT** chứa các claim `NameIdentifier`, `Email`, `FullName`, `Role`.
2. **Client MVC** gọi API login và nhận JWT, sau đó đăng nhập bằng **Cookie Authentication**, sao chép các claim từ JWT và lưu token vào claim `access_token`.
3. Các ApiService ở Client lấy `access_token` để gắn header `Authorization: Bearer ...` khi gọi API.
4. Controller ở cả API và Client đều dùng `[Authorize(Roles = "Admin|Landlord|Tenant")]`.

---

## 2. Tác nhân và chức năng hệ thống

### 2.1. Tác nhân

| Tác nhân | Mô tả |
|---|---|
| **Guest** | Người chưa đăng nhập |
| **Tenant** | Sinh viên thuê trọ |
| **Landlord** | Chủ nhà trọ |
| **Admin** | Quản trị hệ thống |

### 2.2. Chức năng theo tác nhân

**Guest**
- Xem danh sách phòng trống
- Tìm kiếm, lọc, sắp xếp phòng theo giá, quận, diện tích (OData)
- Xem chi tiết phòng và thông tin nhà trọ
- Đăng ký tài khoản (Tenant hoặc Landlord), đăng nhập

**Tenant**
- Gửi yêu cầu thuê phòng, hủy yêu cầu đang chờ
- Xem danh sách hợp đồng của mình
- Xem hóa đơn hàng tháng và trạng thái thanh toán
- Gửi báo cáo sự cố và theo dõi trạng thái xử lý
- Cập nhật hồ sơ cá nhân

**Landlord**
- CRUD nhà trọ (nhà mới chờ Admin duyệt)
- CRUD phòng và ảnh phòng
- Duyệt hoặc từ chối yêu cầu thuê
- Xem và kết thúc hợp đồng
- Nhập chỉ số điện nước hàng tháng
- Tạo hóa đơn, xác nhận đã thanh toán
- Tiếp nhận và cập nhật trạng thái sự cố

**Admin**
- Quản lý người dùng (xem, khóa, mở khóa)
- Duyệt hoặc từ chối nhà trọ mới đăng
- Xem thống kê: số user, số nhà trọ, số phòng trống/đã thuê, doanh thu hóa đơn theo tháng

---

## 3. Phân công

| | **Đạt** | **Khánh** | **Lộc** |
|---|---|---|---|
| **Phạm vi** | Nền tảng, Xác thực, Admin, Sự cố | Nhà trọ, Phòng, Tìm kiếm | Thuê phòng, Hợp đồng, Hóa đơn |
| **Bảng phụ trách** | AspNetUsers, MaintenanceRequests | BoardingHouses, Rooms, RoomImages | RentalRequests, Contracts, UtilityReadings, Invoices |
| **API** | Auth (register/login/profile), Users, Admin statistics, MaintenanceRequests | Houses, Rooms, RoomImages, OData Rooms | RentalRequests, Contracts, UtilityReadings, Invoices, OData Invoices |
| **Client** | Layout chung, Login/Register/Profile, trang Admin, màn sự cố (Tenant + Landlord) | Trang Guest (danh sách, tìm kiếm, chi tiết), Landlord quản lý nhà/phòng, PartialView `_RoomCard`, `_Pagination` | Gửi/duyệt yêu cầu thuê, hợp đồng, nhập điện nước, hóa đơn (Landlord + Tenant) |
| **Trách nhiệm riêng** | Tạo solution, DbContext; **người duy nhất tạo migration**; seed data; ghép báo cáo cuối | Hoàn thành **module mẫu (Rooms)** sớm nhất để cả nhóm theo pattern | Test chéo luồng Thuê → Hợp đồng → Hóa đơn |

**Việc chung cả nhóm:**
- Tuần 4: chốt Use case diagram, ERD, danh sách endpoint, DTO.
- Mỗi người viết phần báo cáo cho module của mình: đặc tả chức năng, bảng endpoint, ảnh giao diện.
- Tuần 8: test chéo module của người khác.

---

## 4. Lộ trình theo tuần

| Tuần | Đạt | Khánh | Lộc | Mốc cần đạt |
|---|---|---|---|---|
| **4** | Tạo solution và repo GitHub; Entities, Enums, DbContext, Fluent API; migration đầu tiên | DTO Houses/Rooms; wireframe màn Guest + Landlord | DTO Rental/Contract/Invoice; wireframe màn thuê + hóa đơn | DB tạo được bằng migration; cả nhóm clone và chạy được. **Cả nhóm:** Use case, ERD, danh sách endpoint |
| **5** | Identity + JWT; Login/Register + cookie auth ở Client; Layout Bootstrap 5 | API Houses/Rooms + validation (test bằng Swagger) | API RentalRequests (test bằng Swagger) | Đăng nhập theo role chạy end-to-end |
| **6** | Seed data đầy đủ; cùng 2 bạn gắn `[Authorize]` vào API | OData Rooms; màn tìm kiếm + chi tiết phòng | Màn gửi/duyệt yêu cầu thuê; API Contracts | Guest tìm được phòng; luồng thuê phòng chạy |
| **7** | API + màn MaintenanceRequests | Màn Landlord quản lý nhà/phòng; upload ảnh | API + màn UtilityReadings, Invoices | Luồng hóa đơn chạy |
| **8** | Admin: quản lý user, duyệt nhà trọ, thống kê; global exception handling | Responsive toàn bộ, hoàn thiện UI, PartialView | OData Invoices; màn Tenant (hợp đồng, hóa đơn) | **Feature freeze cuối tuần 8**, test chéo |
| **9** | Fix bug; ghép báo cáo | Fix bug; chụp ảnh giao diện | Fix bug; chụp ảnh giao diện | Tập demo 2 lần; nộp bài |

### Điểm nghẽn cần lưu ý

- **Tuần 5:** Khánh và Lộc cần phần auth của Đạt. Trong lúc chờ, hai bạn viết API chưa gắn `[Authorize]` và test bằng Swagger; khi auth xong thì bổ sung.
- **Room.Status:** chỉ `RoomService` (Khánh) được đổi trạng thái phòng. Khi duyệt thuê hoặc kết thúc hợp đồng, Lộc gọi `RoomService` chứ không sửa trực tiếp.
- **Migration:** mọi thay đổi Entity phải báo Đạt để Đạt tạo migration, tránh conflict file migration.

---

## 5. Quy tắc làm việc nhóm

**Git**
- Nhánh: `main` ← `dev` ← `feature/<ten-chuc-nang>` (ví dụ `feature/invoice-api`).
- Chỉ merge vào `dev` qua Pull Request, cần ít nhất 1 người review.
- Cuối mỗi tuần merge `dev` → `main` khi chạy ổn.
- Commit message: `feat: ...`, `fix: ...`, `refactor: ...`, `docs: ...`.

**Code**
- Tên class, biến, API bằng tiếng Anh; message trả về người dùng bằng tiếng Việt.
- Dùng async/await cho mọi thao tác DB và HTTP.
- Controller không gọi DbContext trực tiếp: Controller → Service → Repository.
- API không bao giờ trả Entity, luôn trả DTO.
- Format response lỗi thống nhất: `{ "message": "...", "errors": { ... } }`.

**Họp**
- 2 buổi cố định mỗi tuần, mỗi buổi 15 phút: mỗi người nói đã làm gì, sắp làm gì, đang vướng gì.

---

## 6. Checklist đối chiếu yêu cầu đề

| Yêu cầu | Cách đáp ứng | Người phụ trách chính |
|---|---|---|
| 1.1 Tác nhân sử dụng API | Mục 2.1 + phân quyền endpoint trong báo cáo | Cả nhóm |
| 1.2 Chức năng các endpoint | Bảng endpoint (Method, Route, Role, Mô tả, Request, Response) | Mỗi người viết phần mình |
| 1.3 ASP.NET Web API | Project Nestu.API | Cả nhóm |
| 1.4 SQL Server + EF Core | Code First, NestuDbContext | Đạt |
| 1.5 Ràng buộc trong code và CSDL | CHECK/UNIQUE/FK bằng Fluent API + kiểm tra nghiệp vụ ở Service | Cả nhóm |
| 1.6 DTO cho CRUD | Project Nestu.DTOs | Cả nhóm |
| 1.7 Validation | DataAnnotations trên DTO + kiểm tra ở Service | Cả nhóm |
| 1.8 Routing | Route theo nghiệp vụ, ví dụ `api/houses/{houseId}/rooms` | Cả nhóm |
| 1.9 OData | `/odata/Rooms`, `/odata/Invoices` | Khánh, Lộc |
| 1.10 JWT + Role | Identity + JWT ở API | Đạt |
| 2.1–2.2 Tác nhân và chức năng Client | Mục 2 | Cả nhóm |
| 2.3 MVC | Project Nestu.Client | Cả nhóm |
| 2.4 HTML5, CSS3, Bootstrap 5 responsive | Layout chung + từng View | Đạt (layout), Khánh (responsive) |
| 2.5 TagHelper, PartialView | `asp-for`, `asp-validation-for`, `_RoomCard`, `_Pagination`, `_InvoiceRow` | Cả nhóm |
| 2.6 Client dùng Web API | ApiServices dùng HttpClient | Cả nhóm |
| 2.7 Identity phân quyền | Cookie auth + role claims | Đạt |
| 2.8 Ràng buộc dữ liệu trong code | Validation DTO + jQuery unobtrusive validation | Cả nhóm |
| 3 Báo cáo | Phân tích thiết kế, phân công, mã nguồn, ảnh giao diện | Đạt ghép, cả nhóm viết |

---

## 7. Hướng dẫn dùng AI hỗ trợ

### 7.1. Nguyên tắc chung

1. **Luôn đưa ngữ cảnh trước.** AI không biết dự án của bạn; thiếu ngữ cảnh thì code sinh ra sẽ lệch kiến trúc.
2. **Làm mẫu một lần, nhân bản nhiều lần.** Khánh làm module Rooms thật chuẩn trước, sau đó mọi module khác đều bảo AI "làm theo đúng pattern này".
3. **Chia nhỏ yêu cầu.** Mỗi lần chỉ xin một module hoặc một màn hình, chạy thử được rồi mới làm tiếp.
4. **Không để AI tự đổi database.** Nếu AI đề xuất thêm cột hay bảng, báo Đạt trước.
5. **Hiểu code trước khi commit.** Giảng viên sẽ hỏi vấn đáp; phần nào không giải thích được thì hỏi lại AI cho đến khi hiểu.
6. **Tạo cuộc chat mới cho mỗi module.** Chat quá dài thì AI dễ quên hoặc trộn ngữ cảnh.

### 7.2. File ngữ cảnh chung: `PROJECT_CONTEXT.md`

Đặt file này ở thư mục gốc repo và dán vào đầu mỗi cuộc chat với AI (hoặc thêm vào Project Knowledge nếu dùng Claude Projects):

```markdown
# Nestu – Ngữ cảnh dự án
Đề tài: Quản lý nhà trọ sinh viên (môn PRN232, FPT University)
Stack: .NET 8, ASP.NET Core Web API, EF Core Code First, SQL Server,
ASP.NET Core Identity + JWT, OData, MVC Client + Bootstrap 5.

Kiến trúc:
- Nestu.BusinessObjects: Entities, Enums, NestuDbContext
- Nestu.DTOs: Request/Response DTO
- Nestu.Repositories: Repository pattern
- Nestu.Services: business logic
- Nestu.API: Controllers, OData
- Nestu.Client: MVC, gọi API bằng HttpClient, cookie auth chứa JWT

Quy ước:
- async/await mọi nơi; Controller → Service → Repository
- API chỉ trả DTO, không trả Entity
- Tên tiếng Anh, message lỗi trả về tiếng Việt
- Lỗi trả về dạng { "message": "...", "errors": {...} }
- Roles: Admin, Landlord, Tenant
- Lấy userId từ claim ClaimTypes.NameIdentifier

Database: [dán nội dung file Nestu_ThietKe_Database.md]
```

### 7.3. Mẫu prompt theo từng loại việc

**A. Tạo Entity + cấu hình Fluent API** (Đạt, tuần 4)

```
[Dán PROJECT_CONTEXT.md]
Tạo cho tôi toàn bộ Entities, Enums và NestuDbContext (kế thừa
IdentityDbContext<ApplicationUser>) theo đúng thiết kế database ở trên.
Yêu cầu:
- Cấu hình bằng Fluent API trong các class IEntityTypeConfiguration<T> riêng
- Đủ mọi CHECK constraint, UNIQUE index, filtered index, precision decimal(18,2)
- DeleteBehavior.Restrict cho mọi FK trừ Rooms → RoomImages (Cascade)
- Seed 3 role Admin, Landlord, Tenant
Liệt kê lệnh migration cần chạy ở cuối.
```

**B. Tạo module API mẫu đầu tiên** (Khánh, tuần 5)

```
[Dán PROJECT_CONTEXT.md]
Tạo module Room hoàn chỉnh:
1. DTOs: RoomCreateDto, RoomUpdateDto, RoomResponseDto (có validation DataAnnotations,
   message tiếng Việt)
2. IRoomRepository + RoomRepository
3. IRoomService + RoomService: Landlord chỉ được thao tác phòng thuộc nhà trọ của mình
   (so OwnerId với userId), không cho trùng RoomNumber trong cùng nhà trọ
4. RoomsController với route api/houses/{houseId}/rooms
5. Đăng ký DI trong Program.cs
Giải thích ngắn vì sao chia tầng như vậy để tôi trả lời được khi bảo vệ.
```

**C. Nhân bản module theo mẫu** (tất cả, từ tuần 5)

```
[Dán PROJECT_CONTEXT.md]
Đây là module Room mẫu của nhóm:
[dán RoomDto, IRoomRepository, RoomRepository, RoomService, RoomsController]

Tạo module <TÊN MODULE> theo ĐÚNG pattern trên (cùng cách đặt tên, cùng cách
xử lý lỗi, cùng cách lấy userId).
Nghiệp vụ:
- <liệt kê quy tắc nghiệp vụ của module, lấy từ mục "Ràng buộc nghiệp vụ"
  trong file database>
Phân quyền: <role nào được gọi endpoint nào>
```

Ví dụ phần nghiệp vụ cho Invoice (Lộc):

```
- ElectricFee = (ElectricNew - ElectricOld) * BoardingHouse.ElectricPrice
- WaterFee = (WaterNew - WaterOld) * BoardingHouse.WaterPrice
- TotalAmount = RoomFee + ElectricFee + WaterFee + OtherFee
- Chỉ tạo hóa đơn cho hợp đồng đang Active
- Không cho trùng (ContractId, Month, Year) → trả 400 "Hóa đơn tháng này đã tồn tại"
- Landlord chỉ tạo hóa đơn cho phòng thuộc nhà trọ của mình
```

**D. Identity + JWT** (Đạt, tuần 5)

```
[Dán PROJECT_CONTEXT.md]
Viết phần xác thực cho Nestu.API:
- AuthController: register (chọn role Tenant hoặc Landlord), login trả JWT, get profile
- JWT chứa claim NameIdentifier, Email, FullName, Role; hết hạn 2 giờ
- Cấu hình JWT Bearer trong Program.cs, đọc key từ appsettings.json
- Chặn login nếu IsActive = false
- Cấu hình Swagger có nút Authorize nhập Bearer token
Sau đó viết phần Client (Nestu.Client):
- AccountController gọi API login, tạo cookie auth với claim copy từ JWT,
  lưu token vào claim "access_token"
- DelegatingHandler tự gắn Bearer token vào mọi request HttpClient
- Views Login/Register dùng TagHelper + Bootstrap 5
```

**E. OData** (Khánh, tuần 6)

```
[Dán PROJECT_CONTEXT.md]
Thêm OData cho Rooms trong Nestu.API:
- Endpoint /odata/Rooms trả RoomResponseDto (không expose Entity)
- Cho phép $filter, $orderby, $top, $skip, $count, $select
- Chỉ trả phòng thuộc nhà trọ Approved và IsActive
- Cấu hình EDM model trong Program.cs
Rồi viết phía Client: form tìm kiếm (giá từ–đến, quận, diện tích tối thiểu, sắp xếp),
chuyển thành query OData, hiển thị bằng PartialView _RoomCard và phân trang _Pagination.
Cho tôi 3 ví dụ URL OData để demo với giảng viên.
```

**F. Màn hình Client** (tất cả)

```
[Dán PROJECT_CONTEXT.md]
Dựa trên <TÊN>ResponseDto và <TÊN>CreateDto: [dán code]
Tạo cho Nestu.Client:
- <TÊN>ApiService dùng HttpClient (đã có DelegatingHandler gắn token)
- <TÊN>Controller với [Authorize(Roles = "...")]
- Views: Index (bảng Bootstrap 5 responsive, dùng PartialView _<TÊN>Row),
  Create, Edit, Details
- Form dùng asp-for, asp-validation-for, partial _ValidationScriptsPartial
- Hiển thị message lỗi từ API (format { message, errors }) lên form
- Dùng TempData để thông báo thành công
```

**G. Debug**

```
Lỗi: [dán nguyên văn stack trace đầy đủ]
File liên quan: [dán code]
Tôi đang làm: <mô tả thao tác gây lỗi>
Tôi đã thử: <những gì đã thử>
Hãy giải thích nguyên nhân trước, sau đó mới đưa đoạn code cần sửa (chỉ phần thay đổi).
```

**H. Review code trước khi tạo PR**

```
[Dán PROJECT_CONTEXT.md]
Review đoạn code sau theo tiêu chí: đúng kiến trúc Controller → Service → Repository,
không trả Entity, có kiểm tra quyền sở hữu (Landlord chỉ sửa dữ liệu của mình),
có validation, xử lý null, async đúng cách.
Liệt kê vấn đề theo mức độ nghiêm trọng.
[dán code]
```

**I. Chuẩn bị vấn đáp**

```
Đây là code module <TÊN> tôi phụ trách: [dán code]
Đóng vai giảng viên PRN232, hỏi tôi 10 câu vấn đáp về code này
(JWT, Identity, OData, DTO, validation, EF Core, routing). Hỏi từng câu một,
chờ tôi trả lời rồi nhận xét.
```

**J. Viết báo cáo**

```
Đây là các Controller của module <TÊN>: [dán code]
Viết phần báo cáo gồm:
1. Bảng endpoint: Method | Route | Role | Mô tả | Request body | Response
2. Đặc tả use case chính (tên, tác nhân, tiền điều kiện, luồng chính, luồng thay thế)
Chỉ mô tả những gì có trong code, không thêm chức năng không tồn tại.
```

### 7.4. Những điều không nên làm

- Không dán cả solution vào một lần; chỉ dán các file liên quan.
- Không chấp nhận code AI dùng thư viện lạ ngoài stack đã thống nhất.
- Không commit code chưa chạy thử.
- Không để AI viết báo cáo mô tả chức năng mà hệ thống chưa có.
