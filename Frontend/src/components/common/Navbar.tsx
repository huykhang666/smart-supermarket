import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';

export const Navbar: React.FC = () => {
  const [cartCount, setCartCount] = useState(0);

  const updateCount = () => {
    try {
      const cartStr = localStorage.getItem('smart_supermarket_cart');
      if (cartStr) {
        const items = JSON.parse(cartStr);
        setCartCount(items.reduce((acc: number, item: { quantity: number }) => acc + (item.quantity || 1), 0));
      } else {
        setCartCount(0);
      }
    } catch {
      setCartCount(0);
    }
  };

  useEffect(() => {
    updateCount();
    window.addEventListener('cart-updated', updateCount);
    window.addEventListener('storage', updateCount);
    return () => {
      window.removeEventListener('cart-updated', updateCount);
      window.removeEventListener('storage', updateCount);
    };
  }, []);

  return (
    <header className="navbar">
      <h1>Smart Supermarket</h1>
      <nav>
        <Link to="/">Trang chủ</Link>
        <Link to="/orders">Đơn hàng & Hóa đơn</Link>
        <Link to="/cart" style={{ position: 'relative' }}>
          🛒 Giỏ hàng
          {cartCount > 0 && (
            <span
              style={{
                marginLeft: 6,
                backgroundColor: '#dc2626',
                color: '#fff',
                fontSize: 11,
                fontWeight: 700,
                padding: '2px 6px',
                borderRadius: '10px',
              }}
            >
              {cartCount}
            </span>
          )}
        </Link>
        <Link to="/promotions">Khuyến mãi</Link>
        <a href="/profile">Thẻ thành viên</a>
      </nav>
    </header>
  );
};
