import { useRef, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { QRCodeSVG } from 'qrcode.react';
import { CircleCheck, FileText, Printer, XCircle } from 'lucide-react';
import { useBookings } from '../store/BookingContext';
import { cancellationFeePercent, daysUntil, fmtDate, vnd } from '../utils/format';
import { Steps } from './Checkout';

export const STATUS = {
  PendingPayment: { label: 'Chờ thanh toán', cls: 'st-warn' },
  DepositPaid: { label: 'Đã đặt cọc', cls: 'st-info' },
  Confirmed: { label: 'Đã xác nhận', cls: 'st-ok' },
  CancellationRequested: { label: 'Đang yêu cầu hủy', cls: 'st-warn' },
  Cancelled: { label: 'Đã hủy', cls: 'st-muted' },
  Expired: { label: 'Hết hạn giữ chỗ', cls: 'st-muted' },
  Completed: { label: 'Hoàn thành', cls: 'st-ok' },
};

export default function BookingDetail() {
  const { code } = useParams();
  const [params] = useSearchParams();
  const { getBooking, updateBooking } = useBookings();
  const b = getBooking(code);
  const cancelRef = useRef(null);
  const invoiceRef = useRef(null);
  const [reason, setReason] = useState('');

  if (!b) return <div className="container page empty">Không tìm thấy đơn {code}. <Link to="/my-bookings">Xem đơn của tôi</Link></div>;

  const st = STATUS[b.status] ?? { label: b.status, cls: '' };
  const days = daysUntil(b.departureDate);
  const feePct = cancellationFeePercent(days);
  const penalty = Math.round((b.price.total * feePct) / 100);
  const refund = Math.max(0, b.paidAmount - penalty);
  const canCancel = ['PendingPayment', 'DepositPaid', 'Confirmed'].includes(b.status);
  const justPaid = params.get('paid') === '1';

  const onCancelClose = () => {
    const dlg = cancelRef.current;
    if (dlg.returnValue === 'confirm') {
      updateBooking(code, b.paidAmount > 0
        ? { status: 'CancellationRequested', cancelRequest: { reason, feePct, penalty, refund, requestedAt: new Date().toISOString() } }
        : { status: 'Cancelled' });
    }
    dlg.returnValue = '';
  };

  return (
    <div className="container page">
      {justPaid && <Steps current={2} />}
      {justPaid && (
        <div className="success-banner">
          <CircleCheck size={48} />
          <div>
            <h1>Thanh toán thành công!</h1>
            <p>Đơn <b>{b.code}</b> {b.status === 'Confirmed' ? 'đã được xác nhận chỗ chính thức' : 'đã đặt cọc thành công, chỗ đã được giữ'}. Email xác nhận đã gửi tới <b>{b.contact.email}</b>.</p>
          </div>
        </div>
      )}

      <div className="detail-layout">
        <div>
          <section className="card">
            <div className="bk-head">
              <div>
                <div className="muted small">Mã đặt tour</div>
                <h2 className="bk-code">{b.code}</h2>
              </div>
              <span className={`status ${st.cls}`}>{st.label}</span>
            </div>
            <div className="bk-tour">
              <img src={b.image} alt="" />
              <div>
                <Link to={`/tours/${b.tourSlug}`}><b>{b.tourTitle}</b></Link>
                <div className="muted small">Mã tour {b.tourCode} · Khởi hành {fmtDate(b.departureDate)} ({days >= 0 ? `còn ${days} ngày` : 'đã khởi hành'})</div>
                <div className="muted small">{b.adults} người lớn{b.children ? `, ${b.children} trẻ em` : ''}{b.infants ? `, ${b.infants} em bé` : ''}{b.singleRooms ? ` · ${b.singleRooms} phòng đơn` : ''}</div>
              </div>
            </div>
          </section>

          <section className="card">
            <h3>Hành khách</h3>
            <div className="table-wrap">
              <table className="dep-table">
                <thead><tr><th scope="col">#</th><th scope="col">Họ tên</th><th scope="col">Loại</th><th scope="col">Giới tính</th><th scope="col">Ngày sinh</th><th scope="col">Giấy tờ</th></tr></thead>
                <tbody>
                  {b.passengers.map((p, i) => (
                    <tr key={i}><td>{i + 1}</td><td>{p.fullName}</td><td>{p.type}</td><td>{p.gender}</td><td>{p.dob ? fmtDate(p.dob) : '—'}</td><td>{p.idNumber ? `•••${p.idNumber.slice(-4)}` : <span className="muted">Bổ sung sau</span>}</td></tr>
                  ))}
                </tbody>
              </table>
            </div>
            <p className="muted small">Liên hệ: {b.contact.name} · {b.contact.phone} · {b.contact.email}{b.contact.note ? ` · Ghi chú: ${b.contact.note}` : ''}</p>
          </section>

          <section className="card">
            <h3>Lịch sử thanh toán</h3>
            {b.payments.length ? (
              <ul className="pay-history">
                {b.payments.map((p) => (
                  <li key={p.txCode}><CircleCheck size={16} className="green" /> <b>{vnd(p.amount)}</b> qua {p.method} · Mã GD {p.txCode} · Ref {p.bankRef} · {new Date(p.paidAt).toLocaleString('vi-VN')}</li>
                ))}
              </ul>
            ) : <p className="muted">Chưa có giao dịch.</p>}
            {b.cancelRequest && (
              <div className="alert alert-warn">
                Yêu cầu hủy gửi lúc {new Date(b.cancelRequest.requestedAt).toLocaleString('vi-VN')} · Phí hủy {b.cancelRequest.feePct}% ({vnd(b.cancelRequest.penalty)}) · Dự kiến hoàn <b>{vnd(b.cancelRequest.refund)}</b> sau khi kế toán duyệt (3–7 ngày làm việc).
              </div>
            )}
          </section>
        </div>

        <aside className="booking-widget">
          <div className="ticket-qr">
            <QRCodeSVG value={`BOOKING:${b.code}`} size={120} />
            <div className="muted small">Xuất trình mã này cho HDV khi check-in</div>
          </div>
          <div className="bw-summary">
            <div><span>Tạm tính</span><span>{vnd(b.price.subTotal)}</span></div>
            {b.price.discount > 0 && <div className="green"><span>Giảm giá ({b.promoCode})</span><span>−{vnd(b.price.discount)}</span></div>}
            <div className="total"><span>Tổng cộng</span><span>{vnd(b.price.total)}</span></div>
            <div className="muted small"><span>Trong đó VAT 8%</span><span>{vnd(b.price.vatAmount)}</span></div>
            <div><span>Đã thanh toán</span><span className="green">{vnd(b.paidAmount)}</span></div>
            <div className="pay-now"><span>Còn lại</span><span>{vnd(b.price.total - b.paidAmount)}</span></div>
          </div>

          {['PendingPayment', 'DepositPaid'].includes(b.status) && (
            <Link to={`/payment/${b.code}`} className="btn btn-primary btn-block">{b.status === 'DepositPaid' ? 'Thanh toán phần còn lại' : 'Tiếp tục thanh toán'}</Link>
          )}
          {b.invoiceNo && <button type="button" className="btn btn-outline btn-block" onClick={() => invoiceRef.current.showModal()}><FileText size={16} /> Xem hóa đơn VAT</button>}
          {b.invoice && !b.invoiceNo && <p className="muted small center">Hóa đơn VAT sẽ được phát hành khi thanh toán đủ.</p>}
          {canCancel && <button type="button" className="btn btn-ghost-danger btn-block" onClick={() => cancelRef.current.showModal()}><XCircle size={16} /> Yêu cầu hủy tour</button>}
          <Link to="/my-bookings" className="btn btn-ghost btn-block">← Danh sách đơn của tôi</Link>
        </aside>
      </div>

      {/* Hộp thoại hủy tour: dùng <dialog> native */}
      <dialog ref={cancelRef} className="modal" aria-labelledby="cancel-title" onClose={onCancelClose} closedby="any">
        <h2 id="cancel-title">Yêu cầu hủy đơn {b.code}?</h2>
        {b.paidAmount > 0 ? (
          <table className="policy-table">
            <tbody>
              <tr><td>Số ngày trước khởi hành</td><td>{days} ngày</td></tr>
              <tr><td>Phí hủy theo chính sách</td><td>{feePct}% × {vnd(b.price.total)} = <b>{vnd(penalty)}</b></td></tr>
              <tr><td>Đã thanh toán</td><td>{vnd(b.paidAmount)}</td></tr>
              <tr><td>Số tiền dự kiến hoàn</td><td className="green"><b>{vnd(refund)}</b></td></tr>
            </tbody>
          </table>
        ) : <p>Đơn chưa thanh toán, chỗ giữ sẽ được trả lại ngay và không mất phí.</p>}
        <form method="dialog">
          <div className="field">
            <label htmlFor="cancel-reason">Lý do hủy</label>
            <select id="cancel-reason" value={reason} onChange={(e) => setReason(e.target.value)}>
              <option value="">— Chọn lý do —</option>
              <option>Thay đổi kế hoạch cá nhân</option>
              <option>Lý do sức khỏe</option>
              <option>Tìm được tour khác phù hợp hơn</option>
              <option>Khác</option>
            </select>
          </div>
          <div className="modal-actions">
            <button value="cancel" className="btn btn-outline" autoFocus>Giữ lại đơn</button>
            <button value="confirm" className="btn btn-danger">Xác nhận hủy</button>
          </div>
        </form>
      </dialog>

      {/* Hóa đơn VAT mô phỏng */}
      {b.invoiceNo && (
        <dialog ref={invoiceRef} className="modal invoice" aria-labelledby="inv-title" closedby="any">
          <div className="watermark">HÓA ĐƠN MÔ PHỎNG – KHÔNG CÓ GIÁ TRỊ PHÁP LÝ</div>
          <h2 id="inv-title">HÓA ĐƠN GIÁ TRỊ GIA TĂNG</h2>
          <p className="center small">Ký hiệu: 1C26TVT · Số: <b>{b.invoiceNo}</b> · Ngày {new Date().toLocaleDateString('vi-VN')}</p>
          <div className="inv-parties">
            <div><b>Đơn vị bán:</b> CÔNG TY TNHH VIETTOUR<br />MST: 0312345678<br />190 Pasteur, Q.3, TP.HCM</div>
            <div><b>Người mua:</b> {b.invoice.company}<br />MST: {b.invoice.taxCode}<br />{b.invoice.address}</div>
          </div>
          <table className="dep-table">
            <thead><tr><th scope="col">Dịch vụ</th><th scope="col">Thành tiền trước thuế</th></tr></thead>
            <tbody>
              <tr><td>Tour {b.tourCode} – KH {fmtDate(b.departureDate)} ({b.passengers.length} khách)</td><td>{vnd(b.price.amountBeforeTax)}</td></tr>
              <tr><td>Thuế suất GTGT 8%</td><td>{vnd(b.price.vatAmount)}</td></tr>
              <tr><td><b>Tổng tiền thanh toán</b></td><td><b>{vnd(b.price.total)}</b></td></tr>
            </tbody>
          </table>
          <p className="muted small">Mã tra cứu (mô phỏng): {b.invoiceNo.replace('INV-', 'TC')} · Gửi tới {b.invoice.email}</p>
          <form method="dialog" className="modal-actions">
            <button type="button" className="btn btn-outline" onClick={() => window.print()}><Printer size={16} /> In</button>
            <button value="close" className="btn btn-primary" autoFocus>Đóng</button>
          </form>
        </dialog>
      )}
    </div>
  );
}
