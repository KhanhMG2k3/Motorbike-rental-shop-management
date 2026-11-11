# Test case – Dev 1 (Auth, JWT, Branches, Users, Dashboard, Phân quyền)

> Người thực hiện: **Dev 1** · Ngày test: 18/10/2026
> Công cụ: **Postman / Newman** (Collection: `docs/postman/MotoRent_Dev1.postman_collection.json`)

---

## 1. Hướng dẫn chạy kiểm thử tự động

### Bằng Postman:
1. Chạy `MotoRent.API` trên cổng `http://localhost:5001`.
2. Mở Postman → **Import** → chọn file `docs/postman/MotoRent_Dev1.postman_collection.json`.
3. Chuột phải vào collection → chọn **Run collection** → **Run MotoRent - Dev 1 Test Suite**.

### Bằng dòng lệnh (Newman):
```bash
npx newman run docs/postman/MotoRent_Dev1.postman_collection.json
```

---

## 2. Bảng Test Case chi tiết

### 2.1 Xác thực & Hồ sơ cá nhân (Auth & Profile)
| ID | Chức năng | Dữ liệu vào | Kết quả mong đợi | KQ |
|---|---|---|---|---|
| TC-A01 | Đăng nhập Admin hợp lệ | `admin@motorent.vn` / `123456` | 200 OK, trả JWT token, Role = `Admin` | ✅ |
| TC-A02 | Đăng nhập Staff hợp lệ | `staff1@motorent.vn` / `123456` | 200 OK, trả JWT token, Role = `Staff` | ✅ |
| TC-A03 | Đăng nhập Customer hợp lệ | `customer1@example.com` / `123456` | 200 OK, trả JWT token, Role = `Customer` | ✅ |
| TC-A04 | Đăng nhập sai mật khẩu | `admin@motorent.vn` / `sai_mat_khau` | 400 Bad Request, báo "Email hoặc mật khẩu không chính xác." | ✅ |
| TC-A05 | Đăng nhập email sai định dạng | `email-sai-dinh-dang` | 400 Bad Request, ModelState validation lỗi Email | ✅ |
| TC-A06 | Xem hồ sơ cá nhân `/api/auth/me` | Gắn Bearer Token Admin | 200 OK, trả đúng profile của Admin | ✅ |
| TC-A07 | Xem hồ sơ cá nhân không có token | Không gắn Header Authorization | 401 Unauthorized | ✅ |
| TC-A08 | Đăng ký tài khoản khách hàng dưới 18 tuổi | `dateOfBirth`: 2015-01-01 | 400 Bad Request, báo "Khách hàng phải từ đủ 18 tuổi trở lên." | ✅ |
| TC-A09 | Đăng ký trùng email đã tồn tại | Email `admin@motorent.vn` | 409 Conflict, báo "Email này đã được đăng ký." | ✅ |
| TC-A10 | Đổi mật khẩu thành công | Đúng mật khẩu cũ + mật khẩu mới $\ge$ 6 ký tự | 200 OK, "Đổi mật khẩu thành công." | ✅ |
| TC-A11 | Đổi mật khẩu sai mật khẩu cũ | Nhập sai CurrentPassword | 400 Bad Request, báo "Mật khẩu hiện tại không chính xác." | ✅ |

---

### 2.2 Quản lý Chi nhánh (Branches)
| ID | Chức năng | Dữ liệu vào | Kết quả mong đợi | KQ |
|---|---|---|---|---|
| TC-BR01 | Lấy danh sách chi nhánh (Public) | `GET /api/branches` | 200 OK, danh sách các chi nhánh đang hoạt động | ✅ |
| TC-BR02 | Thêm chi nhánh không có token | `POST /api/branches` (No Auth) | 401 Unauthorized | ✅ |
| TC-BR03 | Khách hàng thêm chi nhánh | Gắn Bearer Token Customer | 403 Forbidden | ✅ |
| TC-BR04 | Thêm chi nhánh giờ đóng $\le$ giờ mở | `openTime: 21:00`, `closeTime: 07:00` | 400 Bad Request, báo "Giờ đóng cửa phải sau giờ mở cửa." | ✅ |
| TC-BR05 | Admin thêm chi nhánh hợp lệ | Tên, địa chỉ, SĐT hợp lệ | 201 Created, trả về chi nhánh mới tạo | ✅ |
| TC-BR06 | Thêm chi nhánh trùng tên | Tên chi nhánh đã có trong DB | 409 Conflict, báo "Chi nhánh đã tồn tại." | ✅ |
| TC-BR07 | Ngừng chi nhánh còn xe/nhân viên | Chi nhánh Quận 1 (có xe đang hoạt động) | 400 Bad Request, báo không thể ngừng hoạt động | ✅ |
| TC-BR08 | Ngừng chi nhánh không có xe/nhân viên | Chi nhánh test mới tạo | 200 OK, `IsActive` chuyển thành `false` | ✅ |

---

### 2.3 Quản lý Tài khoản (Users)
| ID | Chức năng | Dữ liệu vào | Kết quả mong đợi | KQ |
|---|---|---|---|---|
| TC-U01 | Xem danh sách users (Admin) | `GET /api/users` kèm Token Admin | 200 OK, trả danh sách users | ✅ |
| TC-U02 | Khách hàng xem danh sách users | Gắn Token Customer | 403 Forbidden | ✅ |
| TC-U03 | Lọc người dùng theo vai trò/chi nhánh | `?roleId=2&branchId=1` | 200 OK, chỉ trả về nhân viên thuộc chi nhánh 1 | ✅ |
| TC-U04 | Tạo tài khoản nhân viên hợp lệ | Đầy đủ thông tin, chọn chi nhánh 1 | 201 Created, Role = `Staff` | ✅ |
| TC-U05 | Tạo nhân viên trùng email | Email đã đăng ký | 409 Conflict, báo "Email này đã được sử dụng." | ✅ |
| TC-U06 | Tạo nhân viên với chi nhánh không tồn tại | `branchId = 99999` | 400 Bad Request, báo "Chi nhánh không tồn tại" | ✅ |
| TC-U07 | Admin tự khoá tài khoản chính mình | ID tài khoản Admin đang login | 400 Bad Request, báo "Không thể tự khoá tài khoản của chính mình." | ✅ |
| TC-U08 | Khoá/mở tài khoản nhân viên | ID của nhân viên | 200 OK, đảo trạng thái `IsActive` | ✅ |

---

### 2.4 Dashboard & Thống kê
| ID | Chức năng | Dữ liệu vào | Kết quả mong đợi | KQ |
|---|---|---|---|---|
| TC-D01 | Xem tổng quan KPIs (Admin) | `GET /api/dashboard/summary` | 200 OK, đủ các trường tổng số xe, đơn hoàn thành, doanh thu | ✅ |
| TC-D02 | Xem doanh thu 12 tháng theo năm | `GET /api/dashboard/monthly-revenue?year=2026` | 200 OK, mảng 12 tháng với doanh thu thuê và phạt | ✅ |
| TC-D03 | Xem top xe có doanh thu cao | `GET /api/dashboard/top-motorbikes?top=10` | 200 OK, danh sách top xe sắp xếp theo doanh thu giảm dần | ✅ |
| TC-D04 | Khách hàng xem Dashboard | Gắn Token Customer | 403 Forbidden | ✅ |
| TC-D05 | Chưa đăng nhập xem Dashboard | Không có Token | 401 Unauthorized | ✅ |

---

### 2.5 Kiểm thử giao diện WebApp (MVC)
| ID | Thao tác trên trình duyệt | Kết quả mong đợi | KQ |
|---|---|---|---|
| TC-W01 | Đăng nhập tài khoản Admin | Chuyển hướng sang trang Quản trị, navbar hiện menu Quản trị & tên User | ✅ |
| TC-W02 | Đăng xuất | Huỷ cookie phiên làm việc, chuyển hướng về Trang chủ | ✅ |
| TC-W03 | Đăng nhập với tư cách Customer rồi vào `/Admin/Dashboard` | Bị chuyển hướng sang trang `/Auth/AccessDenied` (403) | ✅ |
| TC-W04 | Chưa đăng nhập vào thẳng `/Admin/Branches` | Bị chuyển hướng sang trang `/Auth/Login?returnUrl=...` | ✅ |
| TC-W05 | Tạo chi nhánh trên WebApp | Validate form phía client + server, hiển thị thông báo thành công | ✅ |
| TC-W06 | Tạo tài khoản nhân viên trên WebApp | Dropdown chọn chi nhánh hiển thị đúng, tạo xong hiển thị trong danh sách | ✅ |
| TC-W07 | Xem trang Dashboard | Biểu đồ Chart.js vẽ mượt mà, đầy đủ các thẻ KPI số liệu thực tế | ✅ |
| TC-W08 | Cập nhật hồ sơ cá nhân và đổi mật khẩu | Lưu thành công vào DB, không mất phiên đăng nhập | ✅ |
