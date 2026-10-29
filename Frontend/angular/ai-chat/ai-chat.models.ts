export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  isStreaming?: boolean;
  mealData?: MealPlanChatResponse;
}

export interface ChatSession {
  sessionId: string;
  title: string;
  createdAt: Date;
  messages: ChatMessage[];
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

export interface MealOption {
  optionName: string;
  dishName: string;
  description: string;
  totalEstimatedCost: number;
  totalSavedAmount: number;
  ingredients: MealIngredientItem[];
  cookingTips: string;
}

export interface MealPlanChatResponse {
  assistantMessage: string;
  dishName: string;
  servings: number;
  totalEstimatedCost: number;
  totalSavedAmount: number;
  isWithinBudget: boolean;
  hasMealSuggestion: boolean;
  ingredients: MealIngredientItem[];
  mealOptions?: MealOption[];
  cookingTips: string;
}

export interface AiChatRequest {
  message: string;
  sessionId?: string;
  numberOfPeople?: number;
  targetBudget?: number;
  prioritizePromotions?: boolean;
}

export interface AiChatResponse {
  reply: string;
  sessionId: string;
  hasMealSuggestion: boolean;
  servings: number;
  totalEstimatedPrice: number;
  agentUsed: string;
}
