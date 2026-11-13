import { Compass, Mail, MapPin, Phone, ShieldCheck } from 'lucide-react';

export default function Footer() {
  return (
    <footer className="footer">
      <div className="container footer-grid">
        <div>
          <div className="logo light"><Compass size={26} /><span>viettour</span></div>
          <p className="muted-light">Nền tảng đặt tour du lịch trọn gói — minh bạch giá, giữ chỗ tức thì, thanh toán QR an toàn.</p>
          <p className="muted-light small"><ShieldCheck size={14} /> Giấy phép lữ hành quốc tế (mô phỏng): 79-000/2026/TCDL-GP LHQT</p>
        </div>
        <div>
          <h4>Về VietTour</h4>
          <ul><li>Giới thiệu</li><li>Tuyển dụng</li><li>Tin tức du lịch</li><li>Đối tác lữ hành</li></ul>
        </div>
        <div>
          <h4>Sản phẩm & Dịch vụ</h4>
          <ul><li>Tour trong nước</li><li>Tour nước ngoài</li><li>Du thuyền Hạ Long</li><li>Chính sách hoàn hủy</li></ul>
        </div>
        <div>
          <h4>Liên hệ</h4>
          <ul>
            <li><Phone size={14} /> 1900 1234 (8:00 – 22:00)</li>
            <li><Mail size={14} /> cskh@viettour.vn</li>
            <li><MapPin size={14} /> 190 Pasteur, Q.3, TP.HCM</li>
          </ul>
        </div>
      </div>
      <div className="container footer-bottom">© 2026 VietTour — Giao diện học tập theo bố cục Traveloka. Thanh toán & hóa đơn là mô phỏng.</div>
    </footer>
  );
}
