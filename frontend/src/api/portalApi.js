import { apiGet, apiPatch, apiPost, apiPut } from './client';

export const portalApi = {
  get: (path) => apiGet(path),
  post: (path, body) => apiPost(path, body),
  put: (path, body) => apiPut(path, body),
  patch: (path, body) => apiPatch(path, body),
};
