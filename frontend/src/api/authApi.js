import { apiGet, apiPost } from './client';

export const authApi = {
  login: (credentials) => apiPost('/api/v1/auth/login', credentials),
  register: (details) => apiPost('/api/v1/auth/register', details),
  me: () => apiGet('/api/v1/auth/me'),
  refresh: (refreshToken) => apiPost('/api/v1/auth/refresh-token', { refreshToken }),
  logout: (refreshToken) => apiPost('/api/v1/auth/logout', { refreshToken }),
};
