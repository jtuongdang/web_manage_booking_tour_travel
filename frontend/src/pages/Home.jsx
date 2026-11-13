import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Check, Compass, Copy, Gift, Headphones, Info, Percent, QrCode, ShieldCheck, Sparkles, Ticket } from 'lucide-react';
import SearchBar from '../components/SearchBar';
import TourCard from '../components/TourCard';
import { destinations, HERO_IMAGE, tours } from '../data/tours';

const COUPONS = [
  { code: 'WELCOME10', icon: Compass, color: '#0194f3', title: 'Giảm 10% tối đa 500K cho đơn tour đầu tiên', sub: 'Áp dụng cho khách hàng mới trên website' },
  { code: 'GROUP300', icon: Ticket, color: '#0b63ce', title: 'Giảm ngay 300.000 ₫ cho nhóm từ 4 khách', sub: 'Áp dụng cho toàn bộ tour trong nước & quốc tế' },
  { code: 'COC30', icon: Percent, color: '#ff5e1f', title: 'Đặt cọc 30% giữ chỗ ngay – Không phí ẩn', sub: 'Thanh toán qua VietQR, xác nhận tức thì' },
];

const PROMO_BANNERS = [
  { code: 'WELCOME10', title: 'Nâng tầm hành trình cùng VietTour', sub: 'Giảm 10% (tối đa 500K) tour biển đảo', image: '/images/phu-quoc.jpg', bg: 'linear-gradient(120deg, #0b63ce, #0194f3)' },
  { code: 'GROUP300', title: 'Nghỉ dưỡng 5 sao & Du thuyền', sub: 'Phú Quốc · Đà Nẵng · Hạ Long · Sapa', image: '/images/luxury-resort.jpg', bg: 'linear-gradient(120deg, #1c2430, #374151)' },
  { code: 'COC30', title: 'Vi vu Nhật Bản & Hàn Quốc', sub: 'Bao visa · Giá trọn gói minh bạch', image: '/images/han-quoc.jpg', bg: 'linear-gradient(120deg, #ff5e1f, #f59e0b)' },
];

export default function Home() {
  const [copiedCode, setCopiedCode] = useState('');

  const copyCode = async (code) => {
    await navigator.clipboard?.writeText(code);
    setCopiedCode(code);
    setTimeout(() => setCopiedCode(''), 1800);
  };

  return (
    <>
      <section className="hero">
        <img className="hero-bg" src={HERO_IMAGE} alt="Bãi biển nhiệt đới Việt Nam" fetchPriority="high" onError={(e) => { e.currentTarget.style.opacity = '0'; }} />
        <div className="hero-overlay" />
        <div className="container hero-content">
          <h1>Nền tảng đặt tour hàng đầu – một chạm, vạn hành trình</h1>
          <SearchBar />
        </div>
      </section>

      {/* New User Coupons section — giống hệt bố cục Traveloka trong ảnh */}
      <section className="container section">
        <div className="section-head">
          <div>
            <h2>Ưu đãi khách hàng mới</h2>
            <p className="muted small" style={{ margin: 0 }}>Áp dụng trực tiếp ở bước thanh toán đặt tour</p>
          </div>
          <Link to="/tours">Xem tất cả →</Link>
        </div>
        <div className="coupon-grid">
          {COUPONS.map((c) => {
            const Icon = c.icon;
            return (
              <div key={c.code} className="coupon-card">
                <div className="coupon-top">
                  <span className="coupon-ic" style={{ background: `${c.color}18`, color: c.color }}><Icon size={18} /></span>
                  <div className="coupon-text">
                    <b>{c.title}</b>
                    <span className="muted small">{c.sub}</span>
                  </div>
                  <Info size={16} className="muted" />
                </div>
                <div className="coupon-bottom">
                  <span className="coupon-code"><Copy size={13} /> {c.code}</span>
                  <button type="button" className="btn-copy-pill" onClick={() => copyCode(c.code)}>
                    {copiedCode === c.code ? <><Check size={13} /> ĐÃ CHÉP</> : 'SAO CHÉP'}
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      </section>

      <section className="container section">
        <div className="promo-grid">
          {PROMO_BANNERS.map((p) => (
            <Link key={p.title} to="/tours" className="promo-card" style={{ background: p.bg }}>
              <img className="promo-img" src={p.image} alt="" loading="lazy" />
              <div className="promo-scrim" />
              <div className="promo-inner">
                <div>
                  <span className="promo-tag">KHUYẾN MÃI HOT</span>
                  <h3>{p.title}</h3>
                  <p>{p.sub}</p>
                </div>
                <div className="promo-footer">
                  <code>{p.code}</code>
                  <span className="promo-go">Xem ngay →</span>
                </div>
              </div>
            </Link>
          ))}
        </div>
      </section>

      <section className="container section">
        <div className="section-head">
          <h2><Sparkles size={22} className="orange-ic" /> Tour bán chạy giá tốt</h2>
          <Link to="/tours">Xem tất cả →</Link>
        </div>
        <div className="tour-grid">
          {tours.slice(0, 4).map((t) => <TourCard key={t.id} tour={t} />)}
        </div>
      </section>

      <section className="container section">
        <div className="section-head">
          <h2>Điểm đến nổi bật</h2>
          <Link to="/tours">Khám phá thêm →</Link>
        </div>
        <div className="dest-grid">
          {destinations.map((d) => (
            <Link key={d.name} to={`/tours?q=${encodeURIComponent(d.name.split(' - ')[0])}`} className="dest-card">
              <img src={d.image} alt={d.name} loading="lazy" width="600" height="800" />
              <div><b>{d.name}</b><span>{d.count} tour</span></div>
            </Link>
          ))}
        </div>
      </section>

      <section className="container section">
        <div className="section-head">
          <h2>Hành trình nước ngoài được yêu thích</h2>
          <Link to="/tours?cat=international">Xem tất cả →</Link>
        </div>
        <div className="tour-grid">
          {tours.filter((t) => t.tags.includes('international')).map((t) => <TourCard key={t.id} tour={t} />)}
          {tours.filter((t) => t.tags.includes('domestic')).slice(0, 2).map((t) => <TourCard key={`dom-${t.id}`} tour={t} />)}
        </div>
      </section>

      <section className="container usp">
        <div><ShieldCheck /><div><b>Giá minh bạch</b><p>Đã gồm VAT, không phí ẩn</p></div></div>
        <div><QrCode /><div><b>Thanh toán VietQR</b><p>Quét mã, xác nhận tức thì</p></div></div>
        <div><Gift /><div><b>Hoàn hủy linh hoạt</b><p>Chính sách rõ ràng theo ngày</p></div></div>
        <div><Headphones /><div><b>Hỗ trợ 24/7</b><p>Hotline 1900 1234</p></div></div>
      </section>
    </>
  );
}
