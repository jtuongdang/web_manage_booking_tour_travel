import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Bus, Calendar, Check, CircleCheck, Clock, MapPin, Minus, Plane, Plus, ShieldCheck, Star, Utensils, X, Zap } from 'lucide-react';
import { toursApi } from '../api/toursApi';
import { calcPrice, fmtDate, vnd } from '../utils/format';

function Stepper({ label, hint, value, setValue, min = 0, max }) {
  return (
    <div className="stepper">
      <div><b>{label}</b><div className="muted small">{hint}</div></div>
      <div className="stepper-ctrl">
        <button type="button" aria-label={`Giảm ${label}`} disabled={value <= min} onClick={() => setValue(value - 1)}><Minus size={16} /></button>
        <output aria-live="polite">{value}</output>
        <button type="button" aria-label={`Tăng ${label}`} disabled={max !== undefined && value >= max} onClick={() => setValue(value + 1)}><Plus size={16} /></button>
      </div>
    </div>
  );
}

export default function TourDetail() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const [tour, setTour] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [depId, setDepId] = useState();
  const [adults, setAdults] = useState(2);
  const [children, setChildren] = useState(0);
  const [infants, setInfants] = useState(0);
  const [singleRooms, setSingleRooms] = useState(0);
  const [tab, setTab] = useState('overview');
  useEffect(() => {
    let active = true;
    setLoading(true);
    toursApi.get(slug).then((result) => {
      if (active) {
        setTour(result);
        setDepId(result.departures.find((departure) => departure.available > 0)?.id);
        setError('');
      }
    }).catch((requestError) => {
      if (active) setError(requestError.message || 'Không thể tải thông tin tour.');
    }).finally(() => {
      if (active) setLoading(false);
    });
    return () => { active = false; };
  }, [slug]);
  const departures = tour?.departures ?? [];

  if (loading) return <div className="container page muted">Đang tải thông tin tour…</div>;
  if (error || !tour) return <div className="container page empty" role="alert">{error ? `Không thể tải tour: ${error}` : 'Không tìm thấy tour.'} <Link to="/tours">Quay lại danh sách</Link></div>;

  const dep = departures.find((d) => String(d.id) === String(depId));
  const seats = adults + children;
  const notEnough = dep && seats > dep.available;
  const price = dep ? calcPrice({ tour, departure: dep, adults, children, infants, singleRooms }) : null;

  const book = () => {
    const p = new URLSearchParams({ dep: dep.id, a: adults, c: children, i: infants, s: singleRooms });
    navigate(`/checkout/${tour.slug}?${p}`);
  };

  const TransportIcon = tour.transport === 'Máy bay' ? Plane : Bus;

  return (
    <div className="container page">
      <nav className="breadcrumb"><Link to="/">Trang chủ</Link> / <Link to="/tours">Tour</Link> / <span>{tour.destination}</span></nav>
      <h1 className="detail-title">{tour.title}</h1>
      <div className="detail-meta">
        <span className="rating"><Star size={16} fill="currentColor" /> <b>{tour.rating}</b> ({tour.reviewCount.toLocaleString('vi-VN')} đánh giá)</span>
        {tour.bookedCount > 0 && <span>{Number(tour.bookedCount).toLocaleString('vi-VN')}+ khách đã đặt</span>}
        <span>Mã tour: <b>{tour.code}</b></span>
      </div>

      <div className="gallery">
        {tour.images.map((src, i) => <img key={`${src}-${i}`} src={src} alt={`${tour.title} - ảnh ${i + 1}`} loading={i ? 'lazy' : 'eager'} />)}
      </div>

      <div className="detail-layout">
        <div>
          <div className="info-chips">
            <span><Clock size={16} /> {tour.durationDays} ngày {tour.durationNights} đêm</span>
            <span><MapPin size={16} /> Khởi hành: {tour.departureFrom}</span>
            <span><TransportIcon size={16} /> {tour.transport}</span>
            <span className="ok"><ShieldCheck size={16} /> Hủy trước {tour.freeCancelDays} ngày chỉ mất 10%</span>
            <span className="ok"><Zap size={16} /> Xác nhận ngay sau thanh toán</span>
          </div>

          <div className="detail-tabs" role="tablist">
            {[['overview', 'Tổng quan'], ['itinerary', 'Lịch trình'], ['departures', 'Lịch khởi hành & giá'], ['policy', 'Chính sách'], ['reviews', 'Đánh giá']].map(([k, l]) => (
              <a key={k} href={`#${k}`} role="tab" aria-selected={tab === k} className={tab === k ? 'active' : ''} onClick={() => setTab(k)}>{l}</a>
            ))}
          </div>

          <section id="overview" className="card">
            <h2>Điểm nổi bật</h2>
            <ul className="check-list">{tour.highlights.map((h) => <li key={h}><CircleCheck size={18} /> {h}</li>)}</ul>
            <div className="two-col">
              <div>
                <h3>Giá tour bao gồm</h3>
                <ul className="inc-list">{tour.includes.map((x) => <li key={x}><Check size={16} className="green" /> {x}</li>)}</ul>
              </div>
              <div>
                <h3>Không bao gồm</h3>
                <ul className="inc-list">{tour.excludes.map((x) => <li key={x}><X size={16} className="red" /> {x}</li>)}</ul>
              </div>
            </div>
          </section>

          <section id="itinerary" className="card">
            <h2>Lịch trình chi tiết</h2>
            <ol className="timeline">
              {tour.itinerary.map((d) => (
                <li key={d.day}>
                  <div className="tl-dot">N{d.day}</div>
                  <div>
                    <h3>Ngày {d.day}: {d.title}</h3>
                    <div className="muted small"><Utensils size={14} /> Bữa ăn: {d.meals}</div>
                    <p>{d.description}</p>
                  </div>
                </li>
              ))}
            </ol>
          </section>

          <section id="departures" className="card">
            <h2>Lịch khởi hành & giá</h2>
            <div className="table-wrap">
              <table className="dep-table">
                <thead><tr><th scope="col">Ngày khởi hành</th><th scope="col">Giá người lớn</th><th scope="col">Chỗ còn</th><th scope="col"><span className="sr-only">Chọn</span></th></tr></thead>
                <tbody>
                  {departures.map((d) => (
                    <tr key={d.id} className={String(d.id) === String(depId) ? 'selected' : ''}>
                      <td><Calendar size={14} /> {fmtDate(d.departureDate)}</td>
                      <td className="price-cell">{vnd(d.adultPrice)}</td>
                      <td>{d.available > 0 ? <span className={d.available <= 5 ? 'urgent' : 'green'}>{d.available}/{d.capacity}</span> : <span className="sold">Hết chỗ</span>}</td>
                      <td><button type="button" className="btn btn-sm btn-outline" disabled={!d.available} onClick={() => setDepId(d.id)}>{String(d.id) === String(depId) ? 'Đã chọn' : 'Chọn'}</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <p className="muted small">Trẻ em 2–11 tuổi: {tour.childPercent}% giá người lớn · Em bé dưới 2 tuổi: {tour.infantPrice ? vnd(tour.infantPrice) : 'Miễn phí'} · Phụ thu phòng đơn: {vnd(tour.singleSupplement)}</p>
          </section>

          <section id="policy" className="card">
            <h2>Chính sách hoàn / hủy</h2>
            <table className="policy-table">
              <tbody>{(tour.policy?.rules ?? []).map((rule) => <tr key={rule.id}><td>{rule.label}</td><td>Phí {rule.penaltyPercent}%</td></tr>)}</tbody>
            </table>
            {!tour.policy?.rules?.length && <p className="muted">Chưa có chi tiết điều khoản hủy từ hệ thống.</p>}
            <p className="muted small">{tour.policy?.policy?.description || 'Mức cọc, thuế và phí được tính lại theo cấu hình backend khi tạo booking.'}</p>
          </section>

          <section id="reviews" className="card">
            <h2>Đánh giá từ khách hàng</h2>
            <div className="review-summary">
              <div className="big-score">{tour.rating}<span>/5</span></div>
              <div>
                {[
                  ['Hướng dẫn viên', tour.reviewBreakdown?.guideAverage],
                  ['Lịch trình', tour.reviewBreakdown?.itineraryAverage],
                  ['Khách sạn & ăn uống', tour.reviewBreakdown?.hotelFoodAverage],
                  ['Phương tiện', tour.reviewBreakdown?.transportAverage],
                ].filter(([, v]) => v != null).map(([l, v]) => (
                  <div key={l} className="bar-row"><span>{l}</span><meter min="0" max="5" value={v} /><b>{v}</b></div>
                ))}
              </div>
            </div>
            {tour.reviews.map((r) => (
              <article key={r.id} className="review">
                <div className="review-head">
                  <div className="avatar">{r.name.split(' ').pop()[0]}</div>
                  <div>
                    <b>{r.name}</b> {r.verified && <span className="verified"><CircleCheck size={12} /> Đã đi tour</span>}
                    <div className="muted small">{r.date}</div>
                  </div>
                  <div className="stars" aria-label={`${r.rating} sao`}>{'★'.repeat(r.rating)}{'☆'.repeat(5 - r.rating)}</div>
                </div>
                <p>{r.content}</p>
                {r.reply && <div className="reply"><b>Phản hồi từ VietTour:</b> {r.reply}</div>}
              </article>
            ))}
            {!tour.reviews.length && <p className="muted">Chưa có đánh giá chi tiết cho tour này.</p>}
          </section>
        </div>

        <aside className="booking-widget">
          <div className="bw-price">
            <s className="muted">{vnd(tour.originalPrice)}</s>
            <div><span className="price big">{vnd(dep?.adultPrice ?? tour.price)}</span> <span className="muted">/người lớn</span></div>
          </div>

          <label className="field">
            <span>Ngày khởi hành</span>
            <select value={depId ?? ''} onChange={(e) => setDepId(e.target.value)}>
              {departures.map((d) => (
                <option key={d.id} value={d.id} disabled={!d.available}>
                  {fmtDate(d.departureDate)} — {d.available ? `còn ${d.available} chỗ` : 'Hết chỗ'}
                </option>
              ))}
            </select>
          </label>

          <Stepper label="Người lớn" hint="Từ 12 tuổi" value={adults} setValue={setAdults} min={1} />
          <Stepper label="Trẻ em" hint={`2 – 11 tuổi · ${tour.childPercent}% giá`} value={children} setValue={setChildren} />
          <Stepper label="Em bé" hint="Dưới 2 tuổi" value={infants} setValue={setInfants} max={adults} />
          <Stepper label="Phòng đơn" hint={`+${vnd(tour.singleSupplement)}/phòng`} value={singleRooms} setValue={setSingleRooms} max={adults} />

          {price && (
            <div className="bw-summary">
              <div><span>Người lớn × {adults}</span><span>{vnd(adults * price.adultPrice)}</span></div>
              {children > 0 && <div><span>Trẻ em × {children}</span><span>{vnd(children * price.childPrice)}</span></div>}
              {infants > 0 && <div><span>Em bé × {infants}</span><span>{vnd(infants * tour.infantPrice)}</span></div>}
              {singleRooms > 0 && <div><span>Phụ thu phòng đơn × {singleRooms}</span><span>{vnd(singleRooms * tour.singleSupplement)}</span></div>}
              <div className="total"><span>Tổng cộng</span><span>{vnd(price.total)}</span></div>
              <div className="muted small">Đã bao gồm VAT 8%. Đặt cọc từ {vnd(price.deposit)}</div>
            </div>
          )}

          {notEnough && <p className="error-text" role="alert">Lịch này chỉ còn {dep.available} chỗ, không đủ cho {seats} khách.</p>}
          <button type="button" className="btn btn-primary btn-block btn-lg" disabled={!dep || notEnough} onClick={book}>Đặt tour ngay</button>
          <p className="muted small center"><ShieldCheck size={14} /> Giữ chỗ 15 phút · Thanh toán QR an toàn</p>
        </aside>
      </div>

      {price && (
        <div className="mobile-cta">
          <div><div className="muted small">Tổng {seats} khách</div><b className="price">{vnd(price.total)}</b></div>
          <button type="button" className="btn btn-primary" disabled={!dep || notEnough} onClick={book}>Đặt ngay</button>
        </div>
      )}
    </div>
  );
}
