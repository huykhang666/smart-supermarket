import { API_CONFIG } from '../../config/api.config';
import type {
  ApiResult,
  Promotion,
  PromotionPagedResult,
  CreatePromotionRequest,
  UpdatePromotionRequest,
  DiscountRule,
  ExpiringProduct,
  CheckProductExpiryDiscountResponse,
  ApplyPromotionRequest,
  ApplyPromotionResponse,
  Product,
  Category,
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

export const promotionApi = {
  getPaged: (page = 1, pageSize = 10, search?: string, isActive?: boolean) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (search) params.set('search', search);
    if (isActive !== undefined) params.set('isActive', String(isActive));
    return request<ApiResult<PromotionPagedResult>>(`/v1/promotions?${params}`);
  },

  getActive: () =>
    request<ApiResult<Promotion[]>>('/v1/promotions/active'),

  getById: (id: number) =>
    request<ApiResult<Promotion>>(`/v1/promotions/${id}`),

  getByCode: (code: string) =>
    request<ApiResult<Promotion>>(`/v1/promotions/code/${code}`),

  create: (data: CreatePromotionRequest) =>
    request<ApiResult<Promotion>>('/v1/promotions', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  update: (id: number, data: UpdatePromotionRequest) =>
    request<ApiResult<Promotion>>(`/v1/promotions/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),

  delete: (id: number) =>
    request<ApiResult<boolean>>(`/v1/promotions/${id}`, { method: 'DELETE' }),

  apply: (data: ApplyPromotionRequest) =>
    request<ApiResult<ApplyPromotionResponse>>('/v1/promotions/apply', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
};

export const discountRuleApi = {
  getAll: (includeInactive = true) =>
    request<ApiResult<DiscountRule[]>>(`/v1/discount-rules?includeInactive=${includeInactive}`),

  getById: (id: number) =>
    request<ApiResult<DiscountRule>>(`/v1/discount-rules/${id}`),

  create: (data: { daysBeforeExpiry: number; discountPercent: number; isActive: boolean; description?: string }) =>
    request<ApiResult<DiscountRule>>('/v1/discount-rules', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  update: (id: number, data: { daysBeforeExpiry: number; discountPercent: number; isActive: boolean; description?: string }) =>
    request<ApiResult<boolean>>(`/v1/discount-rules/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),

  delete: (id: number) =>
    request<ApiResult<boolean>>(`/v1/discount-rules/${id}`, { method: 'DELETE' }),

  getExpiringProducts: () =>
    request<ApiResult<ExpiringProduct[]>>('/v1/discount-rules/expiring-products'),

  checkProductDiscount: (query: string) =>
    request<ApiResult<CheckProductExpiryDiscountResponse>>(`/v1/discount-rules/check-discount/${query}`),
};

export const productApi = {
  getAll: () => request<ApiResult<Product[]>>('/v1/products'),
};

export const categoryApi = {
  getAll: () => request<ApiResult<Category[]>>('/v1/categories'),
};
