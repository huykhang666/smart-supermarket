# 09_Roadmap.md

# AI Roadmap

Version: 1.1

---

## Phase 1 — AI Foundation
- [x] Thiết kế kiến trúc AI
- [x] Xây dựng AI Core Runtime
- [x] Xây dựng Tool Dispatcher
- [x] Xây dựng BaseAgent
- [x] Xây dựng LLM Gateway
- [x] Xây dựng Conversation Service
- [x] Xây dựng Prompt Builder
- [x] Xây dựng AIController
- [x] Xây dựng Chat API
- [x] Xây dựng Angular Chat UI

---

## Phase 1.5 — Architecture Verification
- [x] Đối soát mã nguồn với docs/10_AI
- [x] Kiểm tra layering architecture
- [x] Kiểm tra dependency direction
- [x] Kiểm tra agent boundary
- [x] Kiểm tra tool boundary
- [x] Kiểm tra repository boundary
- [x] Kiểm tra code quality
- [x] Kiểm tra build và test
- [x] Refactor toàn bộ AI module

---

## Phase 1.6 — Observability & Reliability
- [x] Triển khai đa tầng caching
- [x] Triển khai OpenTelemetry metrics
- [x] Triển khai live monitoring dashboard
- [x] Triển khai 3-tier LLM failover gateway
- [x] Triển khai graceful fallback khi tool lỗi
- [x] Triển khai retry / timeout guard

---

## Phase 1.7 — Validation
- [x] 235/235 Unit & Integration tests passed
- [x] Build backend success
- [x] Build frontend success
- [x] Audit kiến trúc đạt chuẩn enterprise
- [x] Không phát hiện captive dependency

---

## Phase 2 — Business Intelligence
- [ ] Intent hierarchy nâng cao
- [ ] Meal planning intelligence
- [ ] Budget optimization engine
- [ ] Explainable recommendations
- [ ] Conversation memory dài hạn
- [ ] Kịch bản mua sắm theo gia đình / khẩu vị / ngân sách

---

## Phase 3 — Knowledge + RAG
- [ ] Embedding pipeline
- [ ] Vector database
- [ ] Retrieval layer
- [ ] Recipe knowledge base
- [ ] FAQ knowledge base
- [ ] Nutrition knowledge base

---

## Phase 4 — Optimization
- [ ] Prompt versioning
- [ ] Model routing optimization
- [ ] Cache tuning
- [ ] Cost reduction
- [ ] Latency reduction

---

## Phase 5 — Production
- [ ] Observability hoàn chỉnh
- [ ] Security hardening
- [ ] Role-based authorization cho monitoring
- [ ] Secret management qua environment variables / vault
- [ ] Release checklist
- [ ] Production deployment
