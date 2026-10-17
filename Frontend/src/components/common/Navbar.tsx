import React from 'react';
import { Link } from 'react-router-dom';

export const Navbar: React.FC = () => {
  return (
    <header className="navbar">
      <h1>Smart Supermarket</h1>
      <nav>
        <Link to="/">Trang chủ</Link>
        <Link to="/cart">Giỏ hàng</Link>
        <Link to="/promotions">Khuyến mãi</Link>
        <a href="/profile">Thẻ thành viên</a>
      </nav>
    </header>
  );
};
