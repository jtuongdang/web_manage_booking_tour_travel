import { createContext, useContext, useEffect, useState } from 'react';

// Lưu booking tạm ở localStorage (mô phỏng backend). Sau này thay bằng API /api/v1/bookings
const BookingContext = createContext(null);
const KEY = 'vt_bookings';

export function BookingProvider({ children }) {
  const [bookings, setBookings] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem(KEY)) ?? [];
    } catch {
      return [];
    }
  });

  useEffect(() => {
    localStorage.setItem(KEY, JSON.stringify(bookings));
  }, [bookings]);

  const createBooking = (data) => {
    const now = new Date();
    const code = `BK${now.toISOString().slice(2, 10).replaceAll('-', '')}${String(Math.floor(Math.random() * 9000) + 1000)}`;
    const booking = { ...data, code, status: 'PendingPayment', paidAmount: 0, createdAt: now.toISOString(), payments: [] };
    setBookings((b) => [booking, ...b]);
    return booking;
  };

  const updateBooking = (code, patch) =>
    setBookings((list) => list.map((b) => (b.code === code ? { ...b, ...(typeof patch === 'function' ? patch(b) : patch) } : b)));

  const getBooking = (code) => bookings.find((b) => b.code === code);

  return <BookingContext.Provider value={{ bookings, createBooking, updateBooking, getBooking }}>{children}</BookingContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export const useBookings = () => useContext(BookingContext);
