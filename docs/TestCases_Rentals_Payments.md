# Test case – Thuê xe, Giờ mở cửa, Đánh giá, Thanh toán, Phân quyền

> Kịch bản tự động: [`docs/e2e/rental-flow.e2e.js`](e2e/rental-flow.e2e.js) · **73 kiểm tra, 0 lỗi** trên database mới (70 nếu chạy lại mà không reset DB, vì khoản thanh toán Pending có sẵn chỉ xử lý được một lần) — chạy ngày 29/10/2026 trên bản tích hợp Dev 1 + Dev 2 + Dev 3.
> Cách chạy: chạy API, rồi `node docs/e2e/rental-flow.e2e.js` (nên chạy lại `MotorbikeRentalDB.sql` trước để có dữ liệu sạch).

## A. Đặt xe
| ID | Tình huống | Kết quả mong đợi |
|---|---|---|
| TC-R01 | Customer đặt xe 2 ngày | 201, trạng thái Pending, tiền thuê = giá/ngày × 2 |
| TC-R02 | Customer tự đặt | `CustomerId` lấy từ token |
| TC-R03 | Khách khác đặt trùng xe + trùng giờ | 409 |
| TC-R04 | Đặt xe ở quá khứ / ngày trả trước ngày nhận | 400 |
| TC-R05 | Xe đang Maintenance | 400 |
| TC-R06 | Xe thuộc chi nhánh khác chi nhánh của đơn | 400 |

## B. Giờ mở cửa chi nhánh (BR12) — CN1 mở 07:00–21:00, CN2 mở 07:30–20:30
| ID | Tình huống | Kết quả mong đợi |
|---|---|---|
| TC-H01 | Nhận xe 03:00 sáng | 400, thông báo nêu rõ khung giờ của chi nhánh |
| TC-H02 | Hẹn trả 22:00 (sau giờ đóng cửa) | 400 |
| TC-H03 | Nhận 07:00 và trả 21:00 (đúng mốc biên CN1) | Hợp lệ |
| TC-H04 | Nhận 06:59 (sớm 1 phút) | 400 |
| TC-H05 | CN2: nhận 07:00 (trước giờ mở 07:30) | 400, thông báo ghi 07:30 |
| TC-H06 | CN2: nhận 07:30 và trả 20:30 (biên) | Hợp lệ |
| TC-H07 | Trang đặt xe của khách | Hiện dòng "Giờ mở cửa: 07:00 – 21:00"; đặt lúc 03:00 hiện lỗi trên form |

## C. Staff đặt hộ khách
| ID | Tình huống | Kết quả mong đợi |
|---|---|---|
| TC-S01 | Staff đặt hộ customer2 (id 5) | Đơn thuộc đúng khách |
| TC-S02 | `customerId` không tồn tại | 404 |
| TC-S03 | Staff đặt hộ nhưng không gửi `customerId` | 400 (không tự gán cho chính Staff) |
| TC-S04 | Staff đặt hộ cho tài khoản Admin | 400 (không phải Customer) |
| TC-S05 | Customer gửi `customerId` của người khác | 400 |

## D. Phân quyền đơn thuê
| ID | Tình huống | Kết quả mong đợi |
|---|---|---|
| TC-P01 | Customer xem / huỷ đơn của người khác | **403** |
| TC-P02 | Chủ đơn xem đơn của mình; Staff xem đơn bất kỳ | 200 |
| TC-P03 | Customer tự duyệt đơn | 403 |
| TC-P04 | Không token duyệt đơn | 401 |
| TC-P05 | Staff gọi `/api/rentals/my` | 403 |

## E. Duyệt → Giao → Trả
| ID | Tình huống | Kết quả mong đợi |
|---|---|---|
| TC-F01 | Giao xe khi đơn còn Pending | 400 |
| TC-F02 | Staff duyệt → giao xe | Confirmed → Renting; xe chuyển **Rented** |
| TC-F03 | Huỷ đơn đang Renting | 400 |
| TC-F04 | Trả xe với số km nhỏ hơn lúc giao | 400 |
| TC-F05 | Trả xe trễ 3 giờ | Completed; phí trễ = 2 giờ × 10% × giá ngày; tổng = thuê − giảm giá + trễ + hư hỏng; xe về **Available**, số km cập nhật |
| TC-F06 | Giao xe cho khách thiếu CCCD/GPLX | 400 |

## F. Đánh giá
| ID | Tình huống | Kết quả mong đợi |
|---|---|---|
| TC-V01 | Không đăng nhập gửi đánh giá / xem đánh giá của đơn / check-eligibility | **401** |
| TC-V02 | Staff gửi đánh giá | 403 (chỉ Customer) |
| TC-V03 | Khách khác đánh giá hoặc xem đánh giá của đơn người ta | 403 |
| TC-V04 | Rating 6 | 400 |
| TC-V05 | Đánh giá 5 sao đơn Completed của mình | 201 |
| TC-V06 | Đánh giá lần 2 cùng xe / đơn | 409 |
| TC-V07 | Đánh giá đơn chưa hoàn thành | 400 |
| TC-V08 | Xem đánh giá của xe không cần đăng nhập | 200 |

## G. Huỷ đơn
| ID | Tình huống | Kết quả mong đợi |
|---|---|---|
| TC-C01 | Staff huỷ đơn Pending | Cancelled |
| TC-C02 | Huỷ lại đơn đã huỷ / huỷ không có lý do | 400 |

## H. Thanh toán (Payments)
| ID | Tình huống | Kết quả mong đợi |
|---|---|---|
| TC-M01 | Không token / Customer xem toàn bộ thanh toán | 401 / 403 |
| TC-M02 | Staff xem danh sách | 200, có `totalCollected`, `totalRefunded`, `revenue`; doanh thu ≤ tổng đã thu |
| TC-M03 | Lọc loại Penalty + trạng thái Paid | Toàn khoản phạt đã trả |
| TC-M04 | Thanh toán của đơn vừa hoàn thành | Đủ Deposit, RentalFee, Penalty, Refund |
| TC-M05 | Loại không hợp lệ / ngày kết thúc trước ngày bắt đầu / `pageSize` > 50 | 400 |
| TC-M06 | Customer xem `/payments/my` | Chỉ khoản của mình; Staff gọi → 403 |
| TC-M07 | Staff xác nhận khoản Pending | Paid, có `paidAt`, `processedBy`, mã giao dịch |
| TC-M08 | Xác nhận lại khoản đã Paid / huỷ khoản đã Paid | 400 |
| TC-M09 | Huỷ khoản không có lý do | 400 |
| TC-M10 | Huỷ khoản Pending có lý do | Cancelled, ghi lý do và người xử lý |
| TC-M11 | Xác nhận khoản thuộc đơn đã huỷ | 400 |
| TC-M12 | Customer xác nhận thanh toán; xác nhận khoản không tồn tại | 403; 404 |

> TC-M10 và TC-M11 được kiểm thử thủ công (thêm khoản Pending vào database tạm), vì dữ liệu mẫu chỉ có 1 khoản Pending.

## I. Giao diện WebApp (kiểm thử bằng gửi form như trình duyệt)
| ID | Tình huống | Kết quả mong đợi |
|---|---|---|
| TC-U01 | Chưa đăng nhập vào `/Payments/My`, `/Admin/Payments` | Chuyển sang trang Đăng nhập |
| TC-U02 | Customer vào `/Admin/Payments`; Staff vào `/Payments/My` | Trang "Không có quyền" |
| TC-U03 | Staff lọc sai (ngày đảo ngược, loại không hợp lệ) | Hiện thông báo lỗi trên trang, không gọi API |
| TC-U04 | Staff bấm Xác nhận / Huỷ khoản | Thông báo kết quả; Huỷ không nhập lý do bị báo lỗi |
| TC-U05 | Gửi form không có token chống CSRF; `returnUrl` trỏ ra trang ngoài | 400; không chuyển hướng ra ngoài |
| TC-U06 | Menu của Staff | Có "Thanh toán", "Đơn thuê xe", "Bảo dưỡng", "Xe máy"; không có Hãng, Loại, Chi nhánh |
