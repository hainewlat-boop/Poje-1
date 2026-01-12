import { create } from 'zustand';

/**
 * SECURITY: Auth state is stored in RAM only (Zustand).
 * NO localStorage or sessionStorage is used for sensitive data.
 * Tokens are kept in memory and cleared on page refresh/close.
 */

interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  mfaEnabled: boolean;
}

interface AuthState {
  // State
  isAuthenticated: boolean;
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  mfaToken: string | null;
  requiresMfa: boolean;
  fingerprintId: string | null;

  // Actions
  setCredentials: (data: {
    accessToken: string;
    refreshToken: string;
    user?: User;
  }) => void;
  setMfaRequired: (mfaToken: string) => void;
  setUser: (user: User) => void;
  setFingerprintId: (fingerprintId: string) => void;
  logout: () => void;
  refreshTokens: (accessToken: string, refreshToken: string) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  // Initial state - everything in RAM
  isAuthenticated: false,
  user: null,
  accessToken: null,
  refreshToken: null,
  mfaToken: null,
  requiresMfa: false,
  fingerprintId: null,

  setCredentials: ({ accessToken, refreshToken, user }) =>
    set({
      isAuthenticated: true,
      accessToken,
      refreshToken,
      user: user || null,
      requiresMfa: false,
      mfaToken: null,
    }),

  setMfaRequired: (mfaToken) =>
    set({
      isAuthenticated: false,
      requiresMfa: true,
      mfaToken,
      accessToken: null,
      refreshToken: null,
    }),

  setUser: (user) => set({ user }),

  setFingerprintId: (fingerprintId) => set({ fingerprintId }),

  logout: () =>
    set({
      isAuthenticated: false,
      user: null,
      accessToken: null,
      refreshToken: null,
      mfaToken: null,
      requiresMfa: false,
    }),

  refreshTokens: (accessToken, refreshToken) =>
    set({ accessToken, refreshToken }),
}));

// Helper hook to check permissions
export const useHasPermission = (permission: string): boolean => {
  const user = useAuthStore((state) => state.user);
  
  if (!user) return false;
  
  // Admin has all permissions
  if (user.roles.includes('admin')) return true;
  
  // Check specific permission based on role
  // This is simplified - in production, check against actual permissions
  return user.roles.some((role) => {
    const rolePermissions: Record<string, string[]> = {
      operator: ['applications:read', 'applications:write', 'payments:read'],
      reviewer: ['applications:read', 'applications:approve'],
      citizen: ['applications:read:own', 'payments:read:own'],
      auditor: ['audit:read', 'audit:export'],
    };
    return rolePermissions[role]?.includes(permission);
  });
};
