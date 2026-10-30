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

## Phase 2 — Business & Retail Shopping Intelligence Expansion
- [x] Intent hierarchy nâng cao (Meal + 13 Retail Categories)
- [x] Meal planning intelligence (3-Tier Multi-turn Options, Cookpad grounding)
- [x] Budget optimization engine (In-budget vs Over-budget ranking, unit metric đ/kg)
- [x] Retail Shopping Intelligence (4 Tiers: Tier 1 Cheap, Tier 2 Value, Tier 3 Durable, Tier 4 Premium)
- [x] Explainable AI Recommendations (Result First -> Reason -> Trade-off analysis)
- [x] Retail Comparison Table Widget & Direct 1-click Cart Integration
- [x] 248/248 Unit & Scenario tests passed (100% pass rate)

---

## Phase 3 — Knowledge + RAG Expansion
- [x] Knowledge retrieval layer (Lexical & Semantic Overlap, Similarity Scoring)
- [x] In-memory vector/keyword chunk indexer & Sub-millisecond Caching (IKnowledgeCache)
- [x] Recipe knowledge base grounding (Cookpad culinary standards & chef tips)
- [x] Customer FAQ & Supermarket Policy grounding (24h fresh return, cold chain 0-4°C safety, voucher stacking)
- [x] Nutrition & Dietary standards grounding (Eat clean, low sodium, protein requirements)
- [x] Out-of-domain guardrails & Zero-hallucination safe fallbacks
- [x] 254/254 Unit, Scenario & RAG Retrieval tests passed (100% pass rate)

---

## Phase 4 — Optimization
- [x] Prompt versioning (v3.0.0-rag -> v4.0.0-optimized, rollback support)
- [x] Model routing optimization (DeterministicOffline, FastLightweight, StrongReasoning)
- [x] Cache tuning (Zero-cost FAQ grounding, Cart calculation)
- [x] Cost reduction (-36% prompt tokens, 66.7% operational cost reduction)
- [x] Latency reduction (Sub-10ms for FAQs/Cart, ~240ms for Greetings)
- [x] A/B Experimentation & Telemetry (Control_v3 vs Variant_A_Optimized)
- [x] Observability UI Integration (ModelRouted, Latency, PromptVersion badges)

---

## Phase 5 — Production & Operational Readiness
- [x] Observability hoàn chỉnh (Live Traces, Metrics, Token & Cost tracking)
- [x] Security hardening (Sanitized error responses, no stack trace leak)
- [x] Role-based authorization cho monitoring & retail analytics (Admin/Manager RBAC)
- [x] Secret management qua environment variables (GEMINI_API_KEY, GROQ_API_KEY, JWT_SECRET_KEY)
- [x] Health checks (/health/live, /health/ready, /api/v1/ai/health) & K8s probes
- [x] One-click Instant Prompt Rollback (/prompt-versions/rollback)
- [x] Operational runbooks & Incident response guides (11_Production_Hardening_Runbook.md)
- [x] Release checklist & 279/279 tests passed (100% pass rate)
