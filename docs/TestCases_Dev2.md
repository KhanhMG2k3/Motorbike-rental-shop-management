# Test case – Dev 2 (Catalog, Bảo dưỡng, OData)

> Người thực hiện: **Đạt (Dev 2)** · Ngày test: 17/10/2026
> Kết quả Postman/Newman: **57 request · 92 assertion · 0 lỗi** (chạy 2 lần liên tiếp đều pass)

## 1. Cách chạy test tự động

**Postman**
1. Chạy `MotoRent.API` (http://localhost:5001).
2. Postman → **Import** → chọn `docs/postman/MotoRent_Dev2.postman_collection.json`.
3. Chuột phải collection → **Run collection** → **Run**. Các request chạy theo thứ tự: tạo dữ liệu test → kiểm tra → cuối cùng chuyển dữ liệu test sang ngừng hoạt động.

**Newman (dòng lệnh, không cần mở Postman)**
```bash
npx newman run docs/postman/MotoRent_Dev2.postman_collection.json
```

> Collection tạo hãng / loại / xe có tên `Test ... <số ngẫu nhiên>` và để lại ở trạng thái ngừng hoạt động. Muốn sạch hẳn thì chạy lại `MotorbikeRentalDB.sql`.

## 2. Bảng test case

### 2.1 Hãng xe / Loại xe
| ID | Chức năng | Dữ liệu vào | Kết quả mong đợi | KQ |
|---|---|---|---|---|
| TC-B01 | Xem danh sách hãng | `GET /api/brands` | 200, danh sách hãng đang hoạt động | ✅ |
| TC-B02 | Xem hãng không tồn tại | Id = 99999 | 404 "Không tìm thấy hãng xe" | ✅ |
| TC-B03 | Thêm hãng hợp lệ | Tên mới | 201, trả về hãng vừa tạo | ✅ |
| TC-B04 | Thêm hãng trùng tên | "Honda" | 409 "Hãng xe 'Honda' đã tồn tại" | ✅ |
| TC-B05 | Thêm hãng tên rỗng | "" | 400, lỗi ở `BrandName` | ✅ |
| TC-B06 | Sửa hãng | Tên mới | 200, tên đã đổi | ✅ |
| TC-B07 | Ngừng hãng còn xe hoạt động | Honda | 400 "Hãng xe vẫn còn xe đang hoạt động" | ✅ |
| TC-C01 | Thêm loại xe trùng tên | "Xe số" | 409 | ✅ |
| TC-C02 | Thêm loại xe tên quá ngắn | "A" | 400, lỗi ở `CategoryName` | ✅ |
| TC-C03 | Sửa loại không tồn tại | Id = 99999 | 404 | ✅ |

### 2.2 Xe máy
| ID | Chức năng | Dữ liệu vào | Kết quả mong đợi | KQ |
|---|---|---|---|---|
| TC-M01 | Xem chi tiết xe | Id = 3 | 200, có `brandName`, `categoryName` (DTO làm phẳng) | ✅ |
| TC-M02 | Xem xe không tồn tại | Id = 99999 | 404 | ✅ |
| TC-M03 | Thêm xe hợp lệ | Biển số mới | 201, trạng thái mặc định `Available`, biển số tự viết hoa | ✅ |
| TC-M04 | Thêm xe trùng biển số | 59A1-123.45 | 409 "Biển số đã tồn tại" | ✅ |
| TC-M05 | Thêm xe sai nhiều trường | Biển số "abc", năm 1990, giá 5đ… | 400, báo lỗi đủ 6 trường bằng tiếng Việt | ✅ |
| TC-M06 | Thêm xe với hãng không tồn tại | BrandId = 99999 | 400 "Hãng xe không tồn tại" | ✅ |
| TC-M07 | Sửa xe | Đổi giá, tên | 200 | ✅ |
| TC-M08 | Sửa giảm số km | Odometer nhỏ hơn hiện tại | 400 "Số km không được nhỏ hơn…" | ✅ |
| TC-M09 | Đổi trạng thái hợp lệ | Available ↔ Maintenance | 200 | ✅ |
| TC-M10 | Đặt tay trạng thái "Rented" | Status = Rented | 400 (chỉ luồng thuê xe được đặt) | ✅ |
| TC-M11 | Đổi trạng thái xe đang được thuê | Xe 2 → Inactive | 400 "Xe đang được thuê" | ✅ |
| TC-M12 | Ngừng cho thuê (xoá mềm) | DELETE | 200, xe → `Inactive` | ✅ |
| TC-M13 | Tìm xe trống theo ngày | 10 → 12 ngày tới, loại tay ga | 200, chỉ xe tay ga, không có xe Maintenance/Inactive | ✅ |
| TC-M14 | Tìm xe trống: ngày trả < ngày nhận | | 400, lỗi ở `EndDate` | ✅ |
| TC-M15 | Tìm xe trống: thiếu ngày | | 400, lỗi ở `StartDate` | ✅ |
| TC-M16 | Upload ảnh hợp lệ | PNG 1 KB | 200, `imageUrl` = `/uploads/motorbikes/...`, ảnh cũ bị xoá | ✅ |
| TC-M17 | Upload file giả đuôi .jpg | File text đổi tên .jpg | 400 "Nội dung file không phải ảnh hợp lệ" | ✅ |
| TC-M18 | Upload ảnh > 2 MB | 3 MB | 400 "Ảnh tối đa 2 MB" | ✅ |
| TC-M19 | Upload không chọn file | | 400, lỗi ở `File` | ✅ |

### 2.3 Bảo dưỡng
| ID | Chức năng | Dữ liệu vào | Kết quả mong đợi | KQ |
|---|---|---|---|---|
| TC-MT01 | Lên lịch bảo dưỡng | Xe Available, ngày mai | 201, trạng thái `Scheduled` | ✅ |
| TC-MT02 | Lên lịch khi xe đã có phiếu mở | Cùng xe | 409 | ✅ |
| TC-MT03 | Lên lịch cho xe đã ngừng | Xe Inactive | 400 | ✅ |
| TC-MT04 | Dữ liệu sai | Xe = 0, nội dung "ab", chi phí âm | 400, báo lỗi 3 trường | ✅ |
| TC-MT05 | Ngày xong < ngày bắt đầu | | 400, lỗi ở `EndDate` | ✅ |
| TC-MT06 | Bắt đầu bảo dưỡng | Phiếu Scheduled | 200, phiếu `InProgress`, xe → `Maintenance` | ✅ |
| TC-MT07 | Đổi tay xe về Available khi đang bảo dưỡng | | 400 "hãy hoàn thành hoặc huỷ phiếu" | ✅ |
| TC-MT08 | Xe đang bảo dưỡng không hiện trong "xe trống" | | Không có trong danh sách | ✅ |
| TC-MT09 | Hoàn thành với ngày tương lai | | 400 | ✅ |
| TC-MT10 | Hoàn thành | Chi phí thực tế | 200, phiếu `Completed`, xe → `Available` | ✅ |
| TC-MT11 | Huỷ phiếu đã hoàn thành | | 400 | ✅ |
| TC-MT12 | Phiếu không tồn tại | Id = 99999 | 404 | ✅ |
| TC-MT13 | Lọc trạng thái không hợp lệ | status=Abc | 400 | ✅ |

### 2.4 OData
| ID | Truy vấn | Kết quả mong đợi | KQ |
|---|---|---|---|
| TC-O01 | `$filter=Status eq 'Available' and PricePerDay le 200000 and CategoryName eq 'Xe tay ga'&$orderby=PricePerDay&$count=true` | 200, đúng điều kiện, sắp tăng dần, có `@odata.count` | ✅ |
| TC-O02 | `$filter=contains(tolower(ModelName),'honda')&$select=MotorbikeId,ModelName,PricePerDay` | Chỉ trả 3 cột | ✅ |
| TC-O03 | `$orderby=MotorbikeId&$top=5&$skip=5` | Tối đa 5 xe | ✅ |
| TC-O04 | `$top=500` (vượt MaxTop 50) | 400 | ✅ |
| TC-O05 | `$filter=Abc eq 1` (cột không tồn tại) | 400 | ✅ |
| TC-O06 | `/odata/Rentals?$filter=Status eq 'Renting'` | Toàn đơn Renting | ✅ |
| TC-O07 | `/odata/Rentals?$expand=Customer` | 400 – DTO không có quan hệ, không thể lộ `PasswordHash` | ✅ |

### 2.5 WebApp – Trang quản trị (test tay trên trình duyệt)
| ID | Thao tác | Kết quả mong đợi | KQ |
|---|---|---|---|
| TC-W01 | Thêm xe bỏ trống / nhập sai | Báo lỗi ngay dưới ô nhập (client + server) | ✅ |
| TC-W02 | Thêm xe trùng biển số | Thông báo lỗi từ API hiện trên form | ✅ |
| TC-W03 | Thêm xe hợp lệ | Chuyển sang trang Sửa, thông báo thành công | ✅ |
| TC-W04 | Sửa xe | Không mất ảnh đã upload | ✅ |
| TC-W05 | Upload ảnh / upload file giả | Thành công / báo lỗi | ✅ |
| TC-W06 | Đổi trạng thái xe đang thuê | Báo lỗi | ✅ |
| TC-W07 | Lên lịch → Bắt đầu → Hoàn thành bảo dưỡng | Trạng thái xe đổi theo từng bước | ✅ |
| TC-W08 | Thêm / sửa / ngừng hãng, ngừng hãng còn xe | Đúng thông báo | ✅ |
| TC-W09 | Gửi form không có antiforgery token | 400 (chống CSRF) | ✅ |
| TC-W10 | `returnUrl` trỏ ra trang ngoài | Không chuyển hướng ra ngoài (chống open redirect) | ✅ |
| TC-W11 | Giao diện trên điện thoại (375px) | Bảng cuộn ngang, form 1 cột | ✅ |
