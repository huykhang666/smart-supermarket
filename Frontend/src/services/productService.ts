import axiosInstance from '../shared/api/axiosInstance';

export interface Product {
  productId: number;
  barcode: string;
  productName: string;
  categoryName?: string;
  price: number;
  costPrice?: number;
  discountPrice?: number;
  discountPercent?: number;
  imageUrl?: string;
  unit: string;
  expiryDate?: string;
  isFlashSale?: boolean;
  isNearExpiry?: boolean;
}

export const productService = {
  // Lấy danh sách sản phẩm hiển thị trên sàn thương mại
  getProducts: async (page = 1, pageSize = 40, categoryId?: number) => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    if (categoryId) params.append('categoryId', categoryId.toString());

    const response = await axiosInstance.get(`/api/v1/products?${params.toString()}`);
    return response.data?.data;
  },

  // Lấy danh sách sản phẩm Flash Sale & Cận date tiết kiệm
  getBargainAndFlashSale: async () => {
    const response = await axiosInstance.get('/api/v1/products/promotions');
    return response.data?.data;
  },
};
