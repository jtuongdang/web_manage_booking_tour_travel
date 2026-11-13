import { createContext, useContext, useEffect, useSyncExternalStore } from 'react';
import { authApi } from '../api/authApi';
import { clearAuthTokens, getRefreshToken, onTokenChange, setAuthTokens } from '../api/client';

export const AuthContext = createContext(null);
let state = { user: null, initializing: true, error: null };
const listeners = new Set();
let initialization;

function publish(next) {
  state = { ...state, ...next };
  listeners.forEach((listener) => listener());
}

function subscribe(listener) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

onTokenChange(() => {
  if (!getRefreshToken()) publish({ user: null, initializing: false });
});

async function loadCurrentUser() {
  const user = await authApi.me();
  publish({ user, initializing: false, error: null });
  return user;
}

export async function initializeAuth() {
  if (!initialization) {
    initialization = (async () => {
      if (!getRefreshToken()) {
        publish({ initializing: false });
        return null;
      }
      try {
        await authApi.refresh(getRefreshToken()).then(setAuthTokens);
        return await loadCurrentUser();
      } catch {
        clearAuthTokens();
        publish({ user: null, initializing: false });
        return null;
      }
    })().finally(() => {
      initialization = undefined;
    });
  }
  return initialization;
}

export async function signIn(credentials) {
  const tokens = await authApi.login(credentials);
  setAuthTokens(tokens);
  try {
    return await loadCurrentUser();
  } catch (error) {
    clearAuthTokens();
    publish({ user: null, initializing: false });
    throw error;
  }
}

export async function signUp(details) {
  const tokens = await authApi.register(details);
  setAuthTokens(tokens);
  try {
    return await loadCurrentUser();
  } catch (error) {
    clearAuthTokens();
    publish({ user: null, initializing: false });
    throw error;
  }
}

export async function signOut() {
  const refreshToken = getRefreshToken();
  try {
    if (refreshToken) await authApi.logout(refreshToken);
  } finally {
    clearAuthTokens();
    publish({ user: null, initializing: false });
  }
}

export function requestSignIn() {
  publish({ error: 'signin-required' });
}

export function clearAuthMessage() {
  publish({ error: null });
}

export function AuthProvider({ children }) {
  useEffect(() => {
    initializeAuth();
  }, []);
  const value = useSyncExternalStore(subscribe, () => state, () => state);
  return <AuthContext.Provider value={{ ...value, signIn, signUp, signOut }}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  useEffect(() => {
    initializeAuth();
  }, []);
  return useSyncExternalStore(subscribe, () => state, () => state);
}

export const useAuthActions = () => useContext(AuthContext) ?? { signIn, signUp, signOut };
