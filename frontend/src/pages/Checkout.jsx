import { useMemo, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { Calendar, FileText, QrCode, Tag, Users } from 'lucide-react';
import { findTour, promotions } from '../data/tours';
import { useBookings } from '../store/BookingContext';
import { calcPrice, DEPOSIT_PERCENT, fmtDate, HOLD_MINUTES, vnd } from '../utils/format';

function Steps({ current }) {
  const steps = ['Thông tin đặt tour', 'Thanh toán QR', 'Hoàn tất'];
  return (
    <ol className="steps">
      {steps.map((s, i) => (
        <li key={s} className={i < current ? 'done' : i === current ? 'current' : ''} aria-current={i === current ? 'step' : undefined}>
          <span>{i + 1}</span>{s}
        </li>
      ))}
    </ol>
  );
}
export { Steps };

function Field({ label, id, hint, error, ...props }) {
  return (
    <div className="field">
      <label htmlFor={id}>{label}{props.required && <span className="req" aria-hidden="true"> *</span>}</label>
      {hint && <span className="hint" id={`${id}-hint`}>{hint}</span>}
      <input id={id} aria-describedby={hint ? `${id}-hint` : undefined} {...props} />
      {error && <div className="error-msg"><span role="img" aria-label="Lỗi">⚠️</span> {error}</div>}
    </div>
  );
}

export default function Checkout() {
  const { slug } = useParams();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { createBooking } = useBookings();
  const tour = findTour(slug);
  const dep = useMemo(() => tour?.departures.find((d) => d.id === params.get('dep')), [tour, params]);
  const adults = +params.get('a') || 1;
  const children = +params.get('c') || 0;
  const infants = +params.get('i') || 0;
  const singleRooms = +params.get('s') || 0;

  const [promoInput, setPromoInput] = useState('');
  const [promoCode, setPromoCode] = useState('');
  const [promoMsg, setPromoMsg] = useState(null);
  const [payOption, setPayOption] = useState('full');
  const [wantInvoice, setWantInvoice] = useState(false);

  if (!tour || !dep) return <div className="container page empty">Thông tin đặt tour không hợp lệ. <Link to="/tours">Chọn lại tour</Link></div>;

  const promo = promoCode ? promotions[promoCode] : null;
  const price = calcPrice({ tour, departure: dep, adults, children, infants, singleRooms, promo });
  const payNow = payOption === 'deposit' ? price.deposit : price.total;

  const paxList = [
    ...Array.from({ length: adults }, (_, i) => ({ key: `a${i}`, type: 'Người lớn', n: i + 1 })),
    ...Array.from({ length: children }, (_, i) => ({ key: `c${i}`, type: 'Trẻ em', n: i + 1 })),
    ...Array.from({ length: infants }, (_, i) => ({ key: `i${i}`, type: 'Em bé', n: i + 1 })),
  ];

  const applyPromo = () => {
    const code = promoInput.trim().toUpperCase();
    const p = promotions[code];
    if (!p) return setPromoMsg({ ok: false, text: 'Mã giảm giá không tồn tại hoặc đã hết hạn.' });
    if (p.minPax && adults + children < p.minPax) return setPromoMsg({ ok: false, text: `Mã chỉ áp dụng cho nhóm từ ${p.minPax} khách.` });
    setPromoCode(code);
    setPromoMsg({ ok: true, text: p.label });
  };

  const submit = (e) => {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    const passengers = paxList.map((p) => ({
      type: p.type,
      fullName: fd.get(`${p.key}-name`),
      gender: fd.get(`${p.key}-gender`),
      dob: fd.get(`${p.key}-dob`),
      idNumber: fd.get(`${p.key}-id`) || '',
    }));
    const booking = createBooking({
      tourSlug: tour.slug,
      tourTitle: tour.title,
      tourCode: tour.code,
      image: tour.images[0],
      departureId: dep.id,
      departureDate: dep.departureDate,
      adults, children, infants, singleRooms,
      contact: { name: fd.get('contactName'), phone: fd.get('contactPhone'), email: fd.get('contactEmail'), note: fd.get('note') },
      passengers,
      promoCode: promoCode || null,
      price,
      payOption,
      invoice: wantInvoice ? { company: fd.get('invCompany'), taxCode: fd.get('invTax'), address: fd.get('invAddress'), email: fd.get('invEmail') } : null,
      holdExpiresAt: new Date(Date.now() + HOLD_MINUTES * 60000).toISOString(),
    });
    navigate(`/payment/${booking.code}`);
  };

  return (
    <div className="container page">
      <Steps current={0} />
      <form className="checkout-layout" onSubmit={submit}>
        <div>
          <section className="card">
            <h2>Thông tin liên hệ</h2>
            <p className="muted small">Chúng tôi sẽ gửi xác nhận đặt tour và vé điện tử qua email & SĐT này.</p>
            <div className="grid-2">
              <Field id="contactName" name="contactName" label="Họ và tên" required autoComplete="name" error="Vui lòng nhập họ tên người liên hệ." />
              <Field id="contactPhone" name="contactPhone" label="Số điện thoại" type="tel" required autoComplete="tel" pattern="0[0-9]{9}" hint="10 số, bắt đầu bằng 0" error="Nhập số điện thoại 10 số, ví dụ 0901234567." />
              <Field id="contactEmail" name="contactEmail" label="Email" type="email" required autoComplete="email" error="Nhập email theo định dạng ten@vidu.com." />
              <div className="field">
                <label htmlFor="note">Ghi chú / Yêu cầu đặc biệt</label>
                <input id="note" name="note" placeholder="Ăn chay, dị ứng, xe lăn..." />
              </div>
            </div>
          </section>

          <section className="card">
            <h2><Users size={20} /> Danh sách hành khách ({paxList.length})</h2>
            <p className="muted small">Họ tên phải trùng với CCCD/Hộ chiếu. Có thể bổ sung số giấy tờ trước ngày chốt đoàn.</p>
            {paxList.map((p) => (
              <fieldset key={p.key} className="pax">
                <legend>{p.type} {p.n}</legend>
                <div className="grid-4">
                  <Field id={`${p.key}-name`} name={`${p.key}-name`} label="Họ và tên" required error="Nhập họ tên hành khách." />
                  <div className="field">
                    <label htmlFor={`${p.key}-gender`}>Giới tính</label>
                    <select id={`${p.key}-gender`} name={`${p.key}-gender`}><option>Nam</option><option>Nữ</option></select>
                  </div>
                  <Field id={`${p.key}-dob`} name={`${p.key}-dob`} label="Ngày sinh" type="date" required error="Chọn ngày sinh." />
                  <Field id={`${p.key}-id`} name={`${p.key}-id`} label={tour.tags.includes('international') ? 'Số hộ chiếu' : 'CCCD (không bắt buộc)'} required={tour.tags.includes('international') && p.type !== 'Em bé'} error="Nhập số hộ chiếu." />
                </div>
              </fieldset>
            ))}
          </section>

          <section className="card">
            <h2>Hình thức thanh toán</h2>
            <div className="pay-options">
              <label className={`pay-opt ${payOption === 'full' ? 'active' : ''}`}>
                <input type="radio" name="payOption" value="full" checked={payOption === 'full'} onChange={() => setPayOption('full')} />
                <div><b>Thanh toán toàn bộ</b><div className="muted small">{vnd(price.total)} — xác nhận chỗ ngay</div></div>
              </label>
              <label className={`pay-opt ${payOption === 'deposit' ? 'active' : ''}`}>
                <input type="radio" name="payOption" value="deposit" checked={payOption === 'deposit'} onChange={() => setPayOption('deposit')} />
                <div><b>Đặt cọc {DEPOSIT_PERCENT}%</b><div className="muted small">{vnd(price.deposit)} — phần còn lại thanh toán trước 5 ngày khởi hành</div></div>
              </label>
            </div>
            <div className="qr-method"><QrCode size={28} /> Chuyển khoản ngân hàng qua mã <b>VietQR</b> (mô phỏng)</div>
          </section>

          <section className="card">
            <label className="check"><input type="checkbox" checked={wantInvoice} onChange={(e) => setWantInvoice(e.target.checked)} /> <FileText size={16} /> Yêu cầu xuất hóa đơn VAT (mô phỏng)</label>
            {wantInvoice && (
              <div className="grid-2 mt">
                <Field id="invCompany" name="invCompany" label="Tên công ty" required error="Nhập tên công ty." />
                <Field id="invTax" name="invTax" label="Mã số thuế" required pattern="[0-9]{10}([0-9]{3})?" hint="10 hoặc 13 chữ số" error="Mã số thuế gồm 10 hoặc 13 chữ số." />
                <Field id="invAddress" name="invAddress" label="Địa chỉ" required error="Nhập địa chỉ công ty." />
                <Field id="invEmail" name="invEmail" label="Email nhận hóa đơn" type="email" required error="Nhập email hợp lệ." />
              </div>
            )}
          </section>
        </div>

        <aside className="order-summary">
          <img src={tour.images[0]} alt={tour.title} />
          <h3>{tour.title}</h3>
          <div className="muted small"><Calendar size={14} /> Khởi hành {fmtDate(dep.departureDate)} · {tour.durationDays}N{tour.durationNights}Đ</div>
          <div className="bw-summary">
            <div><span>Người lớn × {adults}</span><span>{vnd(adults * price.adultPrice)}</span></div>
            {children > 0 && <div><span>Trẻ em × {children}</span><span>{vnd(children * price.childPrice)}</span></div>}
            {infants > 0 && <div><span>Em bé × {infants}</span><span>{vnd(infants * tour.infantPrice)}</span></div>}
            {singleRooms > 0 && <div><span>Phòng đơn × {singleRooms}</span><span>{vnd(singleRooms * tour.singleSupplement)}</span></div>}
            {price.discount > 0 && <div className="green"><span>Giảm giá ({promoCode})</span><span>−{vnd(price.discount)}</span></div>}
            <div className="total"><span>Tổng cộng</span><span>{vnd(price.total)}</span></div>
            <div className="muted small"><span>Giá trước thuế</span> <span>{vnd(price.amountBeforeTax)}</span></div>
            <div className="muted small"><span>VAT 8% (mô phỏng)</span> <span>{vnd(price.vatAmount)}</span></div>
            <div className="pay-now"><span>Thanh toán lần này</span><span>{vnd(payNow)}</span></div>
          </div>

          <div className="promo">
            <label htmlFor="promo"><Tag size={14} /> Mã giảm giá</label>
            <div className="promo-row">
              <input id="promo" value={promoInput} onChange={(e) => setPromoInput(e.target.value)} placeholder="WELCOME10" />
              <button type="button" className="btn btn-outline" onClick={applyPromo}>Áp dụng</button>
            </div>
            {promoMsg && <p className={promoMsg.ok ? 'green small' : 'error-text'} role="status">{promoMsg.text}</p>}
          </div>

          <label className="check small"><input type="checkbox" required /> Tôi đồng ý với điều khoản & chính sách hoàn hủy</label>
          <button type="submit" className="btn btn-primary btn-block btn-lg">Giữ chỗ & Thanh toán</button>
          <p className="muted small center">Chỗ của bạn được giữ {HOLD_MINUTES} phút để thanh toán.</p>
        </aside>
      </form>
    </div>
  );
}
