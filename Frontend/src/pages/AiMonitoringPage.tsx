import React, { useEffect, useState, useCallback } from 'react';
import { aiService, type AiMonitoringDashboard } from '../services/aiService';

export const AiMonitoringPage: React.FC = () => {
  const [data, setData] = useState<AiMonitoringDashboard | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [autoRefresh, setAutoRefresh] = useState<boolean>(true);
  const [lastUpdated, setLastUpdated] = useState<Date>(new Date());
  const [activeTab, setActiveTab] = useState<'overview' | 'tools' | 'models' | 'traces'>('overview');

  const fetchDashboard = useCallback(async () => {
    try {
      const res = await aiService.getMonitoringDashboard();
      setData(res);
      setLastUpdated(new Date());
    } catch (err) {
      console.error('Failed to load AI monitoring dashboard', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchDashboard();
  }, [fetchDashboard]);

  useEffect(() => {
    if (!autoRefresh) return;
    const interval = setInterval(fetchDashboard, 4000);
    return () => clearInterval(interval);
  }, [autoRefresh, fetchDashboard]);

  const handleReset = async () => {
    if (!window.confirm('Bạn có chắc chắn muốn đặt lại các bộ đếm đo lường AI không?')) return;
    try {
      await aiService.resetMonitoringMetrics();
      await fetchDashboard();
    } catch (err) {
      console.error('Failed to reset metrics', err);
    }
  };

  const overview = data?.overview;

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '24px 16px', fontFamily: 'system-ui, sans-serif' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24, flexWrap: 'wrap', gap: 12 }}>
        <div>
          <h1 style={{ margin: 0, fontSize: 26, fontWeight: 800, color: '#0f172a', display: 'flex', alignItems: 'center', gap: 10 }}>
            <span>📊</span> AI Observability & Monitoring Dashboard
          </h1>
          <p style={{ margin: '4px 0 0 0', color: '#64748b', fontSize: 14 }}>
            Theo dõi thời gian thực: Metrics, Tool Duration, Tokens, Cost, Latency & Live Traces
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13, cursor: 'pointer', color: '#334155' }}>
            <input
              type="checkbox"
              checked={autoRefresh}
              onChange={(e) => setAutoRefresh(e.target.checked)}
              style={{ accentColor: '#16a34a' }}
            />
            Tự động cập nhật (4s)
          </label>

          <button
            onClick={() => fetchDashboard()}
            style={{
              backgroundColor: '#0284c7',
              color: '#fff',
              border: 'none',
              borderRadius: 6,
              padding: '6px 14px',
              fontSize: 13,
              fontWeight: 600,
              cursor: 'pointer'
            }}
          >
            🔄 Làm mới
          </button>

          <button
            onClick={handleReset}
            style={{
              backgroundColor: '#ef4444',
              color: '#fff',
              border: 'none',
              borderRadius: 6,
              padding: '6px 12px',
              fontSize: 13,
              fontWeight: 600,
              cursor: 'pointer'
            }}
          >
            🗑️ Reset
          </button>
        </div>
      </div>

      {/* KPI Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: 16, marginBottom: 24 }}>
        {/* Card 1: Requests */}
        <div style={cardStyle}>
          <div style={cardHeaderStyle}>
            <span style={{ color: '#64748b', fontSize: 13 }}>TỔNG YÊU CẦU</span>
            <span style={{ fontSize: 18 }}>🚀</span>
          </div>
          <div style={{ fontSize: 28, fontWeight: 800, color: '#0f172a' }}>
            {overview?.totalRequests.toLocaleString() ?? 0}
          </div>
          <div style={{ fontSize: 12, color: overview?.failedRequests ? '#dc2626' : '#16a34a', marginTop: 4 }}>
            Thành công: {overview?.successfulRequests ?? 0} ({overview?.successRatePercent ?? 100}%)
          </div>
        </div>

        {/* Card 2: Latency */}
        <div style={cardStyle}>
          <div style={cardHeaderStyle}>
            <span style={{ color: '#64748b', fontSize: 13 }}>ĐỘ TRỄ TRUNG BÌNH</span>
            <span style={{ fontSize: 18 }}>⚡</span>
          </div>
          <div style={{ fontSize: 28, fontWeight: 800, color: '#0284c7' }}>
            {overview?.averageLatencyMs.toFixed(0) ?? 0} <span style={{ fontSize: 16, fontWeight: 500 }}>ms</span>
          </div>
          <div style={{ fontSize: 12, color: '#64748b', marginTop: 4 }}>
            P95 Latency: {overview?.p95LatencyMs.toFixed(0) ?? 0} ms
          </div>
        </div>

        {/* Card 3: Tokens */}
        <div style={cardStyle}>
          <div style={cardHeaderStyle}>
            <span style={{ color: '#64748b', fontSize: 13 }}>TOKEN TIÊU THỤ</span>
            <span style={{ fontSize: 18 }}>🪙</span>
          </div>
          <div style={{ fontSize: 28, fontWeight: 800, color: '#8b5cf6' }}>
            {overview?.totalTokens.toLocaleString() ?? 0}
          </div>
          <div style={{ fontSize: 12, color: '#64748b', marginTop: 4 }}>
            Prompt: {overview?.totalPromptTokens.toLocaleString() ?? 0} | Completion: {overview?.totalCompletionTokens.toLocaleString() ?? 0}
          </div>
        </div>

        {/* Card 4: Cost */}
        <div style={cardStyle}>
          <div style={cardHeaderStyle}>
            <span style={{ color: '#64748b', fontSize: 13 }}>CHI PHÍ TÍCH LŨY</span>
            <span style={{ fontSize: 18 }}>💵</span>
          </div>
          <div style={{ fontSize: 26, fontWeight: 800, color: '#16a34a' }}>
            ${overview?.totalCostUsd.toFixed(4) ?? '0.0000'}
          </div>
          <div style={{ fontSize: 12, color: '#64748b', marginTop: 4 }}>
            ≈ {(overview?.totalCostVnd ?? 0).toLocaleString()} ₫
          </div>
        </div>

        {/* Card 5: Cache Hit */}
        <div style={cardStyle}>
          <div style={cardHeaderStyle}>
            <span style={{ color: '#64748b', fontSize: 13 }}>TỈ LỆ CACHE HIT</span>
            <span style={{ fontSize: 18 }}>🎯</span>
          </div>
          <div style={{ fontSize: 28, fontWeight: 800, color: '#f59e0b' }}>
            {overview?.cacheHitRatioPercent.toFixed(1) ?? 0}%
          </div>
          <div style={{ fontSize: 12, color: '#16a34a', marginTop: 4 }}>
            Tiết kiệm: ${(overview?.totalSavedCostUsd ?? 0).toFixed(4)} ({overview?.cacheHitCount ?? 0} hits)
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 8, borderBottom: '2px solid #e2e8f0', marginBottom: 20 }}>
        <button
          onClick={() => setActiveTab('overview')}
          style={tabButtonStyle(activeTab === 'overview')}
        >
          📌 Tổng quan Hệ thống
        </button>
        <button
          onClick={() => setActiveTab('tools')}
          style={tabButtonStyle(activeTab === 'tools')}
        >
          🛠️ Tool Duration ({data?.toolMetrics.length ?? 0})
        </button>
        <button
          onClick={() => setActiveTab('models')}
          style={tabButtonStyle(activeTab === 'models')}
        >
          🤖 LLM & Token ({data?.modelMetrics.length ?? 0})
        </button>
        <button
          onClick={() => setActiveTab('traces')}
          style={tabButtonStyle(activeTab === 'traces')}
        >
          📜 Live Traces ({data?.recentTraces.length ?? 0})
        </button>
      </div>

      {/* Loading indicator */}
      {loading && !data && (
        <div style={{ textAlign: 'center', padding: '60px 0', color: '#64748b' }}>
          Đang kết nối tới AI Monitoring Collector...
        </div>
      )}

      {/* Tab: Overview */}
      {activeTab === 'overview' && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(450px, 1fr))', gap: 20 }}>
          {/* Tool quick summary */}
          <div style={panelStyle}>
            <h3 style={{ margin: '0 0 16px 0', fontSize: 16, fontWeight: 700, color: '#0f172a' }}>
              🛠️ Thời lượng Thực thi Tool (Top Invoked)
            </h3>
            {(!data?.toolMetrics || data.toolMetrics.length === 0) ? (
              <p style={{ color: '#94a3b8', fontSize: 14 }}>Chưa có lượt gọi Tool nào trong phiên giám sát.</p>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {data.toolMetrics.slice(0, 5).map((tool) => (
                  <div key={tool.toolName} style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
                      <span style={{ fontWeight: 600, color: '#1e293b' }}>{tool.toolName}</span>
                      <span style={{ color: '#64748b' }}>
                        Avg: <strong>{tool.averageDurationMs}ms</strong> | {tool.invocationCount} lượt
                      </span>
                    </div>
                    <div style={{ width: '100%', height: 6, backgroundColor: '#f1f5f9', borderRadius: 3, overflow: 'hidden' }}>
                      <div
                        style={{
                          height: '100%',
                          width: `${Math.min(100, (tool.averageDurationMs / 500) * 100)}%`,
                          backgroundColor: tool.averageDurationMs > 300 ? '#f59e0b' : '#10b981',
                          borderRadius: 3
                        }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Model token & cost breakdown */}
          <div style={panelStyle}>
            <h3 style={{ margin: '0 0 16px 0', fontSize: 16, fontWeight: 700, color: '#0f172a' }}>
              🤖 Tiêu thụ Token theo Model LLM
            </h3>
            {(!data?.modelMetrics || data.modelMetrics.length === 0) ? (
              <p style={{ color: '#94a3b8', fontSize: 14 }}>Chưa có yêu cầu LLM nào được ghi nhận.</p>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {data.modelMetrics.map((m) => (
                  <div key={m.modelName} style={{ padding: '10px 12px', backgroundColor: '#f8fafc', borderRadius: 8, border: '1px solid #e2e8f0' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <span style={{ fontWeight: 700, color: '#0f172a', fontSize: 14 }}>{m.modelName}</span>
                      <span style={{ fontSize: 12, padding: '2px 8px', borderRadius: 12, backgroundColor: '#e2e8f0', color: '#334155' }}>
                        {m.provider}
                      </span>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 12, color: '#64748b', marginTop: 6 }}>
                      <span>Tổng Token: <strong>{m.totalTokens.toLocaleString()}</strong></span>
                      <span>Chi phí: <strong>${m.estimatedCostUsd.toFixed(6)}</strong></span>
                      <span>Độ trễ: <strong>{m.averageLatencyMs.toFixed(0)}ms</strong></span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Tab: Tools */}
      {activeTab === 'tools' && (
        <div style={panelStyle}>
          <h3 style={{ margin: '0 0 16px 0', fontSize: 18, fontWeight: 700, color: '#0f172a' }}>
            🛠️ Phân tích Chi tiết Thời lượng & Hiệu năng Tool
          </h3>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: 13 }}>
              <thead>
                <tr style={{ backgroundColor: '#f8fafc', borderBottom: '2px solid #e2e8f0', color: '#475569' }}>
                  <th style={thStyle}>Tool Method</th>
                  <th style={thStyle}>Số lần gọi</th>
                  <th style={thStyle}>Thành công</th>
                  <th style={thStyle}>Thất bại</th>
                  <th style={thStyle}>Độ trễ TB</th>
                  <th style={thStyle}>Min</th>
                  <th style={thStyle}>Max</th>
                  <th style={thStyle}>Tổng thời gian</th>
                </tr>
              </thead>
              <tbody>
                {data?.toolMetrics.map((t) => (
                  <tr key={t.toolName} style={{ borderBottom: '1px solid #f1f5f9' }}>
                    <td style={{ ...tdStyle, fontWeight: 600, color: '#0284c7' }}>{t.toolName}</td>
                    <td style={tdStyle}>{t.invocationCount}</td>
                    <td style={{ ...tdStyle, color: '#16a34a' }}>{t.successCount}</td>
                    <td style={{ ...tdStyle, color: t.failureCount > 0 ? '#dc2626' : '#64748b' }}>{t.failureCount}</td>
                    <td style={{ ...tdStyle, fontWeight: 700 }}>{t.averageDurationMs} ms</td>
                    <td style={tdStyle}>{t.minDurationMs} ms</td>
                    <td style={tdStyle}>{t.maxDurationMs} ms</td>
                    <td style={tdStyle}>{(t.totalDurationMs / 1000).toFixed(2)} s</td>
                  </tr>
                ))}
                {(!data?.toolMetrics || data.toolMetrics.length === 0) && (
                  <tr>
                    <td colSpan={8} style={{ ...tdStyle, textAlign: 'center', color: '#94a3b8' }}>
                      Chưa có dữ liệu Tool.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab: Models */}
      {activeTab === 'models' && (
        <div style={panelStyle}>
          <h3 style={{ margin: '0 0 16px 0', fontSize: 18, fontWeight: 700, color: '#0f172a' }}>
            🤖 Thống kê Mô hình Ngôn ngữ, Token & Chi phí
          </h3>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: 13 }}>
              <thead>
                <tr style={{ backgroundColor: '#f8fafc', borderBottom: '2px solid #e2e8f0', color: '#475569' }}>
                  <th style={thStyle}>Mô hình (Model)</th>
                  <th style={thStyle}>Provider</th>
                  <th style={thStyle}>Lượt gọi</th>
                  <th style={thStyle}>Prompt Tokens</th>
                  <th style={thStyle}>Completion Tokens</th>
                  <th style={thStyle}>Tổng Token</th>
                  <th style={thStyle}>Độ trễ TB</th>
                  <th style={thStyle}>Chi phí ($ USD)</th>
                </tr>
              </thead>
              <tbody>
                {data?.modelMetrics.map((m) => (
                  <tr key={m.modelName} style={{ borderBottom: '1px solid #f1f5f9' }}>
                    <td style={{ ...tdStyle, fontWeight: 600, color: '#0f172a' }}>{m.modelName}</td>
                    <td style={tdStyle}>{m.provider}</td>
                    <td style={tdStyle}>{m.requestCount}</td>
                    <td style={tdStyle}>{m.promptTokens.toLocaleString()}</td>
                    <td style={tdStyle}>{m.completionTokens.toLocaleString()}</td>
                    <td style={{ ...tdStyle, fontWeight: 700, color: '#8b5cf6' }}>{m.totalTokens.toLocaleString()}</td>
                    <td style={tdStyle}>{m.averageLatencyMs.toFixed(0)} ms</td>
                    <td style={{ ...tdStyle, fontWeight: 700, color: '#16a34a' }}>${m.estimatedCostUsd.toFixed(6)}</td>
                  </tr>
                ))}
                {(!data?.modelMetrics || data.modelMetrics.length === 0) && (
                  <tr>
                    <td colSpan={8} style={{ ...tdStyle, textAlign: 'center', color: '#94a3b8' }}>
                      Chưa có dữ liệu gọi LLM.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab: Traces */}
      {activeTab === 'traces' && (
        <div style={panelStyle}>
          <h3 style={{ margin: '0 0 16px 0', fontSize: 18, fontWeight: 700, color: '#0f172a' }}>
            📜 Nhật ký Traces Thực thi AI Thời gian thực
          </h3>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: 12 }}>
              <thead>
                <tr style={{ backgroundColor: '#f8fafc', borderBottom: '2px solid #e2e8f0', color: '#475569' }}>
                  <th style={thStyle}>Thời gian</th>
                  <th style={thStyle}>Trace Id</th>
                  <th style={thStyle}>Loại tác vụ</th>
                  <th style={thStyle}>Model / Tool</th>
                  <th style={thStyle}>Thời lượng</th>
                  <th style={thStyle}>Tokens</th>
                  <th style={thStyle}>Chi phí</th>
                  <th style={thStyle}>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {data?.recentTraces.map((tr) => (
                  <tr key={tr.traceId} style={{ borderBottom: '1px solid #f1f5f9' }}>
                    <td style={tdStyle}>{new Date(tr.timestamp).toLocaleTimeString()}</td>
                    <td style={{ ...tdStyle, fontFamily: 'monospace', color: '#64748b' }}>{tr.traceId}</td>
                    <td style={{ ...tdStyle, fontWeight: 600 }}>{tr.operation}</td>
                    <td style={tdStyle}>{tr.modelOrTool}</td>
                    <td style={tdStyle}>{tr.durationMs} ms</td>
                    <td style={tdStyle}>{tr.promptTokens > 0 ? `${tr.promptTokens + tr.completionTokens} tok` : '-'}</td>
                    <td style={tdStyle}>{tr.costUsd > 0 ? `$${tr.costUsd}` : '-'}</td>
                    <td style={tdStyle}>
                      <span
                        style={{
                          padding: '2px 8px',
                          borderRadius: 10,
                          fontSize: 11,
                          fontWeight: 700,
                          backgroundColor: tr.isSuccess ? '#dcfce7' : '#fee2e2',
                          color: tr.isSuccess ? '#166534' : '#991b1b'
                        }}
                      >
                        {tr.isSuccess ? 'Thành công' : 'Lỗi'}
                      </span>
                    </td>
                  </tr>
                ))}
                {(!data?.recentTraces || data.recentTraces.length === 0) && (
                  <tr>
                    <td colSpan={8} style={{ ...tdStyle, textAlign: 'center', color: '#94a3b8' }}>
                      Chưa có vết thực thi nào được ghi nhận.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Footer info */}
      <div style={{ marginTop: 20, textAlign: 'right', fontSize: 12, color: '#94a3b8' }}>
        Lần cập nhật cuối: {lastUpdated.toLocaleTimeString()} | Máy chủ UTC: {overview?.serverTimeUtc ?? 'N/A'}
      </div>
    </div>
  );
};

const cardStyle: React.CSSProperties = {
  backgroundColor: '#ffffff',
  border: '1px solid #e2e8f0',
  borderRadius: 10,
  padding: '16px 20px',
  boxShadow: '0 1px 3px rgba(0,0,0,0.05)'
};

const cardHeaderStyle: React.CSSProperties = {
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
  marginBottom: 8
};

const panelStyle: React.CSSProperties = {
  backgroundColor: '#ffffff',
  border: '1px solid #e2e8f0',
  borderRadius: 10,
  padding: '20px',
  boxShadow: '0 1px 3px rgba(0,0,0,0.05)'
};

const tabButtonStyle = (isActive: boolean): React.CSSProperties => ({
  background: 'none',
  border: 'none',
  padding: '10px 16px',
  fontSize: 14,
  fontWeight: isActive ? 700 : 500,
  color: isActive ? '#16a34a' : '#64748b',
  borderBottom: isActive ? '3px solid #16a34a' : '3px solid transparent',
  cursor: 'pointer',
  transition: 'all 0.2s'
});

const thStyle: React.CSSProperties = {
  padding: '10px 12px',
  fontWeight: 600,
  fontSize: 12,
  textTransform: 'uppercase',
  letterSpacing: '0.05em'
};

const tdStyle: React.CSSProperties = {
  padding: '10px 12px'
};
