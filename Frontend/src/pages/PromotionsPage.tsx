import React, { useState, useEffect, useCallback } from 'react';
import { promotionApi, discountRuleApi } from '../shared/api/promotionApi';
import { formatCurrency } from '../shared/utils/formatCurrency';
import type {
  Promotion,
  PromotionPagedResult,
  CreatePromotionRequest,
  UpdatePromotionRequest,
  DiscountRule,
  ExpiringProduct,
  CheckProductExpiryDiscountResponse,
  DiscountType,
} from '../shared/types';

type TabKey = 'promotions' | 'discountRules' | 'nearExpiry';

const DISCOUNT_TYPES: { value: DiscountType; label: string }[] = [
  { value: 'Percentage', label: 'Giảm theo %' },
  { value: 'FixedAmount', label: 'Giảm số tiền cố định' },
  { value: 'BuyXGetY', label: 'Mua X tặng Y' },
  { value: 'ComboDiscount', label: 'Mua kèm / Combo' },
  { value: 'CategoryDiscount', label: 'Giảm theo ngành hàng' },
  { value: 'QuantityTier', label: 'Giá sỉ theo số lượng' },
];

function discountTypeLabel(dt: string): string {
  return DISCOUNT_TYPES.find((d) => d.value === dt)?.label ?? dt;
}

function dateInputValue(iso: string): string {
  if (!iso) return '';
  return iso.substring(0, 10);
}

function PromotionsTab() {
  const [data, setData] = useState<PromotionPagedResult | null>(null);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Promotion | null>(null);

  const emptyForm: CreatePromotionRequest = {
    promotionCode: '',
    promotionName: '',
    description: '',
    discountType: 'Percentage',
    discountValue: 0,
    minimumOrderAmount: 0,
    startDate: new Date().toISOString().substring(0, 10),
    endDate: new Date(Date.now() + 30 * 86400000).toISOString().substring(0, 10),
    isActive: true,
  };
  const [form, setForm] = useState<CreatePromotionRequest>(emptyForm);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await promotionApi.getPaged(page, 10, search || undefined);
      setData(res.data);
    } catch (e: any) {
      alert(e.message);
    } finally {
      setLoading(false);
    }
  }, [page, search]);

  useEffect(() => { load(); }, [load]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editing) {
        const updateData: UpdatePromotionRequest = { ...form };
        await promotionApi.update(editing.promotionId, updateData);
      } else {
        await promotionApi.create(form);
      }
      setShowForm(false);
      setEditing(null);
      setForm(emptyForm);
      load();
    } catch (e: any) {
      alert(e.message);
    }
  };

  const handleEdit = (p: Promotion) => {
    setEditing(p);
    setForm({
      promotionCode: p.promotionCode,
      promotionName: p.promotionName,
      description: p.description,
      discountType: p.discountType,
      discountValue: p.discountValue,
      minimumOrderAmount: p.minimumOrderAmount,
      maximumDiscountAmount: p.maximumDiscountAmount,
      triggerProductId: p.triggerProductId,
      targetCategoryId: p.targetCategoryId,
      targetProductId: p.targetProductId,
      requiredQuantity: p.requiredQuantity,
      giftQuantity: p.giftQuantity,
      startDate: dateInputValue(p.startDate),
      endDate: dateInputValue(p.endDate),
      isActive: p.isActive,
    });
    setShowForm(true);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Xóa khuyến mãi này?')) return;
    try {
      await promotionApi.delete(id);
      load();
    } catch (e: any) {
      alert(e.message);
    }
  };

  const setField = (field: string, value: any) => setForm((f) => ({ ...f, [field]: value }));

  const needsTriggerProduct = form.discountType === 'ComboDiscount';
  const needsTargetProduct = form.discountType === 'BuyXGetY' || form.discountType === 'ComboDiscount';
  const needsTargetCategory = form.discountType === 'CategoryDiscount';
  const needsQuantity = form.discountType === 'BuyXGetY' || form.discountType === 'QuantityTier';

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <input
            type="text"
            placeholder="Tìm mã / tên..."
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            style={inputStyle}
          />
        </div>
        <button
          onClick={() => { setShowForm(true); setEditing(null); setForm(emptyForm); }}
          style={btnPrimary}
        >
          + Thêm khuyến mãi
        </button>
      </div>

      {showForm && (
        <div style={modalOverlay}>
          <div style={modalBox}>
            <h3 style={{ margin: '0 0 16px' }}>{editing ? 'Sửa khuyến mãi' : 'Thêm khuyến mãi mới'}</h3>
            <form onSubmit={handleSubmit}>
              <div style={formGrid}>
                {!editing && (
                  <label style={labelStyle}>
                    Mã KM
                    <input style={inputStyle} required value={form.promotionCode} onChange={(e) => setField('promotionCode', e.target.value)} />
                  </label>
                )}
                <label style={labelStyle}>
                  Tên chương trình
                  <input style={inputStyle} required value={form.promotionName} onChange={(e) => setField('promotionName', e.target.value)} />
                </label>
                <label style={labelStyle}>
                  Loại giảm giá
                  <select style={inputStyle} value={form.discountType} onChange={(e) => setField('discountType', e.target.value)}>
                    {DISCOUNT_TYPES.map((d) => (
                      <option key={d.value} value={d.value}>{d.label}</option>
                    ))}
                  </select>
                </label>
                <label style={labelStyle}>
                  {form.discountType === 'FixedAmount' ? 'Số tiền giảm (VNĐ)' : 'Giá trị (%)'}
                  <input style={inputStyle} type="number" required min={0} value={form.discountValue} onChange={(e) => setField('discountValue', +e.target.value)} />
                </label>
                <label style={labelStyle}>
                  Đơn tối thiểu
                  <input style={inputStyle} type="number" min={0} value={form.minimumOrderAmount} onChange={(e) => setField('minimumOrderAmount', +e.target.value)} />
                </label>
                <label style={labelStyle}>
                  Giảm tối đa
                  <input style={inputStyle} type="number" min={0} value={form.maximumDiscountAmount ?? ''} onChange={(e) => setField('maximumDiscountAmount', e.target.value ? +e.target.value : undefined)} />
                </label>
                {needsTriggerProduct && (
                  <label style={labelStyle}>
                    SP kích hoạt (ID)
                    <input style={inputStyle} type="number" value={form.triggerProductId ?? ''} onChange={(e) => setField('triggerProductId', e.target.value ? +e.target.value : undefined)} placeholder="VD: ID Bàn chải" />
                  </label>
                )}
                {needsTargetProduct && (
                  <label style={labelStyle}>
                    SP đích (ID)
                    <input style={inputStyle} type="number" value={form.targetProductId ?? ''} onChange={(e) => setField('targetProductId', e.target.value ? +e.target.value : undefined)} placeholder="VD: ID Kem đánh răng" />
                  </label>
                )}
                {needsTargetCategory && (
                  <label style={labelStyle}>
                    Ngành hàng (ID)
                    <input style={inputStyle} type="number" value={form.targetCategoryId ?? ''} onChange={(e) => setField('targetCategoryId', e.target.value ? +e.target.value : undefined)} />
                  </label>
                )}
                {needsQuantity && (
                  <>
                    <label style={labelStyle}>
                      {form.discountType === 'BuyXGetY' ? 'Mua (SL)' : 'Mua tối thiểu'}
                      <input style={inputStyle} type="number" min={1} value={form.requiredQuantity ?? ''} onChange={(e) => setField('requiredQuantity', e.target.value ? +e.target.value : undefined)} />
                    </label>
                    {form.discountType === 'BuyXGetY' && (
                      <label style={labelStyle}>
                        Tặng (SL)
                        <input style={inputStyle} type="number" min={1} value={form.giftQuantity ?? ''} onChange={(e) => setField('giftQuantity', e.target.value ? +e.target.value : undefined)} />
                      </label>
                    )}
                  </>
                )}
                <label style={labelStyle}>
                  Ngày bắt đầu
                  <input style={inputStyle} type="date" required value={form.startDate} onChange={(e) => setField('startDate', e.target.value)} />
                </label>
                <label style={labelStyle}>
                  Ngày kết thúc
                  <input style={inputStyle} type="date" required value={form.endDate} onChange={(e) => setField('endDate', e.target.value)} />
                </label>
                <label style={labelStyle}>
                  Mô tả
                  <textarea style={{ ...inputStyle, minHeight: 60 }} value={form.description ?? ''} onChange={(e) => setField('description', e.target.value)} />
                </label>
                <label style={{ ...labelStyle, flexDirection: 'row', gap: 8, alignItems: 'center' }}>
                  <input type="checkbox" checked={form.isActive} onChange={(e) => setField('isActive', e.target.checked)} />
                  Kích hoạt
                </label>
              </div>
              <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', marginTop: 16 }}>
                <button type="button" onClick={() => { setShowForm(false); setEditing(null); }} style={btnSecondary}>Hủy</button>
                <button type="submit" style={btnPrimary}>{editing ? 'Cập nhật' : 'Tạo mới'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {loading ? (
        <p>Đang tải...</p>
      ) : !data || data.items.length === 0 ? (
        <p style={{ color: '#888' }}>Chưa có khuyến mãi nào.</p>
      ) : (
        <>
          <table style={tableStyle}>
            <thead>
              <tr>
                <th style={thStyle}>Mã</th>
                <th style={thStyle}>Tên</th>
                <th style={thStyle}>Loại</th>
                <th style={thStyle}>Giá trị</th>
                <th style={thStyle}>Chi tiết</th>
                <th style={thStyle}>Thời gian</th>
                <th style={thStyle}>TT</th>
                <th style={thStyle}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((p) => (
                <tr key={p.promotionId}>
                  <td style={tdStyle}><strong>{p.promotionCode}</strong></td>
                  <td style={tdStyle}>{p.promotionName}</td>
                  <td style={tdStyle}>
                    <span style={{ ...badgeStyle, background: discountTypeBg(p.discountType) }}>
                      {discountTypeLabel(p.discountType)}
                    </span>
                  </td>
                  <td style={tdStyle}>
                    {p.discountType === 'FixedAmount' ? formatCurrency(p.discountValue) : `${p.discountValue}%`}
                  </td>
                  <td style={{ ...tdStyle, fontSize: 12 }}>
                    {renderPromotionDetail(p)}
                  </td>
                  <td style={{ ...tdStyle, fontSize: 12 }}>
                    {dateInputValue(p.startDate)} → {dateInputValue(p.endDate)}
                  </td>
                  <td style={tdStyle}>
                    <span style={{ color: p.isActive ? '#16a34a' : '#dc2626', fontWeight: 600 }}>
                      {p.isActive ? 'ON' : 'OFF'}
                    </span>
                  </td>
                  <td style={tdStyle}>
                    <button onClick={() => handleEdit(p)} style={btnSmall}>Sửa</button>{' '}
                    <button onClick={() => handleDelete(p.promotionId)} style={{ ...btnSmall, background: '#fee2e2', color: '#dc2626' }}>Xóa</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div style={{ display: 'flex', gap: 8, justifyContent: 'center', marginTop: 12 }}>
            <button disabled={page <= 1} onClick={() => setPage(page - 1)} style={btnSecondary}>← Trước</button>
            <span style={{ lineHeight: '36px' }}>Trang {data.page} / {data.totalPages || 1}</span>
            <button disabled={page >= (data.totalPages || 1)} onClick={() => setPage(page + 1)} style={btnSecondary}>Sau →</button>
          </div>
        </>
      )}
    </div>
  );
}

function renderPromotionDetail(p: Promotion): React.ReactNode {
  switch (p.discountType) {
    case 'BuyXGetY':
      return <>Mua {p.requiredQuantity ?? '?'} tặng {p.giftQuantity ?? '?'}{p.targetProductName ? ` (${p.targetProductName})` : ''}</>;
    case 'ComboDiscount':
      return <>Mua {p.triggerProductName ?? `SP#${p.triggerProductId}`} → giảm {p.discountValue}% {p.targetProductName ?? `SP#${p.targetProductId}`}</>;
    case 'CategoryDiscount':
      return <>Giảm {p.discountValue}% ngành {p.targetCategoryName ?? `#${p.targetCategoryId}`}</>;
    case 'QuantityTier':
      return <>Mua từ {p.requiredQuantity} SP → giảm {p.discountValue}%</>;
    default:
      return p.description || '-';
  }
}

function discountTypeBg(dt: string): string {
  const map: Record<string, string> = {
    Percentage: '#dbeafe',
    FixedAmount: '#d1fae5',
    BuyXGetY: '#fef3c7',
    ComboDiscount: '#fce7f3',
    CategoryDiscount: '#e0e7ff',
    QuantityTier: '#f3e8ff',
  };
  return map[dt] ?? '#f3f4f6';
}

function DiscountRulesTab() {
  const [rules, setRules] = useState<DiscountRule[]>([]);
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<DiscountRule | null>(null);
  const [form, setForm] = useState({ daysBeforeExpiry: 7, discountPercent: 10, isActive: true, description: '' });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await discountRuleApi.getAll(true);
      setRules(res.data);
    } catch (e: any) {
      alert(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editing) {
        await discountRuleApi.update(editing.discountRuleId, form);
      } else {
        await discountRuleApi.create(form);
      }
      setShowForm(false);
      setEditing(null);
      setForm({ daysBeforeExpiry: 7, discountPercent: 10, isActive: true, description: '' });
      load();
    } catch (e: any) {
      alert(e.message);
    }
  };

  const handleEdit = (r: DiscountRule) => {
    setEditing(r);
    setForm({ daysBeforeExpiry: r.daysBeforeExpiry, discountPercent: r.discountPercent, isActive: r.isActive, description: r.description ?? '' });
    setShowForm(true);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Xóa quy tắc này?')) return;
    try {
      await discountRuleApi.delete(id);
      load();
    } catch (e: any) {
      alert(e.message);
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <p style={{ margin: 0, color: '#666', fontSize: 14 }}>Quy tắc tự động giảm giá theo số ngày còn lại trước khi hết hạn sản phẩm.</p>
        <button onClick={() => { setShowForm(true); setEditing(null); setForm({ daysBeforeExpiry: 7, discountPercent: 10, isActive: true, description: '' }); }} style={btnPrimary}>
          + Thêm quy tắc
        </button>
      </div>

      {showForm && (
        <div style={modalOverlay}>
          <div style={{ ...modalBox, maxWidth: 420 }}>
            <h3 style={{ margin: '0 0 16px' }}>{editing ? 'Sửa quy tắc' : 'Thêm quy tắc mới'}</h3>
            <form onSubmit={handleSubmit}>
              <label style={labelStyle}>
                Số ngày trước hết hạn
                <input style={inputStyle} type="number" required min={1} value={form.daysBeforeExpiry} onChange={(e) => setForm({ ...form, daysBeforeExpiry: +e.target.value })} />
              </label>
              <label style={labelStyle}>
                % giảm giá
                <input style={inputStyle} type="number" required min={1} max={100} value={form.discountPercent} onChange={(e) => setForm({ ...form, discountPercent: +e.target.value })} />
              </label>
              <label style={labelStyle}>
                Mô tả
                <input style={inputStyle} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
              </label>
              <label style={{ ...labelStyle, flexDirection: 'row', gap: 8, alignItems: 'center' }}>
                <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
                Kích hoạt
              </label>
              <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end', marginTop: 16 }}>
                <button type="button" onClick={() => { setShowForm(false); setEditing(null); }} style={btnSecondary}>Hủy</button>
                <button type="submit" style={btnPrimary}>{editing ? 'Cập nhật' : 'Tạo'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {loading ? <p>Đang tải...</p> : rules.length === 0 ? (
        <p style={{ color: '#888' }}>Chưa có quy tắc nào.</p>
      ) : (
        <table style={tableStyle}>
          <thead>
            <tr>
              <th style={thStyle}>ID</th>
              <th style={thStyle}>Ngày trước HH</th>
              <th style={thStyle}>% Giảm</th>
              <th style={thStyle}>Mô tả</th>
              <th style={thStyle}>Trạng thái</th>
              <th style={thStyle}>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {rules.map((r) => (
              <tr key={r.discountRuleId}>
                <td style={tdStyle}>{r.discountRuleId}</td>
                <td style={tdStyle}>≤ {r.daysBeforeExpiry} ngày</td>
                <td style={tdStyle}><strong>{r.discountPercent}%</strong></td>
                <td style={tdStyle}>{r.description || '-'}</td>
                <td style={tdStyle}>
                  <span style={{ color: r.isActive ? '#16a34a' : '#dc2626', fontWeight: 600 }}>
                    {r.isActive ? 'ON' : 'OFF'}
                  </span>
                </td>
                <td style={tdStyle}>
                  <button onClick={() => handleEdit(r)} style={btnSmall}>Sửa</button>{' '}
                  <button onClick={() => handleDelete(r.discountRuleId)} style={{ ...btnSmall, background: '#fee2e2', color: '#dc2626' }}>Xóa</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

function NearExpiryTab() {
  const [products, setProducts] = useState<ExpiringProduct[]>([]);
  const [loading, setLoading] = useState(false);
  const [checkQuery, setCheckQuery] = useState('');
  const [checkResult, setCheckResult] = useState<CheckProductExpiryDiscountResponse | null>(null);
  const [checking, setChecking] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await discountRuleApi.getExpiringProducts();
      setProducts(res.data);
    } catch (e: any) {
      alert(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleCheck = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!checkQuery.trim()) return;
    setChecking(true);
    setCheckResult(null);
    try {
      const res = await discountRuleApi.checkProductDiscount(checkQuery.trim());
      setCheckResult(res.data);
    } catch (e: any) {
      alert(e.message);
    } finally {
      setChecking(false);
    }
  };

  const alertColor = (level: string) => {
    switch (level) {
      case 'Critical': return '#dc2626';
      case 'Expired': return '#7f1d1d';
      case 'Warning': return '#d97706';
      default: return '#2563eb';
    }
  };

  return (
    <div>
      <div style={{ background: '#fffbeb', border: '1px solid #fbbf24', borderRadius: 8, padding: 16, marginBottom: 20 }}>
        <h4 style={{ margin: '0 0 8px', color: '#92400e' }}>🔍 Kiểm tra chiết khấu cận date tại POS</h4>
        <p style={{ margin: '0 0 12px', fontSize: 13, color: '#78716c' }}>
          Nhập mã vạch hoặc ID sản phẩm để kiểm tra giá sau khi tự động áp dụng chiết khấu cận date.
        </p>
        <form onSubmit={handleCheck} style={{ display: 'flex', gap: 8 }}>
          <input
            style={{ ...inputStyle, flex: 1 }}
            placeholder="Mã vạch hoặc ID sản phẩm..."
            value={checkQuery}
            onChange={(e) => setCheckQuery(e.target.value)}
          />
          <button type="submit" disabled={checking} style={btnPrimary}>
            {checking ? 'Đang tra...' : 'Kiểm tra'}
          </button>
        </form>
        {checkResult && (
          <div style={{ marginTop: 12, padding: 12, background: checkResult.isNearExpiry ? '#fef2f2' : '#f0fdf4', borderRadius: 6, fontSize: 14 }}>
            <strong>{checkResult.productName}</strong> ({checkResult.barcode})
            <br />
            Giá gốc: {formatCurrency(checkResult.originalPrice)}
            {checkResult.isNearExpiry ? (
              <>
                <br />
                <span style={{ color: '#dc2626', fontWeight: 700 }}>
                  ⚠ Cận date! Còn {checkResult.daysRemaining} ngày → Giảm {checkResult.discountPercent}%
                </span>
                <br />
                <span style={{ color: '#dc2626', fontSize: 18, fontWeight: 700 }}>
                  Giá xả: {formatCurrency(checkResult.clearancePrice)}
                </span>
                <br />
                <em style={{ fontSize: 12, color: '#78716c' }}>{checkResult.ruleDescription}</em>
              </>
            ) : (
              <span style={{ color: '#16a34a', marginLeft: 8 }}>✓ Không cận date, giữ nguyên giá.</span>
            )}
          </div>
        )}
      </div>

      <h4 style={{ margin: '0 0 12px' }}>Danh sách hàng cận date (≤ 30 ngày)</h4>
      {loading ? <p>Đang tải...</p> : products.length === 0 ? (
        <p style={{ color: '#888' }}>Không có hàng cận date.</p>
      ) : (
        <table style={tableStyle}>
          <thead>
            <tr>
              <th style={thStyle}>Sản phẩm</th>
              <th style={thStyle}>Mã vạch</th>
              <th style={thStyle}>Lô</th>
              <th style={thStyle}>SL</th>
              <th style={thStyle}>HSD</th>
              <th style={thStyle}>Còn</th>
              <th style={thStyle}>Giá gốc</th>
              <th style={thStyle}>Giảm</th>
              <th style={thStyle}>Giá xả</th>
              <th style={thStyle}>Mức</th>
            </tr>
          </thead>
          <tbody>
            {products.map((p, i) => (
              <tr key={`${p.productId}-${p.batchCode}-${i}`}>
                <td style={tdStyle}>{p.productName}</td>
                <td style={tdStyle}>{p.barcode}</td>
                <td style={tdStyle}>{p.batchCode}</td>
                <td style={tdStyle}>{p.quantity}</td>
                <td style={tdStyle}>{dateInputValue(p.expiryDate)}</td>
                <td style={tdStyle}><strong>{p.daysRemaining}</strong> ngày</td>
                <td style={tdStyle}>{formatCurrency(p.originalPrice)}</td>
                <td style={tdStyle}>{p.discountPercent > 0 ? `${p.discountPercent}%` : '-'}</td>
                <td style={{ ...tdStyle, fontWeight: p.discountPercent > 0 ? 700 : 400, color: p.discountPercent > 0 ? '#dc2626' : undefined }}>
                  {formatCurrency(p.clearancePrice)}
                </td>
                <td style={tdStyle}>
                  <span style={{ ...badgeStyle, background: alertColor(p.alertLevel) + '22', color: alertColor(p.alertLevel), fontWeight: 600 }}>
                    {p.alertLevel}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

export const PromotionsPage: React.FC = () => {
  const [tab, setTab] = useState<TabKey>('promotions');

  const tabs: { key: TabKey; label: string; icon: string }[] = [
    { key: 'promotions', label: 'Chương trình khuyến mãi', icon: '🎁' },
    { key: 'discountRules', label: 'Quy tắc cận date', icon: '⏰' },
    { key: 'nearExpiry', label: 'Xả hàng cận date / POS', icon: '🔥' },
  ];

  return (
    <div style={{ padding: '24px 32px', maxWidth: 1200, margin: '0 auto', textAlign: 'left' }}>
      <h2 style={{ marginTop: 0 }}>Quản lý Khuyến mãi Đa dạng</h2>
      <div style={{ display: 'flex', gap: 0, borderBottom: '2px solid #e5e7eb', marginBottom: 20 }}>
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            style={{
              padding: '10px 20px',
              border: 'none',
              borderBottom: tab === t.key ? '2px solid #3b82f6' : '2px solid transparent',
              background: 'none',
              cursor: 'pointer',
              fontWeight: tab === t.key ? 600 : 400,
              color: tab === t.key ? '#1d4ed8' : '#6b7280',
              fontSize: 14,
              marginBottom: -2,
            }}
          >
            {t.icon} {t.label}
          </button>
        ))}
      </div>

      {tab === 'promotions' && <PromotionsTab />}
      {tab === 'discountRules' && <DiscountRulesTab />}
      {tab === 'nearExpiry' && <NearExpiryTab />}
    </div>
  );
};

const inputStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #d1d5db',
  borderRadius: 6,
  fontSize: 14,
  outline: 'none',
  width: '100%',
  boxSizing: 'border-box',
};

const btnPrimary: React.CSSProperties = {
  padding: '8px 18px',
  background: '#3b82f6',
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

const btnSmall: React.CSSProperties = {
  padding: '4px 10px',
  background: '#eef2ff',
  color: '#4338ca',
  border: 'none',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 12,
  fontWeight: 500,
};

const tableStyle: React.CSSProperties = {
  width: '100%',
  borderCollapse: 'collapse',
  fontSize: 14,
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
  padding: '10px 12px',
  borderBottom: '1px solid #f3f4f6',
  verticalAlign: 'middle',
};

const badgeStyle: React.CSSProperties = {
  padding: '2px 8px',
  borderRadius: 9999,
  fontSize: 11,
  fontWeight: 500,
  whiteSpace: 'nowrap',
};

const labelStyle: React.CSSProperties = {
  display: 'flex',
  flexDirection: 'column',
  gap: 4,
  fontSize: 13,
  fontWeight: 500,
  color: '#374151',
};

const formGrid: React.CSSProperties = {
  display: 'grid',
  gridTemplateColumns: '1fr 1fr',
  gap: 12,
};

const modalOverlay: React.CSSProperties = {
  position: 'fixed',
  inset: 0,
  background: 'rgba(0,0,0,0.3)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  zIndex: 1000,
};

const modalBox: React.CSSProperties = {
  background: '#fff',
  borderRadius: 12,
  padding: 24,
  maxWidth: 640,
  width: '90%',
  maxHeight: '90vh',
  overflow: 'auto',
  boxShadow: '0 20px 60px rgba(0,0,0,0.2)',
};

export default PromotionsPage;
