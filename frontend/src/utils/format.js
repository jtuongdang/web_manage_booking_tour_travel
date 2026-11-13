export const VAT_RATE = 0.08; // Thuế VAT mô phỏng (giá đã bao gồm VAT)
export const HOLD_MINUTES = 15; // Thời gian giữ chỗ
export const DEPOSIT_PERCENT = 30;

export const vnd = (n) => new Intl.NumberFormat('vi-VN').format(Math.round(n)) + ' ₫';

const dateFmt = new Intl.DateTimeFormat('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit', year: 'numeric' });
// Ngày dạng YYYY-MM-DD: tạo Date theo giờ địa phương để không lệch ngày do múi giờ
export const fmtDate = (iso) => {
  const [y, m, d] = iso.split('-').map(Number);
  return dateFmt.format(new Date(y, m - 1, d));
};

export const daysUntil = (iso) => {
  const [y, m, d] = iso.split('-').map(Number);
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  return Math.round((new Date(y, m - 1, d) - start) / 86400000);
};

export const cancellationFeePercent = (days) => {
  if (days >= 15) return 10;
  if (days >= 7) return 30;
  if (days >= 3) return 50;
  return 100;
};

export const calcPrice = ({ tour, departure, adults, children, infants, singleRooms, promo }) => {
  const adultPrice = departure.adultPrice;
  const childPrice = Math.round((adultPrice * tour.childPercent) / 100 / 1000) * 1000;
  const subTotal = adults * adultPrice + children * childPrice + infants * tour.infantPrice + singleRooms * tour.singleSupplement;
  let discount = 0;
  if (promo) {
    if (promo.type === 'percent') discount = Math.min((subTotal * promo.value) / 100, promo.max ?? Infinity);
    if (promo.type === 'fixed') discount = promo.value;
  }
  const total = Math.max(0, subTotal - discount);
  const amountBeforeTax = total / (1 + VAT_RATE);
  const vatAmount = total - amountBeforeTax;
  return { adultPrice, childPrice, subTotal, discount, total, amountBeforeTax, vatAmount, deposit: Math.round((total * DEPOSIT_PERCENT) / 100 / 1000) * 1000 };
};
