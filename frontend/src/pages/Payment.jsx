import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { QRCodeSVG } from 'qrcode.react';
import { Clock, Copy, FlaskConical, Landmark, LoaderCircle, TriangleAlert } from 'lucide-react';
import { bookingsApi } from '../api/bookingsApi';
import { paymentsApi } from '../api/paymentsApi';
import { requestSignIn, useAuth } from '../store/AuthContext';
import { fmtDate, vnd } from '../utils/format';
import { Steps } from './Checkout';

export default function Payment() {
  const { code } = useParams();
  const navigate = useNavigate();
  const auth = useAuth();
  const [booking, setBooking] = useState(null);
  const [transaction, setTransaction] = useState(null);
  const [status, setStatus] = useState('loading');
  const [error, setError] = useState('');
  const [now, setNow] = useState(() => Date.now());
  const [copied, setCopied] = useState('');
  const [simulating, setSimulating] = useState(false);

  useEffect(() => {
    if (auth.initializing) return undefined;
    if (!auth.user) {
      setStatus('auth-required');
      return undefined;
    }
    let active = true;
    setStatus('loading');
    Promise.resolve().then(async () => {
      const currentBooking = await bookingsApi.get(code);
      if (!active) return;
      setBooking(currentBooking);
      if (!['PendingPayment', 'DepositPaid'].includes(currentBooking.status)) {
        setStatus('not-payable');
        return;
      }
      const mode = currentBooking.status === 'DepositPaid'
        ? 'Remaining'
        : currentBooking.payOption === 'Deposit' ? 'Deposit' : 'Full';
      const qr = await paymentsApi.createQr(currentBooking, mode);
      if (active) {
        setTransaction(qr);
        setStatus(qr.status === 'Expired' ? 'expired' : 'pending');
      }
    }).catch((requestError) => {
      if (active) {
        setError(requestError.message || 'Không thể tạo mã QR thanh toán.');
        setStatus('error');
      }
    });
    return () => { active = false; };
  }, [auth.initializing, auth.user, code]);

  useEffect(() => {
    if (status !== 'pending' || !transaction) return undefined;
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    const poller = window.setInterval(async () => {
      try {
        const result = await paymentsApi.status(transaction.transactionCode);
        if (result.status === 'Succeeded') {
          setStatus('success');
          navigate(`/booking/${code}?paid=1`);
        } else if (['Failed', 'Expired', 'NeedReconciliation'].includes(result.status)) {
          setStatus(result.status === 'Expired' ? 'expired' : result.status === 'Failed' ? 'failed' : 'mismatch');
        }
      } catch (requestError) {
        setError(requestError.message || 'Không thể kiểm tra trạng thái thanh toán.');
      }
    }, 3000);
    return () => {
      window.clearInterval(timer);
      window.clearInterval(poller);
    };
  }, [status, transaction, navigate, code]);

  const expireAt = transaction ? new Date(transaction.expiresAt).getTime() : 0;
  const remainSec = Math.max(0, Math.floor((expireAt - now) / 1000));
  const amount = Number(transaction?.expectedAmount ?? 0);
  const transferContent = transaction?.qr?.transferContent ?? `TOUR ${code}`;
  const qrPayload = useMemo(() => transaction?.qr?.qrContent ?? '', [transaction]);

  useEffect(() => {
    if (status === 'pending' && transaction && remainSec === 0) setStatus('expired');
  }, [status, transaction, remainSec]);

  if (status === 'auth-required') {
    return <div className="container page empty">Vui lòng đăng nhập đúng tài khoản đặt tour để thanh toán. <button type="button" className="btn btn-primary" onClick={requestSignIn}>Đăng nhập</button></div>;
  }
  if (status === 'loading') return <div className="container page muted">Đang tạo phiên thanh toán…</div>;
  if (!booking) return <div className="container page empty" role="alert">{error || `Không tìm thấy đơn ${code}.`} <Link to="/my-bookings">Đơn của tôi</Link></div>;
  if (status === 'not-payable') return <div className="container page empty">Đơn {code} không ở trạng thái chờ thanh toán. <Link to={`/booking/${code}`}>Xem chi tiết đơn</Link></div>;
  if (!transaction) return <div className="container page empty" role="alert">{error || 'Không thể tạo phiên thanh toán.'} <Link to={`/booking/${code}`}>Quay lại đơn hàng</Link></div>;

  const copy = async (text, key) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopied(key);
      window.setTimeout(() => setCopied(''), 1500);
    } catch {
      setError('Không thể sao chép nội dung vào clipboard.');
    }
  };

  const simulate = async (scenario, paidAmount) => {
    setSimulating(true);
    setError('');
    try {
      await paymentsApi.simulateDemo(transaction.transactionCode, scenario, paidAmount);
      setStatus('pending');
    } catch (requestError) {
      setError(requestError.message || 'Không thể chạy mô phỏng thanh toán.');
    } finally {
      setSimulating(false);
    }
  };

  const mm = String(Math.floor(remainSec / 60)).padStart(2, '0');
  const ss = String(remainSec % 60).padStart(2, '0');
  const bank = transaction.qr ?? {};

  return (
    <div className="container page">
      <Steps current={1} />
      {error && <p className="error-text" role="alert">{error}</p>}
      <div className="payment-layout">
        <section className="card qr-card">
          <h2>Quét mã QR để thanh toán</h2>
          <p className="muted">Mở ứng dụng ngân hàng bất kỳ → Quét mã QR → Kiểm tra thông tin → Xác nhận chuyển khoản</p>

          {status === 'expired' ? (
            <div className="qr-expired">
              <TriangleAlert size={40} />
              <h3>Mã QR đã hết hạn</h3>
              <p>Vui lòng tạo lại mã thanh toán cho đơn đặt tour.</p>
              <button type="button" className="btn btn-primary" onClick={() => window.location.reload()}>Tạo mã mới</button>
            </div>
          ) : (
            <>
              <div className={`countdown ${remainSec < 120 ? 'warn' : ''}`} role="timer" aria-live="off"><Clock size={18} /> Mã hết hạn sau <b>{mm}:{ss}</b></div>
              <div className="qr-box">
                {qrPayload ? <QRCodeSVG value={qrPayload} size={220} level="M" marginSize={2} /> : <p className="error-text">API không trả về nội dung VietQR.</p>}
                {status === 'pending' && <div className="qr-processing"><LoaderCircle className="spin" size={36} /> Đang chờ xác nhận giao dịch…</div>}
              </div>
              <div className="qr-amount">{vnd(amount)}</div>

              <dl className="bank-info">
                <div><dt><Landmark size={14} /> Ngân hàng</dt><dd>{bank.bankName ?? bank.name ?? 'Theo mã VietQR'}</dd></div>
                <div><dt>Số tài khoản</dt><dd>{bank.accountNumber ?? 'Thông tin trong mã QR'} {bank.accountNumber && <button type="button" className="copy" onClick={() => copy(bank.accountNumber, 'acc')}><Copy size={14} /> {copied === 'acc' ? 'Đã chép' : 'Chép'}</button>}</dd></div>
                <div><dt>Chủ tài khoản</dt><dd>{bank.accountHolder ?? 'Theo mã VietQR'}</dd></div>
                <div><dt>Số tiền</dt><dd><b>{vnd(amount)}</b></dd></div>
                <div><dt>Nội dung CK</dt><dd><b className="hl">{transferContent}</b> <button type="button" className="copy" onClick={() => copy(transferContent, 'ct')}><Copy size={14} /> {copied === 'ct' ? 'Đã chép' : 'Chép'}</button></dd></div>
              </dl>

              {status === 'failed' && <p className="alert alert-error" role="alert">Giao dịch thất bại. Bạn có thể tạo lại mã thanh toán.</p>}
              {status === 'mismatch' && <p className="alert alert-warn" role="alert">Giao dịch cần kế toán đối soát do số tiền nhận không khớp.</p>}
            </>
          )}
        </section>

        <aside>
          <section className="card">
            <h3>Đơn hàng {booking.code}</h3>
            <p className="small">{booking.tourTitle}</p>
            <p className="muted small">Khởi hành {booking.departureDate ? fmtDate(booking.departureDate) : '—'} · {booking.adults + booking.children + booking.infants} khách</p>
            <div className="bw-summary">
              <div><span>Tổng giá trị đơn</span><span>{vnd(booking.price.total)}</span></div>
              <div><span>Đã thanh toán</span><span>{vnd(booking.paidAmount)}</span></div>
              <div className="pay-now"><span>Thanh toán lần này</span><span>{vnd(amount)}</span></div>
            </div>
          </section>

          {!['expired', 'success'].includes(status) && (
            <section className="card simulator">
              <h3><FlaskConical size={18} /> Bộ mô phỏng ngân hàng</h3>
              <p className="muted small">Môi trường Development: xác nhận giao dịch qua endpoint demo có xác thực.</p>
              <div className="sim-buttons">
                <button type="button" className="btn btn-success" disabled={simulating || status === 'processing'} onClick={() => simulate('Success')}>✅ Thanh toán thành công</button>
                <button type="button" className="btn btn-outline" disabled={simulating} onClick={() => simulate('Partial', Math.max(1, amount - 100000))}>⚠️ Chuyển sai số tiền</button>
                <button type="button" className="btn btn-outline" disabled={simulating} onClick={() => simulate('Failed', 0)}>❌ Giao dịch thất bại</button>
              </div>
            </section>
          )}
        </aside>
      </div>
    </div>
  );
}
