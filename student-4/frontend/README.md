# Student 4 Frontend - Account & Settings

Frontend for the Account & Settings microservice, built with Vue 3, TypeScript, and Vite. Uses the shared `@better-canvas/ui-kit` for consistent neobrutalist styling across all microservices.

## Features

- **Login/Sign Up Page** - Unified authentication flow with support for Student, Teacher, and Admin account types
- **Profile Page** - View and edit personal details, with role-specific fields (Student/Teacher)

## Tech Stack

- **Vue 3** with Composition API and `<script setup>`
- **TypeScript** for type safety
- **Vue Router** for client-side routing
- **Vite** for fast development and building
- **Sass** for styling (neobrutalism design system)
- **@better-canvas/ui-kit** - Shared UI components and design tokens

## Project Structure

```
src/
├── api/              # API client functions
│   ├── http.ts       # Base fetch wrapper
│   └── users.ts      # User/student/teacher API calls
├── composables/      # Vue composables
│   └── useAuth.ts    # Authentication state management
├── components/       # Reusable components (future)
├── router/           # Vue Router configuration
├── styles/           # Feature-specific styles
│   └── neobrutalism.scss
├── views/            # Page components
│   ├── LoginView.vue
│   ├── ProfileView.vue
│   └── TwoFactorView.vue
├── App.vue           # Root component with navbar
├── main.ts           # App entry point
└── config.ts         # Runtime configuration
```

## Getting Started

### Prerequisites

- Node.js 20+
- npm or pnpm

### Installation

```bash
cd student-4/frontend
npm install
```

### Development

```bash
npm run dev
```

The app will be available at `http://localhost:5173` (or next available port).

### Build

```bash
npm run build
```

### Type Checking

```bash
npm run type-check
```

## Environment Variables

Create a `.env` file based on `.env.example`:

```env
VITE_ACCOUNT_API_BASE_URL=http://localhost:5104
VITE_USER_ID=your-user-id-for-testing
```

## API Integration

The frontend expects the following backend endpoints (from student-4/backend):

- `GET /api/users/{id}` - Get user profile
- `PUT /api/users/{id}` - Update user profile
- `GET /api/students/{userId}` - Get student details
- `PUT /api/students/{userId}` - Update student details
- `GET /api/teachers/{userId}` - Get teacher details
- `PUT /api/teachers/{userId}` - Update teacher details
- `POST /api/users` - Create new user (sign up)

**Note:** Authentication endpoints (login, password reset, 2FA) need to be implemented in the backend.

## Design System

This project uses the **neobrutalist** design system from `@better-canvas/ui-kit`:

- **Colors**: Off-white background (`--nb-color-bg`), near-black ink (`--nb-color-ink`), safety orange accent (`--nb-color-accent-orange`), hazard yellow (`--nb-color-accent-yellow`)
- **Typography**: Space Grotesk (display), JetBrains Mono (mono)
- **Borders**: Solid, no radius (2px/3px/4px widths)
- **Shadows**: Hard offset shadows (6px, no blur)
- **Components**: Buttons, chips, toggles, panels, form inputs

## Routing

| Path | Name | Component |
|------|------|-----------|
| `/` | `login` | LoginView |
| `/profile` | `profile` | ProfileView |
| `/2fa` | `twofa` | TwoFactorView |

## Docker

```bash
# Build image
docker build -t student-4-frontend .

# Run container
docker run -p 80:80 student-4-frontend
```

## Shared UI Kit

The `@better-canvas/ui-kit` package provides:
- `Navbar` - Cross-service navigation
- `ThemeToggle` - Dark/light mode toggle
- `ChannelToggle` - Notification channel toggle
- `SERVICES` - Canonical service list for navigation
- Design tokens (CSS custom properties)
- Primitive components (buttons, chips, panels, form inputs)

## Backend Requirements

The following backend endpoints need to be implemented for full functionality:

1. **Authentication**
   - `POST /api/auth/login` - Email/password login
   - `POST /api/auth/register` - User registration
   - `POST /api/auth/forgot-password` - Password reset request
   - `POST /api/auth/reset-password` - Password reset confirmation

2. **Two-Factor Authentication**
   - `POST /api/auth/2fa/setup` - Generate 2FA secret + QR code
   - `POST /api/auth/2fa/verify` - Verify TOTP code
   - `POST /api/auth/2fa/disable` - Disable 2FA
   - `GET /api/auth/2fa/backup-codes` - Get backup codes

3. **Session Management**
   - JWT token issuance/validation
   - Refresh token rotation
   - Secure cookie storage

## License

Internal project - Better Canvas