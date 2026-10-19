# Azure Cloud Deployment (`infra/azure/`)

This directory contains the **Microsoft Azure Container Apps (ACA)** Infrastructure-as-Code (`main.bicep`), deployment automation (`deploy.sh`), and automated post-deployment CRUD & baseline verification (`validate-cloud.sh`) for **Release 2 (Assessment 3)**.

---

## 1. Two Supported Cloud Profiles

### A. Baseline Cloud Profile (Default — Criterion 7, 15 Marks)
Deploys the core web application (`shared-shell`, `shared-backend`, `student-1`..`student-5` backends and isolated persistence databases) with **all four AI tiers explicitly disabled**:
- `AI_MODE_ENABLED=false`
- `MCP_ENABLED=false`
- `RAG_ENABLED=false`
- `MULTI_AGENT_ENABLED=false`

All core CRUD workflows (`GET /notifications`, `POST /notifications/push`, `PATCH /notifications/{id}/read`, `DELETE /notifications/{id}`, `GET/PUT /preferences/{userId}`) operate against `student-1-database` (`PostgreSQL 16`) with zero dependency on external LLM services.

```bash
# Validate Bicep template in dry-run mode
./infra/azure/deploy.sh --mode baseline --dry-run

# Deploy Baseline Cloud Profile to Azure Container Apps
./infra/azure/deploy.sh --mode baseline

# Run automated CRUD & AI-disabled verification against deployed URL
./infra/azure/validate-cloud.sh --url https://<your-aca-fqdn> --expect-ai-enabled false
```

### B. Bonus Cloud Profile (Bonus B1–B6, up to +6 Marks)
Opt-in profile (`--mode bonus-full`) that additionally provisions:
- **B1 (`+1`)**: `ai-mode` Container App (`:5001`) with secret-injected `OPENROUTER_API_KEY`
- **B2 (`+1`)**: `mcp-server` Container App (`:5002`)
- **B3 (`+1`)**: `rag-server` Container App (`:5003`)
- **B4 (`+1`)**: `multi-agent-server` Container App (`:5004`)
- **B5 (`+1`)**: Production Cloud Security (HTTPS/TLS termination on `shared-shell`, strict CORS policy, rate-limiting headers, secret refs)
- **B6 (`+1`)**: Full Cloud Infrastructure Architecture (internal-only ingress for all feature backends, databases, and AI services inside the managed ACA environment)

```bash
# Deploy Bonus Cloud Profile (B1–B6)
./infra/azure/deploy.sh --mode bonus-full

# Verify Bonus Cloud Profile with AI enabled
./infra/azure/validate-cloud.sh --url https://<your-aca-fqdn> --expect-ai-enabled true
```
