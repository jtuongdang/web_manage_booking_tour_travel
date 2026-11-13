import { useEffect } from 'react';
import { NavLink, Navigate, Outlet, useLocation } from 'react-router-dom';
import { LayoutDashboard, BriefcaseBusiness, Compass, ClipboardCheck, Wallet, LogOut, Globe, ShieldCheck } from 'lucide-react';
import { useAuth, useAuthActions } from '../store/AuthContext';

const workspaces = {
  ADMIN: { name: 'Quản trị', path: '/admin', icon: ShieldCheck },
  SALES: { name: 'Kinh doanh', path: '/sales', icon: BriefcaseBusiness },
  OPERATOR: { name: 'Điều hành', path: '/operator', icon: Compass },
  GUIDE: { name: 'Hướng dẫn viên', path: '/guide', icon: ClipboardCheck },
  ACCOUNTANT: { name: 'Kế toán', path: '/accountant', icon: Wallet },
};

export function PortalGuard({ children }) {
  const { user, initializing } = useAuth();
  const location = useLocation();
  if (initializing) return <div className="portal-loading">Đang kiểm tra phiên đăng nhập…</div>;
  if (!user) return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  const role = user.roles?.find((item) => workspaces[item]);
  if (!role) return <Navigate to="/" replace />;
  if (location.pathname !== workspaces[role].path) return <Navigate to={workspaces[role].path} replace />;
  return children;
}

export default function PortalLayout() {
  const { user } = useAuth();
  const { signOut } = useAuthActions();
  const role = user?.roles?.find((item) => workspaces[item]);
  const active = workspaces[role];
  useEffect(() => { document.body.classList.add('portal-mode'); return () => document.body.classList.remove('portal-mode'); }, []);
  return <div className="portal-frame">
    <aside className="portal-sidebar">
      <NavLink to="/" className="portal-brand"><span className="brand-mark">V</span><span>VietTour<small>OPERATIONS</small></span></NavLink>
      <div className="portal-side-label">KHÔNG GIAN LÀM VIỆC</div>
      <NavLink to={active.path} className="portal-nav active"><active.icon size={18} />{active.name}<span className="nav-dot" /></NavLink>
      <div className="portal-side-note"><LayoutDashboard size={17} /><span>Hệ thống quản lý tour<br /><b>Single tenant · Nội bộ</b></span></div>
      <div className="portal-user"><div className="user-avatar">{user?.fullName?.slice(0, 1) || 'U'}</div><div className="user-meta"><b>{user?.fullName || user?.username}</b><small>{role}</small></div><button title="Đăng xuất" onClick={() => signOut()}><LogOut size={17} /></button></div>
    </aside>
    <section className="portal-main"><header className="portal-topbar"><div className="portal-breadcrumb">VietTour <span>/</span> {active.name}</div><div className="portal-top-actions"><span className="portal-live"><i /> Môi trường phát triển</span><a href="/" className="portal-public-link"><Globe size={16} /> Trang khách hàng</a></div></header><div className="portal-content"><Outlet /></div></section>
  </div>;
}
