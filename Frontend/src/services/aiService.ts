import axiosInstance from '../shared/api/axiosInstance';

export interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

export interface MealPlanChatRequest {
  userPrompt: string;
  targetBudget?: number;
  numberOfPeople?: number;
  prioritizePromotions?: boolean;
  conversationHistory?: ChatMessage[];
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

export type RetailTier = 1 | 2 | 3 | 4;
export const RetailTier = {
  Tier1CheapestAcceptable: 1,
  Tier2BalancedValue: 2,
  Tier3DurableHighPerformance: 3,
  Tier4PremiumLuxury: 4,
} as const;

export interface RetailProductRecommendation {
  productId: number;
  productName: string;
  barcode: string;
  imageUrl: string;
  unit: string;
  regularPrice: number;
  finalPrice: number;
  discountPercent: number;
  inStockQuantity: number;
  tier: RetailTier;
  tierLabel: string;
  highlightReason: string;
  pricePerUnitMetric: string;
  pros: string[];
  cons: string[];
  isRecommendedChoice: boolean;
  isWithinBudget: boolean;
  categoryName: string;
}

export interface ComparisonRow {
  criteria: string;
  tier1CheapValue: string;
  tier2StandardValue: string;
  tier3PremiumValue: string;
}

export interface RetailComparisonTable {
  title: string;
  headers: string[];
  rows: ComparisonRow[];
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

  // Phase 2: Retail Shopping Intelligence
  hasRetailRecommendation?: boolean;
  shoppingCategory?: string;
  retailRecommendations?: RetailProductRecommendation[];
  comparisonTable?: RetailComparisonTable;

  // Phase 3: Knowledge & Grounding RAG
  knowledgeSource?: string;
  groundedFacts?: string[];

  // Phase 4: Optimization, Performance & Production Readiness
  modelRouted?: string;
  promptVersion?: string;
  experimentVariant?: string;
  executionLatencyMs?: number;
  estimatedCostUsd?: number;
  isCacheHit?: boolean;
}

const unwrapData = (res: any) => {
  if (res?.data?.data !== undefined) return res.data.data;
  if (res?.data !== undefined) return res.data;
  return res;
};

export const aiService = {
  // Trợ lý AI Đi chợ & Lên mâm cơm theo ngân sách
  planMealAndShop: async (request: MealPlanChatRequest): Promise<MealPlanChatResponse> => {
    const response = await axiosInstance.post('/api/v1/ai/shopping/meal-plan', request);
    return unwrapData(response);
  },

  // Phân tích rủi ro kho hàng & đề xuất chiết khấu (dành cho Admin / Manager)
  getProductRisks: async () => {
    const response = await axiosInstance.post('/api/v1/ai/retail/product-risks');
    return unwrapData(response);
  },

  // Phân tích rổ hàng & gợi ý combo
  getMarketBasket: async (daysBack = 30) => {
    const response = await axiosInstance.post(`/api/v1/ai/retail/market-basket?daysBack=${daysBack}`);
    return unwrapData(response);
  },

  // AI Monitoring Dashboard
  getMonitoringDashboard: async (): Promise<AiMonitoringDashboard> => {
    const response = await axiosInstance.get('/api/v1/ai/monitoring/dashboard');
    return unwrapData(response);
  },

  // AI Monitoring Metrics Overview
  getMonitoringMetrics: async (): Promise<AiOverviewMetrics> => {
    const response = await axiosInstance.get('/api/v1/ai/monitoring/metrics');
    return unwrapData(response);
  },

  // Reset Metrics
  resetMonitoringMetrics: async (): Promise<any> => {
    const response = await axiosInstance.post('/api/v1/ai/monitoring/reset');
    return unwrapData(response);
  },

  // Phase 5: Operations, Rollback & Health
  getPromptVersions: async (): Promise<any> => {
    const response = await axiosInstance.get('/api/v1/ai/monitoring/prompt-versions');
    return unwrapData(response);
  },

  setActivePromptVersion: async (version: string): Promise<any> => {
    const response = await axiosInstance.post('/api/v1/ai/monitoring/prompt-versions/active', { version });
    return unwrapData(response);
  },

  rollbackPromptVersion: async (targetVersion: string): Promise<any> => {
    const response = await axiosInstance.post('/api/v1/ai/monitoring/prompt-versions/rollback', { targetVersion });
    return unwrapData(response);
  },

  getExperimentStats: async (): Promise<any> => {
    const response = await axiosInstance.get('/api/v1/ai/monitoring/experiments');
    return unwrapData(response);
  },

  getAiHealth: async (): Promise<any> => {
    const response = await axiosInstance.get('/api/v1/ai/health');
    return unwrapData(response);
  },
};

export interface AiOverviewMetrics {
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  successRatePercent: number;
  averageLatencyMs: number;
  p95LatencyMs: number;
  totalPromptTokens: number;
  totalCompletionTokens: number;
  totalTokens: number;
  totalCostUsd: number;
  totalCostVnd: number;
  totalSavedCostUsd: number;
  cacheHitCount: number;
  cacheHitRatioPercent: number;
  serverTimeUtc: string;
}

export interface AiToolMetric {
  toolName: string;
  invocationCount: number;
  successCount: number;
  failureCount: number;
  averageDurationMs: number;
  minDurationMs: number;
  maxDurationMs: number;
  totalDurationMs: number;
}

export interface AiModelUsageMetric {
  modelName: string;
  provider: string;
  requestCount: number;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  estimatedCostUsd: number;
  averageLatencyMs: number;
}

export interface AiTraceItem {
  traceId: string;
  correlationId: string;
  timestamp: string;
  operation: string;
  modelOrTool: string;
  durationMs: number;
  promptTokens: number;
  completionTokens: number;
  costUsd: number;
  isSuccess: boolean;
  errorMessage?: string;
  fromCache: boolean;
}

export interface AiMonitoringDashboard {
  overview: AiOverviewMetrics;
  toolMetrics: AiToolMetric[];
  modelMetrics: AiModelUsageMetric[];
  recentTraces: AiTraceItem[];
}
