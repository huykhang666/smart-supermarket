import React, { useState, useRef, useEffect } from 'react';
import {
  aiService,
  type MealPlanChatResponse,
  type MealIngredientItem,
  type ChatMessage,
} from '../../services/aiService';

interface ChatItem {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  mealData?: MealPlanChatResponse;
  timestamp: Date;
}

interface SelectedIngredientState {
  item: MealIngredientItem;
  selected: boolean;
  quantity: number;
}

export const AiShoppingAssistant: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [inputText, setInputText] = useState('');
  const [loading, setLoading] = useState(false);
  const [prioritizeFlashSale, setPrioritizeFlashSale] = useState(true);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  // Danh sách tin nhắn hội thoại đa lượt
  const [messages, setMessages] = useState<ChatItem[]>([
    {
      id: 'welcome-1',
      role: 'assistant',
      content:
        'Xin chào bạn! 👋 Em là **Đầu Bếp & Trợ Lý Đi Chợ Thông Minh** của Smart Supermarket.\n\n' +
        'Em có thể tư vấn các món ăn ngon chuẩn vị Việt, bí quyết nấu nướng từ chuyên gia, hoặc gợi ý thực đơn theo ngân sách & số người ăn và tự động chọn mâm nguyên liệu tươi ngon đang có sẵn trong kho siêu thị.\n\n' +
        'Hôm nay bạn muốn nấu món gì hay cần em tư vấn mâm cơm cho mấy người ăn ạ?',
      timestamp: new Date(),
    },
  ]);

  // Modal tùy chỉnh mâm nguyên liệu
  const [activeMealModal, setActiveMealModal] = useState<MealPlanChatResponse | null>(null);
  const [customizingItems, setCustomizingItems] = useState<SelectedIngredientState[]>([]);

  const chatEndRef = useRef<HTMLDivElement>(null);

  // Tự động cuộn xuống tin nhắn mới nhất
  useEffect(() => {
    if (isOpen) {
      chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [messages, isOpen, loading]);

  // Khi mở modal mâm nguyên liệu, khởi tạo danh sách món được chọn
  const handleOpenMealModal = (meal: MealPlanChatResponse) => {
    setActiveMealModal(meal);
    setCustomizingItems(
      meal.ingredients.map((ing) => ({
        item: ing,
        selected: true,
        quantity: ing.quantityToBuy,
      }))
    );
  };

  const handleToggleItem = (productId: number) => {
    setCustomizingItems((prev) =>
      prev.map((i) =>
        i.item.productId === productId ? { ...i, selected: !i.selected } : i
      )
    );
  };

  const handleUpdateQuantity = (productId: number, delta: number) => {
    setCustomizingItems((prev) =>
      prev.map((i) => {
        if (i.item.productId === productId) {
          const newQty = Math.max(1, i.quantity + delta);
          return { ...i, quantity: newQty };
        }
        return i;
      })
    );
  };

  const handleToggleAllItems = (selectAll: boolean) => {
    setCustomizingItems((prev) =>
      prev.map((i) => ({ ...i, selected: selectAll }))
    );
  };

  // Tính toán tổng tiền theo thời gian thực khi khách chọn/bỏ bớt nguyên liệu
  const calculatedTotal = customizingItems
    .filter((i) => i.selected)
    .reduce((sum, i) => sum + i.item.finalPrice * i.quantity, 0);

  const calculatedSaved = customizingItems
    .filter((i) => i.selected && i.item.discountPercent > 0)
    .reduce((sum, i) => sum + (i.item.regularPrice - i.item.finalPrice) * i.quantity, 0);

  const selectedCount = customizingItems.filter((i) => i.selected).length;

  // Thêm các món đã chọn vào giỏ hàng
  const handleConfirmAddToCart = () => {
    const selectedItems = customizingItems.filter((i) => i.selected);
    if (selectedItems.length === 0) {
      setToastMessage('⚠️ Bạn chưa chọn nguyên liệu nào để thêm vào giỏ.');
      return;
    }

    try {
      const existingCartStr = localStorage.getItem('smart_supermarket_cart');
      const existingCart: { productId: number; productName: string; price: number; quantity: number; unit: string; imageUrl: string }[] =
        existingCartStr ? JSON.parse(existingCartStr) : [];

      selectedItems.forEach(({ item, quantity }) => {
        const found = existingCart.find((c) => c.productId === item.productId);
        if (found) {
          found.quantity += quantity;
        } else {
          existingCart.push({
            productId: item.productId,
            productName: item.productName,
            price: item.finalPrice,
            quantity: quantity,
            unit: item.unit,
            imageUrl: item.imageUrl,
          });
        }
      });

      localStorage.setItem('smart_supermarket_cart', JSON.stringify(existingCart));
      window.dispatchEvent(new Event('cart-updated'));

      const dishTitle = activeMealModal?.dishName || 'món ăn';
      setToastMessage(`✓ Đã thêm ${selectedItems.length} nguyên liệu món "${dishTitle}" vào giỏ hàng!`);

      // Bổ sung tin nhắn xác nhận của hệ thống vào chat
      setMessages((prev) => [
        ...prev,
        {
          id: `cart-${Date.now()}`,
          role: 'assistant',
          content: `🛒 **Đã thêm thành công:** ${selectedItems.length} nguyên liệu món **"${dishTitle}"** (tổng ${calculatedTotal.toLocaleString()} đ) vào giỏ hàng của bạn. Bạn có thể mở giỏ hàng để đặt hàng ngay!`,
          timestamp: new Date(),
        },
      ]);

      setActiveMealModal(null);
      setTimeout(() => setToastMessage(null), 5000);
    } catch (e) {
      console.error('Lỗi khi nạp giỏ hàng:', e);
      setToastMessage('Lỗi khi lưu giỏ hàng.');
    }
  };

  // Gửi tin nhắn chat
  const handleSendMessage = async (customPrompt?: string) => {
    const textToSend = (customPrompt ?? inputText).trim();
    if (!textToSend || loading) return;

    const userMsg: ChatItem = {
      id: `user-${Date.now()}`,
      role: 'user',
      content: textToSend,
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, userMsg]);
    setInputText('');
    setLoading(true);
    setToastMessage(null);

    // Chuẩn bị lịch sử hội thoại gửi lên Backend
    const historyPayload: ChatMessage[] = messages
      .slice(-6)
      .map((m) => ({ role: m.role, content: m.content }));

    try {
      const response = await aiService.planMealAndShop({
        userPrompt: textToSend,
        prioritizePromotions: prioritizeFlashSale,
        conversationHistory: historyPayload,
      });

      const assistantMsg: ChatItem = {
        id: `assistant-${Date.now()}`,
        role: 'assistant',
        content: response.assistantMessage || 'Em đã tìm thấy các nguyên liệu phù hợp với yêu cầu của bạn.',
        mealData: response.hasMealSuggestion ? response : undefined,
        timestamp: new Date(),
      };

      setMessages((prev) => [...prev, assistantMsg]);
    } catch (err) {
      console.error('Lỗi khi gọi AI:', err);
      setMessages((prev) => [
        ...prev,
        {
          id: `err-${Date.now()}`,
          role: 'assistant',
          content: '⚠️ Không thể kết nối tới máy chủ AI hoặc kho hàng. Bạn vui lòng kiểm tra lại kết nối mạng và thử lại nhé!',
          timestamp: new Date(),
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  // Các gợi ý câu hỏi nhanh
  const quickPills = [
    '🍲 Lẩu Thái hải sản 4 người',
    '🥩 Thịt ba chỉ kho tàu',
    '🥖 Bò Úc sốt vang kèm bánh mì',
    '🥗 Mâm cơm chay thanh đạm',
    '🥬 Cách luộc rau muống xanh giòn',
    '🥩 Bí quyết ướp thịt bò mềm mọng',
  ];

  return (
    <div style={{ position: 'fixed', bottom: 20, right: 20, zIndex: 9999, fontFamily: 'Segoe UI, Roboto, sans-serif' }}>
      {/* Floating Button mở chat */}
      {!isOpen && (
        <button
          onClick={() => setIsOpen(true)}
          style={{
            backgroundColor: '#0F5132',
            color: '#fff',
            border: 'none',
            borderRadius: '50px',
            padding: '14px 24px',
            cursor: 'pointer',
            boxShadow: '0 8px 24px rgba(15,81,50,0.38)',
            display: 'flex',
            alignItems: 'center',
            gap: '12px',
            fontWeight: 700,
            fontSize: '15px',
            transition: 'all 0.25s ease',
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.transform = 'scale(1.04)';
            e.currentTarget.style.boxShadow = '0 12px 30px rgba(15,81,50,0.48)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.transform = 'scale(1)';
            e.currentTarget.style.boxShadow = '0 8px 24px rgba(15,81,50,0.38)';
          }}
        >
          <span style={{ fontSize: '22px' }}>🤖</span>
          <span>Trợ Lý Đầu Bếp & Đi Chợ AI</span>
        </button>
      )}

      {/* Cửa sổ Chat chuẩn phong cách ChatGPT */}
      {isOpen && (
        <div
          style={{
            width: 520,
            maxWidth: 'calc(100vw - 32px)',
            height: 740,
            maxHeight: 'calc(100vh - 40px)',
            backgroundColor: '#ffffff',
            borderRadius: 18,
            boxShadow: '0 16px 50px rgba(0,0,0,0.25)',
            display: 'flex',
            flexDirection: 'column',
            overflow: 'hidden',
            border: '1px solid #cbd5e1',
          }}
        >
          {/* Header */}
          <div
            style={{
              backgroundColor: '#0F5132',
              color: '#fff',
              padding: '14px 18px',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <div
                style={{
                  backgroundColor: 'rgba(255,255,255,0.2)',
                  width: 40,
                  height: 40,
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: 22,
                }}
              >
                🤖
              </div>
              <div>
                <strong style={{ fontSize: 16, display: 'block' }}>Đầu Bếp & Trợ Lý Đi Chợ AI</strong>
                <div style={{ fontSize: 12, opacity: 0.9 }}>
                  Tư vấn món ăn chuẩn vị • Mâm nguyên liệu từ kho siêu thị
                </div>
              </div>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <button
                onClick={() =>
                  setMessages([
                    {
                      id: `welcome-${Date.now()}`,
                      role: 'assistant',
                      content:
                        'Chào bạn! Mình có thể tư vấn món ăn ngon, bí quyết nấu nướng chuẩn vị, hoặc lên thực đơn với nguyên liệu từ kho siêu thị. Bạn muốn hỏi gì ạ?',
                      timestamp: new Date(),
                    },
                  ])
                }
                style={{
                  background: 'rgba(255,255,255,0.16)',
                  border: 'none',
                  color: '#fff',
                  borderRadius: 6,
                  padding: '5px 10px',
                  fontSize: 12,
                  cursor: 'pointer',
                }}
                title="Bắt đầu đoạn chat mới"
              >
                🧹 Xóa chat
              </button>
              <button
                onClick={() => setIsOpen(false)}
                style={{
                  background: 'rgba(255,255,255,0.16)',
                  border: 'none',
                  color: '#fff',
                  width: 30,
                  height: 30,
                  borderRadius: '50%',
                  fontSize: 15,
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
                title="Thu nhỏ cửa sổ"
              >
                ✕
              </button>
            </div>
          </div>

          {/* Toast thông báo nhanh */}
          {toastMessage && (
            <div
              style={{
                backgroundColor: '#dcfce7',
                color: '#166534',
                padding: '8px 16px',
                fontSize: 12.5,
                fontWeight: 600,
                borderBottom: '1px solid #86efac',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
              }}
            >
              <span>{toastMessage}</span>
              <a href="/cart" style={{ color: '#15803d', textDecoration: 'underline', fontWeight: 700 }}>
                Xem giỏ hàng →
              </a>
            </div>
          )}

          {/* Thân cửa sổ chat: Cuộn tin nhắn */}
          <div
            style={{
              flex: 1,
              overflowY: 'auto',
              padding: '16px',
              backgroundColor: '#f8fafc',
              display: 'flex',
              flexDirection: 'column',
              gap: 14,
            }}
          >
            {messages.map((msg) => (
              <div
                key={msg.id}
                style={{
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: msg.role === 'user' ? 'flex-end' : 'flex-start',
                }}
              >
                {/* Bong bóng chat */}
                <div
                  style={{
                    maxWidth: '85%',
                    backgroundColor: msg.role === 'user' ? '#0F5132' : '#ffffff',
                    color: msg.role === 'user' ? '#ffffff' : '#1e293b',
                    padding: '12px 16px',
                    borderRadius: msg.role === 'user' ? '16px 16px 4px 16px' : '16px 16px 16px 4px',
                    boxShadow: '0 2px 8px rgba(0,0,0,0.06)',
                    border: msg.role === 'user' ? 'none' : '1px solid #e2e8f0',
                    fontSize: 13.5,
                    lineHeight: 1.6,
                    whiteSpace: 'pre-line',
                  }}
                >
                  {/* Nội dung tin nhắn dạng văn bản/markdown đơn giản */}
                  <div>
                    {msg.content.split('\n').map((line, idx) => {
                      // Đậm chữ với **
                      const parts = line.split(/(\*\*.*?\*\*)/g);
                      return (
                        <div key={idx} style={{ minHeight: line.trim() ? undefined : 8 }}>
                          {parts.map((p, pIdx) => {
                            if (p.startsWith('**') && p.endsWith('**')) {
                              return <strong key={pIdx}>{p.slice(2, -2)}</strong>;
                            }
                            return p;
                          })}
                        </div>
                      );
                    })}
                  </div>

                  {/* Nút bấm tương tác nếu AI đề xuất mâm món ăn */}
                  {msg.mealData && msg.mealData.hasMealSuggestion && (
                    <div
                      style={{
                        marginTop: 12,
                        padding: 12,
                        backgroundColor: '#f0fdf4',
                        borderRadius: 10,
                        border: '1px solid #bbf7d0',
                      }}
                    >
                      <div
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'space-between',
                          marginBottom: 8,
                        }}
                      >
                        <strong style={{ fontSize: 14, color: '#0F5132' }}>
                          🍽️ {msg.mealData.dishName}
                        </strong>
                        <span
                          style={{
                            fontSize: 11.5,
                            backgroundColor: '#dcfce7',
                            color: '#166534',
                            fontWeight: 700,
                            padding: '2px 8px',
                            borderRadius: 12,
                          }}
                        >
                          {msg.mealData.servings} người • {msg.mealData.totalEstimatedCost.toLocaleString()} đ
                        </span>
                      </div>

                      <div style={{ fontSize: 12, color: '#475569', marginBottom: 10 }}>
                        Kho siêu thị có sẵn {msg.mealData.ingredients.length} nguyên liệu. Bạn có thể xem mâm chi tiết và tùy ý thêm/bớt món.
                      </div>

                      {/* Nút hành động chính theo đúng yêu cầu người dùng */}
                      <button
                        onClick={() => handleOpenMealModal(msg.mealData!)}
                        style={{
                          width: '100%',
                          backgroundColor: '#2563eb',
                          color: '#fff',
                          border: 'none',
                          borderRadius: 8,
                          padding: '10px 14px',
                          cursor: 'pointer',
                          fontWeight: 700,
                          fontSize: 13,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          gap: 6,
                          boxShadow: '0 2px 6px rgba(37,99,235,0.28)',
                        }}
                      >
                        👉 Bạn có muốn chọn mâm nguyên liệu cho món này?
                      </button>
                    </div>
                  )}
                </div>

                {/* Nhãn thời gian */}
                <span
                  style={{
                    fontSize: 10.5,
                    color: '#94a3b8',
                    marginTop: 4,
                    padding: '0 6px',
                  }}
                >
                  {msg.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </span>
              </div>
            ))}

            {/* Trạng thái AI đang suy nghĩ */}
            {loading && (
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, color: '#0F5132', fontSize: 13, padding: '4px 8px' }}>
                <span style={{ fontSize: 18 }}>🤖</span>
                <span>Đang tra cứu dữ liệu kho hàng & chuẩn bị công thức chuẩn vị...</span>
              </div>
            )}

            <div ref={chatEndRef} />
          </div>

          {/* Thanh gợi ý câu hỏi nhanh */}
          <div
            style={{
              padding: '6px 12px',
              backgroundColor: '#ffffff',
              borderTop: '1px solid #e2e8f0',
              display: 'flex',
              gap: 6,
              overflowX: 'auto',
              whiteSpace: 'nowrap',
            }}
          >
            {quickPills.map((pill, idx) => (
              <button
                key={idx}
                onClick={() => handleSendMessage(pill)}
                disabled={loading}
                style={{
                  fontSize: 11.5,
                  padding: '5px 10px',
                  borderRadius: 16,
                  border: '1px solid #cbd5e1',
                  backgroundColor: '#f1f5f9',
                  color: '#334155',
                  cursor: loading ? 'not-allowed' : 'pointer',
                  fontWeight: 500,
                  flexShrink: 0,
                }}
              >
                {pill}
              </button>
            ))}
          </div>

          {/* Ô nhập tin nhắn */}
          <div
            style={{
              padding: '10px 14px',
              backgroundColor: '#ffffff',
              borderTop: '1px solid #e2e8f0',
            }}
          >
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <input
                type="text"
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    handleSendMessage();
                  }
                }}
                placeholder="Nhắn tin với AI (VD: Lẩu Thái 4 người tầm 350k, cách ướp sườn...)"
                disabled={loading}
                style={{
                  flex: 1,
                  padding: '10px 14px',
                  borderRadius: 24,
                  border: '1px solid #cbd5e1',
                  fontSize: 13.5,
                  outline: 'none',
                }}
              />
              <button
                onClick={() => handleSendMessage()}
                disabled={loading || !inputText.trim()}
                style={{
                  backgroundColor: '#0F5132',
                  color: '#fff',
                  border: 'none',
                  borderRadius: '50%',
                  width: 40,
                  height: 40,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  cursor: loading || !inputText.trim() ? 'not-allowed' : 'pointer',
                  opacity: loading || !inputText.trim() ? 0.6 : 1,
                  fontSize: 16,
                  boxShadow: '0 2px 6px rgba(15,81,50,0.3)',
                }}
                title="Gửi tin nhắn"
              >
                ➤
              </button>
            </div>

            {/* Checkbox ưu tiên flash sale */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 6, fontSize: 11.5, color: '#64748b' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: 5, cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={prioritizeFlashSale}
                  onChange={(e) => setPrioritizeFlashSale(e.target.checked)}
                />
                <span>⚡ Ưu tiên gom hàng cận date / Flash Sale giảm 30% để tiết kiệm</span>
              </label>
              <span>Nhấn Enter để gửi</span>
            </div>
          </div>
        </div>
      )}

      {/* Modal Tùy chỉnh & Chọn mâm nguyên liệu thêm vào giỏ hàng */}
      {activeMealModal && (
        <div
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0,0,0,0.5)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 10000,
            padding: 16,
          }}
        >
          <div
            style={{
              backgroundColor: '#fff',
              borderRadius: 16,
              width: 560,
              maxWidth: '100%',
              maxHeight: '90vh',
              display: 'flex',
              flexDirection: 'column',
              boxShadow: '0 20px 60px rgba(0,0,0,0.3)',
              overflow: 'hidden',
            }}
          >
            {/* Modal Header */}
            <div
              style={{
                backgroundColor: '#0F5132',
                color: '#fff',
                padding: '16px 20px',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
              }}
            >
              <div>
                <strong style={{ fontSize: 17, display: 'block' }}>
                  📋 Tùy Chỉnh Mâm Nguyên Liệu: {activeMealModal.dishName}
                </strong>
                <div style={{ fontSize: 12, opacity: 0.9, marginTop: 2 }}>
                  Tùy ý chọn hoặc bỏ nguyên liệu • Giá tiền tự động tính lại theo thời gian thực
                </div>
              </div>
              <button
                onClick={() => setActiveMealModal(null)}
                style={{
                  background: 'none',
                  border: 'none',
                  color: '#fff',
                  fontSize: 20,
                  cursor: 'pointer',
                }}
              >
                ✕
              </button>
            </div>

            {/* Thanh tổng quan chi phí trực tiếp */}
            <div
              style={{
                backgroundColor: '#f1f5f9',
                padding: '12px 20px',
                borderBottom: '1px solid #e2e8f0',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                fontSize: 13,
              }}
            >
              <div>
                Đã chọn: <strong style={{ color: '#0F5132' }}>{selectedCount} / {customizingItems.length}</strong> món
              </div>
              <div style={{ textAlign: 'right' }}>
                {calculatedSaved > 0 && (
                  <span style={{ color: '#16a34a', fontWeight: 600, marginRight: 12, fontSize: 12 }}>
                    Tiết kiệm: -{calculatedSaved.toLocaleString()} đ
                  </span>
                )}
                <span>
                  Tổng tiền: <strong style={{ fontSize: 16, color: '#dc2626' }}>{calculatedTotal.toLocaleString()} đ</strong>
                </span>
              </div>
            </div>

            {/* Quick buttons: Chọn tất cả / Bỏ tất cả */}
            <div
              style={{
                padding: '8px 20px',
                backgroundColor: '#fafafa',
                borderBottom: '1px solid #f1f5f9',
                display: 'flex',
                justifyContent: 'space-between',
                fontSize: 12,
              }}
            >
              <button
                onClick={() => handleToggleAllItems(true)}
                style={{
                  background: 'none',
                  border: 'none',
                  color: '#2563eb',
                  fontWeight: 600,
                  cursor: 'pointer',
                  padding: 0,
                }}
              >
                ✓ Chọn tất cả
              </button>
              <button
                onClick={() => handleToggleAllItems(false)}
                style={{
                  background: 'none',
                  border: 'none',
                  color: '#64748b',
                  fontWeight: 600,
                  cursor: 'pointer',
                  padding: 0,
                }}
              >
                ✕ Bỏ chọn tất cả
              </button>
            </div>

            {/* Danh sách nguyên liệu với checkbox & điều chỉnh số lượng */}
            <div
              style={{
                flex: 1,
                overflowY: 'auto',
                padding: '12px 20px',
                display: 'flex',
                flexDirection: 'column',
                gap: 10,
              }}
            >
              {customizingItems.map(({ item, selected, quantity }) => (
                <div
                  key={item.productId}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 12,
                    padding: '10px 14px',
                    borderRadius: 10,
                    backgroundColor: selected ? (item.isPromotionOrNearExpiry ? '#fffbeb' : '#ffffff') : '#f8fafc',
                    border: selected
                      ? (item.isPromotionOrNearExpiry ? '1px solid #fde047' : '1px solid #cbd5e1')
                      : '1px solid #e2e8f0',
                    opacity: selected ? 1 : 0.6,
                    transition: 'all 0.15s ease',
                  }}
                >
                  {/* Checkbox */}
                  <input
                    type="checkbox"
                    checked={selected}
                    onChange={() => handleToggleItem(item.productId)}
                    style={{ width: 18, height: 18, cursor: 'pointer', accentColor: '#0F5132' }}
                  />

                  {/* Chi tiết sản phẩm */}
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ fontWeight: 600, fontSize: 14, color: selected ? '#0f172a' : '#64748b' }}>
                      {item.productName}
                    </div>
                    <div style={{ fontSize: 11.5, color: '#64748b', marginTop: 2 }}>
                      Đơn vị: {item.unit} | Tồn kho: {item.inStockQuantity}
                    </div>
                    {item.promotionNote && (
                      <div style={{ fontSize: 11, color: '#b45309', fontWeight: 700, marginTop: 2 }}>
                        ⚡ {item.promotionNote}
                      </div>
                    )}
                  </div>

                  {/* Nút điều chỉnh số lượng +/- */}
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <button
                      onClick={() => handleUpdateQuantity(item.productId, -1)}
                      disabled={!selected || quantity <= 1}
                      style={{
                        width: 26,
                        height: 26,
                        borderRadius: 6,
                        border: '1px solid #cbd5e1',
                        backgroundColor: '#fff',
                        cursor: selected && quantity > 1 ? 'pointer' : 'not-allowed',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        fontWeight: 700,
                        fontSize: 14,
                      }}
                    >
                      -
                    </button>
                    <span style={{ minWidth: 24, textAlign: 'center', fontWeight: 700, fontSize: 13 }}>
                      {quantity}
                    </span>
                    <button
                      onClick={() => handleUpdateQuantity(item.productId, 1)}
                      disabled={!selected}
                      style={{
                        width: 26,
                        height: 26,
                        borderRadius: 6,
                        border: '1px solid #cbd5e1',
                        backgroundColor: '#fff',
                        cursor: selected ? 'pointer' : 'not-allowed',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        fontWeight: 700,
                        fontSize: 14,
                      }}
                    >
                      +
                    </button>
                  </div>

                  {/* Giá tiền thành phần */}
                  <div style={{ textAlign: 'right', minWidth: 80 }}>
                    {item.discountPercent > 0 && (
                      <div style={{ fontSize: 11, color: '#94a3b8', textDecoration: 'line-through' }}>
                        {(item.regularPrice * quantity).toLocaleString()} đ
                      </div>
                    )}
                    <div
                      style={{
                        fontWeight: 700,
                        fontSize: 14,
                        color: item.discountPercent > 0 ? '#dc2626' : '#1e293b',
                      }}
                    >
                      {(item.finalPrice * quantity).toLocaleString()} đ
                    </div>
                  </div>
                </div>
              ))}

              {/* Mẹo nấu ăn từ đầu bếp */}
              {activeMealModal.cookingTips && (
                <div
                  style={{
                    backgroundColor: '#f8fafc',
                    borderRadius: 10,
                    padding: 12,
                    border: '1px solid #e2e8f0',
                    marginTop: 6,
                  }}
                >
                  <strong style={{ fontSize: 13, color: '#0F5132', display: 'block', marginBottom: 4 }}>
                    👨‍🍳 Mẹo nấu nhanh từ Đầu Bếp:
                  </strong>
                  <div style={{ fontSize: 12, color: '#475569', whiteSpace: 'pre-line', lineHeight: 1.5 }}>
                    {activeMealModal.cookingTips}
                  </div>
                </div>
              )}
            </div>

            {/* Modal Footer: Nút Thêm vào giỏ hàng */}
            <div
              style={{
                padding: '14px 20px',
                borderTop: '1px solid #e2e8f0',
                backgroundColor: '#ffffff',
                display: 'flex',
                gap: 12,
                justifyContent: 'flex-end',
              }}
            >
              <button
                onClick={() => setActiveMealModal(null)}
                style={{
                  padding: '10px 18px',
                  borderRadius: 8,
                  border: '1px solid #cbd5e1',
                  backgroundColor: '#fff',
                  color: '#475569',
                  fontWeight: 600,
                  fontSize: 13,
                  cursor: 'pointer',
                }}
              >
                Đóng
              </button>
              <button
                onClick={handleConfirmAddToCart}
                disabled={selectedCount === 0}
                style={{
                  padding: '10px 22px',
                  borderRadius: 8,
                  border: 'none',
                  backgroundColor: selectedCount > 0 ? '#0F5132' : '#94a3b8',
                  color: '#fff',
                  fontWeight: 700,
                  fontSize: 13.5,
                  cursor: selectedCount > 0 ? 'pointer' : 'not-allowed',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 8,
                  boxShadow: selectedCount > 0 ? '0 4px 12px rgba(15,81,50,0.3)' : 'none',
                }}
              >
                🛒 Thêm {selectedCount} món vào giỏ hàng ({calculatedTotal.toLocaleString()} đ)
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
