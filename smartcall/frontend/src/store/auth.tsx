import { createContext, useContext, useState, ReactNode } from 'react';
import { setToken } from '../api/client';
import type { AuthResult } from '../api/types';

interface AuthState {
  user: AuthResult | null;
  login: (result: AuthResult) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthState>({ user: null, login: () => {}, logout: () => {} });

const USER_KEY = 'smartcall.user';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthResult | null>(() => {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthResult) : null;
  });

  const login = (result: AuthResult) => {
    setToken(result.token);
    localStorage.setItem(USER_KEY, JSON.stringify(result));
    setUser(result);
  };

  const logout = () => {
    setToken(null);
    localStorage.removeItem(USER_KEY);
    setUser(null);
  };

  return <AuthContext.Provider value={{ user, login, logout }}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);
