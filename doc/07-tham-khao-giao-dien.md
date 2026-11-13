# 07. Báo Cáo Nguồn Tham Khảo & Thiết Kế Giao Diện Khách Hàng

## 1. Nguồn tham khảo & Phương pháp thiết kế

- Giao diện khách hàng **VietTour** được xây dựng theo **tỷ lệ bố cục 1:1 và hệ màu của Traveloka (`https://www.traveloka.com/en-vn` & `/vi-vn`)**, kết hợp tham khảo quy chuẩn nghiệp vụ lữ hành từ **Klook**, **GetYourGuide** và **Vietravel**.
- Chúng tôi trực tiếp khảo sát cấu trúc giao diện trên trình duyệt tại `traveloka.com/en-vn` để chuẩn hóa tỷ lệ header 2 tầng, hero search card, popup lịch 2 tháng, popup chọn khách & phòng, thẻ mã ưu đãi, thẻ tour 4 cột và hệ màu (`#0194f3` xanh chủ đạo, `#ff5e1f` cam giá/khuyến mãi, `#03121a` chữ navy đậm).
- **Không sao chép mã nguồn, logo hay tài sản bản quyền.** Toàn bộ mã nguồn React, thương hiệu demo **VietTour** và bộ 10 ảnh du lịch nội bộ trong `frontend/public/images/` được tạo mới phục vụ sản phẩm.

## 2. Chi tiết các thành phần áp dụng theo tỷ lệ Traveloka

| Thành phần | Quy cách áp dụng | Tập tin triển khai |
| :--- | :--- | :--- |
| **Header 2 tầng dính cố định** | Tầng tiện ích (logo, tiền tệ/ngôn ngữ, khuyến mãi, đặt chỗ, pill Đăng nhập `#d8ecfe` & Đăng ký `#0194f3`) + Tầng menu sản phẩm tour | `src/components/Header.jsx` |
| **Hero & Khung tìm kiếm nổi** | Tab dịch vụ (`Tour trọn gói`, `Trong nước`, `Nước ngoài`, `Du thuyền`...), pill lọc nhanh, thanh 3 cột liền khối + nút tìm kiếm `#0194f3` | `src/components/SearchBar.jsx` |
| **Popup Điểm đến / Lịch 2 tháng / Khách & Phòng** | Gợi ý điểm đến nổi bật, lịch chọn khoảng ngày 2 tháng cạnh nhau (`T2–CN`), bộ đếm `−`/`+` Người lớn / Trẻ em / Phòng | `src/components/SearchBar.jsx` |
| **Ưu đãi khách hàng mới & Banner 2:1** | 3 thẻ coupon trắng có đường kẻ đứt nét + nút `SAO CHÉP`, kèm 3 banner ảnh tỷ lệ `2:1` | `src/pages/Home.jsx` |
| **Thẻ Tour 4 cột** | Nhãn địa điểm góc trên trái, badge `Tiết kiệm X%` cam góc dưới phải, thang điểm `/10` xanh dương, giá gốc gạch ngang & giá cam nổi bật | `src/components/TourCard.jsx` |
| **Chi tiết Tour & Booking Widget** | Gallery 5 ảnh, timeline lịch trình, bảng lịch khởi hành & số chỗ còn, đánh giá theo tiêu chí, khung đặt tour dính cột phải | `src/pages/TourDetail.jsx` |

## 3. Danh sách các trang giao diện khách hàng (`frontend/`)

| Đường dẫn | Trang | Chức năng chính |
| :--- | :--- | :--- |
| `/` | Trang chủ | Hero + thanh tìm kiếm Traveloka 1:1, coupon mới, banner khuyến mãi, tour bán chạy, điểm đến nổi bật |
| `/tours` | Danh sách tour | Tìm kiếm không dấu, lọc theo danh mục / ngân sách / thời lượng / ngày đi, sắp xếp |
| `/tours/:slug` | Chi tiết tour | Bộ 5 ảnh, lịch trình timeline, bảng lịch khởi hành & giá, chính sách hủy, đánh giá, widget chọn số khách |
| `/checkout/:slug` | Đặt tour | Thông tin liên hệ, danh sách hành khách, mã giảm giá (`WELCOME10`, `GROUP300`), chọn cọc 30% hoặc trả đủ, yêu cầu hóa đơn VAT |
| `/payment/:code` | Thanh toán QR (mô phỏng) | Mã VietQR động, đếm ngược 15 phút, thông tin chuyển khoản, bộ giả lập ngân hàng (thành công / sai tiền / thất bại / hết hạn) |
| `/booking/:code` | Chi tiết đơn | Trạng thái đơn, QR check-in, lịch sử thanh toán, hộp thoại hủy tính phí phạt, hóa đơn VAT mô phỏng |
| `/my-bookings` | Đơn của tôi | Tra cứu theo mã đơn, lọc theo trạng thái |
