import React, { useState, useEffect } from 'react';
import { MainLayout } from '../layouts/MainLayout';

interface CartItem {
  productId: number;
  productName: string;
  price: number;
  quantity: number;
  unit: string;
  imageUrl: string;
}

export const CartPage: React.FC = () => {
  const [cartItems, setCartItems] = useState<CartItem[]>([]);
  const [checkedOut, setCheckedOut] = useState(false);

  const loadCart = () => {
    try {
      const saved = localStorage.getItem('smart_supermarket_cart');
      if (saved) {
        setCartItems(JSON.parse(saved));
      } else {
        setCartItems([]);
      }
    } catch {
      setCartItems([]);
    }
  };

  useEffect(() => {
    loadCart();
  }, []);

  const updateQuantity = (productId: number, delta: number) => {
    const updated = cartItems
      .map((item) => {
        if (item.productId === productId) {
          return { ...item, quantity: item.quantity + delta };
        }
        return item;
      })
      .filter((item) => item.quantity > 0);

    setCartItems(updated);
    localStorage.setItem('smart_supermarket_cart', JSON.stringify(updated));
    window.dispatchEvent(new Event('cart-updated'));
  };

  const removeItem = (productId: number) => {
    const updated = cartItems.filter((item) => item.productId !== productId);
    setCartItems(updated);
    localStorage.setItem('smart_supermarket_cart', JSON.stringify(updated));
    window.dispatchEvent(new Event('cart-updated'));
  };

  const clearCart = () => {
    setCartItems([]);
    localStorage.removeItem('smart_supermarket_cart');
    window.dispatchEvent(new Event('cart-updated'));
  };

  const totalAmount = cartItems.reduce((acc, item) => acc + item.price * item.quantity, 0);

  const handleCheckout = () => {
    setCheckedOut(true);
    clearCart();
  };

  return (
    <MainLayout>
      <div style={{ maxWidth: 900, margin: '20px auto', padding: '0 16px', fontFamily: 'Segoe UI, sans-serif' }}>
        <h2 style={{ fontSize: 24, fontWeight: 700, color: '#0F5132', marginBottom: 16 }}>
          🛒 Giỏ hàng của bạn
        </h2>

        {checkedOut ? (
          <div style={{ backgroundColor: '#dcfce7', border: '1px solid #86efac', padding: 24, borderRadius: 12, textAlign: 'center' }}>
            <h3 style={{ color: '#166534', margin: '0 0 8px' }}>🎉 Đặt hàng thành công!</h3>
            <p style={{ color: '#15803d', margin: '0 0 16px' }}>Cảm ơn bạn đã mua sắm tại Smart SuperMarket. Đơn hàng của bạn đang được chuẩn bị.</p>
            <a href="/" style={{ display: 'inline-block', backgroundColor: '#0F5132', color: '#fff', padding: '10px 20px', borderRadius: 8, textDecoration: 'none', fontWeight: 600 }}>
              Tiếp tục mua sắm
            </a>
          </div>
        ) : cartItems.length === 0 ? (
          <div style={{ backgroundColor: '#f8fafc', border: '1px dashed #cbd5e1', padding: 36, borderRadius: 12, textAlign: 'center', color: '#64748b' }}>
            <p style={{ fontSize: 16, margin: '0 0 12px' }}>Giỏ hàng của bạn đang trống.</p>
            <p style={{ fontSize: 13, margin: '0 0 16px' }}>Hãy thử nhờ <strong>Trợ lý AI</strong> ở góc dưới phải gợi ý thực đơn món ăn cho gia đình nhé!</p>
            <a href="/" style={{ display: 'inline-block', backgroundColor: '#0F5132', color: '#fff', padding: '8px 18px', borderRadius: 8, textDecoration: 'none', fontSize: 13 }}>
              Về trang chủ
            </a>
          </div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 24 }}>
            {/* List */}
            <div style={{ backgroundColor: '#fff', border: '1px solid #e2e8f0', borderRadius: 12, padding: 16 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
                <span style={{ fontWeight: 600, color: '#334155' }}>Danh sách sản phẩm ({cartItems.length} món)</span>
                <button onClick={clearCart} style={{ background: 'none', border: 'none', color: '#ef4444', fontSize: 12, cursor: 'pointer' }}>
                  Xóa tất cả
                </button>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {cartItems.map((item) => (
                  <div
                    key={item.productId}
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      padding: 10,
                      border: '1px solid #f1f5f9',
                      borderRadius: 8,
                      backgroundColor: '#fcfcfc',
                    }}
                  >
                    <div style={{ flex: 1 }}>
                      <div style={{ fontWeight: 600, fontSize: 14 }}>{item.productName}</div>
                      <div style={{ fontSize: 12, color: '#64748b' }}>
                        Đơn vị: {item.unit} | Đơn giá: {item.price.toLocaleString()} đ
                      </div>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                      <button
                        onClick={() => updateQuantity(item.productId, -1)}
                        style={{ width: 26, height: 26, borderRadius: 4, border: '1px solid #cbd5e1', background: '#fff', cursor: 'pointer' }}
                      >
                        -
                      </button>
                      <span style={{ fontWeight: 600, fontSize: 14, minWidth: 20, textAlign: 'center' }}>
                        {item.quantity}
                      </span>
                      <button
                        onClick={() => updateQuantity(item.productId, 1)}
                        style={{ width: 26, height: 26, borderRadius: 4, border: '1px solid #cbd5e1', background: '#fff', cursor: 'pointer' }}
                      >
                        +
                      </button>
                    </div>

                    <div style={{ width: 110, textAlign: 'right', fontWeight: 700, color: '#0F5132' }}>
                      {(item.price * item.quantity).toLocaleString()} đ
                    </div>

                    <button
                      onClick={() => removeItem(item.productId)}
                      style={{ marginLeft: 8, background: 'none', border: 'none', color: '#94a3b8', cursor: 'pointer', fontSize: 16 }}
                    >
                      ×
                    </button>
                  </div>
                ))}
              </div>
            </div>

            {/* Summary */}
            <div style={{ backgroundColor: '#fff', border: '1px solid #e2e8f0', borderRadius: 12, padding: 18, height: 'fit-content' }}>
              <h3 style={{ fontSize: 16, fontWeight: 700, margin: '0 0 14px', color: '#1e293b' }}>
                Tóm tắt đơn hàng
              </h3>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, fontSize: 14 }}>
                <span style={{ color: '#64748b' }}>Tạm tính:</span>
                <span>{totalAmount.toLocaleString()} đ</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 12, fontSize: 14 }}>
                <span style={{ color: '#64748b' }}>Phí vận chuyển:</span>
                <span style={{ color: '#16a34a', fontWeight: 600 }}>Miễn phí</span>
              </div>
              <div style={{ borderTop: '1px solid #e2e8f0', paddingTop: 12, marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <strong style={{ fontSize: 15 }}>Tổng cộng:</strong>
                <strong style={{ fontSize: 18, color: '#dc2626' }}>{totalAmount.toLocaleString()} đ</strong>
              </div>

              <button
                onClick={handleCheckout}
                style={{
                  width: '100%',
                  backgroundColor: '#0F5132',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 8,
                  padding: '12px',
                  fontWeight: 700,
                  fontSize: 15,
                  cursor: 'pointer',
                  boxShadow: '0 4px 12px rgba(15,81,50,0.25)',
                }}
              >
                Tiến hành đặt hàng
              </button>
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
};
