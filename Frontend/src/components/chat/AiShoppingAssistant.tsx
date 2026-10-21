import React, { useState } from 'react';
import { aiService, type MealPlanChatResponse } from '../../services/aiService';

export const AiShoppingAssistant: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [prompt, setPrompt] = useState('');
  const [budget, setBudget] = useState<number | undefined>(undefined);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<MealPlanChatResponse | null>(null);

  const handleSendPrompt = async () => {
    if (!prompt.trim()) return;
    setLoading(true);
    try {
      const data = await aiService.planMealAndShop({
        userPrompt: prompt,
        targetBudget: budget,
        prioritizePromotions: true,
      });
      setResult(data);
    } catch (err) {
      console.error('Lỗi khi gọi AI Shopping Concierge:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ position: 'fixed', bottom: 24, right: 24, zIndex: 9999 }}>
      {!isOpen ? (
        <button
          onClick={() => setIsOpen(true)}
          style={{
            backgroundColor: '#0F5132',
            color: '#fff',
            border: 'none',
            borderRadius: '50px',
            padding: '12px 20px',
            cursor: 'pointer',
            boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            fontWeight: 600,
          }}
        >
          <span>🤖 Trợ Lý Đi Chợ AI</span>
        </button>
      ) : (
        <div
          style={{
            width: 380,
            maxHeight: 560,
            backgroundColor: '#fff',
            borderRadius: 16,
            boxShadow: '0 8px 30px rgba(0,0,0,0.2)',
            display: 'flex',
            flexDirection: 'column',
            overflow: 'hidden',
            border: '1px solid #e0e0e0',
          }}
        >
          {/* Header */}
          <div
            style={{
              backgroundColor: '#0F5132',
              color: '#fff',
              padding: '12px 16px',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
            }}
          >
            <div>
              <strong>Trợ Lý Mua Sắm & Bữa Ăn AI</strong>
              <div style={{ fontSize: 11, opacity: 0.85 }}>Lên thực đơn theo ngân sách & kho hàng</div>
            </div>
            <button
              onClick={() => setIsOpen(false)}
              style={{
                background: 'transparent',
                border: 'none',
                color: '#fff',
                fontSize: 18,
                cursor: 'pointer',
              }}
            >
              ✕
            </button>
          </div>

          {/* Body */}
          <div style={{ padding: 16, flex: 1, overflowY: 'auto', fontSize: 14 }}>
            <p style={{ margin: 0, color: '#555', marginBottom: 12 }}>
              Bạn muốn nấu món gì hôm nay? (Ví dụ: <em>"Lẩu Thái cho 4 người tầm 350k"</em> hoặc{' '}
              <em>"Cơm gia đình tiết kiệm 150k"</em>)
            </p>

            <textarea
              rows={3}
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              placeholder="Nhập yêu cầu món ăn và số người..."
              style={{
                width: '100%',
                padding: 8,
                borderRadius: 8,
                border: '1px solid #ccc',
                boxSizing: 'border-box',
                resize: 'none',
              }}
            />

            <div style={{ marginTop: 8, display: 'flex', gap: 8, alignItems: 'center' }}>
              <input
                type="number"
                placeholder="Ngân sách (VNĐ, tùy chọn)"
                value={budget || ''}
                onChange={(e) => setBudget(e.target.value ? Number(e.target.value) : undefined)}
                style={{
                  flex: 1,
                  padding: 8,
                  borderRadius: 8,
                  border: '1px solid #ccc',
                }}
              />
              <button
                onClick={handleSendPrompt}
                disabled={loading || !prompt.trim()}
                style={{
                  backgroundColor: '#0F5132',
                  color: '#fff',
                  border: 'none',
                  padding: '8px 16px',
                  borderRadius: 8,
                  cursor: loading ? 'not-allowed' : 'pointer',
                  fontWeight: 600,
                }}
              >
                {loading ? 'Đang lọc...' : 'Tư vấn'}
              </button>
            </div>

            {/* AI Result Card */}
            {result && (
              <div
                style={{
                  marginTop: 16,
                  padding: 12,
                  backgroundColor: '#f8f9fa',
                  borderRadius: 8,
                  border: '1px solid #e9ecef',
                }}
              >
                <div style={{ fontWeight: 600, color: '#0F5132' }}>{result.dishName}</div>
                <div style={{ fontSize: 13, color: '#333', marginTop: 4 }}>
                  {result.assistantMessage}
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
