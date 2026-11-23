/*
 * Kiểm thử end-to-end luồng thuê xe + thanh toán + phân quyền qua API.
 *
 * Cách chạy (cần Node 18+, API đang chạy):
 *   1) Nên chạy lại MotorbikeRentalDB.sql để có dữ liệu mẫu sạch (script dùng dữ liệu mẫu có sẵn).
 *   2) node docs/e2e/rental-flow.e2e.js            (mặc định API ở http://localhost:5001)
 *      API_URL=http://localhost:5001 node docs/e2e/rental-flow.e2e.js
 *
 * Script tự tạo đơn thuê ở các ngày ngẫu nhiên trong tương lai (tránh trùng lịch giữa các lần chạy)
 * và huỷ các đơn phụ khi xong. Các khoản thanh toán Pending có sẵn (id 17) chỉ xử lý được MỘT lần:
 * lần chạy thứ hai bước "xác nhận thanh toán" sẽ báo khoản đã được xử lý -> chạy lại file SQL để reset.
 *
 * Mật khẩu tài khoản mẫu: 123456.
 */
const A = process.env.API_URL || 'http://localhost:5001';
const results = [];

const pad = (n) => String(n).padStart(2, '0');
const iso = (d) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:00`;
const OFFSET = 30 + Math.floor(Math.random() * 300);          // lệch ngày ngẫu nhiên mỗi lần chạy
const at = (day, hour, minute = 0) => { const d = new Date(); d.setDate(d.getDate() + OFFSET + day); d.setHours(hour, minute, 0, 0); return d; };
const atPast = (day, hour) => { const d = new Date(); d.setDate(d.getDate() + day); d.setHours(hour, 0, 0, 0); return d; };

async function call(method, path, token, body) {
  const res = await fetch(A + path, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: 'Bearer ' + token } : {}) },
    body: body ? JSON.stringify(body) : undefined
  });
  let json = null;
  try { json = await res.json(); } catch { /* không phải JSON */ }
  return { status: res.status, json };
}
function check(name, ok, detail = '') {
  results.push({ name, ok });
  console.log(`${ok ? '✅' : '❌'} ${name}${detail ? '  → ' + detail : ''}`);
}
const login = async (email) => (await call('POST', '/api/auth/login', null, { email, password: '123456' })).json.data.token;
const err = (r) => r.json?.errors ? Object.values(r.json.errors).flat().join(' | ') : (r.json?.message ?? '');
const created = (r) => r.status === 200 || r.status === 201;
const cancelQuietly = (id, token) => call('PUT', `/api/rentals/${id}/cancel`, token, { cancelReason: 'dọn dữ liệu e2e', refundMethod: 'Cash' });

(async () => {
  try { await fetch(A + '/api/brands'); } catch { console.error(`Không kết nối được API ở ${A}. Hãy chạy MotoRent.API trước.`); process.exit(2); }

  const admin = await login('admin@motorent.vn');
  const staff = await login('staff1@motorent.vn');     // chi nhánh 1
  const staff2 = await login('staff2@motorent.vn');    // chi nhánh 2
  const cus1 = await login('customer1@example.com');   // UserId 4
  const cus2 = await login('customer2@example.com');   // UserId 5
  const cus5 = await login('customer5@example.com');   // UserId 8, chưa có CCCD/GPLX

  // Xe mẫu: 3 = Air Blade (CN1), 4 = SH Mode (CN1), 9 = Raider (CN2), 10 = Axelo (CN2), 6 = Exciter (Maintenance)
  // Số km hiện tại của xe 3 (đọc từ API để script chạy được nhiều lần, không phụ thuộc dữ liệu mẫu ban đầu)
  const odo0 = (await call("GET", "/api/motorbikes/3")).json.data.odometer;
  const base = { branchId: 1, startDate: iso(at(0, 9)), expectedReturnDate: iso(at(2, 9)), motorbikeIds: [3], note: 'e2e' };

  console.log('\n=== A. Đặt xe');
  let r = await call('POST', '/api/rentals', cus1, base);
  check('Customer đặt xe 2 ngày → Pending', created(r) && r.json?.data?.status === 'Pending', `HTTP ${r.status} ${r.json?.data?.rentalCode ?? err(r)}`);
  const rental = r.json?.data; const rid = rental?.rentalId;
  check('Tiền thuê = giá/ngày × 2 ngày', rental && Math.abs(rental.subTotal - 360000) < 1, `subTotal=${rental?.subTotal}`);
  check('CustomerId lấy từ token (customer1 = 4)', rental?.customerId === 4, `customerId=${rental?.customerId}`);
  r = await call('POST', '/api/rentals', cus2, base);
  check('Khách khác đặt trùng xe + trùng giờ → 409', r.status === 409, `HTTP ${r.status}`);
  r = await call('POST', '/api/rentals', cus1, { ...base, startDate: iso(atPast(-3, 9)), expectedReturnDate: iso(atPast(-1, 9)) });
  check('Đặt xe ở quá khứ → 400', r.status === 400, `HTTP ${r.status}`);
  r = await call('POST', '/api/rentals', cus1, { ...base, expectedReturnDate: iso(at(-1, 9)) });
  check('Ngày trả trước ngày nhận → 400', r.status === 400, `HTTP ${r.status}`);
  r = await call('POST', '/api/rentals', cus1, { ...base, motorbikeIds: [6], startDate: iso(at(10, 9)), expectedReturnDate: iso(at(11, 9)) });
  check('Xe đang Maintenance → bị từ chối', r.status === 400 || r.status === 409, `HTTP ${r.status} ${err(r)}`);
  r = await call('POST', '/api/rentals', cus1, { ...base, motorbikeIds: [9], startDate: iso(at(12, 9)), expectedReturnDate: iso(at(13, 9)) });
  check('Xe thuộc chi nhánh khác (xe 9 ở CN2, đặt ở CN1) → 400', r.status === 400, `HTTP ${r.status} ${err(r)}`);

  console.log('\n=== B. Giờ mở cửa chi nhánh (BR12) — CN1 mở 07:00–21:00, CN2 mở 07:30–20:30');
  r = await call('POST', '/api/rentals', cus1, { ...base, motorbikeIds: [4], startDate: iso(at(20, 3)), expectedReturnDate: iso(at(21, 9)) });
  check('Nhận xe lúc 03:00 sáng → 400', r.status === 400 && /mở cửa/.test(err(r)), `HTTP ${r.status} ${err(r)}`);
  r = await call('POST', '/api/rentals', cus1, { ...base, motorbikeIds: [4], startDate: iso(at(22, 9)), expectedReturnDate: iso(at(23, 22)) });
  check('Hẹn trả lúc 22:00 (sau giờ đóng cửa) → 400', r.status === 400 && /mở cửa/.test(err(r)), `HTTP ${r.status} ${err(r)}`);
  r = await call('POST', '/api/rentals', cus1, { ...base, motorbikeIds: [4], startDate: iso(at(24, 7, 0)), expectedReturnDate: iso(at(25, 21, 0)) });
  check('Đúng giờ mở cửa 07:00 và đóng cửa 21:00 (biên) → hợp lệ', created(r), `HTTP ${r.status} ${err(r)}`);
  const ridEdge = r.json?.data?.rentalId;
  r = await call('POST', '/api/rentals', cus1, { ...base, motorbikeIds: [4], startDate: iso(at(26, 6, 59)), expectedReturnDate: iso(at(27, 9)) });
  check('06:59 (sớm 1 phút) → 400', r.status === 400, `HTTP ${r.status}`);
  r = await call('POST', '/api/rentals', cus1, { ...base, branchId: 2, motorbikeIds: [10], startDate: iso(at(28, 7, 0)), expectedReturnDate: iso(at(29, 9)) });
  check('CN2 mở 07:30: nhận lúc 07:00 → 400', r.status === 400 && /07:30/.test(err(r)), `HTTP ${r.status} ${err(r)}`);
  r = await call('POST', '/api/rentals', cus1, { ...base, branchId: 2, motorbikeIds: [10], startDate: iso(at(30, 7, 30)), expectedReturnDate: iso(at(31, 20, 30)) });
  check('CN2: 07:30 → 20:30 (biên) → hợp lệ', created(r), `HTTP ${r.status} ${err(r)}`);
  const ridEdge2 = r.json?.data?.rentalId;

  console.log('\n=== C. Staff đặt hộ khách');
  r = await call('POST', '/api/rentals', staff, { ...base, motorbikeIds: [4], startDate: iso(at(40, 9)), expectedReturnDate: iso(at(41, 9)), customerId: 5 });
  check('Staff đặt hộ customer2 (id 5) → đúng khách', created(r) && r.json?.data?.customerId === 5, `customerId=${r.json?.data?.customerId ?? err(r)}`);
  const ridHo = r.json?.data?.rentalId;
  r = await call('POST', '/api/rentals', staff, { ...base, motorbikeIds: [4], startDate: iso(at(42, 9)), expectedReturnDate: iso(at(43, 9)), customerId: 99 });
  check('Staff đặt hộ CustomerId không tồn tại → 404/400', [400, 404].includes(r.status), `HTTP ${r.status} ${err(r)}`);
  r = await call('POST', '/api/rentals', staff, { ...base, motorbikeIds: [4], startDate: iso(at(44, 9)), expectedReturnDate: iso(at(45, 9)) });
  check('Staff đặt hộ nhưng KHÔNG gửi CustomerId → 400', r.status === 400, `HTTP ${r.status} ${err(r)}`);
  r = await call('POST', '/api/rentals', staff, { ...base, motorbikeIds: [4], startDate: iso(at(46, 9)), expectedReturnDate: iso(at(47, 9)), customerId: 1 });
  check('Staff đặt hộ cho tài khoản Admin (không phải Customer) → 400', r.status === 400, `HTTP ${r.status} ${err(r)}`);
  r = await call('POST', '/api/rentals', cus1, { ...base, motorbikeIds: [4], startDate: iso(at(48, 9)), expectedReturnDate: iso(at(49, 9)), customerId: 5 });
  check('Customer cố đặt hộ người khác (gửi customerId) → 400', r.status === 400, `HTTP ${r.status} ${err(r)}`);

  console.log('\n=== D. Phân quyền đơn thuê');
  r = await call('GET', `/api/rentals/${rid}`, cus2);
  check('Customer khác xem đơn của người ta → 403', r.status === 403, `HTTP ${r.status} ${err(r)}`);
  r = await call('GET', `/api/rentals/${rid}`, cus1);
  check('Chủ đơn xem đơn của mình → 200', r.status === 200, `HTTP ${r.status}`);
  r = await call('GET', `/api/rentals/${rid}`, staff);
  check('Staff xem đơn bất kỳ → 200', r.status === 200, `HTTP ${r.status}`);
  r = await call('PUT', `/api/rentals/${rid}/confirm`, cus1, { paymentMethod: 'Cash' });
  check('Customer tự duyệt đơn → 403', r.status === 403, `HTTP ${r.status}`);
  r = await call('PUT', `/api/rentals/${rid}/confirm`, null, { paymentMethod: 'Cash' });
  check('Không token duyệt đơn → 401', r.status === 401, `HTTP ${r.status}`);
  r = await call('GET', '/api/rentals/my', cus1);
  check('"Đơn của tôi" có đơn vừa đặt', r.status === 200 && r.json?.data?.some(x => x.rentalId === rid), `${r.json?.data?.length} đơn`);
  r = await call('GET', '/api/rentals/my', staff);
  check('Staff gọi /my → 403', r.status === 403, `HTTP ${r.status}`);

  console.log('\n=== E. Duyệt → Giao → Trả');
  const pick = { items: [{ motorbikeId: 3, pickupOdometer: odo0, pickupCondition: 'Tốt' }] };
  r = await call('PUT', `/api/rentals/${rid}/pickup`, staff, pick);
  check('Giao xe khi đơn còn Pending → 400', r.status === 400, `HTTP ${r.status} ${err(r)}`);
  r = await call('PUT', `/api/rentals/${rid}/confirm`, staff, { paymentMethod: 'Cash' });
  check('Staff duyệt đơn → Confirmed', r.status === 200 && r.json?.data?.status === 'Confirmed', `HTTP ${r.status} ${err(r)}`);
  r = await call('PUT', `/api/rentals/${rid}/pickup`, staff, pick);
  check('Giao xe → Renting', r.status === 200 && r.json?.data?.status === 'Renting', `HTTP ${r.status} ${err(r)}`);
  let bike = (await call('GET', '/api/motorbikes/3')).json?.data;
  check('Xe 3 chuyển sang Rented', bike?.status === 'Rented', `status=${bike?.status}`);
  r = await call('PUT', `/api/rentals/${rid}/cancel`, cus1, { cancelReason: 'đổi ý', refundMethod: 'Cash' });
  check('Huỷ đơn đang Renting → 400', r.status === 400, `HTTP ${r.status}`);
  const lateReturn = new Date(at(2, 9).getTime() + 3 * 3600000);   // trễ 3 giờ
  const ret = (km) => ({ actualReturnDate: iso(lateReturn), damageFee: 100000, discountAmount: 0, paymentMethod: 'Cash', items: [{ motorbikeId: 3, returnOdometer: km, returnCondition: 'Trầy nhẹ', isDamaged: false }] });
  r = await call('PUT', `/api/rentals/${rid}/return`, staff, ret(odo0 - 50));
  check('Trả xe với số km nhỏ hơn lúc giao → 400', r.status === 400, `HTTP ${r.status} ${err(r)}`);
  r = await call('PUT', `/api/rentals/${rid}/return`, staff, ret(odo0 + 300));
  const done = r.json?.data;
  check('Trả xe trễ 3 giờ → Completed', r.status === 200 && done?.status === 'Completed', `HTTP ${r.status} ${err(r)}`);
  check('Tổng = tiền thuê − giảm giá + phí trễ + hư hỏng', done && Math.abs(done.totalAmount - (done.subTotal - done.discountAmount + done.lateFee + done.damageFee)) < 1, `${done?.totalAmount}`);
  check('Phí trễ = 2 giờ × 10% × 180.000 = 36.000đ', done?.lateFee === 36000, `lateFee=${done?.lateFee}`);
  bike = (await call('GET', '/api/motorbikes/3')).json?.data;
  check('Xe 3 về Available, số km cập nhật (+300 km)', bike?.status === 'Available' && bike?.odometer === odo0 + 300, `status=${bike?.status} odometer=${bike?.odometer}`);

  console.log('\n=== F. Đánh giá + phân quyền đánh giá');
  r = await call('POST', '/api/reviews', null, { rentalId: rid, motorbikeId: 3, rating: 5, comment: 'ẩn danh' });
  check('Không đăng nhập gửi đánh giá → 401', r.status === 401, `HTTP ${r.status}`);
  r = await call('GET', `/api/reviews/rental/${rid}`, null);
  check('Không đăng nhập xem đánh giá của đơn → 401', r.status === 401, `HTTP ${r.status}`);
  r = await call('GET', `/api/reviews/check-eligibility?rentalId=${rid}&motorbikeId=3`, null);
  check('Không đăng nhập check-eligibility → 401', r.status === 401, `HTTP ${r.status}`);
  r = await call('POST', '/api/reviews', staff, { rentalId: rid, motorbikeId: 3, rating: 5 });
  check('Staff gửi đánh giá → 403 (chỉ Customer)', r.status === 403, `HTTP ${r.status}`);
  r = await call('GET', `/api/reviews/check-eligibility?rentalId=${rid}&motorbikeId=3`, cus1);
  check('Chủ đơn Completed được phép đánh giá', r.json?.data?.canReview === true, `HTTP ${r.status}`);
  r = await call('POST', '/api/reviews', cus2, { rentalId: rid, motorbikeId: 3, rating: 5, comment: 'người lạ' });
  check('Khách khác đánh giá đơn người ta → 403', r.status === 403, `HTTP ${r.status} ${err(r)}`);
  r = await call('GET', `/api/reviews/rental/${rid}`, cus2);
  check('Khách khác xem đánh giá của đơn người ta → 403', r.status === 403, `HTTP ${r.status}`);
  r = await call('POST', '/api/reviews', cus1, { rentalId: rid, motorbikeId: 3, rating: 6 });
  check('Rating 6 → 400', r.status === 400, `HTTP ${r.status}`);
  r = await call('POST', '/api/reviews', cus1, { rentalId: rid, motorbikeId: 3, rating: 5, comment: 'Xe tốt (e2e)' });
  check('Đánh giá 5 sao → thành công', created(r), `HTTP ${r.status} ${err(r)}`);
  r = await call('POST', '/api/reviews', cus1, { rentalId: rid, motorbikeId: 3, rating: 4 });
  check('Đánh giá lần 2 cùng xe/đơn → 409', r.status === 409, `HTTP ${r.status}`);
  r = await call('POST', '/api/reviews', cus2, { rentalId: ridHo, motorbikeId: 4, rating: 5 });
  check('Đánh giá đơn chưa hoàn thành → 400', r.status === 400, `HTTP ${r.status} ${err(r)}`);
  r = await call('GET', '/api/motorbikes/3/reviews', null);
  check('Xem đánh giá của xe không cần đăng nhập → 200', r.status === 200, `${r.json?.data?.length} đánh giá`);

  console.log('\n=== G. Huỷ đơn');
  r = await call('PUT', `/api/rentals/${ridHo}/cancel`, cus1, { cancelReason: 'không phải đơn của tôi', refundMethod: 'Cash' });
  check('Customer huỷ đơn của người khác → 403', r.status === 403, `HTTP ${r.status}`);
  r = await call('PUT', `/api/rentals/${ridHo}/cancel`, staff, { cancelReason: 'khách đổi lịch', refundMethod: 'Cash' });
  check('Staff huỷ đơn Pending → Cancelled', r.status === 200 && r.json?.data?.status === 'Cancelled', `HTTP ${r.status} ${err(r)}`);
  r = await call('PUT', `/api/rentals/${ridHo}/cancel`, staff, { cancelReason: 'huỷ lần 2', refundMethod: 'Cash' });
  check('Huỷ lại đơn đã huỷ → 400', r.status === 400, `HTTP ${r.status}`);
  r = await call('PUT', `/api/rentals/${ridHo}/cancel`, staff, { cancelReason: '', refundMethod: 'Cash' });
  check('Huỷ không có lý do → 400', r.status === 400, `HTTP ${r.status}`);
  for (const x of [ridEdge]) if (x) await cancelQuietly(x, cus1);
  if (ridEdge2) await cancelQuietly(ridEdge2, cus1);

  console.log('\n=== H. Giao xe thiếu CCCD/GPLX (BR4)');
  r = await call('POST', '/api/rentals', cus5, { ...base, branchId: 2, motorbikeIds: [10], startDate: iso(at(60, 9)), expectedReturnDate: iso(at(61, 9)) });
  const ridNo = r.json?.data?.rentalId;
  check('Khách chưa có CCCD/GPLX vẫn đặt giữ xe được', created(r), `HTTP ${r.status} ${err(r)}`);
  if (ridNo) {
    await call('PUT', `/api/rentals/${ridNo}/confirm`, staff2, { paymentMethod: 'Cash' });
    r = await call('PUT', `/api/rentals/${ridNo}/pickup`, staff2, { items: [{ motorbikeId: 10, pickupOdometer: 25000, pickupCondition: 'Tốt' }] });
    check('Giao xe cho khách thiếu CCCD/GPLX → 400', r.status === 400, `HTTP ${r.status} ${err(r)}`);
    await cancelQuietly(ridNo, staff2);
  }

  console.log('\n=== I. OData tra cứu đơn');
  r = await call('GET', "/odata/Rentals?$filter=Status eq 'Completed'&$count=true", staff);
  check('Staff tra cứu OData đơn Completed → 200', r.status === 200, `count=${r.json?.['@odata.count']}`);
  r = await call('GET', '/odata/Rentals', cus1);
  check('Customer tra cứu OData → 403', r.status === 403, `HTTP ${r.status}`);

  console.log('\n=== J. Thanh toán (Payments)');
  r = await call('GET', '/api/payments', null);
  check('Không token xem thanh toán → 401', r.status === 401, `HTTP ${r.status}`);
  r = await call('GET', '/api/payments', cus1);
  check('Customer xem toàn bộ thanh toán → 403', r.status === 403, `HTTP ${r.status}`);
  r = await call('GET', '/api/payments?pageSize=5', staff);
  const pl = r.json?.data;
  check('Staff xem danh sách thanh toán → 200, có tổng hợp tiền', r.status === 200 && pl?.totalCount > 0 && pl?.items?.length <= 5 && pl?.totalCollected > 0, `tổng ${pl?.totalCount} khoản, đã thu ${pl?.totalCollected}, hoàn ${pl?.totalRefunded}, doanh thu ${pl?.revenue}`);
  check('Doanh thu = tiền thuê + phạt (không tính cọc)', pl && pl.revenue <= pl.totalCollected && pl.revenue >= 0, `revenue=${pl?.revenue}`);
  r = await call('GET', '/api/payments?type=Penalty&status=Paid', staff);
  check('Lọc loại Penalty + Paid → toàn khoản phạt đã trả', r.status === 200 && r.json.data.items.every(p => p.paymentType === 'Penalty' && p.status === 'Paid'), `${r.json?.data?.totalCount} khoản`);
  r = await call('GET', `/api/payments?rentalId=${rid}`, staff);
  const types = (r.json?.data?.items ?? []).map(p => p.paymentType).sort().join(',');
  check('Đơn vừa hoàn thành có đủ Deposit, Penalty, Refund, RentalFee', types === 'Deposit,Penalty,Refund,RentalFee', types);
  r = await call('GET', '/api/payments?type=Abc', staff);
  check('Loại thanh toán không hợp lệ → 400', r.status === 400, `HTTP ${r.status}`);
  r = await call('GET', '/api/payments?from=2026-10-10&to=2026-10-01', staff);
  check('Ngày kết thúc trước ngày bắt đầu → 400', r.status === 400, `HTTP ${r.status}`);
  r = await call('GET', '/api/payments?pageSize=500', staff);
  check('pageSize vượt giới hạn 50 → 400', r.status === 400, `HTTP ${r.status}`);
  r = await call('GET', '/api/payments/my', cus1);
  check('Customer xem lịch sử thanh toán của mình → chỉ khoản của mình', r.status === 200 && r.json.data.length > 0 && r.json.data.every(p => p.customerName === 'Phạm Minh Tuấn'), `${r.json?.data?.length} khoản`);
  r = await call('GET', '/api/payments/my', staff);
  check('Staff gọi /payments/my → 403', r.status === 403, `HTTP ${r.status}`);

  // Khoản đang chờ có sẵn trong dữ liệu mẫu: id 17 (VNPay, Pending, đơn RNT-20261008-0007)
  r = await call('PUT', '/api/payments/17/cancel', staff, { reason: '' });
  check('Huỷ khoản thanh toán không có lý do → 400', r.status === 400, `HTTP ${r.status}`);
  r = await call('PUT', '/api/payments/17/confirm', cus1, {});
  check('Customer xác nhận thanh toán → 403', r.status === 403, `HTTP ${r.status}`);
  r = await call('PUT', '/api/payments/99999/confirm', staff, {});
  check('Xác nhận khoản không tồn tại → 404', r.status === 404, `HTTP ${r.status}`);
  r = await call('PUT', '/api/payments/17/confirm', staff, { transactionRef: 'VNP-E2E-001', note: 'đã nhận tiền (e2e)' });
  if (r.status === 400 && /Paid|Cancelled/.test(err(r))) {
    console.log('ℹ️  Khoản 17 đã được xử lý ở lần chạy trước — chạy lại MotorbikeRentalDB.sql để kiểm thử bước này.');
  } else {
    check('Staff xác nhận khoản Pending → Paid, ghi người xử lý + mã giao dịch', r.status === 200 && r.json?.data?.status === 'Paid' && r.json.data.paidAt && r.json.data.processedBy === 2 && r.json.data.transactionRef === 'VNP-E2E-001', `status=${r.json?.data?.status} processedBy=${r.json?.data?.processedBy} ${err(r)}`);
    r = await call('PUT', '/api/payments/17/confirm', staff, {});
    check('Xác nhận lại khoản đã Paid → 400', r.status === 400, `HTTP ${r.status}`);
    r = await call('PUT', '/api/payments/17/cancel', staff, { reason: 'thử huỷ khoản đã trả' });
    check('Huỷ khoản đã Paid → 400', r.status === 400, `HTTP ${r.status}`);
  }

  const fail = results.filter(x => !x.ok);
  console.log(`\n========== KẾT QUẢ: ${results.length - fail.length}/${results.length} đạt ==========`);
  fail.forEach(f => console.log('  ❌ ' + f.name));
  process.exit(fail.length ? 1 : 0);
})().catch(e => { console.error('LỖI KỊCH BẢN:', e); process.exit(2); });
