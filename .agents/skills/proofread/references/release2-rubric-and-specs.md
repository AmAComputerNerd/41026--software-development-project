# Release 2 Marking Rubric, Assignment Specifications & Microservice Ground Truth

> **Sources**:
> 1. **Release 2 Canvas Assignment Page** ([Course 39716, Assignment 267943](https://canvas.uts.edu.au/courses/39716/assignments/267943)) — fetched via Isaac's Canvas API Gateway (`shared-backend` `GET /api/canvas/courses/39716/assignments`)
> 2. **ASD 2026 Project Specifications PDF** ([Google Drive](https://drive.google.com/file/d/1foChRW51MBtemJWPHmD5zLwRejxuxJQb/view?usp=sharing))
> 3. **Release 2 Technical Report & Logs (Google Doc)** ([Google Doc](https://docs.google.com/document/d/19wEstx_ulZECxXk-INTRDa-nWoNjEaTHW4ewxbZe6kQ/edit?usp=sharing))
> 4. **Canonical Repository Specification** ([`AGENTS.md`](../../../AGENTS.md))

---

## 1. Key Submission Rules & Constraints

- **Assessment Title**: Assessment 3 – Release 2: Multi-Agent Systems, Testing & Cloud Deployment
- **Weight**: 30% base mark (30 marks across 10 criteria) + up to **+6 bonus marks** (B1–B6)
- **Due Date**: 25 October 2026, 11:59 PM AEST (per Canvas Assignment 267943)
- **Report Format & Filename**: ONE group PDF submission (`group-<your group number>.pdf`)
- **Maximum Word Count**: **3,000 words**
- **Showcase Video**: Maximum **10 minutes** published URL included in the technical report; all 5 students must participate and demonstrate their feature.
- **Integration Rule**: An individual feature is **not** marked separately from the integrated group application. A non-integrated individual feature receives **zero** for the relevant working-software criteria.
- **Cloud Baseline Rule**: `AI-Mode`, `MCP Server`, `RAG Server`, and `Multi-Agent Server` must be **disabled by default** in the required baseline cloud deployment. Enabling them in the cloud is optional and assessed under Bonus Criteria B1–B4.

---

## 2. Marking Rubric (30 Marks Base + 6 Bonus Marks)

### 2.1 Core Marking Criteria (30 Marks)

| No. | Criterion | Description & Required Evidence | Marks |
|---|---|---|---|
| **1** | **Integrated Local Application and AI Services** | The complete Release 2 application runs locally as one integrated system. Each student's frontend, backend/API, and database microservices remain operational and support required CRUD functions (with >= 10 records per table). `AI-Mode`, `MCP Server`, and `RAG Server` from Release 0/1 remain integrated as shared non-containerised local services, together with the Release 2 `Multi-Agent Server`. | **3** |
| **2** | **Multi-Agent Server and Workflow** | The shared non-containerised `Multi-Agent Server` is implemented and integrated with the application. It executes the **Planner → Worker → Reviewer → Human Review** workflow through both the terminal and frontend UI. The technical report includes workflow history and coordination/audit evidence. | **3** |
| **3** | **Pre-Commit Security Testing** | Local pre-commit security testing is configured and executed during the commit workflow using **Ruff security rules**, **detect-secrets**, and **pip-audit** (or approved equivalents). The technical report includes the pre-commit security testing report and corresponding results per student. | **3** |
| **4** | **Post-Commit CI/CD Endpoint Testing** | Each student's assigned `student-x.yml` (`student-1.yml` to `student-5.yml`) workflow executes automated post-commit tests for **two endpoint functions** of that student's assigned backend/API service. The technical report includes each student's post-commit CI/CD testing report and successful workflow execution evidence. | **3** |
| **5** | **Cloud Deployment Scripts** | Reusable deployment scripts / IaC are implemented for the selected Microsoft Azure (Container Apps / App Service) or AWS (ECS with Fargate) environment and support deployment of the integrated Release 2 application. The technical report includes deployment scripts and execution evidence. | **3** |
| **6** | **Cloud Deployment CI/CD Workflow** | `cloud-deployment.yml` deploys the integrated application to the selected Azure or AWS environment through GitHub Actions after required CI/CD validation succeeds. The technical report includes successful cloud deployment workflow logs. | **3** |
| **7** | **Integrated Cloud Application – CRUD** | The integrated application is deployed to the selected Azure or AWS platform. Deployed frontend UI, backend/API, database, and required CRUD functions are operational. `AI-Mode`, `MCP`, `RAG`, and `Multi-Agent` remain **disabled by default** in the required baseline cloud deployment. | **3** |
| **8** | **Agentic-Loop Review Modes** | The shared non-containerised agentic loop (`tools/agentic_loop.py`) retains Release 1 modes and implements the 3 new Release 2 modes: **Multi-Agent Workflow Review**, **Testing Report Review**, and **Cloud Deployment Report Review**. Each mode reviews the required evidence/report and generates a corresponding review/validation log included in the technical report. | **3** |
| **9** | **Technical Report and Evidence** | The group technical report (<= 3,000 words) includes the Release 2 design flow, cloud deployment and CI/CD architecture, pre-commit security testing report, post-commit CI/CD testing reports, local deployment report (multi-terminal logs), cloud deployment report (plus human release decision), Multi-Agent workflow evidence, agentic-loop review outputs and validation logs, GitHub URL, contribution logs, and attendance checkpoints. | **3** |
| **10** | **Showcase and Q&A** | All 5 students participate in the max 10-minute group showcase and Q&A. Demonstrates the Multi-Agent workflow through the frontend UI, frontend UI in both local and cloud deployments, pre-commit security scans, post-commit CI/CD endpoint testing, agentic-loop review modes, and each student defends their Release 2 contribution. | **3** |
| | **Total Base Marks** | | **30** |

### 2.2 Optional Bonus Criteria – Cloud Extension (Maximum +6 Marks)

*Awarded only after the required Release 2 baseline cloud deployment (with AI services disabled by default) is operational.*

| No. | Bonus Criterion | Description & Required Evidence | Bonus |
|---|---|---|---|
| **B1** | **Cloud AI-Mode** | Enable and demonstrate `AI-Mode` in the selected Azure or AWS cloud deployment, including a successful interaction through the application UI. | **+1** |
| **B2** | **Cloud MCP** | Enable and demonstrate the `MCP Server` in the selected Azure or AWS cloud deployment, including a successful MCP tool interaction through the application. | **+1** |
| **B3** | **Cloud RAG** | Enable and demonstrate the `RAG Server` in the selected Azure or AWS cloud deployment, including a successful grounded response using retrieved context. | **+1** |
| **B4** | **Cloud Multi-Agent Server** | Enable and demonstrate the `Multi-Agent Server` in the selected Azure or AWS cloud deployment, including a successful `Planner → Worker → Reviewer` workflow. | **+1** |
| **B5** | **Cloud Endpoint Security** | Implement and demonstrate security controls protecting exposed cloud frontend/API endpoints (HTTPS/TLS, ingress/WAF, CORS, rate limiting, auth), with configuration and validation evidence. | **+1** |
| **B6** | **Cloud Data Security** | Implement and demonstrate security controls protecting cloud application data, secrets, credentials, and stored data (Key Vault / Secrets Manager, encryption at rest/in transit), with configuration and validation evidence. | **+1** |

---

## 3. Required Technical Report Deliverables Checklist

Every proofread pass over the Google Doc must verify that all required sections from Canvas Assignment 267943 Section 4 are complete and internally consistent:

1. **Project Overview**: Project overview, team members, individual feature allocation, and how Release 2 extends Release 1.
2. **Final Repository Structure**: Shared GitHub repository tree showing Release 1 artefacts + Release 2 Multi-Agent Server, tests, workflows (`student-1.yml`–`student-5.yml`, `cloud-deployment.yml`), agentic-loop additions, deployment scripts, and evidence logs.
3. **Release 2 Design Flow**:
   - Group integrated design flow: `Frontend UI → Backend/API → Database` + Release 1 `AI-Mode`, `MCP`, `RAG` flows + Release 2 `Frontend UI → Backend/API → Multi-Agent Server → Planner → Worker → Reviewer → Human Review → Response to Frontend UI`.
   - Individual updated architecture & design flows for Students 1–5 showing Multi-Agent Server and deployment flow.
4. **Cloud Deployment and CI/CD Architecture**:
   - Group Azure or AWS cloud reference architecture diagram.
   - DevOps pipeline flow: `source control → pre-commit security scans → GitHub Actions → post-commit testing of 2 backend/API endpoint functions per student → Docker build/validation → cloud-deployment.yml → Azure/AWS deployment`.
5. **Pre-Commit Security Testing Report**: Ruff security rules, `detect-secrets`, and `pip-audit` (or equivalents) configuration + per-student scan outputs.
6. **Post-Commit CI/CD Testing Reports**: Each student's report on automated tests for **2 backend/API endpoint functions** executed via `student-1.yml` to `student-5.yml`.
7. **Testing Review**: Agentic-loop `Testing Review` output report + validation log.
8. **Local Deployment Evidence**: Multi-server / multi-terminal execution logs showing Docker Compose containers + non-containerised local `AI-Mode`, `MCP`, `RAG`, `Multi-Agent Server`, and `agentic_loop`, plus per-student local deployment reports.
9. **Cloud Deployment Report and Review**: Reusable deployment scripts + `cloud-deployment.yml` logs + live cloud CRUD verification (with AI services disabled by default) + agentic-loop `Cloud Deployment Review` output & validation log + **final human release decision**.
10. **Multi-Agent Workflow Evidence**: Terminal and frontend UI execution evidence of `Planner → Worker → Reviewer → Human Review`, plus workflow history and coordination/audit logs, plus agentic-loop `Multi-Agent Review` output & validation log.
11. **GitHub Commit Logs & Contribution Logs**: Direct GitHub repository URL, identifiable Release 2 commit logs per student, contribution records, and attendance checkpoints.
12. **Bonus Evidence (if claimed)**: Configuration and live cloud evidence for B1–B6.
13. **Showcase Video URL**: Published video link (max 10 minutes).

---

## 4. Canonical Microservice & Team Ground Truth (Conflict Detection Matrix)

When verifying that there is **no conflicting material between microservices** in the Google Doc or repository documentation, enforce the following canonical matrix:

| Student Role | Student Name | Student ID | Slice Name | Frontend Route | Backend Service(s) & Host Port | Database Service / Engine | CI Workflow |
|---|---|---|---|---|---|---|---|
| **Student 1** | Bryan Lee | `25495108` | Notifications & AI Digest | `/notifications/` | `student-1-backend` (`:5101`) | `student-1-database` — **PostgreSQL 16** (`notifications_db`, `:5432`) | `student-1.yml` |
| **Student 2** | Isaac Thomas | `25341708` | Automations Service | `/automations/` | `student-2-backend` (`:5102`) | **SQLite** (`student-2-db`, owned directly by `student-2-backend`) | `student-2.yml` |
| **Student 3** | Jonathon Thomson | `25488154` | Deadlines & Task Tracker | `/deadlines/` | `student-3-backend` (`:5103`) | `student-3-database` — **SQLite persistence service** (`:5203` standalone, network `student-3-data`) | `student-3.yml` |
| **Student 4** | Tristan Huang | `25322025` | Account & Authentication | `/account/` | `student-4-backend` (`:5104`), `student-4-authentication` (`:5114`) | `student-4-database` — **SQLite persistence service** (`:5204` standalone, network `student-4-data`) | `student-4.yml` |
| **Student 5** | William Hannah | `25494675` | Grades & Progress | `/grades/` | `student-5-backend` (`:5105`) | `student-5-database` — **SQLite persistence service** (`:5205` standalone, network `student-5-data`) | `student-5.yml` |
| **Shared Shell** | Shared Team | — | Dashboard & Reverse Proxy | `/` | `shared-shell` (`:8080`) | None (Nginx + Vue 3 SPA) | — |
| **Shared Backend** | Shared Team (Isaac) | — | Canvas LMS Gateway | `/api/canvas/*` | `shared-backend` (`:5110`, `:5010` local launch) | **SQLite** (`shared-db` audit log) | — |
| **AI Services** | Shared Team | — | Non-containerised Host AI Services | `host.docker.internal` | `ai-mode` (`:5001`), `mcp-server` (`:5002`), `rag-server` (`:5003`), `multi-agent-server` (`:5004`) | Non-containerised local processes launched via `tools/run_ai_services.py` | — |

### Common Cross-Specification Pitfalls to Flag During Proofreading
1. **Database Engine Mismatches**:
   - Only **Student 1** uses **PostgreSQL 16** (`student-1-database`, `notifications_db`). Never describe Student 1 as SQLite, and never describe Students 2–5 as PostgreSQL.
   - **Student 2** uses an in-service SQLite database (`student-2-db`), whereas **Students 3, 4, and 5** use dedicated internal SQLite persistence microservices (`student-3-database`, `student-4-database`, `student-5-database`) on isolated Docker networks.
2. **Non-Containerised AI Services vs Generic PDF Diagrams**:
   - The generic semester PDF (`ASD_2026_Project_Specifications.pdf`) shows generic Flask/HTMX/containerised diagrams, whereas the **authoritative Release 2 Canvas Assignment 267943** explicitly specifies that `AI-Mode`, `MCP Server`, `RAG Server`, `Multi-Agent Server`, and the `agentic_loop` **must remain non-containerised local services** and **must not be added as Docker Compose services** in local deployment.
3. **Cloud Baseline AI State**:
   - Per **Canvas Assignment 267943** (Sections 1, 2, 4, 5 Criterion 7, and Section 6 Bonus Marks), `AI-Mode`, `MCP Server`, `RAG Server`, and `Multi-Agent Server` are **all disabled by default** in the required cloud baseline deployment. Enabling `AI-Mode` in the cloud is Bonus Criterion **B1 (+1 mark)**. Ensure the Google Doc does not contradict itself between Section 3.1, Section 7.3, and Section 8.
4. **Canvas Gateway Boundary (Rule 2)**:
   - No student microservice may call Canvas LMS directly; all Canvas API traffic goes through `shared-backend` (`http://shared-backend:8080/api/canvas/*`).
5. **Centralised AI Gateway (Rule 3)**:
   - Only `ai-mode` (`:5001`) holds `OPENROUTER_API_KEY`.
