import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Check, Compass, Copy, Gift, Headphones, Info, Percent, QrCode, ShieldCheck, Sparkles, Ticket } from 'lucide-react';
import SearchBar from '../components/SearchBar';
import TourCard from '../components/TourCard';
import { destinations, HERO_IMAGE } from '../data/tours';
import { promotionsApi, toursApi } from '../api/toursApi';
import { vnd } from '../utils/format';

export default function Home() {
  const [copiedCode, setCopiedCode] = useState('');
  const [tours, setTours] = useState([]);
  const [promotions, setPromotions] = useState([]);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    toursApi.list({ pageIndex: 1, pageSize: 100 }).then((result) => {
      if (active) setTours(result.items);
    }).catch((requestError) => {
      if (active) setError(`Tour: ${requestError.message || 'không thể tải dữ liệu.'}`);
    });
    promotionsApi.list().then((result) => {
      if (active) setPromotions(result);
    }).catch((requestError) => {
      if (active) setError((message) => `${message ? `${message} ` : ''}Ưu đãi: ${requestError.message || 'không thể tải dữ liệu.'}`);
    });
    return () => { active = false; };
  }, []);

  const coupons = promotions.slice(0, 3).map((promotion, index) => {
    const Icon = [Compass, Ticket, Percent][index];
    const amount = promotion.discountType === 'Percent'
      ? `Giảm ${promotion.discountValue}%${promotion.maxDiscountAmount ? ` tối đa ${vnd(promotion.maxDiscountAmount)}` : ''}`
      : `Giảm ${vnd(promotion.discountValue)}`;
    return {
      code: promotion.code,
      icon: Icon,
      color: ['#0194f3', '#0b63ce', '#ff5e1f'][index],
      title: `${amount} ${promotion.minPax > 1 ? `cho nhóm từ ${promotion.minPax} khách` : ''}`,
      sub: promotion.description || `Áp dụng đến ${new Date(promotion.validTo).toLocaleDateString('vi-VN')}`,
    };
  });
  const promoBanners = promotions.slice(0, 3).map((promotion, index) => ({
    code: promotion.code,
    title: promotion.name,
    sub: promotion.description || `Áp dụng đến ${new Date(promotion.validTo).toLocaleDateString('vi-VN')}`,
    image: ['/images/phu-quoc.jpg', '/images/luxury-resort.jpg', '/images/han-quoc.jpg'][index],
    bg: ['linear-gradient(120deg, #0b63ce, #0194f3)', 'linear-gradient(120deg, #1c2430, #374151)', 'linear-gradient(120deg, #ff5e1f, #f59e0b)'][index],
  }));

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
          {error && <p className="error-text" role="alert">Không tải được dữ liệu API: {error}</p>}
          {coupons.map((c) => {
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
          {!coupons.length && <p className="muted small">Hiện chưa có ưu đãi đang áp dụng.</p>}
        </div>
      </section>

      <section className="container section">
        <div className="promo-grid">
          {promoBanners.map((p) => (
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
          {!promoBanners.length && <p className="muted small">Chưa có chiến dịch khuyến mãi đang chạy.</p>}
        </div>
      </section>

      <section className="container section">
        <div className="section-head">
          <h2><Sparkles size={22} className="orange-ic" /> Tour bán chạy giá tốt</h2>
          <Link to="/tours">Xem tất cả →</Link>
        </div>
        <div className="tour-grid">
          {tours.slice(0, 4).map((t) => <TourCard key={t.id} tour={t} />)}
          {!tours.length && !error && <p className="muted">Đang tải tour từ hệ thống…</p>}
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
