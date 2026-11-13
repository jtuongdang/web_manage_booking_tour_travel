import { apiGet, apiPost, apiPut, toUiBooking } from './client';
import { toursApi } from './toursApi';

const tourMetaCache = new Map();

function loadTourMeta(tourId) {
  if (!tourMetaCache.has(tourId)) {
    const req = toursApi.get(String(tourId)).catch(() => null);
    tourMetaCache.set(tourId, req);
  }
  return tourMetaCache.get(tourId);
}

async function hydrate(booking) {
  const tour = await loadTourMeta(booking.tourId);
  const departure = tour?.departures?.find((item) => item.id === booking.departureId);
  return toUiBooking(booking, { tour, departure });
}

export const bookingsApi = {
  create: (request) => apiPost('/api/v1/bookings', request),
  mine: async () => Promise.all((await apiGet('/api/v1/bookings/my')).map(hydrate)),
  get: async (code, params = {}) => {
    const query = new URLSearchParams();
    if (params.phone) query.set('phone', params.phone);
    if (params.email) query.set('email', params.email);
    const qs = query.size ? `?${query}` : '';
    return hydrate(await apiGet(`/api/v1/bookings/${encodeURIComponent(code)}${qs}`));
  },
  updatePassengers: (id, passengers) => apiPut(`/api/v1/bookings/${id}/passengers`, passengers),
  cancel: (id, reason) => apiPost(`/api/v1/bookings/${id}/cancel-request`, { reason }),
  reschedule: (id, newDepartureId, reason) => apiPost(`/api/v1/bookings/${id}/reschedule`, { newDepartureId, reason }),
  invoice: (id) => apiGet(`/api/v1/bookings/${id}/invoice`),
  submitReview: (id, review) => apiPost(`/api/v1/reviews/bookings/${id}`, review),
};
