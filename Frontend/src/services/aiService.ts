import axiosInstance from '../shared/api/axiosInstance';

export interface MealPlanChatRequest {
  userPrompt: string;
  targetBudget?: number;
  numberOfPeople?: number;
  prioritizePromotions?: boolean;
}

export interface MealIngredientItem {
  productId: number;
  productName: string;
  barcode: string;
  imageUrl: string;
  unit: string;
  quantityToBuy: number;
  regularPrice: number;
  finalPrice: number;
  discountPercent: number;
  isPromotionOrNearExpiry: boolean;
  promotionNote: string;
  inStockQuantity: number;
}

export interface MealPlanChatResponse {
  assistantMessage: string;
  dishName: string;
  servings: number;
  totalEstimatedCost: number;
  totalSavedAmount: number;
  isWithinBudget: boolean;
  ingredients: MealIngredientItem[];
  cookingTips: string;
}

export const aiService = {
  // Trợ lý AI Đi chợ & Lên mâm cơm theo ngân sách
  planMealAndShop: async (request: MealPlanChatRequest): Promise<MealPlanChatResponse> => {
    const response = await axiosInstance.post('/api/v1/ai/shopping/meal-plan', request);
    return response.data?.data;
  },

  // Phân tích rủi ro kho hàng & đề xuất chiết khấu (dành cho Admin / Manager)
  getProductRisks: async () => {
    const response = await axiosInstance.post('/api/v1/ai/retail/product-risks');
    return response.data?.data;
  },

  // Phân tích rổ hàng & gợi ý combo
  getMarketBasket: async (daysBack = 30) => {
    const response = await axiosInstance.post(`/api/v1/ai/retail/market-basket?daysBack=${daysBack}`);
    return response.data?.data;
  },
};
