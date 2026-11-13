import { useEffect, useState } from 'react';
import { Link, NavLink } from 'react-router-dom';
import { ChevronDown, Compass, Percent, Smartphone, Ticket, User } from 'lucide-react';
import { ApiError } from '../api/client';
import { clearAuthMessage, useAuth, useAuthActions } from '../store/AuthContext';

export default function Header() {
  const auth = useAuth();
  const { signIn, signUp, signOut } = useAuthActions();
  const [dialog, setDialog] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (auth.error === 'signin-required') {
      setDialog('login');
      setError('');
      clearAuthMessage();
    }
  }, [auth.error]);

  const submitAuth = async (event) => {
    event.preventDefault();
    setBusy(true);
    setError('');
    const form = new FormData(event.currentTarget);
    try {
      if (dialog === 'register') {
        await signUp({
          fullName: form.get('fullName'),
          email: form.get('email'),
          phone: form.get('phone'),
          password: form.get('password'),
        });
      } else {
        await signIn({ usernameOrEmail: form.get('usernameOrEmail'), password: form.get('password') });
      }
      setDialog('');
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Không thể xác thực. Vui lòng thử lại.');
    } finally {
      setBusy(false);
    }
  };

  const staff = auth.user?.roles?.some((role) => ['SALES', 'OPERATOR', 'GUIDE', 'ACCOUNTANT', 'ADMIN'].includes(role));

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
            {auth.user ? (
              <>
                <span className="util-item">{auth.user.fullName}</span>
                {staff && <Link to="/portal" className="btn-tvk-login">Cổng Nội Bộ</Link>}
                <button type="button" className="btn-tvk-register" onClick={async () => {
                  try {
                    await signOut();
                  } catch (logoutError) {
                    setError(logoutError instanceof ApiError ? logoutError.message : 'Không thể đăng xuất.');
                    setDialog('login');
                  }
                }}>Đăng xuất</button>
              </>
            ) : (
              <>
                <button type="button" className="btn-tvk-login" onClick={() => { setDialog('login'); setError(''); }}><User size={15} /> Đăng nhập</button>
                <button type="button" className="btn-tvk-register" onClick={() => { setDialog('register'); setError(''); }}>Đăng ký</button>
              </>
            )}
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

      {dialog && (
        <dialog open className="modal" aria-labelledby="auth-title">
          <h2 id="auth-title">{dialog === 'register' ? 'Tạo tài khoản VietTour' : 'Đăng nhập VietTour'}</h2>
          <form onSubmit={submitAuth}>
            {dialog === 'register' ? (
              <>
                <div className="field"><label htmlFor="auth-name">Họ và tên</label><input id="auth-name" name="fullName" autoComplete="name" required /></div>
                <div className="field"><label htmlFor="auth-email">Email</label><input id="auth-email" name="email" type="email" autoComplete="email" required /></div>
                <div className="field"><label htmlFor="auth-phone">Số điện thoại</label><input id="auth-phone" name="phone" type="tel" autoComplete="tel" required /></div>
                <div className="field"><label htmlFor="auth-password">Mật khẩu</label><input id="auth-password" name="password" type="password" autoComplete="new-password" minLength="8" required /></div>
              </>
            ) : (
              <>
                <div className="field"><label htmlFor="auth-identity">Tên đăng nhập hoặc email</label><input id="auth-identity" name="usernameOrEmail" autoComplete="username" required /></div>
                <div className="field"><label htmlFor="auth-password">Mật khẩu</label><input id="auth-password" name="password" type="password" autoComplete="current-password" required /></div>
              </>
            )}
            {error && <p className="error-text" role="alert">{error}</p>}
            <div className="modal-actions">
              <button type="button" className="btn btn-outline" onClick={() => setDialog('')}>Đóng</button>
              <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? 'Đang xử lý…' : dialog === 'register' ? 'Đăng ký' : 'Đăng nhập'}</button>
            </div>
            <button type="button" className="btn-link" onClick={() => { setDialog(dialog === 'register' ? 'login' : 'register'); setError(''); }}>
              {dialog === 'register' ? 'Đã có tài khoản? Đăng nhập' : 'Chưa có tài khoản? Đăng ký'}
            </button>
          </form>
        </dialog>
      )}
    </header>
  );
}
