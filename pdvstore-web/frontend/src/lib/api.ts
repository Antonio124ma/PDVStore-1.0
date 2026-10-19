import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  async (err: AxiosError) => {
    const originalConfig = err.config as InternalAxiosRequestConfig & { _retry?: boolean };
    if (err.response?.status === 401 && !originalConfig._retry) {
      originalConfig._retry = true;
      try {
        const refresh = localStorage.getItem('refreshToken');
        const { data } = await api.post<{ accessToken: string; refreshToken: string }>('/auth/refresh', { refreshToken: refresh });
        localStorage.setItem('accessToken', data.accessToken);
        localStorage.setItem('refreshToken', data.refreshToken);
        return api(originalConfig);
      } catch {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('usuario');
        window.location.href = '/login';
      }
    }
    return Promise.reject(err);
  }
);