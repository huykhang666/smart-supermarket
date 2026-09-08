import React from 'react';

export const Navbar: React.FC = () => {
  return (
    <header className="navbar">
      <h1>Smart Supermarket</h1>
      <nav>
        <a href="/">Trang chủ</a>
        <a href="/cart">Giỏ hàng</a>
        <a href="/profile">Thẻ thành viên</a>
      </nav>
    </header>
  );
};
