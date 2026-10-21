import { API_CONFIG } from '../../config/api.config';

export const fetchApi = async <T>(endpoint: string, options?: RequestInit): Promise<T> => {
  const token = localStorage.getItem('access_token');
  const headers = {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options?.headers,
  };

  const response = await fetch(`${API_CONFIG.BASE_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    throw new Error(`API Error: ${response.statusText}`);
  }

  return response.json();
};

export const axiosInstance = {
  get: <T = any>(endpoint: string) => fetchApi<{ data: T }>(endpoint, { method: 'GET' }),
  post: <T = any>(endpoint: string, body?: any) =>
    fetchApi<{ data: T }>(endpoint, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    }),
  put: <T = any>(endpoint: string, body?: any) =>
    fetchApi<{ data: T }>(endpoint, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    }),
  delete: <T = any>(endpoint: string) => fetchApi<{ data: T }>(endpoint, { method: 'DELETE' }),
};

export default axiosInstance;
