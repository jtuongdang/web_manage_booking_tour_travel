import { apiGet, apiPost } from './client';

const listValue = (value) => {
  if (Array.isArray(value)) return value;
  if (!value) return [];
  try {
    const parsed = JSON.parse(value);
    if (Array.isArray(parsed)) return parsed;
  } catch {
    return String(value).split(',').map((item) => item.trim()).filter(Boolean);
  }
  return [];
};

const toDeparture = (departure) => ({
  ...departure,
  departureDate: departure.departureDate,
  capacity: departure.totalCapacity,
  available: departure.availableSlots ?? departure.totalCapacity - departure.heldSlots - departure.bookedSlots,
  adultPrice: Number(departure.adultPrice),
});

export function toTourCard(tour) {
  const tags = listValue(tour.tags);
  const scope = String(tour.scopeType ?? '').toLowerCase();
  if (scope === 'domestic' && !tags.includes('domestic')) tags.push('domestic');
  if (scope === 'international' && !tags.includes('international')) tags.push('international');
  return {
    ...tour,
    tags,
    destination: tour.destination,
    price: Number(tour.baseAdultPrice),
    originalPrice: Number(tour.originalPrice ?? tour.baseAdultPrice),
    durationNights: Math.max(0, Number(tour.durationDays) - 1),
    rating: Number(tour.averageRating ?? 0),
    images: [tour.coverImageUrl || '/images/hero-beach.jpg'],
    departures: tour.nextDepartureId ? [{
      id: tour.nextDepartureId,
      departureDate: tour.nextDepartureDate,
      available: Number(tour.availableSlots ?? 0),
    }] : [],
  };
}

export function toTourDetail(result) {
  const source = result.tour ?? result;
  const tour = toTourCard({
    ...source,
    baseAdultPrice: source.baseAdultPrice,
    averageRating: result.reviewsSummary?.averageRating ?? source.averageRating,
    reviewCount: result.reviewsSummary?.reviewCount ?? source.reviewCount,
    coverImageUrl: result.images?.find((image) => image.isCover)?.imageUrl ?? result.images?.[0]?.imageUrl,
  });
  const parseList = (value) => listValue(value);
  const departures = (result.upcomingDepartures ?? []).map(toDeparture);
  return {
    ...tour,
    images: (result.images ?? []).map((image) => image.imageUrl).filter(Boolean).length
      ? (result.images ?? []).map((image) => image.imageUrl).filter(Boolean)
      : tour.images,
    highlights: parseList(source.highlightsJson),
    includes: parseList(source.includesJson),
    excludes: parseList(source.excludesJson),
    itinerary: (result.itineraries ?? []).map((item) => ({
      day: item.dayNumber,
      title: item.title,
      meals: item.meals ?? '—',
      description: item.description,
    })),
    departures,
    bookedCount: Number(source.bookedCountTotal ?? source.bookedCount ?? 0),
    childPercent: Number(source.childPricePercent ?? 75),
    infantPrice: Number(source.infantPrice ?? 0),
    singleSupplement: Number(source.singleSupplement ?? 0),
    freeCancelDays: Number(result.cancellationPolicy?.rules?.[0]?.minDaysBeforeDeparture ?? 0),
    reviewBreakdown: result.reviewsSummary ?? null,
    reviews: (result.reviews ?? []).map((item) => ({
      id: item.id,
      name: item.customerName || 'Khách VietTour',
      rating: Number(item.overallRating ?? 5),
      content: item.comment,
      reply: item.staffReply,
      verified: true,
      date: item.createdAt ? new Date(item.createdAt).toLocaleDateString('vi-VN') : '',
    })),
    policy: result.cancellationPolicy,
  };
}

export const toursApi = {
  list: (params = {}) => {
    const query = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') query.set(key, value);
    });
    return apiGet(`/api/v1/tours${query.size ? `?${query}` : ''}`).then((result) => ({
      ...result,
      items: result.items.map(toTourCard),
    }));
  },
  get: (slug) => apiGet(`/api/v1/tours/${encodeURIComponent(slug)}`).then(toTourDetail),
  departures: (tourId) => apiGet(`/api/v1/tours/${tourId}/departures`).then((items) => items.map(toDeparture)),
};

export const promotionsApi = {
  list: () => apiGet('/api/v1/promotions'),
  validate: (request) => apiPost('/api/v1/promotions/validate', request),
};
