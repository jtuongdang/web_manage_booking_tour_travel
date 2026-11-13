import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { QRCodeSVG } from 'qrcode.react';
import { Clock, Copy, FlaskConical, Landmark, LoaderCircle, TriangleAlert } from 'lucide-react';
import { useBookings } from '../store/BookingContext';
import { fmtDate, HOLD_MINUTES, vnd } from '../utils/format';
import { Steps } from './Checkout';

// Thông tin tài khoản công ty (mô phỏng)
const BANK = { name: 'Vietcombank (mô phỏng)', bin: '970436', account: '1023456789', owner: 'CONG TY TNHH VIETTOUR' };

export default function Payment() {
  const { code } = useParams();
  const navigate = useNavigate();
  const { getBooking, updateBooking } = useBookings();
  const booking = getBooking(code);

  const isRemaining = booking?.status === 'DepositPaid';
  const amount = booking
    ? isRemaining ? booking.price.total - booking.paidAmount : booking.payOption === 'deposit' ? booking.price.deposit : booking.price.total
    : 0;

  // Phiên QR: thanh toán lần đầu dùng hạn giữ chỗ; thanh toán phần còn lại có hạn 15 phút
  const [session] = useState(() => ({
    txCode: `PAY-${Date.now().toString().slice(-8)}`,
    expiresAt: isRemaining || !booking ? Date.now() + HOLD_MINUTES * 60000 : new Date(booking.holdExpiresAt).getTime(),
  }));
  const [now, setNow] = useState(() => Date.now());
  const [state, setState] = useState('pending'); // pending | processing | failed | mismatch | expired
  const [copied, setCopied] = useState('');

  const remainSec = Math.max(0, Math.floor((session.expiresAt - now) / 1000));
  const transferContent = `TOUR ${code}`;
  const qrPayload = useMemo(
    () => `VIETQR|BIN:${BANK.bin}|ACC:${BANK.account}|NAME:${BANK.owner}|AMOUNT:${amount}|CONTENT:${transferContent}|TX:${session.txCode}`,
    [amount, transferContent, session.txCode],
  );

  useEffect(() => {
    if (state !== 'pending') return;
    const t = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(t);
  }, [state]);

  useEffect(() => {
    if (state === 'pending' && remainSec === 0 && booking) {
      setState('expired');
      if (booking.status === 'PendingPayment') updateBooking(code, { status: 'Expired' });
    }
  }, [remainSec, state, booking, code, updateBooking]);

  if (!booking) return <div className="container page empty">Không tìm thấy đơn đặt tour. <Link to="/">Về trang chủ</Link></div>;
  if (!['PendingPayment', 'DepositPaid'].includes(booking.status) && state === 'pending')
    return <div className="container page empty">Đơn {code} không ở trạng thái chờ thanh toán. <Link to={`/booking/${code}`}>Xem chi tiết đơn</Link></div>;

  const copy = async (text, key) => {
    await navigator.clipboard?.writeText(text);
    setCopied(key);
    setTimeout(() => setCopied(''), 1500);
  };

  // === Mô phỏng webhook ngân hàng gọi về hệ thống ===
  const simulate = (scenario) => {
    if (scenario === 'timeout') {
      setNow(session.expiresAt);
      return;
    }
    setState('processing');
    setTimeout(() => {
      if (scenario === 'failed') return setState('failed');
      if (scenario === 'mismatch') return setState('mismatch');
      const paid = booking.paidAmount + amount;
      const fullyPaid = paid >= booking.price.total;
      updateBooking(code, (b) => ({
        paidAmount: paid,
        status: fullyPaid ? 'Confirmed' : 'DepositPaid',
        payments: [...b.payments, { txCode: session.txCode, amount, method: 'VietQR', paidAt: new Date().toISOString(), bankRef: `FT${Date.now()}` }],
        invoiceNo: fullyPaid && b.invoice ? `INV-2026-${String(Math.floor(Math.random() * 900000) + 100000)}` : b.invoiceNo,
      }));
      navigate(`/booking/${code}?paid=1`);
    }, 1500);
  };

  const mm = String(Math.floor(remainSec / 60)).padStart(2, '0');
  const ss = String(remainSec % 60).padStart(2, '0');

  return (
    <div className="container page">
      <Steps current={1} />
      <div className="payment-layout">
        <section className="card qr-card">
          <h2>Quét mã QR để thanh toán</h2>
          <p className="muted">Mở ứng dụng ngân hàng bất kỳ → Quét mã QR → Kiểm tra thông tin → Xác nhận chuyển khoản</p>

          {state === 'expired' ? (
            <div className="qr-expired">
              <TriangleAlert size={40} />
              <h3>Mã QR đã hết hạn</h3>
              <p>{isRemaining ? 'Vui lòng tạo lại mã thanh toán.' : 'Thời gian giữ chỗ đã hết, chỗ đã được trả lại hệ thống. Vui lòng đặt lại tour.'}</p>
              {isRemaining ? <button type="button" className="btn btn-primary" onClick={() => window.location.reload()}>Tạo mã mới</button> : <Link className="btn btn-primary" to={`/tours/${booking.tourSlug}`}>Đặt lại tour</Link>}
            </div>
          ) : (
            <>
              <div className={`countdown ${remainSec < 120 ? 'warn' : ''}`} role="timer" aria-live="off"><Clock size={18} /> Mã hết hạn sau <b>{mm}:{ss}</b></div>
              <div className="qr-box">
                <QRCodeSVG value={qrPayload} size={220} level="M" marginSize={2} />
                {state === 'processing' && <div className="qr-processing"><LoaderCircle className="spin" size={36} /> Đang xác nhận giao dịch...</div>}
              </div>
              <div className="qr-amount">{vnd(amount)}</div>

              <dl className="bank-info">
                <div><dt><Landmark size={14} /> Ngân hàng</dt><dd>{BANK.name}</dd></div>
                <div><dt>Số tài khoản</dt><dd>{BANK.account} <button type="button" className="copy" onClick={() => copy(BANK.account, 'acc')}><Copy size={14} /> {copied === 'acc' ? 'Đã chép' : 'Chép'}</button></dd></div>
                <div><dt>Chủ tài khoản</dt><dd>{BANK.owner}</dd></div>
                <div><dt>Số tiền</dt><dd><b>{vnd(amount)}</b></dd></div>
                <div><dt>Nội dung CK</dt><dd><b className="hl">{transferContent}</b> <button type="button" className="copy" onClick={() => copy(transferContent, 'ct')}><Copy size={14} /> {copied === 'ct' ? 'Đã chép' : 'Chép'}</button></dd></div>
              </dl>

              {state === 'failed' && <p className="alert alert-error" role="alert">Giao dịch thất bại hoặc bị hủy từ phía ngân hàng. Bạn có thể quét lại mã để thử lại.</p>}
              {state === 'mismatch' && <p className="alert alert-warn" role="alert">Số tiền nhận được không khớp ({vnd(amount - 100000)}). Giao dịch được chuyển cho kế toán đối soát thủ công — chúng tôi sẽ liên hệ bạn trong 30 phút.</p>}
              {['failed', 'mismatch'].includes(state) && <button type="button" className="btn btn-outline" onClick={() => setState('pending')}>Thử lại</button>}
            </>
          )}
        </section>

        <aside>
          <section className="card">
            <h3>Đơn hàng {booking.code}</h3>
            <p className="small">{booking.tourTitle}</p>
            <p className="muted small">Khởi hành {fmtDate(booking.departureDate)} · {booking.adults + booking.children + booking.infants} khách</p>
            <div className="bw-summary">
              <div><span>Tổng giá trị đơn</span><span>{vnd(booking.price.total)}</span></div>
              <div><span>Đã thanh toán</span><span>{vnd(booking.paidAmount)}</span></div>
              <div className="pay-now"><span>{isRemaining ? 'Thanh toán phần còn lại' : booking.payOption === 'deposit' ? 'Đặt cọc 30%' : 'Thanh toán toàn bộ'}</span><span>{vnd(amount)}</span></div>
            </div>
          </section>

          {state !== 'expired' && (
            <section className="card simulator">
              <h3><FlaskConical size={18} /> Bộ mô phỏng ngân hàng</h3>
              <p className="muted small">Chỉ dùng cho môi trường demo/test — giả lập ngân hàng gửi webhook xác nhận giao dịch <code>{session.txCode}</code>.</p>
              <div className="sim-buttons">
                <button type="button" className="btn btn-success" disabled={state === 'processing'} onClick={() => simulate('success')}>✅ Thanh toán thành công</button>
                <button type="button" className="btn btn-outline" disabled={state === 'processing'} onClick={() => simulate('mismatch')}>⚠️ Chuyển sai số tiền</button>
                <button type="button" className="btn btn-outline" disabled={state === 'processing'} onClick={() => simulate('failed')}>❌ Giao dịch thất bại</button>
                <button type="button" className="btn btn-outline" disabled={state === 'processing'} onClick={() => simulate('timeout')}>⏱ Hết hạn mã QR</button>
              </div>
            </section>
          )}
        </aside>
      </div>
    </div>
  );
}
