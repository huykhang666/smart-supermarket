export interface Product {
  productId: number;
  productName: string;
  barcode: string;
  price: number;
  imageUrl?: string;
  unit: string;
  categoryId: number;
  categoryName: string;
  quantityInStock: number;
}

export interface CartItem {
  product: Product;
  quantity: number;
}

export interface User {
  userId: number;
  username: string;
  fullName: string;
  email: string;
  role: string;
}

export interface ApiResult<T> {
  isSuccess: boolean;
  message: string;
  data: T;
}

export type DiscountType =
  | 'Percentage'
  | 'FixedAmount'
  | 'BuyXGetY'
  | 'ComboDiscount'
  | 'CategoryDiscount'
  | 'QuantityTier';

export interface Promotion {
  promotionId: number;
  promotionCode: string;
  promotionName: string;
  description: string;
  discountType: DiscountType;
  discountValue: number;
  minimumOrderAmount: number;
  maximumDiscountAmount?: number;
  triggerProductId?: number;
  triggerProductName?: string;
  targetCategoryId?: number;
  targetCategoryName?: string;
  targetProductId?: number;
  targetProductName?: string;
  requiredQuantity?: number;
  giftQuantity?: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreatePromotionRequest {
  promotionCode: string;
  promotionName: string;
  description?: string;
  discountType: DiscountType;
  discountValue: number;
  minimumOrderAmount: number;
  maximumDiscountAmount?: number;
  triggerProductId?: number;
  targetCategoryId?: number;
  targetProductId?: number;
  requiredQuantity?: number;
  giftQuantity?: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export interface UpdatePromotionRequest {
  promotionName: string;
  description?: string;
  discountType: DiscountType;
  discountValue: number;
  minimumOrderAmount: number;
  maximumDiscountAmount?: number;
  triggerProductId?: number;
  targetCategoryId?: number;
  targetProductId?: number;
  requiredQuantity?: number;
  giftQuantity?: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export interface PromotionPagedResult {
  items: Promotion[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface DiscountRule {
  discountRuleId: number;
  daysBeforeExpiry: number;
  discountPercent: number;
  isActive: boolean;
  description?: string;
}

export interface ExpiringProduct {
  productId: number;
  productName: string;
  barcode: string;
  batchCode: string;
  quantity: number;
  expiryDate: string;
  daysRemaining: number;
  originalPrice: number;
  discountPercent: number;
  clearancePrice: number;
  alertLevel: 'Critical' | 'Warning' | 'Notice' | 'Expired';
}

export interface CheckProductExpiryDiscountResponse {
  productId: number;
  barcode: string;
  productName: string;
  originalPrice: number;
  expiryDate?: string;
  daysRemaining: number;
  discountPercent: number;
  clearancePrice: number;
  isNearExpiry: boolean;
  ruleDescription: string;
}

export interface ApplyPromotionRequest {
  promotionCode: string;
  orderTotalAmount: number;
  items?: PromotionCartItem[];
}

export interface PromotionCartItem {
  productId: number;
  barcode: string;
  productName: string;
  categoryId?: number;
  quantity: number;
  unitPrice: number;
}

export interface ApplyPromotionResponse {
  isSuccess: boolean;
  message: string;
  promotionCode: string;
  promotionName: string;
  discountType: string;
  discountAmount: number;
  finalAmount: number;
  appliedDetails: string[];
}

export interface Category {
  categoryId: number;
  categoryName: string;
}
