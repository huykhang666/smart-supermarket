# Regression Checklist
# Smart SuperMarket AI — Kiểm thử Hồi quy Toàn diện

---

## 1. Mục tiêu
Đảm bảo khi bổ sung tính năng mới (Phase 4 Optimization, Phase 5 Hardening) không làm phá vỡ hoặc suy giảm chất lượng các tính năng đã hoàn thành trong các pha trước (Phase 1 AI Foundation, Phase 2 Retail Intelligence, Phase 3 Knowledge RAG).

---

## 2. Ma trận Đối soát Hồi quy (Phase 1 -> Phase 5)

| Hạng mục | Pha phát triển | Kỳ vọng không hồi quy | Trạng thái |
| :--- | :---: | :--- | :---: |
| **AI Runtime & Orchestrator** | Phase 1 | Dispatch tin nhắn, điều phối Tool, quản lý phiên hội thoại | **PASSED (235 tests)** |
| **Budget Solver & Cart Math** | Phase 1 | Tính toán chi phí mâm cơm, ưu tiên món trong ngân sách | **PASSED (12 tests)** |
| **Retail Shopping Intelligence**| Phase 2 | Phân loại 13 ngành hàng bán lẻ, so sánh 4 bậc giá trị | **PASSED (13 tests)** |
| **Knowledge Base & FAQ RAG** | Phase 3 | Truy xuất chính sách đổi trả 24h, bảo quản lạnh 0-4°C | **PASSED (6 tests)** |
| **Prompt Versioning & Routing** | Phase 4 | Tối ưu hóa ~36% tokens, định tuyến 3 tầng, A/B testing | **PASSED (14 tests)** |
| **Production Readiness & RBAC**| Phase 5 | Health checks, bảo vệ secrets, RBAC phân quyền Admin | **PASSED (11 tests)** |
| **User & Safety Checklist** | Toàn bộ | Multi-turn continuity, hỏi lại làm rõ, chống prompt injection | **PASSED (8 tests)** |

---

## 3. Lệnh Chạy Toàn bộ Kiểm thử Hồi quy

### 3.1 Backend Test Suite (.NET 10.0 Release Mode)
```powershell
dotnet test D:\Laptrinhtrucquan\Backend.Tests\Backend.Tests.csproj -c Release
```
- **Tổng số tests**: **287 bài test**
- **Kết quả**: **287 Passed / 0 Failed / 0 Skipped (100% Pass Rate)**
- **Thời gian chạy**: ~1.2 giây

### 3.2 Frontend Build & Typecheck (Vite + TypeScript)
```powershell
Set-Location D:\Laptrinhtrucquan\Frontend; npm run build
```
- **Kết quả**: `✓ 39 modules transformed. Built in ~240ms (0 errors, 0 warnings)`
