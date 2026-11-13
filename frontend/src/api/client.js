const ACCESS_TOKEN_KEY = 'vt_access_token';
const REFRESH_TOKEN_KEY = 'vt_refresh_token';

let refreshRequest;
const tokenListeners = new Set();

export const getAccessToken = () => localStorage.getItem(ACCESS_TOKEN_KEY);
export const getRefreshToken = () => localStorage.getItem(REFRESH_TOKEN_KEY);

export function setAuthTokens(tokens) {
  localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
  tokenListeners.forEach((listener) => listener());
}

export function clearAuthTokens() {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  tokenListeners.forEach((listener) => listener());
}

export function onTokenChange(listener) {
  tokenListeners.add(listener);
  return () => {
    tokenListeners.delete(listener);
  };
}

export class ApiError extends Error {
  constructor(message, { status, title, detail, type, errors, extensions } = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.type = type;
    this.errors = errors;
    this.extensions = extensions;
  }
}

async function parseResponse(response) {
  if (response.status === 204) return null;
  const contentType = response.headers.get('content-type') ?? '';
  if (contentType.includes('application/json') || contentType.includes('+json')) {
    return response.json();
  }
  return response.text();
}

async function problemError(response) {
  const body = await parseResponse(response);
  const title = body && typeof body === 'object' ? body.title : undefined;
  const detail = body && typeof body === 'object' ? body.detail : undefined;
  const message = detail || title || (typeof body === 'string' && body) || `API request failed (${response.status}).`;
  return new ApiError(message, {
    status: response.status,
    title,
    detail,
    type: body?.type,
    errors: body?.errors,
    extensions: body && typeof body === 'object' ? body : undefined,
  });
}

async function renewAccessToken() {
  if (!refreshRequest) {
    refreshRequest = (async () => {
      const refreshToken = getRefreshToken();
      if (!refreshToken) {
        clearAuthTokens();
        throw new ApiError('Your session has expired. Please sign in again.', { status: 401 });
      }
      const response = await fetch('/api/v1/auth/refresh-token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      });
      if (!response.ok) {
        clearAuthTokens();
        throw await problemError(response);
      }
      const tokens = await response.json();
      setAuthTokens(tokens);
      return tokens.accessToken;
    })().finally(() => {
      refreshRequest = undefined;
    });
  }
  return refreshRequest;
}

export async function apiRequest(path, { method = 'GET', body, headers = {}, responseType = 'json', retry = true } = {}) {
  const requestHeaders = new Headers(headers);
  if (body !== undefined && !(body instanceof FormData)) requestHeaders.set('Content-Type', 'application/json');
  const token = getAccessToken();
  if (token) requestHeaders.set('Authorization', `Bearer ${token}`);

  const response = await fetch(path, {
    method,
    headers: requestHeaders,
    body: body === undefined ? undefined : body instanceof FormData ? body : JSON.stringify(body),
  });

  if (response.status === 401 && retry && !path.includes('/auth/refresh-token')) {
    try {
      await renewAccessToken();
      return apiRequest(path, { method, body, headers, responseType, retry: false });
    } catch (error) {
      if (error instanceof ApiError) throw error;
      throw new ApiError('Your session has expired. Please sign in again.', { status: 401 });
    }
  }

  if (!response.ok) throw await problemError(response);
  if (responseType === 'blob') return response.blob();
  if (responseType === 'text') return response.text();
  return parseResponse(response);
}

export const apiGet = (path, options) => apiRequest(path, options);
export const apiPost = (path, body, options = {}) => apiRequest(path, { ...options, method: 'POST', body });
export const apiPut = (path, body, options = {}) => apiRequest(path, { ...options, method: 'PUT', body });
export const apiPatch = (path, body, options = {}) => apiRequest(path, { ...options, method: 'PATCH', body });

export function toUiBooking(booking, { tour, departure } = {}) {
  const total = Number(booking.totalAmount ?? 0);
  return {
    ...booking,
    code: booking.bookingCode,
    status: booking.status,
    tourSlug: tour?.slug ?? String(booking.tourId),
    tourTitle: tour?.title ?? `Tour #${booking.tourId}`,
    tourCode: tour?.code ?? '',
    image: tour?.images?.[0] ?? tour?.coverImageUrl ?? '/images/hero-beach.jpg',
    departureDate: departure?.departureDate ?? booking.departureDate ?? '',
    adults: booking.adultCount ?? 0,
    children: booking.childCount ?? 0,
    infants: booking.infantCount ?? 0,
    singleRooms: booking.singleRoomCount ?? 0,
    contact: {
      name: booking.contactName,
      phone: booking.contactPhone,
      email: booking.contactEmail,
      note: booking.specialNote,
    },
    passengers: booking.passengers ?? [],
    payments: booking.payments ?? [],
    paidAmount: Number(booking.paidAmount ?? 0),
    promoCode: booking.promoCodeApplied,
    holdExpiresAt: booking.holdExpiresAt,
    invoice: booking.requireVatInvoice ? {
      company: booking.invoiceCompanyName,
      taxCode: booking.invoiceTaxCode,
      address: booking.invoiceAddress,
      email: booking.invoiceEmail,
    } : null,
    price: {
      subTotal: Number(booking.subTotal ?? total),
      discount: Number(booking.discountAmount ?? 0),
      amountBeforeTax: Number(booking.amountBeforeTax ?? 0),
      vatAmount: Number(booking.vatAmount ?? 0),
      total,
      deposit: Number(booking.requiredDepositAmount ?? 0),
    },
  };
}
