import React from 'react';
import { MainLayout } from '../layouts/MainLayout';

export const CartPage: React.FC = () => {
  return (
    <MainLayout>
      <h2>Giỏ hàng của bạn</h2>
      {/* Cart Items & Voucher Redemption will be rendered here */}
    </MainLayout>
  );
};
