import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Calendar, Search, Ticket } from 'lucide-react';
import { bookingsApi } from '../api/bookingsApi';
import { requestSignIn, useAuth } from '../store/AuthContext';
import { fmtDate, vnd } from '../utils/format';
import { STATUS } from './BookingDetail';

const TABS = [
  ['all', 'Tất cả', () => true],
  ['upcoming', 'Sắp đi', (b) => ['DepositPaid', 'Confirmed', 'FinalPaymentOverdue'].includes(b.status)],
  ['pending', 'Chờ thanh toán', (b) => b.status === 'PendingPayment'],
  ['cancelled', 'Đã hủy / Hết hạn', (b) => ['Cancelled', 'Expired', 'CancellationRequested'].includes(b.status)],
];

export default function MyBookings() {
  const auth = useAuth();
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [tab, setTab] = useState('all');
  const [lookup, setLookup] = useState('');
  useEffect(() => {
    if (auth.initializing) return undefined;
    if (!auth.user) {
      setBookings([]);
      setLoading(false);
      setError('Đăng nhập để xem các booking gắn với tài khoản của bạn.');
      return undefined;
    }
    let active = true;
    setLoading(true);
    bookingsApi.mine().then((items) => {
      if (active) {
        setBookings(items);
        setError('');
      }
    }).catch((requestError) => {
      if (active) setError(requestError.message || 'Không thể tải đơn đặt tour.');
    }).finally(() => {
      if (active) setLoading(false);
    });
    return () => { active = false; };
  }, [auth.initializing, auth.user]);
  const filter = TABS.find((t) => t[0] === tab)[2];
  const list = bookings.filter(filter).filter((b) => !lookup || b.code.includes(lookup.trim().toUpperCase()));

  return (
    <div className="container page">
      <h1><Ticket /> Đơn đặt tour của tôi</h1>
      {error && <p className="error-text" role="alert">{error} {!auth.user && <button type="button" className="btn-link" onClick={requestSignIn}>Đăng nhập</button>}</p>}
      <div className="lookup">
        <Search size={16} />
        <input type="search" placeholder="Tra cứu theo mã đặt tour (VD: BK261113...)" value={lookup} onChange={(e) => setLookup(e.target.value)} aria-label="Tra cứu mã đặt tour" />
      </div>
      <div className="cat-tabs" role="tablist">
        {TABS.map(([k, l, f]) => (
          <button key={k} type="button" role="tab" aria-selected={tab === k} className={tab === k ? 'active' : ''} onClick={() => setTab(k)}>
            {l} ({bookings.filter(f).length})
          </button>
        ))}
      </div>
      {loading ? <p className="muted">Đang tải đơn đặt tour…</p> : list.length === 0 ? (
        <div className="empty">Chưa có đơn nào. <Link to="/tours">Khám phá tour ngay →</Link></div>
      ) : (
        <div className="bk-list">
          {list.map((b) => {
            const st = STATUS[b.status] ?? { label: b.status };
            return (
              <Link key={b.code} to={`/booking/${b.code}`} className="bk-item">
                <img src={b.image} alt="" />
                <div className="bk-item-body">
                  <div className="bk-item-top"><b>{b.code}</b><span className={`status ${st.cls}`}>{st.label}</span></div>
                  <div className="tour-title">{b.tourTitle}</div>
                  <div className="muted small"><Calendar size={14} /> {b.departureDate ? fmtDate(b.departureDate) : 'Chưa có ngày'} · {b.adults + b.children + b.infants} khách · Đặt lúc {new Date(b.createdAt).toLocaleString('vi-VN')}</div>
                </div>
                <div className="bk-item-price">
                  <div className="price">{vnd(b.price.total)}</div>
                  <div className="muted small">Đã trả {vnd(b.paidAmount)}</div>
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}
