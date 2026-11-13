# 02. Nghiệp Vụ Quản Lý Tour, Giá, Lịch Khởi Hành & Nhà Cung Cấp

## 1. Quản lý Danh mục Tour (Tour Master Data)

Một Tour trong hệ thống đại diện cho sản phẩm du lịch chuẩn hóa, gồm các thông tin bắt buộc:

- **Thông tin định danh:** Mã Tour (duy nhất, VD: `DL-PQ-3N2D`), Tên Tour, Slug URL, Loại hình tour:
  - `GroupTour` (Tour ghép đoàn theo lịch cố định).
  - `PrivateTour` (Tour riêng theo nhóm/gia đình).
  - `CustomTour` (Tour thiết kế theo yêu cầu doanh nghiệp/đoàn thể).
- **Thông tin địa lý & thời lượng:** Điểm khởi hành (VD: TP.HCM, Hà Nội), Điểm đến (có thể nhiều điểm), Số ngày / Số đêm (VD: 3 ngày 2 đêm), Phương tiện chính (Máy bay, Xe giường nằm, Tàu hỏa).
- **Lịch trình chi tiết theo ngày (Itinerary):**
  - Ngày thứ $N$: Tiêu đề chặng, Mô tả hoạt động (Sáng / Trưa / Chiều / Tối), Bữa ăn bao gồm (Sáng/Trưa/Tối), Khách sạn dự kiến, Điểm tham quan.
- **Dịch vụ bao gồm & Không bao gồm:** Danh sách rõ ràng (VD: Đã gồm vé tham quan, bảo hiểm du lịch, nước suối; Chưa gồm chi phí cá nhân, phụ thu phòng đơn, VAT nếu tách riêng).
- **Trạng thái vòng đời Tour:**
  - `Draft` (Nháp — đang soạn thảo, chưa hiển thị bán).
  - `Published` (Đang mở bán).
  - `Suspended` (Tạm ngưng bán — giữ dữ liệu lịch sử).
  - `Archived` (Lưu trữ).

---

## 2. Cấu trúc Bảng giá đa tầng (Pricing Engine)

Giá tour thực tế không chỉ có một con số cố định mà phụ thuộc nhiều yếu tố. Hệ thống cần hỗ trợ:

1. **Giá theo độ tuổi / đối tượng:**
   - **Người lớn (`Adult`):** Từ 12 tuổi trở lên (100% giá cơ bản).
   - **Trẻ em (`Child`):** Từ 2–11 tuổi (thường 50%–75% giá người lớn, có cấu hình ngủ chung/riêng).
   - **Em bé (`Infant`):** Dưới 2 tuổi (thường miễn phí hoặc thu phí bảo hiểm/vé máy bay tượng trưng).
2. **Giá theo mùa / đợt khởi hành (Seasonal / Departure Override):**
   - Mỗi lịch khởi hành có thể kế thừa **Giá mặc định của Tour** hoặc **Ghi đè giá riêng** (dịp Lễ, Tết, Cuối tuần, Mùa cao điểm).
3. **Phụ thu (Surcharges):**
   - Phụ thu phòng đơn (`SingleRoomSupplement`) nếu khách đi 1 mình yêu cầu ở riêng.
   - Phụ thu khách nước ngoài / Visa (nếu có).
4. **Quy tắc bất biến khi đặt (Price Snapshot):**
   - Khi khách tạo Booking, hệ thống **bắt buộc lưu ảnh chụp đơn giá tại thời điểm đặt (`UnitPriceSnapshot`)** vào chi tiết booking. Nếu sau đó Điều hành tăng/giảm giá tour, các booking đã tạo không bị thay đổi giá.

---

## 3. Quản lý Lịch khởi hành & Tồn kho chỗ (Departure & Quota Management)

Mỗi Tour `Published` có nhiều **Lịch khởi hành (`TourDeparture`)** cụ thể theo ngày.

### 3.1. Công thức kiểm soát chỗ (Chống Overbooking)
Mỗi `TourDeparture` quản lý 4 chỉ số nguyên không âm:
- `TotalCapacity`: Tổng số chỗ mở bán của đoàn (VD: 30 chỗ).
- `HeldSlots`: Số chỗ đang được giữ tạm bởi các booking đang trong thời gian chờ thanh toán (TTL).
- `BookedSlots`: Số chỗ đã xác nhận (đã đặt cọc hoặc thanh toán đủ).
- `AvailableSlots = TotalCapacity - HeldSlots - BookedSlots` (Phải luôn $\ge 0$).

### 3.2. Các thông số vận hành của Lịch khởi hành
- `DepartureDate` (Ngày đi) và `ReturnDate` (Ngày về).
- `CutoffDate` / `BookingCloseHoursBeforeDeparture`: Thời điểm ngừng nhận khách online (VD: trước giờ khởi hành 24h hoặc 48h).
- `MinParticipants`: Số khách tối thiểu để đoàn khởi hành (VD: tối thiểu 10 khách; nếu không đủ, Điều hành xử lý dời lịch hoặc hoàn tiền 100%).
- **Trạng thái Lịch khởi hành:**
  - `Open`: Còn chỗ, đang mở bán.
  - `Full`: `AvailableSlots == 0` (vẫn có thể chuyển về `Open` nếu có khách hủy hoặc hết hạn giữ chỗ).
  - `Confirmed` / `ClosedForSale`: Đã chốt đoàn chuẩn bị khởi hành.
  - `InProgress`: Đoàn đang đi tour.
  - `Completed`: Đoàn đã kết thúc tour.
  - `Cancelled`: Hủy lịch khởi hành (kích hoạt quy trình chuyển lịch hoặc hoàn tiền toàn bộ khách đã đặt).

---

## 4. Chính sách Hoàn / Hủy / Đổi lịch (Cancellation Policy)

Mỗi Tour hoặc từng đợt khởi hành gắn với một bộ quy tắc tính phí phạt minh bạch theo số ngày trước ngày khởi hành (`DaysBeforeDeparture`):

Ví dụ bộ quy tắc chuẩn:
- Hủy trước $\ge 15$ ngày: Phí hủy **10%** tổng giá trị booking (hoặc miễn phí nếu là chính sách linh hoạt).
- Hủy từ $7$ đến $14$ ngày: Phí hủy **30%**.
- Hủy từ $3$ đến $6$ ngày: Phí hủy **50%**.
- Hủy dưới $3$ ngày hoặc No-show (không có mặt): Phí hủy **100%**.
- **Ngoại lệ bất khả kháng / Lỗi từ công ty (hủy đoàn do không đủ khách):** Hoàn **100%** hoặc cho phép đổi sang ngày khởi hành khác không mất phí.

---

## 5. Quản lý Nhà cung cấp & Dịch vụ đầu vào (Suppliers)

Để vận hành thực tế và tính được **lợi nhuận thực (Gross Profit)** của từng đoàn, Điều hành cần quản lý:
- **Danh mục Nhà cung cấp (`Supplier`):** Tên đơn vị, Loại hình (Khách sạn, Nhà xe, Hãng bay, Nhà hàng, Vé tham quan), Người liên hệ, SĐT/Email, Thông tin tài khoản thanh toán, Đánh giá nội bộ.
- **Gán dịch vụ vào Lịch khởi hành (`DepartureService`):**
  - Đoàn ngày `20/12/2026` đặt: 15 phòng tại Khách sạn X (đơn giá), 1 xe 35 chỗ của Nhà xe Y (đơn giá), 30 suất ăn tại Nhà hàng Z.
  - Tổng chi phí đầu vào này là cơ sở để Kế toán đối chiếu công nợ nhà cung cấp và tính **Biên lợi nhuận = Doanh thu thu từ khách - Tổng chi phí nhà cung cấp - Hoa hồng/Chi phí khác**.
