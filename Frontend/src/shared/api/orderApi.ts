import { API_CONFIG } from '../../config/api.config';
import type {
  ApiResult,
  Order,
  OrderPagedResult,
  CreateOrderRequest,
} from '../types';

const BASE = API_CONFIG.BASE_URL;

async function request<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const token = localStorage.getItem('access_token');
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
  const res = await fetch(`${BASE}${endpoint}`, { ...options, headers });
  if (!res.ok) throw new Error(`API Error: ${res.status} ${res.statusText}`);
  return res.json();
}

export const orderApi = {
  getPaged: (
    page = 1,
    pageSize = 10,
    branchId?: number,
    employeeId?: number,
    customerId?: number,
    status?: number,
    startDate?: string,
    endDate?: string
  ) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (branchId !== undefined) params.set('branchId', String(branchId));
    if (employeeId !== undefined) params.set('employeeId', String(employeeId));
    if (customerId !== undefined) params.set('customerId', String(customerId));
    if (status !== undefined) params.set('status', String(status));
    if (startDate) params.set('startDate', startDate);
    if (endDate) params.set('endDate', endDate);
    return request<ApiResult<OrderPagedResult>>(`/v1/orders?${params}`);
  },

  getById: (id: number) =>
    request<ApiResult<Order>>(`/v1/orders/${id}`),

  create: (data: CreateOrderRequest) =>
    request<ApiResult<Order>>('/v1/orders', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  cancel: (id: number) =>
    request<ApiResult<boolean>>(`/v1/orders/${id}/cancel`, {
      method: 'POST',
    }),

  getInvoicePdfUrl: (id: number, download = false) => {
    return `${BASE}/v1/orders/${id}/invoice-pdf${download ? '?download=true' : ''}`;
  },

  downloadInvoicePdf: async (id: number) => {
    const token = localStorage.getItem('access_token');
    const headers: Record<string, string> = token ? { Authorization: `Bearer ${token}` } : {};
    const res = await fetch(`${BASE}/v1/orders/${id}/invoice-pdf?download=true`, { headers });
    if (!res.ok) throw new Error('Không thể tải hóa đơn PDF.');
    const blob = await res.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `HoaDon_HD${String(id).padStart(6, '0')}.pdf`;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  },

  printInvoicePdf: async (id: number) => {
    const token = localStorage.getItem('access_token');
    const headers: Record<string, string> = token ? { Authorization: `Bearer ${token}` } : {};
    const res = await fetch(`${BASE}/v1/orders/${id}/invoice-pdf`, { headers });
    if (!res.ok) throw new Error('Không thể mở hóa đơn PDF.');
    const blob = await res.blob();
    const url = window.URL.createObjectURL(blob);
    const win = window.open(url, '_blank');
    if (!win) {
      // Fallback: create hidden iframe and trigger print
      const iframe = document.createElement('iframe');
      iframe.style.position = 'fixed';
      iframe.style.right = '0';
      iframe.style.bottom = '0';
      iframe.style.width = '0';
      iframe.style.height = '0';
      iframe.style.border = '0';
      iframe.src = url;
      document.body.appendChild(iframe);
      iframe.onload = () => {
        iframe.contentWindow?.print();
      };
    }
  },
};
