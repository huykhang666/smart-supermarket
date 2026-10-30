# Admin / Ops AI Tests
# Smart SuperMarket AI — Kiểm thử Quản trị & Vận hành

---

## 1. Mục tiêu & Nguyên tắc
Kiểm thử toàn diện các năng lực vận hành, giám sát và kiểm soát rủi ro của đội ngũ DevOps / Quản trị siêu thị:
- Kiểm tra tính sống động (Liveness) và sẵn sàng (Readiness) của hệ thống AI.
- Xác thực thu thập Telemetry, Latency, Cache Hit và Tokens.
- Khả năng chuyển đổi và Rollback phiên bản prompt tức thì.
- Phân quyền nghiêm ngặt dựa trên vai trò (RBAC).

---

## 2. Kịch bản Kiểm thử Chi tiết

### 2.1 Kiểm tra Sức khỏe (Health Checks)
- `GET /health/live`:
  - Mã HTTP: `200 OK`. Xác nhận tiến trình ASP.NET Core đang sống.
- `GET /health/ready`:
  - Mã HTTP: `200 OK`. Xác nhận kết nối CSDL và các dependency nền tảng sẵn sàng.
- `GET /api/v1/ai/health`:
  - Mã HTTP: `200 OK`. Trả về báo cáo trạng thái chi tiết của 5 thành phần: CSDL, RAG Grounding Index, Prompt Versioning, Model Router, LLM Gateway.

---

### 2.2 Giám sát & Bảng điều khiển (Monitoring Dashboard)
- `GET /api/v1/ai/monitoring/dashboard`:
  - Yêu cầu xác thực: `Admin, Manager`.
  - Dữ liệu trả về: Tổng request, tỷ lệ thành công, P95 latency, thời lượng gọi tool, token tiêu thụ, chi phí USD/VND và live traces.
- `POST /api/v1/ai/monitoring/reset`:
  - Yêu cầu xác thực: `Admin` (chỉ duy nhất Admin được phép reset bộ đếm benchmark).

---

### 2.3 Quản lý Phiên bản & Rollback (Prompt Versioning)
- `GET /api/v1/ai/monitoring/prompt-versions`:
  - Xem phiên bản active hiện hành (`v4.0.0-optimized`), số tokens ước tính và lịch sử thay đổi.
- `POST /api/v1/ai/monitoring/prompt-versions/rollback`:
  - Yêu cầu xác thực: `Admin`.
  - Body: `{"targetVersion": "v3.0.0-rag"}`.
  - Kỳ vọng: Hệ thống chuyển ngay lập tức phiên bản active về `v3.0.0-rag` mà không cần khởi động lại máy chủ; các request sau đó phản ánh ngay nội dung prompt mới.

---

### 2.4 Kiểm soát Truy cập Dựa trên Vai trò (RBAC)
- Khách vãng lai / User thường:
  - Truy cập `/api/v1/ai/shopping/meal-plan`: **Cho phép** (Phục vụ mua sắm).
  - Truy cập `/api/v1/ai/monitoring/dashboard`: **Bị chặn (401 Unauthorized)**.
  - Truy cập `/api/v1/ai/retail/product-risks`: **Bị chặn (401 Unauthorized)**.
- Quản lý (Manager):
  - Xem Dashboard, Metrics, Dự báo nhập hàng: **Cho phép**.
  - Reset bộ đếm hoặc Rollback phiên bản: **Bị chặn (403 Forbidden)**.
- Quản trị viên (Admin):
  - Toàn quyền thực hiện mọi thao tác vận hành và rollback.

---

## 3. Kết quả Xác thực
- Mã kiểm thử tự động: [`ProductionHardeningAndReleaseTests.cs`](file:///D:/Laptrinhtrucquan/Backend.Tests/AI/ProductionHardeningAndReleaseTests.cs) & [`AiTestChecklistTests.cs`](file:///D:/Laptrinhtrucquan/Backend.Tests/AI/AiTestChecklistTests.cs)
- Trạng thái: **PASS 100%.**
