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
