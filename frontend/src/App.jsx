import { useEffect } from 'react';
import { BrowserRouter, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import Header from './components/Header';
import Footer from './components/Footer';
import Home from './pages/Home';
import Tours from './pages/Tours';
import TourDetail from './pages/TourDetail';
import Checkout from './pages/Checkout';
import Payment from './pages/Payment';
import BookingDetail from './pages/BookingDetail';
import MyBookings from './pages/MyBookings';
import { BookingProvider } from './store/BookingContext';
import { useAuth } from './store/AuthContext';
import { PortalGuard } from './portal/PortalLayout';
import PortalLayout from './portal/PortalLayout';
import PortalLogin from './portal/PortalLogin';
import AdminDashboard from './portal/AdminDashboard';
import SalesPortal from './portal/SalesPortal';
import OperatorPortal from './portal/OperatorPortal';
import GuidePortal from './portal/GuidePortal';
import AccountantPortal from './portal/AccountantPortal';

function SiteChrome({ children }) {
  const { pathname } = useLocation();
  const portal = ['/portal', '/admin', '/sales', '/operator', '/guide', '/accountant'].some((path) => pathname === path || pathname.startsWith(`${path}/`));
  return <><div className={portal ? 'site-chrome is-hidden' : 'site-chrome'}><Header /></div>{children}<div className={portal ? 'site-chrome is-hidden' : 'site-chrome'}><Footer /></div></>;
}

function PortalIndex() {
  const { user, initializing } = useAuth();
  if (initializing) return <div className="portal-loading">Đang kiểm tra phiên đăng nhập…</div>;
  const role = user?.roles?.find((item) => ['ADMIN', 'SALES', 'OPERATOR', 'GUIDE', 'ACCOUNTANT'].includes(item));
  return <Navigate to={role ? `/${role.toLowerCase()}` : '/login'} replace />;
}

function ScrollTop() {
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);
  return null;
}

export default function App() {
  return (
    <BookingProvider>
      <BrowserRouter>
        <ScrollTop />
        <SiteChrome><main>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/tours" element={<Tours />} />
            <Route path="/tours/:slug" element={<TourDetail />} />
            <Route path="/checkout/:slug" element={<Checkout />} />
            <Route path="/payment/:code" element={<Payment />} />
            <Route path="/booking/:code" element={<BookingDetail />} />
            <Route path="/my-bookings" element={<MyBookings />} />
            <Route path="/login" element={<PortalLogin />} />
            <Route path="/register" element={<PortalLogin register />} />
            <Route path="/portal" element={<PortalIndex />} />
            <Route element={<PortalGuard><PortalLayout /></PortalGuard>}>
              <Route path="/admin" element={<AdminDashboard />} />
              <Route path="/sales" element={<SalesPortal />} />
              <Route path="/operator" element={<OperatorPortal />} />
              <Route path="/guide" element={<GuidePortal />} />
              <Route path="/accountant" element={<AccountantPortal />} />
            </Route>
            <Route path="*" element={<div className="container page empty">404 — Không tìm thấy trang</div>} />
          </Routes>
        </main></SiteChrome>
      </BrowserRouter>
    </BookingProvider>
  );
}
