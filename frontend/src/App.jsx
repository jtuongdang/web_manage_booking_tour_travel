import { useEffect } from 'react';
import { BrowserRouter, Route, Routes, useLocation } from 'react-router-dom';
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
        <Header />
        <main>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/tours" element={<Tours />} />
            <Route path="/tours/:slug" element={<TourDetail />} />
            <Route path="/checkout/:slug" element={<Checkout />} />
            <Route path="/payment/:code" element={<Payment />} />
            <Route path="/booking/:code" element={<BookingDetail />} />
            <Route path="/my-bookings" element={<MyBookings />} />
            <Route path="*" element={<div className="container page empty">404 — Không tìm thấy trang</div>} />
          </Routes>
        </main>
        <Footer />
      </BrowserRouter>
    </BookingProvider>
  );
}
