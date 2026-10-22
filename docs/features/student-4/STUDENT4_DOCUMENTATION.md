# Student 4 - Account & Authentication Microservice Documentation

## Overview

**Student**: Tristan Huang  
**Student ID**: 25322025  
**Service**: Account & Authentication Microservice  
**Repository Path**: `student-4/`  
**Last Updated**: 2026-10-22

---

## Service Architecture

### Components

| Component | Path | Technology | Port | Description |
|-----------|------|------------|------|-------------|
| **Backend API** | `student-4/backend/Api/` | ASP.NET Core (.NET 10) | 5104 | Profile management, Multi-Agent profile summaries, Student/Teacher records |
| **Authentication** | `student-4/authentication/Authentication/` | ASP.NET Core (.NET 10) | 5114 | Login, registration, password management, cookie-based auth |
| **Database** | `student-4/database/Database/` | ASP.NET Core + EF Core SQLite | 5204 (standalone) | Internal persistence service on private `student-4-data` network |
| **Frontend** | `student-4/frontend/` | Vue 3 + TypeScript + Vite | Proxied at `/account/` | Account UI, login, registration, profile management |
| **Contracts** | `student-4/contracts/Contracts/` | C# Class Library | - | Shared DTOs and commands |

### Network Isolation

- Database runs on private Docker network `student-4-data` (internal only)
- Backend and Authentication services communicate with Database via HTTP on `student-4-data` network
- No direct database access from other services - all via HTTP APIs

---

## Key Features Implemented

### 1. Account Management
- User registration with email, password, names, DOB, gender, role (Student/Teacher/Admin)
- Profile editing (email, names, gender, DOB, profile summary)
- Student details: Course status (FullTime/PartTime/Inactive), International status, Canvas API Key
- Teacher details: Employment status (FullTime/PartTime/Inactive), Canvas API Key
- Account deletion with password confirmation

### 2. Authentication & Security
- **Cookie-based authentication** with 30-day sliding expiration
- HttpOnly, Secure, SameSite=Lax cookies
- BCrypt password hashing
- Password change with current password verification
- Forgot password / reset via email (MailHog in dev, Gmail SMTP in prod)
- Account deletion with password confirmation

### 3. Multi-Agent Profile Summary Review
- **Planner → Worker → Reviewer** workflow via shared Multi-Agent Server (port 5004)
- Human-in-the-loop: Approve / Reject / Request Revision
- Proposal does not auto-save - requires explicit "USE NEW SUMMARY" + "SAVE CHANGES"
- Revision support with feedback
- Canvas API Key, password hashes, DOB, email excluded from AI context

### 4. Account Readiness (MCP)
- Profile completeness check
- Role record validation
- Canvas gateway connectivity probe
- Notification service health check
- Overall status: Ready / Needs Attention / Unavailable

### 5. Account Help (RAG)
- Documentation-grounded Q&A scoped to Student 4
- Source citations with confidence levels
- Insufficient context handling

### 6. Canvas API Key Management
- Required field for Student/Teacher records
- Stored in database and cookie (1-year expiry, SameSite=Lax)
- Auto-stored in cookie on login/profile load/update
- Cleared on logout
- Dedicated endpoint: `GET /api/students/{userId}/canvas-api-key`

### 7. Notifications Integration
- Pushes to Student 1 (Notifications) on:
  - Password change
  - Password reset
  - Profile update
  - AI profile summary generation

---

## API Endpoints

### Authentication (`/api/auth/`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/login` | Login with email/password, sets auth cookie |
| POST | `/logout` | Clears auth cookie |
| GET | `/me` | Returns current user from cookie claims |
| POST | `/change-password` | Change password with current password verification |
| DELETE | `/delete-account` | Delete account with password confirmation |
| POST | `/forgot-password` | Request password reset email |
| POST | `/reset-password` | Reset password with token from email |

### Users (`/api/users/`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | List all users |
| GET | `/{userId}` | Get user by ID |
| POST | `/` | Create new user |
| PUT | `/{userId}` | Update user profile |
| DELETE | `/{userId}` | Delete user |
| POST | `/{userId}/profile-summary` | Generate Multi-Agent profile summary proposal |
| POST | `/{userId}/profile-summary/{workflowId}/review` | Approve/Reject proposal |
| POST | `/{userId}/profile-summary/{workflowId}/revise` | Request revision with feedback |

### Students (`/api/students/`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/{userId}` | Get student record |
| PUT | `/{userId}` | Update student record |
| GET | `/{userId}/canvas-api-key` | Get Canvas API key |

### Teachers (`/api/teachers/`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/{userId}` | Get teacher record |
| PUT | `/{userId}` | Update teacher record |

### Knowledge (`/api/users/`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/{userId}/mcp-readiness` | MCP account readiness check |
| POST | `/help/answers` | RAG-based account help Q&A |

---

## Data Models

### UserRecord
```csharp
Guid Id
string Email
string PasswordHash
string FirstName
string? MiddleNames
string LastName
string Gender
DateTime DateOfBirth
string UserType  // "Student" | "Teacher" | "Admin"
string? UserProfile
```

### StudentRecord
```csharp
Guid UserId
string CourseStatus  // "FullTime" | "PartTime" | "Inactive"
bool IsInternational
string CanvasApiKey  // REQUIRED
```

### TeacherRecord
```csharp
Guid UserId
string EmploymentStatus  // "FullTime" | "PartTime" | "Inactive"
string CanvasApiKey  // REQUIRED
```

---

## Frontend Structure

### Pages
- `/account/` - Login/Registration
- `/account/profile` - Profile management (edit, password, delete, AI summary)
- `/account/knowledge` - MCP readiness + RAG Account Help

### Key Components
- `ProfileView.vue` - Main profile page with Multi-Agent summary review UI
- `LoginView.vue` - Login/Registration/Forgot password
- `KnowledgeView.vue` - MCP readiness + RAG help
- `Navbar.vue` - Shared navigation (in ui-kit)

### State Management
- `useAuth.ts` - Composable for auth state, session restoration, profile operations
- Cookie-based session restoration on app mount (`main.ts`)

---

## Configuration

### Environment Variables (`.env`)
```bash
# Authentication Service
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__Username=your-email@gmail.com
Email__Smtp__Password=your-app-password
Email__Smtp__UseSsl=true
Email__FromAddress=your-email@gmail.com
Email__FromName=Student 4 Account Service

# Database
DatabaseService__BaseUrl=http://student-4-database:8080

# Shared Services
SharedService__BaseUrl=http://shared-backend:8080
NotificationService__BaseUrl=http://student-1-backend:8080

# AI Services (optional)
MultiAgentServer__Enabled=true
MultiAgentServer__BaseUrl=http://host.docker.internal:5004
McpServer__Enabled=true
McpServer__BaseUrl=http://host.docker.internal:5002/mcp
RagServer__Enabled=true
RagServer__BaseUrl=http://host.docker.internal:5003
```

### Docker Compose
```yaml
student-4-backend:
  build: ./student-4/backend/Api
  ports: ["5104:8080"]
  environment:
    DatabaseService__BaseUrl: http://student-4-database:8080
    MultiAgentServer__Enabled: ${MULTI_AGENT_ENABLED}
    MultiAgentServer__BaseUrl: http://host.docker.internal:${MULTI_AGENT_HOST_PORT:-5004}
  networks: [default, student-4-data]
  depends_on: [student-4-database, shared-backend, mailhog]

student-4-authentication:
  build: ./student-4/authentication/Authentication
  ports: ["5114:8080"]
  networks: [default, student-4-data]
  depends_on: [student-4-database, mailhog]

student-4-database:
  build: ./student-4/database/Database
  networks: [student-4-data]  # Private network
```

---

## CI/CD Pipeline (`.github/workflows/student-4.yml`)

### Jobs
1. **Frontend Build** - Type check + production build
2. **Build & Analyze** - .NET build, format check, static analysis
3. **Test** - Unit tests (when added)
4. **EF Core Migrations Check** - Detects pending migrations
5. **NuGet Audit** - Vulnerability scanning

### CI Configuration
- MCP, RAG, Multi-Agent explicitly **disabled** in CI
- Database runs standalone on port 5204
- Backend runs on port 5104
- Smoke tests verify endpoints return 503 when AI services disabled

---

## Commit History Summary

### Major Features
| Date | Commit | Description |
|------|--------|-------------|
| 2026-10-22 | Multi-Agent Profile Summary | Implemented Planner/Worker/Reviewer workflow with human review |
| 2026-10-22 | Cookie Authentication | Added cookie-based auth with 30-day sliding expiration |
| 2026-10-22 | Session Restoration | Auto-restore session on app mount via `/api/auth/me` |
| 2026-10-22 | Canvas API Key Cookie | Store/retrieve Canvas API key in 1-year cookie |
| 2026-10-22 | Canvas API Key Endpoint | Added `GET /api/students/{userId}/canvas-api-key` |
| 2026-10-22 | Required Canvas API Key | Made CanvasApiKey required (non-nullable) in all contracts |
| 2026-10-22 | Seeded API Key Text | Updated seeder to use "Seeded User, No API Key Supplied" |
| 2026-10-22 | Canvas API Key Cookie | Store in 1-year cookie, auto-replace on different account login |

### Contributors
- **Tristan Huang** (Student 4) - Primary developer

---

## Testing

### Manual Test Checklist
- [ ] Login with valid credentials → cookie set, redirect to profile
- [ ] Login with invalid credentials → 401 error
- [ ] Register new Student → profile created, cookie set, redirect to profile
- [ ] Register new Teacher → profile created, cookie set, redirect to profile
- [ ] Edit profile → changes persisted, notification sent
- [ ] Change password → success, notification sent
- [ ] Forgot password → email sent to MailHog
- [ ] Reset password → password updated, notification sent
- [ ] Delete account → account removed, cookie cleared, redirect to login
- [ ] Generate AI summary → Planner/Worker/Reviewer proposal shown
- [ ] Approve summary → "USE NEW SUMMARY" enabled
- [ ] Reject summary → proposal rejected, original unchanged
- [ ] Request revision → new proposal generated
- [ ] MCP readiness check → returns 4 checks with status
- [ ] RAG help → returns grounded answer with citations
- [ ] Canvas API key cookie → set on login, cleared on logout
- [ ] Session restoration → auto-login on page refresh
- [ ] Navigate to other services → session persists via cookie

---

## Known Issues / Future Work

1. **Cross-service cookie sharing** - Cookie works for auth service; other services need nginx proxy config for cookie forwarding
2. **Admin role UI** - Admin users can't manage other users via UI
3. **Email templates** - Could use HTML templates for password reset emails
4. **Rate limiting** - No rate limiting on auth endpoints
5. **Audit logging** - Limited audit trail for sensitive operations

---

## Related Documentation

- [Account Help Guide](./account-help.md) - User-facing help documentation
- [Architecture Overview](../../architecture/overview.md) - System-wide architecture
- [Services Documentation](../../architecture/services.md) - All services reference
- [API Endpoints](../../architecture/services.md#7-student-4-account--authentication-microservice) - Complete endpoint list

---

## Google Doc Update Instructions

To update the Google Doc at https://docs.google.com/document/d/19wEstx_ulZECxXk-INTRDa-nWoNjEaTHW4ewxbZe6kQ/edit?tab=t.0#heading=h.mqkzsetnlplx:

1. Copy relevant sections from this document
2. Update the Student 4 section with:
   - Commit history table
   - Contribution logs
   - Feature list with implementation dates
   - API endpoint table
   - Architecture diagram reference
3. Update the commit history section with the commit history table above
4. Update the architecture section with the service architecture table
5. Ensure all links are accessible (run `python3 .agents/skills/proofread/scripts/verify_doc_and_links.py`)