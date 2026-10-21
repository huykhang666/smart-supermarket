import React from 'react';

export const FlashSaleBanner: React.FC = () => {
  return (
    <div
      style={{
        background: 'linear-gradient(90deg, #b91c1c 0%, #ea580c 100%)',
        color: '#fff',
        borderRadius: 12,
        padding: '16px 24px',
        marginBottom: 24,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        boxShadow: '0 4px 12px rgba(220, 38, 38, 0.25)',
      }}
    >
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ fontSize: 22 }}>⚡</span>
          <h2 style={{ margin: 0, fontSize: 20, fontWeight: 800, textTransform: 'uppercase' }}>
            Flash Sale Chiều Tối Sau 18:00
          </h2>
        </div>
        <p style={{ margin: '6px 0 0 0', fontSize: 14, opacity: 0.9 }}>
          Xả kho tươi sống, bánh mì & rau củ trong ngày - Giảm giá sốc đến 50%!
        </p>
      </div>

      <div
        style={{
          backgroundColor: 'rgba(255, 255, 255, 0.2)',
          padding: '8px 16px',
          borderRadius: 8,
          fontWeight: 700,
          fontSize: 14,
        }}
      >
        Hàng Cận Date & Tươi Sống
      </div>
    </div>
  );
};
