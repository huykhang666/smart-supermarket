# 11_Production_Hardening_Runbook.md
# Smart Supermarket AI — Production Hardening, Release & Operational Runbook (Phase 5)

---

## 1. Executive Summary & Production Readiness Overview

Phase 5 finalizes the enterprise readiness layer of the Smart Supermarket AI Shopping Concierge. The system transitions from a feature-complete AI module to a hardened, observable, rollback-safe, and secure production service.

Key capabilities established:
- **Zero-Secret Static Config**: Production configuration uses `appsettings.Production.json` with strict environment variable injection (`GEMINI_API_KEY`, `GROQ_API_KEY`, `JWT_SECRET_KEY`, `DB_PASSWORD`).
- **Role-Based Authorization (RBAC)**: All administrative, retail forecasting, and operational endpoints are strictly guarded by `[Authorize(Roles = "Admin,Manager")]` or `[Authorize(Roles = "Admin")]`.
- **Dual-Surface Health Probes**: Native ASP.NET Core Health Checks (`/health/live`, `/health/ready`) and rich AI Subsystem diagnostic endpoint (`/api/v1/ai/health`).
- **One-Click Live Rollback**: Support for instantaneous zero-downtime prompt rollback (`POST /api/v1/ai/monitoring/prompt-versions/rollback`).
- **Output Hygiene**: Suppression of internal stack traces, DB connection strings, and socket addresses in non-Development environments.

---

## 2. Secrets Management & Environment Variables Matrix

In production environments (Docker, Kubernetes, Azure App Service), all sensitive credentials MUST be provided through environment variables. Static configuration files committed to Git must only contain placeholder tokens.

| Environment Variable | Target Configuration Key | Purpose / Description | Default / Fallback |
| :--- | :--- | :--- | :--- |
| `GEMINI_API_KEY` | `Gemini:ApiKey` | Primary LLM Provider API Key (Google AI Studio) | Local Fallback / Offline deterministic mode |
| `GROQ_API_KEY` | `Groq:ApiKey` | Fast Lightweight Model API Key (LPU chips) | Local Fallback / groq-llama3-8b |
| `GEMINI_MODEL` | `Gemini:Model` | Overrides reasoning LLM model | `gemini-2.5-flash` |
| `GROQ_MODEL` | `Groq:Model` | Overrides fast tier model | `llama-3.3-70b-versatile` |
| `CONNECTIONSTRINGS__DEFAULTCONNECTION` | `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | `Host=localhost;Database=AISupermarketDB` |
| `CONNECTIONSTRINGS__REDIS` | `ConnectionStrings:Redis` | Redis cluster connection string | In-Memory Distributed Cache |
| `JWT__SECRETKEY` | `Jwt:SecretKey` | HMAC-SHA256 signature key (min 32 chars) | Fails startup validation if blank in Prod |
| `ASPNETCORE_ENVIRONMENT` | Hosting Environment | Controls exception sanitization & logging levels | `Production` |

---

## 3. Endpoint Security & Role-Based Access Control (RBAC)

The AI module adheres to the **Principle of Least Privilege**:

```
                       ┌───────────────────────────────────────────────┐
                       │               Incoming AI Request             │
                       └───────────────────────┬───────────────────────┘
                                               │
             ┌─────────────────────────────────┼─────────────────────────────────┐
             │                                 │                                 │
             ▼                                 ▼                                 ▼
┌───────────────────────────┐     ┌───────────────────────────┐     ┌───────────────────────────┐
│     Customer Facing       │     │     Store Management      │     │      SysAdmin Only        │
├───────────────────────────┤     ├───────────────────────────┤     ├───────────────────────────┤
│ • /shopping/meal-plan     │     │ • /monitoring/dashboard   │     │ • /monitoring/reset       │
│ • /chat                   │     │ • /monitoring/metrics     │     │ • /prompt-versions/active │
│ • /cart/inject            │     │ • /retail/product-risks   │     │ • /prompt-versions/rollback│
│ • /agent/execute          │     │ • /retail/market-basket   │     │ • Secrets Rotation        │
│ • /health (Diagnostics)   │     │ • /retail/draft-import    │     │                           │
│ Policy: [AllowAnonymous]  │     │ Policy: [Roles=Admin,Mgr] │     │ Policy: [Roles=Admin]     │
└───────────────────────────┘     └───────────────────────────┘     └───────────────────────────┘
```

---

## 4. Health Checks & Kubernetes Deployment Probes

The service exposes standardized health check probes for container orchestrators:

### Kubernetes Pod Spec Example:
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 5

startupProbe:
  httpGet:
    path: /api/v1/ai/health
    port: 8080
  failureThreshold: 10
  periodSeconds: 5
```

### Response of `/api/v1/ai/health`
```json
{
  "isSuccess": true,
  "message": "Trạng thái vận hành AI: Healthy",
  "data": {
    "status": "Healthy",
    "environment": "Production",
    "activePromptVersion": "v4.0.0-optimized",
    "responseTimeMs": 4,
    "timestamp": "2026-10-29T13:00:00Z",
    "subsystems": {
      "database": { "status": "Healthy", "latencyMs": 2 },
      "knowledgeRetrievalRAG": { "status": "Healthy", "exactMatchIndexReady": true },
      "promptVersioning": { "status": "Healthy", "active": "v4.0.0-optimized" },
      "modelRouter": { "status": "Healthy", "defaultModel": "groq-llama3-8b" },
      "llmGateway": { "status": "Healthy", "provider": "Gemini" }
    }
  }
}
```

---

## 5. Operational Runbooks (Incident Response)

### Runbook 1: High Latency / LLM Gateway Slowdown (> 2,500ms)
- **Symptoms**: Customer chat takes >2.5s; alert on P95 Latency triggered.
- **Immediate Action**:
  1. Check `GET /api/v1/ai/monitoring/metrics` to isolate whether delay stems from Remote Gemini API or Database queries.
  2. If upstream Gemini API is lagging, the system's `CancellationToken` (10s timeout guard) will automatically abort and fall back to local deterministic responses.
  3. Optionally switch active prompt version to lightweight `v4.0.0-optimized` or temporarily switch router to `FastLightweight` tier (`groq-llama3-8b`).

### Runbook 2: LLM Upstream Outage / Quota Exceeded (HTTP 429/503)
- **Symptoms**: Gemini API returns 429 ResourceExhausted or 503 Service Unavailable.
- **Immediate Action**:
  - The built-in 3-tier gateway automatically routes to `GroqGateway` (Tier 2).
  - If Groq is also unreachable, the system automatically falls back to `LocalFallbackGateway` and `DeterministicOffline` rules.
  - Zero customer crash: meal plans are generated using offline supermarket recipe templates.

### Runbook 3: Malformed / Degraded Prompt Release (Rollback Procedure)
- **Symptoms**: Hallucinations or malformed customer messages after a new prompt rollout.
- **Rollback Procedure**:
  - **Option A (Via Dashboard)**: Go to AI Monitoring Page -> Tab "🛡️ Vận hành & Sức khỏe" -> Click `⏪ Rollback về v3.0.0-rag`.
  - **Option B (Via REST API)**:
    ```bash
    curl -X POST https://api.supermarket.com/api/v1/ai/monitoring/prompt-versions/rollback \
      -H "Authorization: Bearer <ADMIN_JWT_TOKEN>" \
      -H "Content-Type: application/json" \
      -d '{"targetVersion": "v3.0.0-rag"}'
    ```
  - Execution time: < 5ms. No pod restart required.

### Runbook 4: Cache Desynchronization / Inventory Spike
- **Symptoms**: Out-of-stock items still appearing in meal plan recommendations.
- **Immediate Action**:
  - Meal planning queries read directly from `InventoryBatches.Where(b => b.Quantity > 0)`.
  - If Redis cache has stale stock data, call `POST /api/v1/ai/monitoring/reset` with Admin token to purge all cached states.

---

## 6. Pre-Release Verification Checklist

Before deploying any release to production, verify:
- [x] All 279 backend unit and integration tests pass in Release configuration.
- [x] Frontend builds cleanly with zero TypeScript errors (`tsc -b && vite build`).
- [x] All operational endpoints require Admin/Manager authorization.
- [x] Production environment does not leak internal stack traces or connection strings in error responses.
- [x] Health checks (`/health/live`, `/health/ready`, `/api/v1/ai/health`) return HTTP 200 OK.
- [x] `appsettings.Production.json` does not contain hardcoded production secrets.
- [x] Instant live rollback has been validated and proven operational.
