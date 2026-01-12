import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '../store/authStore';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const { accessToken, fingerprintId } = useAuthStore.getState();
    
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    
    if (fingerprintId) {
      config.headers['X-Fingerprint-ID'] = fingerprintId;
    }
    
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    
    // If 401 and we haven't retried yet, try to refresh
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      
      const { refreshToken, fingerprintId, refreshTokens, logout } = useAuthStore.getState();
      
      if (refreshToken && fingerprintId) {
        try {
          const response = await axios.post(`${API_BASE_URL}/auth/refresh`, {
            refreshToken,
            fingerprintId,
          });
          
          const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data;
          refreshTokens(newAccessToken, newRefreshToken);
          
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
          return api(originalRequest);
        } catch (refreshError) {
          logout();
          window.location.href = '/login';
          return Promise.reject(refreshError);
        }
      }
    }
    
    return Promise.reject(error);
  }
);

// Auth API
export const authApi = {
  login: async (email: string, password: string, fingerprintId: string, deviceId?: string) => {
    const response = await api.post('/auth/login', {
      email,
      password,
      fingerprintId,
      deviceId,
    });
    return response.data;
  },
  
  verifyMfa: async (mfaToken: string, code: string, fingerprintId: string) => {
    const response = await api.post('/auth/mfa/verify', {
      mfaToken,
      code,
      fingerprintId,
    });
    return response.data;
  },
  
  refresh: async (refreshToken: string, fingerprintId: string) => {
    const response = await api.post('/auth/refresh', {
      refreshToken,
      fingerprintId,
    });
    return response.data;
  },
  
  logout: async () => {
    await api.post('/auth/logout');
  },
};

// Users API
export const usersApi = {
  getMe: async () => {
    const response = await api.get('/users/me');
    return response.data;
  },
  
  getById: async (id: string) => {
    const response = await api.get(`/users/${id}`);
    return response.data;
  },
};

// OTPT API
export const otptApi = {
  issue: async (route: string, nonce?: string) => {
    const response = await api.post('/otpt/issue', { route, nonce });
    return response.data;
  },
};

// Applications API
export const applicationsApi = {
  list: async (page = 1, pageSize = 20) => {
    const response = await api.get('/applications', {
      params: { page, pageSize },
    });
    return response.data;
  },
  
  getById: async (id: string) => {
    const response = await api.get(`/applications/${id}`);
    return response.data;
  },
  
  create: async (data: { applicationTypeCode: string; description?: string }) => {
    const response = await api.post('/applications', data);
    return response.data;
  },
  
  submit: async (id: string) => {
    const response = await api.post(`/applications/${id}/submit`);
    return response.data;
  },
};

// Payments API
export const paymentsApi = {
  createIntent: async (
    applicationId: string,
    amount: number,
    currency: string,
    returnUrl: string,
    otptToken: string,
    nonce: string
  ) => {
    const response = await api.post('/payments/intent', {
      applicationId,
      amount,
      currency,
      returnUrl,
      otptToken,
      nonce,
    });
    return response.data;
  },
  
  getByApplication: async (applicationId: string) => {
    const response = await api.get(`/payments/application/${applicationId}`);
    return response.data;
  },
};

// Audit API
export const auditApi = {
  search: async (params: {
    fromDate?: string;
    toDate?: string;
    eventType?: string;
    userId?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const response = await api.get('/audit/events', { params });
    return response.data;
  },
  
  getHashChainStatus: async () => {
    const response = await api.get('/audit/chain/status');
    return response.data;
  },
};

export default api;
