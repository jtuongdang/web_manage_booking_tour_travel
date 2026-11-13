import { Link, NavLink } from 'react-router-dom';
import { ChevronDown, Compass, Percent, Smartphone, Ticket, User } from 'lucide-react';

export default function Header() {
  return (
    <header className="header">
      <div className="header-top">
        <div className="container header-top-inner">
          <Link to="/" className="logo">
            <Compass size={28} strokeWidth={2.3} />
            <span>viettour</span>
          </Link>

          <div className="header-utils">
            <span className="util-item lang-pill">🇻🇳 VND | VI <ChevronDown size={14} /></span>
            <Link to="/tours" className="util-item"><Percent size={16} className="blue-ic" /> Khuyến mãi</Link>
            <span className="util-item hide-md"><Smartphone size={16} className="blue-ic" /> Tải ứng dụng</span>
            <span className="util-item hide-md">Hỗ trợ <ChevronDown size={14} /></span>
            <Link to="/my-bookings" className="util-item"><Ticket size={16} className="blue-ic" /> Đặt chỗ của tôi</Link>
            <button type="button" className="btn-tvk-login"><User size={15} /> Đăng nhập</button>
            <button type="button" className="btn-tvk-register">Đăng ký</button>
          </div>
        </div>
      </div>

      <div className="header-nav">
        <div className="container header-nav-inner">
          <NavLink to="/" end>Tour du lịch</NavLink>
          <NavLink to="/tours?cat=domestic">Tour trong nước</NavLink>
          <NavLink to="/tours?cat=international">Tour nước ngoài</NavLink>
          <NavLink to="/tours?cat=beach">Biển đảo</NavLink>
          <NavLink to="/tours?cat=mountain">Núi rừng</NavLink>
          <NavLink to="/tours?cat=culture">Văn hóa & Di sản</NavLink>
          <NavLink to="/tours">Tất cả tour</NavLink>
        </div>
      </div>
    </header>
  );
}
