---
name: proofread
description: Proofread and audit the Release 2 Technical Report Google Doc, agentic-loop logs, and repository documentation against the Release 2 marking rubric, ASD 2026 project specifications PDF, and live Canvas assignment page fetched via Isaac's shared-backend Canvas API. Use after commits or major changes, or whenever asked to proofread, audit logs/docs, check microservice consistency, or verify links.
---

# Proofread & Release 2 Compliance Auditor

Use this skill to proofread the **[Release 2 Technical Report & Logs Google Doc](https://docs.google.com/document/d/19wEstx_ulZECxXk-INTRDa-nWoNjEaTHW4ewxbZe6kQ/edit?usp=sharing)**, repository documentation (`AGENTS.md`, `CLAUDE.md`, `README.md`, `docs/`), and evaluation logs (`tools/agentic_loop/logs/`) against the authoritative marking rubric and assignment specifications.

---

## 1. Context Sources Fed into This Skill

Always ground your proofreading in these four authoritative sources:

1. **Curated Rubric, Specifications & Microservice Matrix**:
   - Read [`references/release2-rubric-and-specs.md`](references/release2-rubric-and-specs.md) first. It contains the full 30-mark rubric (Criteria 1–10), +6 bonus rubric (B1–B6), deliverable checklist, reconciled rules between the PDF spec and Canvas brief, and the canonical `student-1`..`student-5` microservice matrix.
2. **Live Release 2 Canvas Assignment Page (via Isaac's Canvas API `shared-backend`)**:
   - Assignment URL: `https://canvas.uts.edu.au/courses/39716/assignments/267943` (Course `39716`, Assignment `267943`).
   - Following **Rule 2 of `AGENTS.md`**, never call Canvas with raw tokens outside `shared-backend`. Run the bundled helper script to fetch the live assignment description through Isaac's Canvas API (`GET /api/canvas/courses/39716/assignments`):
     ```bash
     python3 .agents/skills/proofread/scripts/fetch_release2_canvas.py
     ```
   - This script connects to a running `shared-backend` (`:5110`, `:8080`, or `:5010`) or automatically boots an ephemeral local `shared-backend` instance using root `.env` credentials to retrieve the latest assignment specification.
3. **ASD 2026 Project Specifications PDF (Google Drive)**:
   - Drive URL: `https://drive.google.com/file/d/1foChRW51MBtemJWPHmD5zLwRejxuxJQb/view?usp=sharing` (File ID: `1foChRW51MBtemJWPHmD5zLwRejxuxJQb`).
   - Downloadable via the `gdrive` CLI skill (`gdrive readonly download 1foChRW51MBtemJWPHmD5zLwRejxuxJQb --out /tmp/ASD_2026_Project_Specifications.pdf`).
4. **Release 2 Technical Report & Logs (Google Doc)**:
   - Google Doc URL: `https://docs.google.com/document/d/19wEstx_ulZECxXk-INTRDa-nWoNjEaTHW4ewxbZe6kQ/edit?usp=sharing` (Doc ID: `19wEstx_ulZECxXk-INTRDa-nWoNjEaTHW4ewxbZe6kQ`).
   - Readable via the `gdocs` CLI skill (`gdocs readonly read 19wEstx_ulZECxXk-INTRDa-nWoNjEaTHW4ewxbZe6kQ`) or text export (`https://docs.google.com/document/d/19wEstx_ulZECxXk-INTRDa-nWoNjEaTHW4ewxbZe6kQ/export?format=txt`).

---

## 2. Step-by-Step Proofreading Workflow

### Step 1: Run Automated Conflict & Link Verification
Run the verification script to check link accessibility across all markdown files and the Google Doc, and to detect cross-microservice conflicts:
```bash
python3 .agents/skills/proofread/scripts/verify_doc_and_links.py
```

### Step 2: Refresh Live Canvas Assignment Context (Isaac's Canvas API)
Verify whether the Canvas Assignment `267943` page has been updated by the subject coordinator:
```bash
python3 .agents/skills/proofread/scripts/fetch_release2_canvas.py --out /tmp/release2_canvas_live.md
```
Compare `updatedAt` and the description against [`references/release2-rubric-and-specs.md`](references/release2-rubric-and-specs.md). If any criteria or deadlines changed on Canvas, update `references/release2-rubric-and-specs.md` immediately.

### Step 3: Cross-Microservice Consistency Audit
Inspect the Google Doc and repository docs to verify **zero conflicting material** across all microservices:
- **Student 1 (Bryan Lee, `25495108`)**: Notifications & AI Digest (`student-1-frontend`, `student-1-backend` `:5101`, PostgreSQL 16 `student-1-database` / `notifications_db` `:5432`, `student-1.yml`).
- **Student 2 (Isaac Thomas, `25341708`)**: Automations Service (`student-2-frontend`, `student-2-backend` `:5102`, SQLite `student-2-db`, `student-2.yml`).
- **Student 3 (Jonathon Thomson, `25488154`)**: Deadlines & Task Tracker (`student-3-frontend`, `student-3-backend` `:5103`, SQLite service `student-3-database` `:5203`, `student-3.yml`).
- **Student 4 (Tristan Huang, `25322025`)**: Account & Authentication (`student-4-frontend`, `student-4-backend` `:5104`, `student-4-authentication` `:5114`, SQLite service `student-4-database` `:5204`, `student-4.yml`).
- **Student 5 (William Hannah, `25494675`)**: Grades & Progress (`student-5-frontend`, `student-5-backend` `:5105`, SQLite service `student-5-database` `:5205`, `student-5.yml`).
- **Shared Services**: `shared-shell` (`:8080`), `shared-backend` (`:5110`), and non-containerised local AI services `ai-mode` (`:5001`), `mcp-server` (`:5002`), `rag-server` (`:5003`), `multi-agent-server` (`:5004`).
- **Endpoint & Log Alignment**: Ensure the 2 tested endpoints listed for each student in Section 6.2 of the Google Doc match the actual tests in `student-1.yml` through `student-5.yml`, and that the log paths in Sections 5.4, 6.3, and 7.4 match the actual files under `tools/agentic_loop/logs/`.

### Step 4: Rubric & Deliverable Completeness Audit
Check the Google Doc against Criteria 1–10 (30 marks) and Bonus B1–B6 (+6 marks) in [`references/release2-rubric-and-specs.md`](references/release2-rubric-and-specs.md):
1. **Word Count**: Verify total word count is **<= 3,000 words** and submission filename is `group-<X>.pdf`.
2. **Placeholders**: Flag any remaining `[Insert ...]` blocks, `group-<X>.pdf`, or `https://github.com/<org>/<repo>` placeholders.
3. **Cloud Baseline Clarity**: Confirm the report consistently states that `AI-Mode`, `MCP`, `RAG`, and `Multi-Agent Server` are **disabled by default** in the required baseline cloud deployment (Criterion 7), and only enabled in the optional Bonus Cloud Profile (B1–B4).
4. **Logs Currency**: Verify that `tools/agentic_loop/logs/` contains up-to-date logs for all required review modes (`Multi-Agent Workflow Review`, `Testing Report Review`, `Cloud Deployment Report Review`, plus retained Release 1 modes) and that their timestamps/excerpts match what is cited in the Google Doc.

### Step 5: Output Report & Mandatory User Prompt
Present a concise, actionable audit table covering:
- **Log & Report Sync Status**
- **Cross-Microservice Conflict Check**
- **Link Accessibility Check**
- **Rubric Coverage & Placeholder Warnings**

Always conclude by prompting the user with the **Post-Change & Post-Commit Safeguard Checklist** defined in `AGENTS.md` Rule 6 & Section 9.3.
