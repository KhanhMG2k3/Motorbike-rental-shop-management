# Hướng dẫn chạy và kiểm thử dự án MotoRent (đúng quy trình)

> Dùng khi: mới clone dự án, trước khi demo, và **mỗi lần gộp code lên `develop` / `main`**.
> Thời gian: khoảng **30–40 phút** cho một vòng đầy đủ (phần tự động chỉ ~1 phút).

## Tổng quan quy trình

```
0. Chuẩn bị  →  1. Tạo database  →  2. Cấu hình  →  3. Chạy API + WebApp  →  4. Kiểm tra nhanh
        →  5. Test tự động (Postman Dev 1, Dev 2, e2e)  →  6. Test tay theo vai trò  →  7. Kịch bản demo  →  8. Chốt
```

**Nguyên tắc:** luôn chạy lại **bước 1 (reset database)** ở đầu mỗi vòng test, vì các bộ test tự động và test tay đều làm thay đổi dữ liệu (tạo đơn, xác nhận thanh toán, thêm hãng/xe test…).

---

## 0. Chuẩn bị (làm một lần)

| Cần có | Kiểm tra | Ghi chú |
|---|---|---|
| .NET 8 SDK | `dotnet --version` (phải ≥ 8) | |
| SQL Server (Express/Developer) + SSMS | Mở SSMS thấy được server | Nhớ **tên server** (VD `.\SQLEXPRESS`, `localhost`) |
| Visual Studio 2022 (hoặc VS Code) | | |
| Node.js 18+ | `node -v` | Chỉ để chạy test tự động (Newman, e2e) |
| Postman (tuỳ chọn) | | Không có cũng được, dùng `npx newman` |

Lấy code mới nhất:
```bash
git checkout develop
git pull origin develop
```

---

## 1. Tạo / reset database

1. Mở **SSMS** → kết nối server → **New Query**.
2. Mở file `MotorbikeRentalDB.sql` → dán toàn bộ → **Execute (F5)**.
3. Cuối kết quả phải có bảng đếm đúng như sau:

| Bảng | Số dòng | | Bảng | Số dòng |
|---|---|---|---|---|
| Roles | 3 | | Rentals | 8 |
| Branches | 2 | | RentalDetails | 9 |
| Users | 8 | | Payments | 17 |
| Brands | 5 | | MaintenanceRecords | 4 |
| Categories | 4 | | Reviews | 3 |
| Motorbikes | 15 | | | |

> ⚠️ Script **xoá và tạo lại** database `MotorbikeRentalDB`. Đây là cách reset dữ liệu nhanh nhất.

---

## 2. Cấu hình kết nối (mỗi máy một lần)

Tạo file `MotoRent.API/appsettings.Local.json` (file này **không** có trên Git):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=MotorbikeRentalDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```
- Đổi `.\\SQLEXPRESS` thành tên server của máy bạn (trong JSON phải viết `\\`).
- Dùng SQL Server mặc định `localhost` thì không cần file này.
- Lỗi hay gặp: viết `\` đơn → API báo *"is an invalid escapable character"*.

---

## 3. Chạy API và WebApp

**Visual Studio:**
1. Chuột phải **Solution 'MotoRent'** → **Configure Startup Projects…** → **Multiple startup projects**.
2. `MotoRent.API` = **Start**, `MotoRent.WebApp` = **Start**, 3 project còn lại = **None**.
3. Ở ô profile cạnh nút ▶ chọn **http** cho cả hai → bấm **Ctrl+F5**.

**Hoặc terminal** (mở 2 cửa sổ):
```bash
dotnet run --project MotoRent.API --launch-profile http
dotnet run --project MotoRent.WebApp --launch-profile http
```

| Thành phần | Địa chỉ |
|---|---|
| API + Swagger | http://localhost:5001/swagger |
| Trang web | http://localhost:5002 |

---

## 4. Kiểm tra nhanh (smoke test, 2 phút)

- [ ] http://localhost:5002 mở được, bấm **Thuê xe** thấy danh sách 14 xe (xe ngừng hoạt động không hiện).
- [ ] http://localhost:5001/swagger mở được.
- [ ] Đăng nhập `admin@motorent.vn` / `123456` thành công, thấy menu **Quản trị**.

Nếu một mục lỗi → xem **mục 9 (Xử lý sự cố)** trước khi test tiếp.

**Tài khoản mẫu** (mật khẩu chung `123456`):

| Email | Vai trò | Ghi chú |
|---|---|---|
| `admin@motorent.vn` | Admin | |
| `staff1@motorent.vn` | Staff | Chi nhánh Quận 1 (mở cửa 07:00–21:00) |
| `staff2@motorent.vn` | Staff | Chi nhánh Thủ Đức (07:30–20:30) |
| `customer1@example.com` … `customer4@example.com` | Customer | Đã có CCCD + GPLX |
| `customer5@example.com` | Customer | **Chưa có** CCCD/GPLX (để test chặn giao xe) |

---

## 5. Test tự động (API đang chạy, DB vừa reset)

Chạy lần lượt tại thư mục gốc dự án:

```bash
npx newman run docs/postman/MotoRent_Dev1.postman_collection.json
npx newman run docs/postman/MotoRent_Dev2.postman_collection.json
node docs/e2e/rental-flow.e2e.js
```

| Bộ test | Nội dung | Kết quả đúng |
|---|---|---|
| Postman Dev 1 | Đăng nhập, hồ sơ, chi nhánh, tài khoản, dashboard, phân quyền | **25 request, 41 assertion, 0 failed** |
| Postman Dev 2 | Hãng, loại, xe, OData, bảo dưỡng, upload ảnh, phân quyền 401/403 | **75 request, 116 assertion, 0 failed** |
| e2e luồng thuê xe | Đặt xe → giờ mở cửa → duyệt → giao → trả → đánh giá → huỷ → thanh toán → phân quyền | **KẾT QUẢ: 73/73 đạt** |

- Bất kỳ dòng nào `0 failed` khác 0 hoặc có `❌` → **dừng lại, ghi lại tên test lỗi** rồi báo người phụ trách (Dev 1: Auth/Chi nhánh/Users/Payments · Dev 2: Xe/Bảo dưỡng · Dev 3: Đơn thuê/Đánh giá).
- Dùng Postman giao diện: **Import** file `.json` → chuột phải collection → **Run collection**.
- e2e chạy lần 2 mà không reset DB thì chỉ còn 70 kiểm tra (khoản thanh toán chờ có sẵn chỉ xử lý được một lần) — bình thường.

---

## 6. Test tay trên giao diện (theo vai trò)

> Reset database (bước 1) trước phần này nếu vừa chạy test tự động. Dùng cửa sổ ẩn danh hoặc đăng xuất giữa các vai trò.

### 6.1 Khách chưa đăng nhập (Guest)
| # | Làm | Kết quả đúng |
|---|---|---|
| 1 | Vào **Thuê xe**, lọc *Xe tay ga + Honda* | Chỉ còn Honda tay ga; có phân trang, sắp xếp |
| 2 | Mở chi tiết Honda Air Blade | Thấy giá, cọc, đánh giá; nhập ngày → **Kiểm tra lịch trống** báo còn trống |
| 3 | Vào thẳng `/Rentals/MyRentals` hoặc `/Admin/Rentals` | Bị chuyển về trang **Đăng nhập** |
| 4 | Bấm **Đăng ký**, đăng ký tài khoản mới (mật khẩu ≥ 6 ký tự) | Đăng ký được; để trống / sai định dạng thì báo lỗi dưới từng ô |

### 6.2 Customer (`customer1@example.com`)
| # | Làm | Kết quả đúng |
|---|---|---|
| 1 | Chọn xe → **Đặt xe**, đặt 2 ngày, nhận **09:00** | Đặt thành công, đơn **Chờ duyệt** |
| 2 | Đặt lại với giờ nhận **03:00** | Báo lỗi *"nằm ngoài giờ mở cửa của chi nhánh … (07:00 – 21:00)"* |
| 3 | Trang đặt xe | Có dòng *"🕒 Giờ mở cửa: 07:00 – 21:00"* |
| 4 | Vào **Đơn thuê của tôi** → mở đơn → **Huỷ đơn** (có lý do) | Đơn thành **Đã huỷ** |
| 5 | Mở đơn người khác bằng cách sửa số trong URL (`/Rentals/Details/2`) | Không xem được (chuyển về danh sách) |
| 6 | **Lịch sử thanh toán** (menu tên người dùng) | Chỉ thấy khoản của mình: tổng đã trả, đã hoàn, khoản chờ |
| 7 | Gõ `/Admin/Rentals` | Trang **Không có quyền** |
| 8 | Đơn đã **Hoàn thành** → gửi đánh giá 5 sao | Thành công; gửi lần 2 bị từ chối |

### 6.3 Staff (`staff1@motorent.vn`)
| # | Làm | Kết quả đúng |
|---|---|---|
| 1 | Menu **Quản trị** | Chỉ thấy: Đơn thuê xe, Thanh toán, Bảo dưỡng, Xe máy (không có Hãng, Loại, Chi nhánh, Tài khoản, Tổng quan) |
| 2 | **Đơn thuê xe** → đơn chờ duyệt → **Duyệt** (chọn tiền mặt) | Đơn → **Đã xác nhận**; có khoản cọc đã thanh toán |
| 3 | **Giao xe** (nhập số km, tình trạng) | Đơn → **Đang thuê**; xe → **Đang cho thuê** |
| 4 | **Nhận xe** trễ vài giờ, thêm phí hư hỏng | Đơn → **Hoàn thành**; có phí trễ; hiện hoá đơn quyết toán; xe → **Sẵn sàng** |
| 5 | Giao xe cho khách thiếu CCCD/GPLX (đặt hộ cho `customer5`) | Bị chặn, báo khách chưa có giấy tờ |
| 6 | **Thanh toán** → lọc *Chờ xác nhận* | Thấy khoản VNPay chờ → **Xác nhận** → thành **Đã thanh toán**, ghi tên người xử lý |
| 7 | **Thanh toán** → **Huỷ** một khoản chờ | Bắt buộc nhập lý do; bỏ trống bị chặn |
| 8 | **Xe máy** | Không có nút Thêm / Sửa / Ngừng; có ô đổi nhanh **Sẵn sàng ↔ Bảo dưỡng** (xe đang thuê thì không đổi được) |
| 9 | **Bảo dưỡng** → lên lịch → **Bắt đầu** → **Hoàn thành** | Xe: Sẵn sàng → Đang bảo dưỡng → Sẵn sàng |
| 10 | Gõ `/Admin/Brands` hoặc `/Admin/Dashboard` | Trang **Không có quyền** |

### 6.4 Admin (`admin@motorent.vn`)
| # | Làm | Kết quả đúng |
|---|---|---|
| 1 | **Tổng quan** | Thấy KPI và biểu đồ doanh thu |
| 2 | **Xe máy** → thêm xe mới (biển số `59A1-888.88`) | Thêm được; trùng biển số thì báo lỗi; sai định dạng báo lỗi từng ô |
| 3 | Sửa xe → **tải ảnh** (.jpg/.png ≤ 2 MB) | Ảnh hiện ra; file không phải ảnh bị từ chối |
| 4 | **Hãng xe** → ngừng *Honda* | Báo hãng còn xe đang hoạt động |
| 5 | **Chi nhánh** → đổi giờ đóng cửa nhỏ hơn giờ mở | Báo lỗi *"Giờ đóng cửa phải sau giờ mở cửa"* |
| 6 | **Tài khoản** → tạo nhân viên Staff; thử khoá chính mình | Tạo được; khoá chính mình bị chặn |
| 7 | **Đơn thuê xe** | Xem được tất cả đơn; **không có** nút Duyệt/Giao/Nhận (chỉ Staff) |
| 8 | **Thanh toán** | Thấy tổng *Đã thu*, *Đã hoàn*, *Doanh thu*; lọc theo loại / phương thức / ngày |

### 6.5 Giao diện điện thoại
Bấm **F12 → biểu tượng điện thoại** (375px), mở: danh sách xe, chi tiết xe, đặt xe, quản lý đơn.
- [ ] Không có thanh cuộn ngang toàn trang; bảng cuộn ngang trong khung; form 1 cột; nút bấm được.

---

## 7. Kịch bản demo (5–7 phút, dùng khi trình bày)

1. **Guest** vào web → lọc xe tay ga Honda → xem chi tiết → kiểm tra lịch trống.
2. **Đăng nhập `customer1`** → đặt Honda SH Mode 2 ngày (nhận 09:00) → thử nhập 03:00 để cho thấy chặn giờ → đặt giờ đúng.
3. **Đăng nhập `staff1`** → **Duyệt** đơn → **Giao xe** → **Nhận xe** trễ 3 giờ → xem **hoá đơn** (phí trễ 2 giờ × 10% giá ngày).
4. Vào **Thanh toán** → thấy cọc, tiền thuê, phạt, hoàn cọc của đơn vừa xong → xác nhận khoản VNPay đang chờ.
5. **Đăng nhập `customer1`** → **Lịch sử thanh toán** → gửi **đánh giá** 5 sao.
6. **Đăng nhập `admin`** → **Tổng quan** (doanh thu) → thêm xe + tải ảnh → khoá một tài khoản.
7. **Phân quyền:** đăng nhập `customer1` gõ `/Admin/Payments` → bị chặn; Staff vào `/Admin/Brands` → bị chặn.

---

## 8. Chốt vòng test (checklist trước khi merge)

- [ ] Build **0 lỗi, 0 cảnh báo** (`dotnet build MotoRent.sln`)
- [ ] Postman Dev 1: `25 / 41 / 0 failed`
- [ ] Postman Dev 2: `75 / 116 / 0 failed`
- [ ] e2e: `73/73 đạt` (trên DB mới)
- [ ] Test tay: đủ 4 vai trò (mục 6.1 – 6.4), không có trang lỗi 500
- [ ] Xem lại `LuongNghiepVu.md` mục 6: mọi quy tắc còn đúng với code
- [ ] Không commit file cấu hình riêng (`appsettings.Local.json`) hay ảnh upload (`wwwroot/uploads`)
- [ ] Nếu đổi database: đã sửa `MotorbikeRentalDB.sql` + báo cả nhóm chạy lại + scaffold lại Entity (lệnh trong README)

**Báo lỗi:** ghi (1) tài khoản đang đăng nhập, (2) trang / API đang thao tác, (3) bước làm, (4) kết quả mong đợi và kết quả thực tế, (5) ảnh chụp màn hình hoặc nội dung lỗi → gửi người phụ trách module.

---

## 9. Xử lý sự cố thường gặp

| Triệu chứng | Nguyên nhân | Cách xử lý |
|---|---|---|
| Web báo *"Không kết nối được tới máy chủ API"* | API chưa chạy | Kiểm tra đã chọn **Start** cả 2 project; mở http://localhost:5001/swagger |
| API lỗi kết nối SQL | Sai tên server trong `appsettings.Local.json` | Xem tên server ở hộp thoại đăng nhập SSMS; nhớ viết `\\` trong JSON |
| API báo lỗi đọc `appsettings.Local.json` | Viết `\` đơn trong JSON | Sửa thành `\\` |
| Cổng 5001/5002 đã bị dùng | Còn phiên chạy cũ | Tắt phiên cũ (Shift+F5) hoặc kết thúc `MotoRent.API.exe` / `MotoRent.WebApp.exe` trong Task Manager |
| Mọi request Postman trả 401 | Chưa đăng nhập lấy token | Chạy từ đầu collection (nhóm "Đăng nhập" tự lấy token) |
| e2e báo *"Không kết nối được API"* | API chưa chạy / khác cổng | Chạy API trước, hoặc `API_URL=http://localhost:5001 node docs/e2e/rental-flow.e2e.js` |
| e2e có test "Giao xe → Renting" lỗi về số km | DB còn dữ liệu của lần chạy trước | Reset database (bước 1) |
| Đăng nhập báo sai mật khẩu với tài khoản mẫu | DB bị sửa tay hoặc dữ liệu cũ | Reset database (bước 1) |
| Solution Explorer thiếu controller (VD không thấy Payments) | Đang mở thư mục / nhánh cũ | **Git → Pull** nhánh `develop`, đóng và mở lại solution |
| Đặt xe báo "ngoài giờ mở cửa" dù đang trong giờ | Chọn sai chi nhánh | Chi nhánh Thủ Đức mở **07:30–20:30**, Quận 1 mở **07:00–21:00** |
| `npx newman` chạy chậm lần đầu | Đang tải gói | Chờ xong, lần sau nhanh hơn |
