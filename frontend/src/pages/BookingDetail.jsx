import { useEffect, useRef, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { QRCodeSVG } from 'qrcode.react';
import { CircleCheck, FileText, Printer, XCircle } from 'lucide-react';
import { apiGet } from '../api/client';
import { bookingsApi } from '../api/bookingsApi';
import { toursApi } from '../api/toursApi';
import { requestSignIn, useAuth } from '../store/AuthContext';
import { Steps } from './Checkout';
import { fmtDate, vnd } from '../utils/format';

export const STATUS = {
  PendingPayment: { label: 'Chờ thanh toán', cls: 'st-warn' },
  DepositPaid: { label: 'Đã đặt cọc', cls: 'st-info' },
  Confirmed: { label: 'Đã xác nhận', cls: 'st-ok' },
  CancellationRequested: { label: 'Đang yêu cầu hủy', cls: 'st-warn' },
  Cancelled: { label: 'Đã hủy', cls: 'st-muted' },
  Expired: { label: 'Hết hạn giữ chỗ', cls: 'st-muted' },
  FinalPaymentOverdue: { label: 'Quá hạn thanh toán', cls: 'st-warn' },
  Refunded: { label: 'Đã hoàn tiền', cls: 'st-muted' },
  Completed: { label: 'Hoàn thành', cls: 'st-ok' },
};

export default function BookingDetail() {
  const { code } = useParams();
  const [params] = useSearchParams();
  const auth = useAuth();
  const cancelRef = useRef(null);
  const [booking, setBooking] = useState(null);
  const [departures, setDepartures] = useState([]);
  const [invoice, setInvoice] = useState(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [reason, setReason] = useState('');
  const [newDepartureId, setNewDepartureId] = useState('');
  const [reviewRating, setReviewRating] = useState(5);
  const [reviewComment, setReviewComment] = useState('');

  const loadBooking = async () => {
    const result = await bookingsApi.get(code);
    setBooking(result);
    const items = await toursApi.departures(result.tourId);
    setDepartures(items.filter((item) => item.available > 0));
  };

  useEffect(() => {
    if (auth.initializing) return undefined;
    if (!auth.user) {
      setBooking(null);
      setLoading(false);
      setError('Đăng nhập để xem chi tiết đơn đặt tour.');
      return undefined;
    }
    let active = true;
    setLoading(true);
    Promise.resolve().then(async () => {
      const result = await bookingsApi.get(code);
      if (!active) return;
      setBooking(result);
      const items = await toursApi.departures(result.tourId);
      if (active) {
        setDepartures(items.filter((item) => item.available > 0));
        setError('');
      }
    }).catch((requestError) => {
      if (active) setError(requestError.message || 'Không thể tải thông tin đơn đặt tour.');
    }).finally(() => {
      if (active) setLoading(false);
    });
    return () => { active = false; };
  }, [code, auth.initializing, auth.user]);

  if (loading) return <div className="container page muted">Đang tải đơn đặt tour…</div>;
  if (!booking) return <div className="container page empty" role="alert">{error || `Không tìm thấy đơn ${code}.`} {!auth.user && <button type="button" className="btn btn-primary" onClick={requestSignIn}>Đăng nhập</button>} <Link to="/my-bookings">Xem đơn của tôi</Link></div>;

  const b = booking;
  const st = STATUS[b.status] ?? { label: b.status, cls: '' };
  const passengers = b.passengers ?? [];
  const payments = b.payments ?? [];
  const canCancel = ['PendingPayment', 'DepositPaid', 'Confirmed'].includes(b.status);
  const canReschedule = ['PendingPayment', 'DepositPaid', 'Confirmed'].includes(b.status);
  const justPaid = params.get('paid') === '1';

  const cancel = async (event) => {
    event.preventDefault();
    if (!reason) {
      setError('Vui lòng chọn lý do hủy.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      await bookingsApi.cancel(b.id, reason);
      cancelRef.current?.close();
      await loadBooking();
    } catch (requestError) {
      setError(requestError.message || 'Không thể gửi yêu cầu hủy.');
    } finally {
      setBusy(false);
    }
  };

  const reschedule = async (event) => {
    event.preventDefault();
    if (!newDepartureId) return;
    setBusy(true);
    setError('');
    try {
      await bookingsApi.reschedule(b.id, Number(newDepartureId), 'Khách yêu cầu đổi ngày khởi hành');
      setNewDepartureId('');
      await loadBooking();
    } catch (requestError) {
      setError(requestError.message || 'Không thể đổi ngày khởi hành.');
    } finally {
      setBusy(false);
    }
  };

  const openInvoice = async (download = false) => {
    const preview = download ? null : window.open('about:blank', '_blank');
    if (!download && !preview) {
      setError('Trình duyệt đã chặn cửa sổ xem hóa đơn.');
      return;
    }
    try {
      const currentInvoice = await bookingsApi.invoice(b.id);
      setInvoice(currentInvoice);
      const html = await apiGet(`/api/v1/invoices/${currentInvoice.id}/preview`, { responseType: 'text' });
      const blob = new Blob([html], { type: 'text/html;charset=utf-8' });
      const url = URL.createObjectURL(blob);
      if (download) {
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `invoice-${currentInvoice.invoiceNumber}.html`;
        anchor.click();
        window.setTimeout(() => URL.revokeObjectURL(url), 60000);
      } else {
        preview.location.href = url;
        window.setTimeout(() => URL.revokeObjectURL(url), 60000);
      }
    } catch (requestError) {
      preview?.close();
      setError(requestError.message || 'Không thể tải hóa đơn VAT.');
    }
  };

  const submitReview = async (event) => {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      await bookingsApi.submitReview(b.id, { overallRating: Number(reviewRating), comment: reviewComment });
      setReviewComment('');
      setError('Đã gửi đánh giá tour.');
    } catch (requestError) {
      setError(requestError.message || 'API đánh giá booking chưa khả dụng.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="container page">
      {justPaid && <Steps current={2} />}
      {justPaid && (
        <div className="success-banner">
          <CircleCheck size={48} />
          <div>
            <h1>Thanh toán thành công!</h1>
            <p>Đơn <b>{b.code}</b> {b.status === 'Confirmed' ? 'đã được xác nhận chỗ chính thức' : 'đã được cập nhật thanh toán'}.</p>
          </div>
        </div>
      )}
      {error && <p className={error.startsWith('Đã gửi') ? 'alert alert-success' : 'error-text'} role="status">{error}</p>}

      <div className="detail-layout">
        <div>
          <section className="card">
            <div className="bk-head">
              <div><div className="muted small">Mã đặt tour</div><h2 className="bk-code">{b.code}</h2></div>
              <span className={`status ${st.cls}`}>{st.label}</span>
            </div>
            <div className="bk-tour">
              <img src={b.image} alt="" />
              <div>
                <Link to={`/tours/${b.tourSlug}`}><b>{b.tourTitle}</b></Link>
                <div className="muted small">Mã tour {b.tourCode || b.tourId} · Khởi hành {b.departureDate ? fmtDate(b.departureDate) : '—'}</div>
                <div className="muted small">{b.adults} người lớn{b.children ? `, ${b.children} trẻ em` : ''}{b.infants ? `, ${b.infants} em bé` : ''}{b.singleRooms ? ` · ${b.singleRooms} phòng đơn` : ''}</div>
              </div>
            </div>
          </section>

          <section className="card">
            <h3>Hành khách</h3>
            {passengers.length ? (
              <div className="table-wrap">
                <table className="dep-table">
                  <thead><tr><th scope="col">#</th><th scope="col">Họ tên</th><th scope="col">Loại</th><th scope="col">Giới tính</th><th scope="col">Ngày sinh</th><th scope="col">Giấy tờ</th></tr></thead>
                  <tbody>{passengers.map((p, index) => <tr key={p.id ?? index}><td>{index + 1}</td><td>{p.fullName}</td><td>{p.paxType}</td><td>{p.gender || '—'}</td><td>{p.dateOfBirth ? fmtDate(p.dateOfBirth) : '—'}</td><td>{p.identityNumber ? `•••${p.identityNumber.slice(-4)}` : '—'}</td></tr>)}</tbody>
                </table>
              </div>
            ) : <p className="muted">Chưa cập nhật danh sách hành khách.</p>}
            <p className="muted small">Liên hệ: {b.contact.name} · {b.contact.phone} · {b.contact.email}{b.contact.note ? ` · Ghi chú: ${b.contact.note}` : ''}</p>
          </section>

          <section className="card">
            <h3>Lịch sử thanh toán</h3>
            {payments.length ? (
              <ul className="pay-history">{payments.map((payment) => <li key={payment.transactionCode}><CircleCheck size={16} className="green" /> <b>{vnd(payment.actualReceivedAmount)}</b> · Mã GD {payment.transactionCode} · {payment.status}</li>)}</ul>
            ) : <p className="muted">Chưa có giao dịch thanh toán nào.</p>}
          </section>

          {canReschedule && (
            <section className="card">
              <h3>Đổi ngày khởi hành</h3>
              <form className="grid-2" onSubmit={reschedule}>
                <div className="field">
                  <label htmlFor="reschedule-departure">Lịch khởi hành mới</label>
                  <select id="reschedule-departure" value={newDepartureId} onChange={(event) => setNewDepartureId(event.target.value)} required>
                    <option value="">Chọn ngày khởi hành</option>
                    {departures.filter((item) => item.id !== b.departureId).map((item) => <option key={item.id} value={item.id}>{fmtDate(item.departureDate)} · còn {item.availableSlots ?? item.available} chỗ</option>)}
                  </select>
                </div>
                <button className="btn btn-outline" type="submit" disabled={busy || !newDepartureId}>Xác nhận đổi ngày</button>
              </form>
            </section>
          )}

          {b.status === 'Completed' && (
            <section className="card">
              <h3>Đánh giá tour</h3>
              <form onSubmit={submitReview}>
                <div className="field"><label htmlFor="review-rating">Điểm đánh giá</label><select id="review-rating" value={reviewRating} onChange={(event) => setReviewRating(event.target.value)}>{[5, 4, 3, 2, 1].map((rating) => <option key={rating} value={rating}>{rating} sao</option>)}</select></div>
                <div className="field"><label htmlFor="review-comment">Nhận xét</label><textarea id="review-comment" value={reviewComment} onChange={(event) => setReviewComment(event.target.value)} required /></div>
                <button className="btn btn-primary" type="submit" disabled={busy}>Gửi đánh giá</button>
              </form>
            </section>
          )}
        </div>

        <aside className="booking-widget">
          <div className="ticket-qr"><QRCodeSVG value={`BOOKING:${b.code}`} size={120} /><div className="muted small">Xuất trình mã này cho HDV khi check-in</div></div>
          <div className="bw-summary">
            <div><span>Tạm tính</span><span>{vnd(b.price.subTotal)}</span></div>
            {b.price.discount > 0 && <div className="green"><span>Giảm giá ({b.promoCode})</span><span>−{vnd(b.price.discount)}</span></div>}
            <div className="total"><span>Tổng cộng</span><span>{vnd(b.price.total)}</span></div>
            <div className="muted small"><span>VAT</span><span>{vnd(b.price.vatAmount)}</span></div>
            <div><span>Đã thanh toán</span><span className="green">{vnd(b.paidAmount)}</span></div>
            <div className="pay-now"><span>Còn lại</span><span>{vnd(b.price.total - b.paidAmount)}</span></div>
          </div>
          {['PendingPayment', 'DepositPaid'].includes(b.status) && <Link to={`/payment/${b.code}`} className="btn btn-primary btn-block">{b.status === 'DepositPaid' ? 'Thanh toán phần còn lại' : 'Tiếp tục thanh toán'}</Link>}
          {b.invoice && <p className="muted small center">Hóa đơn mô phỏng sẽ khả dụng sau khi nhân viên phát hành.</p>}
          {invoice && <p className="muted small">Hóa đơn: {invoice.invoiceNumber}</p>}
          {canCancel && <button type="button" className="btn btn-ghost-danger btn-block" onClick={() => cancelRef.current?.showModal()}><XCircle size={16} /> Yêu cầu hủy tour</button>}
          {canCancel && <dialog ref={cancelRef} className="modal" aria-labelledby="cancel-title" closedby="any">
            <h2 id="cancel-title">Yêu cầu hủy đơn {b.code}?</h2>
            <form onSubmit={cancel}>
              <div className="field">
                <label htmlFor="cancel-reason">Lý do hủy</label>
                <select id="cancel-reason" value={reason} onChange={(event) => setReason(event.target.value)} required>
                  <option value="">— Chọn lý do —</option><option>Thay đổi kế hoạch cá nhân</option><option>Lý do sức khỏe</option><option>Tìm được tour khác phù hợp hơn</option><option>Khác</option>
                </select>
              </div>
              <div className="modal-actions"><button type="button" className="btn btn-outline" onClick={() => cancelRef.current?.close()}>Giữ lại đơn</button><button type="submit" className="btn btn-danger" disabled={busy}>Xác nhận hủy</button></div>
            </form>
          </dialog>}
          {b.requireVatInvoice && <div className="invoice-actions">
            <button type="button" className="btn btn-outline btn-block" onClick={() => openInvoice()}><FileText size={16} /> Xem hóa đơn VAT</button>
            <button type="button" className="btn btn-ghost btn-block" onClick={() => openInvoice(true)}><Printer size={16} /> Tải hóa đơn HTML</button>
          </div>}
          <Link to="/my-bookings" className="btn btn-ghost btn-block">← Danh sách đơn của tôi</Link>
        </aside>
      </div>
    </div>
  );
}
