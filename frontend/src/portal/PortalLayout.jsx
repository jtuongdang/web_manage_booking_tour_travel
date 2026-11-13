import { useEffect } from 'react';
import { NavLink, Navigate, Outlet, useLocation, useSearchParams } from 'react-router-dom';
import {
  LayoutDashboard,
  BriefcaseBusiness,
  Compass,
  ClipboardCheck,
  Wallet,
  LogOut,
  Globe,
  ShieldCheck,
  BarChart3,
  Users,
  TicketPercent,
  Landmark,
  SlidersHorizontal,
  FileClock,
} from 'lucide-react';
import { useAuth, useAuthActions } from '../store/AuthContext';

const workspaces = {
  ADMIN: { name: 'Quản trị hệ thống', path: '/admin', icon: ShieldCheck },
  SALES: { name: 'Kinh doanh & Booking', path: '/sales', icon: BriefcaseBusiness },
  OPERATOR: { name: 'Điều hành Tour', path: '/operator', icon: Compass },
  GUIDE: { name: 'Hướng dẫn viên', path: '/guide', icon: ClipboardCheck },
  ACCOUNTANT: { name: 'Kế toán & Đối soát', path: '/accountant', icon: Wallet },
};

const ADMIN_TABS = [
  { id: 'overview', label: 'Tổng quan & Báo cáo', icon: BarChart3 },
  { id: 'staff', label: 'Nhân sự & Phân quyền', icon: Users },
  { id: 'promotions', label: 'Khuyến mãi & Ưu đãi', icon: TicketPercent },
  { id: 'banks', label: 'Tài khoản VietQR', icon: Landmark },
  { id: 'settings', label: 'Cấu hình Hệ thống', icon: SlidersHorizontal },
  { id: 'audit', label: 'Nhật ký Kiểm toán', icon: FileClock },
];

export function PortalGuard({ children }) {
  const { user, initializing } = useAuth();
  const location = useLocation();
  if (initializing) return <div className="portal-loading">Đang kiểm tra phiên đăng nhập…</div>;
  if (!user) return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  const role = user.roles?.find((item) => workspaces[item]);
  if (!role) return <Navigate to="/" replace />;
  const allowedPaths = role === 'ADMIN'
    ? Object.values(workspaces).map((w) => w.path)
    : [workspaces[role].path];
  if (!allowedPaths.includes(location.pathname)) return <Navigate to={workspaces[role].path} replace />;
  return children;
}

export default function PortalLayout() {
  const { user } = useAuth();
  const { signOut } = useAuthActions();
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();
  const role = user?.roles?.find((item) => workspaces[item]);
  const currentWorkspace = Object.values(workspaces).find((w) => w.path === location.pathname) || workspaces[role];
  const activeAdminTab = searchParams.get('tab') || 'overview';
  const activeAdminTabMeta = ADMIN_TABS.find((t) => t.id === activeAdminTab) || ADMIN_TABS[0];

  useEffect(() => {
    document.body.classList.add('portal-mode');
    return () => document.body.classList.remove('portal-mode');
  }, []);

  const visibleWorkspaces = role === 'ADMIN' ? Object.entries(workspaces) : [[role, workspaces[role]]];

  return (
    <div className="portal-frame">
      <aside className="portal-sidebar">
        <NavLink to="/" className="portal-brand">
          <span className="brand-mark">V</span>
          <span>VietTour<small>OPERATIONS</small></span>
        </NavLink>

        {location.pathname === '/admin' && (
          <div className="portal-side-group">
            <div className="portal-side-label">MODULE QUẢN TRỊ</div>
            {ADMIN_TABS.map((tab) => {
              const Icon = tab.icon;
              const isSelected = activeAdminTab === tab.id;
              return (
                <button
                  key={tab.id}
                  type="button"
                  className={`portal-nav portal-nav-button ${isSelected ? 'active' : ''}`}
                  onClick={() => setSearchParams({ tab: tab.id })}
                >
                  <Icon size={17} />
                  <span>{tab.label}</span>
                  {isSelected && <span className="nav-dot" />}
                </button>
              );
            })}
          </div>
        )}

        <div className="portal-side-group">
          <div className="portal-side-label">{role === 'ADMIN' ? 'PHÂN HỆ NGHIỆP VỤ' : 'KHÔNG GIAN LÀM VIỆC'}</div>
          {visibleWorkspaces.map(([key, ws]) => {
            const Icon = ws.icon;
            const isActive = location.pathname === ws.path;
            return (
              <NavLink key={key} to={ws.path} className={`portal-nav ${isActive ? 'active' : ''}`}>
                <Icon size={17} />
                <span>{ws.name}</span>
                {isActive && <span className="nav-dot" />}
              </NavLink>
            );
          })}
        </div>

        <div className="portal-side-note">
          <LayoutDashboard size={17} />
          <span>Hệ thống quản lý tour<br /><b>Single tenant · Nội bộ</b></span>
        </div>

        <div className="portal-user">
          <div className="user-avatar">{user?.fullName?.slice(0, 1) || 'U'}</div>
          <div className="user-meta">
            <b>{user?.fullName || user?.username}</b>
            <small>{role}</small>
          </div>
          <button type="button" title="Đăng xuất" onClick={() => signOut()}>
            <LogOut size={17} />
          </button>
        </div>
      </aside>

      <section className="portal-main">
        <header className="portal-topbar">
          <div className="portal-breadcrumb">
            VietTour <span>/</span> {currentWorkspace?.name}
            {location.pathname === '/admin' && (
              <>
                <span>/</span>
                <strong>{activeAdminTabMeta.label}</strong>
              </>
            )}
          </div>
          <div className="portal-top-actions">
            <span className="portal-live"><i /> Hệ thống Trực tuyến</span>
            <a href="/" className="portal-public-link"><Globe size={16} /> Trang khách hàng</a>
          </div>
        </header>
        <div className="portal-content"><Outlet /></div>
      </section>
    </div>
  );
}
