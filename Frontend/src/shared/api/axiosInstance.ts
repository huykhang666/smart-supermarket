import { API_CONFIG } from '../../config/api.config';

const getFullUrl = (endpoint: string): string => {
  const base = API_CONFIG.BASE_URL.replace(/\/+$/, '');
  const clean = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
  return clean.startsWith('/api') ? `${base}${clean}` : `${base}/api${clean}`;
};

export const fetchApi = async <T>(endpoint: string, options?: RequestInit & { timeout?: number }): Promise<T> => {
  const timeoutMs = options?.timeout ?? 12000; // Mặc định timeout 12 giây phòng ngừa DB/server bị treo
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const token = localStorage.getItem('access_token');
    const headers = {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    };

    const response = await fetch(getFullUrl(endpoint), {
      ...options,
      headers,
      signal: options?.signal ?? controller.signal,
    });

    clearTimeout(timeoutId);

    if (!response.ok) {
      const errorJson = await response.json().catch(() => null);
      const msg = errorJson?.message || `API Error: ${response.statusText}`;
      throw new Error(msg);
    }

    return response.json();
  } catch (error: any) {
    clearTimeout(timeoutId);
    if (error.name === 'AbortError') {
      throw new Error('Yêu cầu vượt quá thời gian chờ (Timeout). Cơ sở dữ liệu hoặc máy chủ phản hồi quá lâu, vui lòng thử lại!');
    }
    throw error;
  }
};

export const axiosInstance = {
  get: <T = any>(endpoint: string, options?: { timeout?: number }) => fetchApi<{ data: T }>(endpoint, { method: 'GET', ...options }),
  post: <T = any>(endpoint: string, body?: any, options?: { timeout?: number }) =>
    fetchApi<{ data: T }>(endpoint, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
      ...options,
    }),
  put: <T = any>(endpoint: string, body?: any, options?: { timeout?: number }) =>
    fetchApi<{ data: T }>(endpoint, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
      ...options,
    }),
  delete: <T = any>(endpoint: string, options?: { timeout?: number }) => fetchApi<{ data: T }>(endpoint, { method: 'DELETE', ...options }),
};

export default axiosInstance;
