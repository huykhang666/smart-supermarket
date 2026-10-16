import React from 'react';
import type { Product } from '../../services/productService';
import { formatCurrency } from '../../shared/utils/formatCurrency';

interface ProductCardProps {
  product: Product;
  onAddToCart?: (product: Product) => void;
}

export const ProductCard: React.FC<ProductCardProps> = ({ product, onAddToCart }) => {
  const isDiscounted = (product.discountPercent ?? 0) > 0 || (product.discountPrice && product.discountPrice < product.price);
  const currentPrice = product.discountPrice ?? product.price;

  return (
    <div
      style={{
        border: '1px solid #e5e7eb',
        borderRadius: 12,
        padding: 12,
        backgroundColor: '#fff',
        display: 'flex',
        flexDirection: 'column',
        position: 'relative',
        boxShadow: '0 2px 4px rgba(0,0,0,0.04)',
      }}
    >
      {/* Promotion / Near-expiry Badge */}
      {product.discountPercent && product.discountPercent > 0 && (
        <span
          style={{
            position: 'absolute',
            top: 8,
            left: 8,
            backgroundColor: '#dc2626',
            color: '#fff',
            fontSize: 11,
            fontWeight: 700,
            padding: '2px 8px',
            borderRadius: 6,
          }}
        >
          -{product.discountPercent}%
        </span>
      )}

      {/* Product Image */}
      <div
        style={{
          width: '100%',
          height: 160,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          overflow: 'hidden',
          borderRadius: 8,
          backgroundColor: '#f9fafb',
          marginBottom: 10,
        }}
      >
        {product.imageUrl ? (
          <img
            src={product.imageUrl}
            alt={product.productName}
            style={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }}
            loading="lazy"
          />
        ) : (
          <div style={{ color: '#9ca3af', fontSize: 13 }}>Không có ảnh</div>
        )}
      </div>

      {/* Product Name */}
      <h3
        style={{
          fontSize: 14,
          fontWeight: 600,
          color: '#1f2937',
          lineHeight: '1.3',
          margin: '0 0 8px 0',
          height: 38,
          overflow: 'hidden',
          textOverflow: 'ellipsis',
          display: '-webkit-box',
          WebkitLineClamp: 2,
          WebkitBoxOrient: 'vertical',
        }}
      >
        {product.productName}
      </h3>

      {/* Price section */}
      <div style={{ marginTop: 'auto', marginBottom: 10 }}>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: 6 }}>
          <span style={{ fontSize: 16, fontWeight: 700, color: '#0F5132' }}>
            {formatCurrency(currentPrice)}
          </span>
          {isDiscounted && (
            <span
              style={{
                fontSize: 12,
                color: '#9ca3af',
                textDecoration: 'line-through',
              }}
            >
              {formatCurrency(product.price)}
            </span>
          )}
        </div>
        <span style={{ fontSize: 11, color: '#6b7280' }}>Đơn vị: {product.unit}</span>
      </div>

      {/* Add to Cart button */}
      <button
        onClick={() => onAddToCart && onAddToCart(product)}
        style={{
          width: '100%',
          padding: '8px 0',
          backgroundColor: '#0F5132',
          color: '#fff',
          border: 'none',
          borderRadius: 8,
          fontSize: 13,
          fontWeight: 600,
          cursor: 'pointer',
        }}
      >
        + Thêm vào giỏ
      </button>
    </div>
  );
};
