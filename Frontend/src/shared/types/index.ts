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

export type OrderStatus = 'Completed' | 'Cancelled' | 'Pending';

export type PaymentMethodType = 'Cash' | 'QRCode' | 'CreditCard' | number;

export interface OrderDetail {
  orderDetailId: number;
  orderId: number;
  productId: number;
  productName: string;
  barcode: string;
  unit: string;
  quantity: number;
  unitPrice: number;
  subTotal: number;
}

export interface OrderPromotion {
  orderPromotionId: number;
  promotionId: number;
  discountAmount: number;
}

export interface Payment {
  paymentId: number;
  paymentMethod: PaymentMethodType;
  amountPaid: number;
  paymentDate: string;
}

export interface Order {
  orderId: number;
  employeeId: number;
  employeeName?: string;
  customerId?: number;
  customerName?: string;
  customerPhone?: string;
  branchId: number;
  orderDate: string;
  totalAmount: number;
  discountAmount: number;
  voucherId?: number;
  finalAmount: number;
  status: OrderStatus | number;
  paymentMethod: PaymentMethodType;
  orderDetails: OrderDetail[];
  appliedPromotions: OrderPromotion[];
  payments: Payment[];
}

export interface CreateOrderDetailRequest {
  productId: number;
  quantity: number;
  unitPrice: number;
}

export interface CreatePaymentRequest {
  paymentMethod: number;
  amountPaid: number;
}

export interface CreateOrderRequest {
  employeeId: number;
  customerId?: number;
  branchId: number;
  voucherId?: number;
  promotionCode?: string;
  paymentMethod?: number;
  payments?: CreatePaymentRequest[];
  items: CreateOrderDetailRequest[];
}

export interface OrderPagedResult {
  items: Order[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

