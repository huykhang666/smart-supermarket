import React, { useState, useEffect, useCallback } from 'react';
import { orderApi } from '../shared/api/orderApi';
import { promotionApi } from '../shared/api/promotionApi';
import { formatCurrency } from '../shared/utils/formatCurrency';
import type {
  Order,
  OrderPagedResult,
  Product,
  CreateOrderRequest,
  CreateOrderDetailRequest,
} from '../shared/types';

type TabKey = 'history' | 'create';

function formatPaymentMethodName(pm: any): string {
  if (pm === 1 || pm === 'Cash') return 'Tiền mặt';
  if (pm === 2 || pm === 'QRCode') return 'Chuyển khoản (VietQR)';
  if (pm === 3 || pm === 'CreditCard') return 'Thẻ ngân hàng';
  return String(pm ?? 'Tiền mặt');
}

function formatStatusName(status: any): { label: string; color: string; bg: string } {
  if (status === 1 || status === 'Completed') {
    return { label: 'Hoàn tất', color: '#16a34a', bg: '#dcfce7' };
  }
  if (status === 2 || status === 'Cancelled') {
    return { label: 'Đã hủy', color: '#dc2626', bg: '#fee2e2' };
  }
  return { label: 'Đang xử lý', color: '#ca8a04', bg: '#fef9c3' };
}

export const OrdersPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TabKey>('history');

  // Order list state
  const [data, setData] = useState<OrderPagedResult | null>(null);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [loading, setLoading] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [printingId, setPrintingId] = useState<number | null>(null);

  // New order state
  const [products, setProducts] = useState<Product[]>([]);
  const [selectedProductId, setSelectedProductId] = useState<number>(0);
  const [cartItems, setCartItems] = useState<{ product: Product; quantity: number }[]>([]);
  const [customerId, setCustomerId] = useState<string>('');
  const [employeeId, setEmployeeId] = useState<number>(1);
  const [promotionCode, setPromotionCode] = useState<string>('');
  const [paymentMethod, setPaymentMethod] = useState<number>(1);
  const [cashGiven, setCashGiven] = useState<number>(0);
  const [submitting, setSubmitting] = useState(false);
  const [appliedDiscount, setAppliedDiscount] = useState<number>(0);

  // Load orders
  const loadOrders = useCallback(async () => {
    setLoading(true);
    try {
      const res = await orderApi.getPaged(page, 10, undefined, undefined, undefined, statusFilter);
      setData(res.data);
    } catch (e: any) {
      alert(`Lỗi tải đơn hàng: ${e.message}`);
    } finally {
      setLoading(false);
    }
  }, [page, statusFilter]);

  // Load products for POS
  const loadProducts = async () => {
    try {
      // Using mock or fetching from products if available
      const token = localStorage.getItem('access_token');
      const res = await fetch('https://localhost:7123/api/v1/products', {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (res.ok) {
        const json = await res.json();
        if (json.data && Array.isArray(json.data)) {
          setProducts(json.data);
          if (json.data.length > 0) setSelectedProductId(json.data[0].productId);
        }
      }
    } catch {
      // fallback sample products
      setProducts([
        { productId: 1, productName: 'Sữa chua Vinamilk 100g', barcode: '8934588012345', price: 12000, unit: 'Hộp', categoryId: 1, categoryName: 'Sữa', quantityInStock: 80 },
        { productId: 2, productName: 'Bánh mì tươi Kinh Đô', barcode: '8934588012346', price: 15000, unit: 'Ổ', categoryId: 2, categoryName: 'Bánh', quantityInStock: 45 },
        { productId: 3, productName: 'Bàn chải Colgate Slim', barcode: '8934588012347', price: 32000, unit: 'Cái', categoryId: 3, categoryName: 'Chăm sóc cá nhân', quantityInStock: 60 },
        { productId: 4, productName: 'Kem đánh răng PS 200g', barcode: '8934588012348', price: 38000, unit: 'Tuýp', categoryId: 3, categoryName: 'Chăm sóc cá nhân', quantityInStock: 50 },
      ]);
      setSelectedProductId(1);
    }
  };

  useEffect(() => {
    loadOrders();
  }, [loadOrders]);

  useEffect(() => {
    loadProducts();
  }, []);

  // Handle Export / Download PDF
  const handleDownloadPdf = async (orderId: number) => {
    setPrintingId(orderId);
    try {
      await orderApi.downloadInvoicePdf(orderId);
    } catch (e: any) {
      alert(`Lỗi xuất PDF: ${e.message}`);
    } finally {
      setPrintingId(null);
    }
  };

  // Handle Print PDF
  const handlePrintPdf = async (orderId: number) => {
    setPrintingId(orderId);
    try {
      await orderApi.printInvoicePdf(orderId);
    } catch (e: any) {
      alert(`Lỗi in hóa đơn: ${e.message}`);
    } finally {
      setPrintingId(null);
    }
  };

  // Handle Cancel Order
  const handleCancelOrder = async (orderId: number) => {
    if (!confirm(`Bạn có chắc chắn muốn hủy đơn hàng #HD-${String(orderId).padStart(6, '0')}?`)) return;
    try {
      await orderApi.cancel(orderId);
      alert('Hủy đơn hàng thành công! Đã hoàn trả tồn kho.');
      loadOrders();
      if (selectedOrder?.orderId === orderId) {
        setSelectedOrder(null);
      }
    } catch (e: any) {
      alert(`Lỗi hủy đơn: ${e.message}`);
    }
  };

  // Cart operations
  const addToCart = () => {
    const prod = products.find((p) => p.productId === selectedProductId);
    if (!prod) return;

    setCartItems((prev) => {
      const idx = prev.findIndex((i) => i.product.productId === prod.productId);
      if (idx >= 0) {
        const next = [...prev];
        next[idx].quantity += 1;
        return next;
      }
      return [...prev, { product: prod, quantity: 1 }];
    });
  };

  const updateCartQty = (productId: number, qty: number) => {
    if (qty <= 0) {
      setCartItems((prev) => prev.filter((i) => i.product.productId !== productId));
    } else {
      setCartItems((prev) =>
        prev.map((i) => (i.product.productId === productId ? { ...i, quantity: qty } : i))
      );
    }
  };

  const subTotal = cartItems.reduce((acc, i) => acc + i.product.price * i.quantity, 0);
  const finalTotal = Math.max(0, subTotal - appliedDiscount);
  const changeDue = Math.max(0, cashGiven - finalTotal);

  // Apply promotion code check
  const handleCheckPromotion = async () => {
    if (!promotionCode.trim()) return;
    try {
      const res = await promotionApi.apply({
        promotionCode: promotionCode.trim().toUpperCase(),
        orderTotalAmount: subTotal,
        items: cartItems.map((i) => ({
          productId: i.product.productId,
          barcode: i.product.barcode,
          productName: i.product.productName,
          categoryId: i.product.categoryId,
          quantity: i.quantity,
          unitPrice: i.product.price,
        })),
      });

      if (res.data.isSuccess) {
        setAppliedDiscount(res.data.discountAmount);
        alert(`Áp dụng thành công: ${res.data.message}\nGiảm: ${formatCurrency(res.data.discountAmount)}`);
      } else {
        setAppliedDiscount(0);
        alert(res.data.message || 'Mã khuyến mãi không khả dụng');
      }
    } catch (e: any) {
      alert(`Lỗi kiểm tra mã KM: ${e.message}`);
    }
  };

  // Submit and Save Order
  const handleSaveOrder = async (printImmediately = false) => {
    if (cartItems.length === 0) {
      alert('Vui lòng thêm ít nhất một sản phẩm vào đơn hàng.');
      return;
    }

    setSubmitting(true);
    try {
      const items: CreateOrderDetailRequest[] = cartItems.map((i) => ({
        productId: i.product.productId,
        quantity: i.quantity,
        unitPrice: i.product.price,
      }));

      const req: CreateOrderRequest = {
        employeeId: employeeId || 1,
        branchId: 1,
        customerId: customerId ? Number(customerId) : undefined,
        promotionCode: promotionCode ? promotionCode.trim().toUpperCase() : undefined,
        paymentMethod: paymentMethod,
        payments: [
          {
            paymentMethod: paymentMethod,
            amountPaid: paymentMethod === 1 && cashGiven > 0 ? cashGiven : finalTotal,
          },
        ],
        items: items,
      };

      const res = await orderApi.create(req);
      const createdOrder = res.data;

      alert(`Lưu hóa đơn thành công! Mã đơn: #HD-${String(createdOrder.orderId).padStart(6, '0')}`);

      const savedId = createdOrder.orderId;

      // Reset cart
      setCartItems([]);
      setPromotionCode('');
      setAppliedDiscount(0);
      setCashGiven(0);
      setCustomerId('');

      if (printImmediately && savedId) {
        await handlePrintPdf(savedId);
      }

      // Switch to history tab
      setActiveTab('history');
      loadOrders();
    } catch (e: any) {
      alert(`Lỗi tạo đơn hàng: ${e.message}`);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div style={{ padding: '24px 32px', maxWidth: 1200, margin: '0 auto', textAlign: 'left' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
        <div>
          <h2 style={{ margin: 0 }}>Quản Lý & Xuất / In Hóa Đơn PDF (07_Order)</h2>
          <p style={{ margin: '4px 0 0', color: '#6b7280', fontSize: 14 }}>
            Lưu hóa đơn bán hàng POS, quản lý lịch sử thanh toán và xuất file / in trực tiếp hóa đơn PDF chuẩn A5.
          </p>
        </div>
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 0, borderBottom: '2px solid #e5e7eb', marginBottom: 20 }}>
        <button
          onClick={() => setActiveTab('history')}
          style={{
            padding: '10px 20px',
            border: 'none',
            borderBottom: activeTab === 'history' ? '2px solid #2563eb' : '2px solid transparent',
            background: 'none',
            cursor: 'pointer',
            fontWeight: activeTab === 'history' ? 600 : 400,
            color: activeTab === 'history' ? '#2563eb' : '#6b7280',
            fontSize: 14,
            marginBottom: -2,
          }}
        >
          📄 Danh Sách Hóa Đơn & Xuất / In PDF
        </button>
        <button
          onClick={() => setActiveTab('create')}
          style={{
            padding: '10px 20px',
            border: 'none',
            borderBottom: activeTab === 'create' ? '2px solid #2563eb' : '2px solid transparent',
            background: 'none',
            cursor: 'pointer',
            fontWeight: activeTab === 'create' ? 600 : 400,
            color: activeTab === 'create' ? '#2563eb' : '#6b7280',
            fontSize: 14,
            marginBottom: -2,
          }}
        >
          🛒 Lập & Lưu Hóa Đơn Mới (POS)
        </button>
      </div>

      {/* TAB 1: HISTORY & PDF EXPORT */}
      {activeTab === 'history' && (
        <div>
          {/* Filters */}
          <div style={{ display: 'flex', gap: 12, alignItems: 'center', marginBottom: 16, flexWrap: 'wrap' }}>
            <span style={{ fontSize: 13, fontWeight: 500, color: '#374151' }}>Trạng thái:</span>
            <select
              style={selectStyle}
              value={statusFilter === undefined ? '' : statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value === '' ? undefined : Number(e.target.value));
                setPage(1);
              }}
            >
              <option value="">Tất cả trạng thái</option>
              <option value="1">Hoàn tất (Completed)</option>
              <option value="2">Đã hủy (Cancelled)</option>
            </select>

            <button onClick={loadOrders} style={btnSecondary}>
              🔄 Làm mới
            </button>
          </div>

          {loading ? (
            <p>Đang tải danh sách hóa đơn...</p>
          ) : !data || data.items.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '40px 0', color: '#9ca3af' }}>
              <p style={{ fontSize: 16 }}>Chưa có đơn hàng nào trong hệ thống.</p>
              <button onClick={() => setActiveTab('create')} style={btnPrimary}>
                + Lập hóa đơn đầu tiên ngay
              </button>
            </div>
          ) : (
            <>
              <table style={tableStyle}>
                <thead>
                  <tr>
                    <th style={thStyle}>Mã HĐ</th>
                    <th style={thStyle}>Thời Gian</th>
                    <th style={thStyle}>Thu Ngân</th>
                    <th style={thStyle}>Khách Hàng</th>
                    <th style={thStyle}>Tạm Tính</th>
                    <th style={thStyle}>Giảm Giá</th>
                    <th style={thStyle}>Thanh Toán</th>
                    <th style={thStyle}>Hình Thức</th>
                    <th style={thStyle}>Trạng Thái</th>
                    <th style={{ ...thStyle, textAlign: 'center' }}>Thao Tác</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((order) => {
                    const st = formatStatusName(order.status);
                    const isCompleted = order.status === 1 || order.status === 'Completed';
                    const isBusy = printingId === order.orderId;

                    return (
                      <tr key={order.orderId}>
                        <td style={tdStyle}>
                          <strong style={{ color: '#1e40af' }}>
                            HD-{String(order.orderId).padStart(6, '0')}
                          </strong>
                        </td>
                        <td style={{ ...tdStyle, fontSize: 12 }}>
                          {new Date(order.orderDate).toLocaleString('vi-VN')}
                        </td>
                        <td style={tdStyle}>{order.employeeName || `NV #${order.employeeId}`}</td>
                        <td style={tdStyle}>
                          {order.customerName || 'Khách lẻ'}
                          {order.customerPhone && (
                            <div style={{ fontSize: 11, color: '#6b7280' }}>{order.customerPhone}</div>
                          )}
                        </td>
                        <td style={tdStyle}>{formatCurrency(order.totalAmount)}</td>
                        <td style={{ ...tdStyle, color: order.discountAmount > 0 ? '#dc2626' : undefined }}>
                          {order.discountAmount > 0 ? `-${formatCurrency(order.discountAmount)}` : '0 đ'}
                        </td>
                        <td style={{ ...tdStyle, fontWeight: 700, color: '#0d6efd' }}>
                          {formatCurrency(order.finalAmount)}
                        </td>
                        <td style={tdStyle}>{formatPaymentMethodName(order.paymentMethod)}</td>
                        <td style={tdStyle}>
                          <span
                            style={{
                              ...badgeStyle,
                              color: st.color,
                              background: st.bg,
                            }}
                          >
                            {st.label}
                          </span>
                        </td>
                        <td style={{ ...tdStyle, textAlign: 'center', whiteSpace: 'nowrap' }}>
                          <button
                            onClick={() => setSelectedOrder(order)}
                            title="Xem chi tiết"
                            style={btnSmallAction}
                          >
                            👁 Chi tiết
                          </button>{' '}
                          <button
                            onClick={() => handlePrintPdf(order.orderId)}
                            disabled={isBusy}
                            title="In hóa đơn PDF"
                            style={{ ...btnSmallAction, background: '#eff6ff', color: '#1d4ed8' }}
                          >
                            🖨 In PDF
                          </button>{' '}
                          <button
                            onClick={() => handleDownloadPdf(order.orderId)}
                            disabled={isBusy}
                            title="Xuất file PDF"
                            style={{ ...btnSmallAction, background: '#f0fdf4', color: '#15803d' }}
                          >
                            ⬇ Xuất PDF
                          </button>{' '}
                          {isCompleted && (
                            <button
                              onClick={() => handleCancelOrder(order.orderId)}
                              title="Hủy đơn hàng"
                              style={{ ...btnSmallAction, background: '#fef2f2', color: '#b91c1c' }}
                            >
                              ❌ Hủy
                            </button>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>

              {/* Pagination */}
              <div style={{ display: 'flex', gap: 8, justifyContent: 'center', marginTop: 16 }}>
                <button
                  disabled={page <= 1}
                  onClick={() => setPage(page - 1)}
                  style={btnSecondary}
                >
                  ← Trước
                </button>
                <span style={{ lineHeight: '36px', fontSize: 14 }}>
                  Trang {data.page} / {data.totalPages || 1} ({data.totalCount} hóa đơn)
                </span>
                <button
                  disabled={page >= (data.totalPages || 1)}
                  onClick={() => setPage(page + 1)}
                  style={btnSecondary}
                >
                  Sau →
                </button>
              </div>
            </>
          )}

          {/* DETAIL MODAL */}
          {selectedOrder && (
            <div style={modalOverlay}>
              <div style={modalBox}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '1px solid #e5e7eb', paddingBottom: 12, marginBottom: 16 }}>
                  <div>
                    <h3 style={{ margin: 0, color: '#111827' }}>
                      Chi Tiết Hóa Đơn #HD-{String(selectedOrder.orderId).padStart(6, '0')}
                    </h3>
                    <span style={{ fontSize: 12, color: '#6b7280' }}>
                      Ngày lập: {new Date(selectedOrder.orderDate).toLocaleString('vi-VN')}
                    </span>
                  </div>
                  <div style={{ display: 'flex', gap: 8 }}>
                    <button
                      onClick={() => handlePrintPdf(selectedOrder.orderId)}
                      style={{ ...btnPrimary, background: '#1d4ed8' }}
                    >
                      🖨 In Hóa Đơn PDF
                    </button>
                    <button
                      onClick={() => handleDownloadPdf(selectedOrder.orderId)}
                      style={{ ...btnPrimary, background: '#16a34a' }}
                    >
                      ⬇ Tải PDF
                    </button>
                    <button onClick={() => setSelectedOrder(null)} style={btnSecondary}>
                      Đóng
                    </button>
                  </div>
                </div>

                {/* Customer & Cashier Info */}
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, background: '#f9fafb', padding: 12, borderRadius: 8, marginBottom: 16, fontSize: 13 }}>
                  <div>
                    <strong>Thu ngân:</strong> {selectedOrder.employeeName || `NV #${selectedOrder.employeeId}`}
                    <br />
                    <strong>Hình thức thanh toán:</strong> {formatPaymentMethodName(selectedOrder.paymentMethod)}
                  </div>
                  <div>
                    <strong>Khách hàng:</strong> {selectedOrder.customerName || 'Khách lẻ'}
                    {selectedOrder.customerPhone && (
                      <>
                        <br />
                        <strong>SĐT:</strong> {selectedOrder.customerPhone}
                      </>
                    )}
                  </div>
                </div>

                {/* Items table */}
                <h4 style={{ margin: '0 0 8px', fontSize: 14 }}>Danh sách mặt hàng</h4>
                <table style={{ ...tableStyle, marginBottom: 16 }}>
                  <thead>
                    <tr>
                      <th style={thStyle}>#</th>
                      <th style={thStyle}>Tên Sản Phẩm</th>
                      <th style={thStyle}>ĐVT</th>
                      <th style={{ ...thStyle, textAlign: 'center' }}>SL</th>
                      <th style={{ ...thStyle, textAlign: 'right' }}>Đơn Giá</th>
                      <th style={{ ...thStyle, textAlign: 'right' }}>Thành Tiền</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selectedOrder.orderDetails.map((item, idx) => (
                      <tr key={item.orderDetailId || idx}>
                        <td style={tdStyle}>{idx + 1}</td>
                        <td style={tdStyle}>
                          <strong>{item.productName}</strong>
                          <div style={{ fontSize: 11, color: '#6b7280' }}>{item.barcode}</div>
                        </td>
                        <td style={tdStyle}>{item.unit || 'Cái'}</td>
                        <td style={{ ...tdStyle, textAlign: 'center' }}>{item.quantity}</td>
                        <td style={{ ...tdStyle, textAlign: 'right' }}>{formatCurrency(item.unitPrice)}</td>
                        <td style={{ ...tdStyle, textAlign: 'right', fontWeight: 600 }}>{formatCurrency(item.subTotal)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {/* Summary */}
                <div style={{ marginLeft: 'auto', width: 280, fontSize: 14, borderTop: '1px solid #e5e7eb', paddingTop: 8 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                    <span>Tạm tính:</span>
                    <span>{formatCurrency(selectedOrder.totalAmount)}</span>
                  </div>
                  {selectedOrder.discountAmount > 0 && (
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4, color: '#dc2626' }}>
                      <span>Chiết khấu / Giảm giá:</span>
                      <span>-{formatCurrency(selectedOrder.discountAmount)}</span>
                    </div>
                  )}
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4, fontWeight: 700, fontSize: 16, color: '#0d6efd', borderTop: '1px dashed #d1d5db', paddingTop: 6 }}>
                    <span>TỔNG THANH TOÁN:</span>
                    <span>{formatCurrency(selectedOrder.finalAmount)}</span>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* TAB 2: CREATE & SAVE ORDER (POS CHECKOUT) */}
      {activeTab === 'create' && (
        <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 1fr', gap: 20 }}>
          {/* Left panel: Product Selection & Cart */}
          <div style={{ background: '#fff', border: '1px solid #e5e7eb', borderRadius: 8, padding: 16 }}>
            <h3 style={{ margin: '0 0 12px', fontSize: 16 }}>1. Chọn Sản Phẩm Vào Đơn Hàng</h3>

            {/* Add product row */}
            <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
              <select
                style={{ ...selectStyle, flex: 1 }}
                value={selectedProductId}
                onChange={(e) => setSelectedProductId(Number(e.target.value))}
              >
                {products.map((p) => (
                  <option key={p.productId} value={p.productId}>
                    {p.productName} - {formatCurrency(p.price)} (Tồn: {p.quantityInStock})
                  </option>
                ))}
              </select>
              <button onClick={addToCart} style={btnPrimary}>
                + Thêm món
              </button>
            </div>

            {/* Cart Table */}
            <h4 style={{ margin: '0 0 8px', fontSize: 14 }}>Giỏ hàng hiện tại ({cartItems.length} món)</h4>
            {cartItems.length === 0 ? (
              <p style={{ color: '#9ca3af', fontStyle: 'italic' }}>Giỏ hàng đang trống.</p>
            ) : (
              <table style={tableStyle}>
                <thead>
                  <tr>
                    <th style={thStyle}>Sản phẩm</th>
                    <th style={{ ...thStyle, textAlign: 'center' }}>SL</th>
                    <th style={{ ...thStyle, textAlign: 'right' }}>Đơn giá</th>
                    <th style={{ ...thStyle, textAlign: 'right' }}>Thành tiền</th>
                    <th style={{ ...thStyle, textAlign: 'center' }}>Xóa</th>
                  </tr>
                </thead>
                <tbody>
                  {cartItems.map((item) => (
                    <tr key={item.product.productId}>
                      <td style={tdStyle}>
                        <strong>{item.product.productName}</strong>
                        <div style={{ fontSize: 11, color: '#6b7280' }}>{item.product.barcode}</div>
                      </td>
                      <td style={{ ...tdStyle, textAlign: 'center' }}>
                        <div style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                          <button
                            onClick={() => updateCartQty(item.product.productId, item.quantity - 1)}
                            style={btnSmallQty}
                          >
                            -
                          </button>
                          <span style={{ minWidth: 24, textAlign: 'center' }}>{item.quantity}</span>
                          <button
                            onClick={() => updateCartQty(item.product.productId, item.quantity + 1)}
                            style={btnSmallQty}
                          >
                            +
                          </button>
                        </div>
                      </td>
                      <td style={{ ...tdStyle, textAlign: 'right' }}>{formatCurrency(item.product.price)}</td>
                      <td style={{ ...tdStyle, textAlign: 'right', fontWeight: 600 }}>
                        {formatCurrency(item.product.price * item.quantity)}
                      </td>
                      <td style={{ ...tdStyle, textAlign: 'center' }}>
                        <button
                          onClick={() => updateCartQty(item.product.productId, 0)}
                          style={{ ...btnSmallAction, background: '#fee2e2', color: '#dc2626' }}
                        >
                          ✕
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Right panel: Checkout & Save Order */}
          <div style={{ background: '#f9fafb', border: '1px solid #e5e7eb', borderRadius: 8, padding: 16 }}>
            <h3 style={{ margin: '0 0 12px', fontSize: 16 }}>2. Thông Tin Thanh Toán & Lưu Hóa Đơn</h3>

            {/* Customer & Cashier */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 12 }}>
              <div>
                <label style={labelStyle}>Mã / ID Khách hàng (tùy chọn)</label>
                <input
                  style={inputStyle}
                  type="number"
                  placeholder="ID khách tích điểm..."
                  value={customerId}
                  onChange={(e) => setCustomerId(e.target.value)}
                />
              </div>
              <div>
                <label style={labelStyle}>ID Thu ngân (NV)</label>
                <input
                  style={inputStyle}
                  type="number"
                  value={employeeId}
                  onChange={(e) => setEmployeeId(Number(e.target.value))}
                />
              </div>
            </div>

            {/* Promotion code */}
            <div style={{ marginBottom: 12 }}>
              <label style={labelStyle}>Mã Khuyến mãi / Combo (nếu có)</label>
              <div style={{ display: 'flex', gap: 8 }}>
                <input
                  style={{ ...inputStyle, flex: 1 }}
                  placeholder="Nhập mã KM (VD: CHAOMUNG, COMBO...)"
                  value={promotionCode}
                  onChange={(e) => setPromotionCode(e.target.value)}
                />
                <button type="button" onClick={handleCheckPromotion} style={btnSecondary}>
                  Áp dụng
                </button>
              </div>
            </div>

            {/* Payment method */}
            <div style={{ marginBottom: 16 }}>
              <label style={labelStyle}>Phương thức thanh toán</label>
              <select
                style={selectStyle}
                value={paymentMethod}
                onChange={(e) => setPaymentMethod(Number(e.target.value))}
              >
                <option value={1}>💵 Tiền mặt (Cash)</option>
                <option value={2}>📱 Chuyển khoản QR (VietQR)</option>
                <option value={3}>💳 Thẻ ngân hàng (POS Card)</option>
              </select>
            </div>

            {paymentMethod === 1 && (
              <div style={{ marginBottom: 16 }}>
                <label style={labelStyle}>Tiền khách đưa (VNĐ)</label>
                <input
                  style={inputStyle}
                  type="number"
                  min={0}
                  value={cashGiven || ''}
                  onChange={(e) => setCashGiven(Number(e.target.value))}
                  placeholder="Nhập số tiền..."
                />
              </div>
            )}

            {/* Price calculation summary */}
            <div style={{ background: '#fff', border: '1px solid #d1d5db', borderRadius: 8, padding: 12, marginBottom: 16, fontSize: 14 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
                <span>Tạm tính:</span>
                <strong>{formatCurrency(subTotal)}</strong>
              </div>
              {appliedDiscount > 0 && (
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, color: '#dc2626' }}>
                  <span>Giảm giá:</span>
                  <strong>-{formatCurrency(appliedDiscount)}</strong>
                </div>
              )}
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, fontSize: 17, fontWeight: 700, color: '#2563eb', borderTop: '1px solid #e5e7eb', paddingTop: 8 }}>
                <span>CẦN THANH TOÁN:</span>
                <span>{formatCurrency(finalTotal)}</span>
              </div>
              {paymentMethod === 1 && cashGiven > 0 && (
                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 4, color: '#16a34a', fontWeight: 600 }}>
                  <span>Tiền thối lại:</span>
                  <span>{formatCurrency(changeDue)}</span>
                </div>
              )}
            </div>

            {/* Action buttons */}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
              <button
                type="button"
                disabled={submitting || cartItems.length === 0}
                onClick={() => handleSaveOrder(true)}
                style={{
                  ...btnPrimary,
                  background: '#1d4ed8',
                  padding: '12px 20px',
                  fontSize: 15,
                  textAlign: 'center',
                }}
              >
                {submitting ? 'Đang lưu...' : '💾 Lưu & Xuất / In Hóa Đơn PDF Ngay'}
              </button>

              <button
                type="button"
                disabled={submitting || cartItems.length === 0}
                onClick={() => handleSaveOrder(false)}
                style={{
                  ...btnSecondary,
                  padding: '10px 18px',
                  fontWeight: 600,
                  textAlign: 'center',
                }}
              >
                💾 Chỉ Lưu Hóa Đơn (Không In)
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

// Styles
const inputStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #d1d5db',
  borderRadius: 6,
  fontSize: 14,
  outline: 'none',
  width: '100%',
  boxSizing: 'border-box',
};

const selectStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #d1d5db',
  borderRadius: 6,
  fontSize: 14,
  outline: 'none',
  width: '100%',
  boxSizing: 'border-box',
  background: '#fff',
};

const btnPrimary: React.CSSProperties = {
  padding: '8px 18px',
  background: '#2563eb',
  color: '#fff',
  border: 'none',
  borderRadius: 6,
  cursor: 'pointer',
  fontWeight: 600,
  fontSize: 14,
  whiteSpace: 'nowrap',
};

const btnSecondary: React.CSSProperties = {
  padding: '8px 18px',
  background: '#f3f4f6',
  color: '#374151',
  border: '1px solid #d1d5db',
  borderRadius: 6,
  cursor: 'pointer',
  fontSize: 14,
};

const btnSmallAction: React.CSSProperties = {
  padding: '4px 10px',
  background: '#f3f4f6',
  color: '#374151',
  border: '1px solid #e5e7eb',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 12,
  fontWeight: 500,
};

const btnSmallQty: React.CSSProperties = {
  width: 26,
  height: 26,
  padding: 0,
  background: '#e5e7eb',
  border: 'none',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 14,
  fontWeight: 700,
};

const tableStyle: React.CSSProperties = {
  width: '100%',
  borderCollapse: 'collapse',
  fontSize: 13,
};

const thStyle: React.CSSProperties = {
  textAlign: 'left',
  padding: '10px 12px',
  borderBottom: '2px solid #e5e7eb',
  background: '#f9fafb',
  fontWeight: 600,
  fontSize: 13,
  color: '#374151',
  whiteSpace: 'nowrap',
};

const tdStyle: React.CSSProperties = {
  padding: '9px 12px',
  borderBottom: '1px solid #f3f4f6',
  verticalAlign: 'middle',
};

const badgeStyle: React.CSSProperties = {
  padding: '2px 8px',
  borderRadius: 9999,
  fontSize: 11,
  fontWeight: 600,
  whiteSpace: 'nowrap',
};

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontSize: 12,
  fontWeight: 500,
  color: '#4b5563',
  marginBottom: 4,
};

const modalOverlay: React.CSSProperties = {
  position: 'fixed',
  inset: 0,
  background: 'rgba(0,0,0,0.45)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  zIndex: 1000,
};

const modalBox: React.CSSProperties = {
  background: '#fff',
  borderRadius: 12,
  padding: 24,
  maxWidth: 680,
  width: '92%',
  maxHeight: '90vh',
  overflow: 'auto',
  boxShadow: '0 20px 60px rgba(0,0,0,0.25)',
};

export default OrdersPage;
