import { apiGet, apiPost } from './client';

export const paymentsApi = {
  createQr: (booking, mode) => apiPost('/api/v1/payments/qr-create', {
    bookingId: booking.id,
    bookingCode: booking.bookingCode,
    mode,
  }),
  status: (transactionCode) => apiGet(`/api/v1/payments/${encodeURIComponent(transactionCode)}/status`),
  simulateDemo: (transactionCode, scenario = 'Success', paidAmount) => apiPost('/api/v1/payments/qr-simulate-demo', {
    transactionCode,
    scenario,
    paidAmount,
    idempotencyKey: crypto.randomUUID(),
  }),
};
