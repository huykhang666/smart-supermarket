import React from 'react';

export const Footer: React.FC = () => {
  return (
    <footer className="footer">
      <p>&copy; {new Date().getFullYear()} Smart Supermarket System. All rights reserved.</p>
    </footer>
  );
};
