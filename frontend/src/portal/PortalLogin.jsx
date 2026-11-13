import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { ArrowLeft, BriefcaseBusiness, Compass, ShieldCheck, Wallet, UserRoundCheck } from 'lucide-react';
import { useAuthActions } from '../store/AuthContext';

const demos = [
  { username: 'admin', label: 'Quản trị viên', role: 'ADMIN', Icon: ShieldCheck },
  { username: 'sales', label: 'Kinh doanh', role: 'SALES', Icon: BriefcaseBusiness },
  { username: 'operator', label: 'Điều hành', role: 'OPERATOR', Icon: Compass },
  { username: 'guide', label: 'Hướng dẫn viên', role: 'GUIDE', Icon: UserRoundCheck },
  { username: 'accountant', label: 'Kế toán', role: 'ACCOUNTANT', Icon: Wallet },
  { username: 'customer', label: 'Khách hàng', role: 'CUSTOMER', Icon: UserRoundCheck },
];
const destination = (user) => `/${user?.roles?.find((role) => ['ADMIN', 'SALES', 'OPERATOR', 'GUIDE', 'ACCOUNTANT'].includes(role))?.toLowerCase() || ''}`;

export default function PortalLogin({ register = false }) {
  const { signIn, signUp } = useAuthActions(); const navigate = useNavigate(); const location = useLocation();
  const [form, setForm] = useState({ usernameOrEmail: '', password: '', fullName: '', email: '', phone: '' }); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const submit = async (credentials = form) => { setBusy(true); setError(''); try { const user = register ? await signUp({ ...form, username: form.email?.split('@')[0] }) : await signIn(credentials); navigate(location.state?.from || destination(user) || '/', { replace: true }); } catch (e) { setError(e.message || 'Không thể đăng nhập. Kiểm tra tài khoản và thử lại.'); } finally { setBusy(false); } };
  return <div className="login-page"><div className="login-back"><Link to="/"><ArrowLeft size={16} /> Về trang khách hàng</Link></div><div className="login-card"><Link className="portal-brand login-brand" to="/"><span className="brand-mark">V</span><span>VietTour<small>OPERATIONS</small></span></Link><div className="portal-eyebrow">{register ? 'TẠO TÀI KHOẢN' : 'WORKSPACE ACCESS'}</div><h1>{register ? 'Đăng ký khách hàng' : 'Đăng nhập hệ thống'}</h1><p className="login-intro">{register ? 'Tạo tài khoản để quản lý hành trình của bạn.' : 'Đăng nhập để tiếp tục tới không gian làm việc VietTour.'}</p>
    <form className="portal-form login-form" onSubmit={(e) => { e.preventDefault(); submit(); }}>{register && <><label className="portal-field"><span>Họ và tên</span><input required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></label><label className="portal-field"><span>Email</span><input type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></label><label className="portal-field"><span>Số điện thoại</span><input required value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></label></>}
      {!register && <label className="portal-field"><span>Tên đăng nhập hoặc email</span><input required autoComplete="username" value={form.usernameOrEmail} onChange={(e) => setForm({ ...form, usernameOrEmail: e.target.value })} placeholder="username hoặc email" /></label>}
      <label className="portal-field"><span>Mật khẩu</span><input required type="password" autoComplete={register ? 'new-password' : 'current-password'} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /></label>
      {error && <p className="portal-login-error" role="alert">{error}</p>}<button className="portal-button primary login-submit" disabled={busy}>{busy ? 'Đang xác thực…' : register ? 'Tạo tài khoản' : 'Đăng nhập'}</button>
    </form>
    {!register && <><div className="demo-heading"><span>TÀI KHOẢN DEMO PHÁT TRIỂN</span><i /></div><div className="demo-grid">{demos.map(({ username, label, role, Icon }) => <button className="demo-account" key={username} disabled={busy} onClick={() => submit({ usernameOrEmail: username, password: 'VietTour@2026!' })}><Icon size={17} /><span><b>{label}</b><small>{username} · {role}</small></span><span className="demo-arrow">↗</span></button>)}</div><p className="demo-footnote">Dùng cho môi trường Development: mật khẩu chung <code>VietTour@2026!</code></p></>}
    <div className="login-switch">{register ? 'Đã có tài khoản?' : 'Bạn là khách hàng mới?'} <Link to={register ? '/login' : '/register'}>{register ? 'Đăng nhập' : 'Tạo tài khoản'}</Link></div>
  </div></div>;
}
