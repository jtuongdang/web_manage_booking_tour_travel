import { apiGet, apiPost, apiPut, toUiBooking } from './client';
import { toursApi } from './toursApi';

async function hydrate(booking) {
  const [tour, departures] = await Promise.all([
    toursApi.get(String(booking.tourId)),
    toursApi.departures(booking.tourId),
  ]);
  const departure = departures.find((item) => item.id === booking.departureId);
  return toUiBooking(booking, { tour, departure });
}

export const bookingsApi = {
  create: (request) => apiPost('/api/v1/bookings', request),
  mine: async () => Promise.all((await apiGet('/api/v1/bookings/my')).map(hydrate)),
  get: async (code) => hydrate(await apiGet(`/api/v1/bookings/${encodeURIComponent(code)}`)),
  updatePassengers: (id, passengers) => apiPut(`/api/v1/bookings/${id}/passengers`, passengers),
  cancel: (id, reason) => apiPost(`/api/v1/bookings/${id}/cancel-request`, { reason }),
  reschedule: (id, newDepartureId, reason) => apiPost(`/api/v1/bookings/${id}/reschedule`, { newDepartureId, reason }),
  invoice: (id) => apiGet(`/api/v1/bookings/${id}/invoice`),
  submitReview: (id, review) => apiPost(`/api/v1/reviews/bookings/${id}`, review),
};
