# Playbook: Adding a New Student Frontend Microservice

Follow this guide when scaffolding a new frontend microservice (such as `student-2` for Automations or `student-4` for Account), using `student-1/frontend`, `student-3/frontend`, and `student-5/frontend` as reference implementations.

---

## 1. Scaffold the Project

Use Vue 3 with TypeScript, `<script setup>`, and plain SCSS (no Vuetify). Match the existing project dependencies:
- `vue ^3.5.x`
- `vue-router ^4.x`
- `sass-embedded`
- `vue-tsc` (for type-checking)
- `@better-canvas/ui-kit` (workspace dependency)

---

## 2. Add to the Root npm Workspace

In root `package.json`, ensure your frontend path is listed in `"workspaces"`:

```json
"workspaces": [
  "shared/ui-kit",
  "shared/frontend",
  "student-1/frontend",
  "student-3/frontend",
  "student-5/frontend",
  "student-2/frontend",
  "student-4/frontend"
]
```

---

## 3. Depend on `@better-canvas/ui-kit`

In your `student-N/frontend/package.json`:

```json
"dependencies": {
  "@better-canvas/ui-kit": "*"
}
```

In your application entrypoint (`src/main.ts`):

```ts
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

// Import Neobrutalism design system tokens and styles
import '@better-canvas/ui-kit/styles/tokens.css'
import '@better-canvas/ui-kit/styles/primitives.css'

const app = createApp(App)
app.use(router)
app.mount('#app')
```

Use the shared `TopNav` component from `@better-canvas/ui-kit`:

```vue
<script setup lang="ts">
import { TopNav } from '@better-canvas/ui-kit'
</script>

<template>
  <TopNav title="Automations" current-service="automations" />
  <main class="page-container">
    <!-- Feature content -->
  </main>
</template>
```

---

## 4. Multi-Stage Dockerfile

Create `student-N/frontend/Dockerfile`:

```dockerfile
FROM node:22-alpine AS build
WORKDIR /app

# Copy root workspace manifests
COPY package.json package-lock.json ./
COPY shared/ui-kit/ ./shared/ui-kit/
COPY student-N/frontend/ ./student-N/frontend/

RUN npm ci

# Set backend API proxy path
ENV VITE_FEATURE_API_BASE_URL=/api/feature

WORKDIR /app/student-N/frontend
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY --from=build /app/student-N/frontend/dist /usr/share/nginx/html
COPY student-N/frontend/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

Your `student-N/frontend/nginx.conf` only needs standard SPA fallback:

```nginx
server {
    listen 80;
    server_name localhost;

    location / {
        root /usr/share/nginx/html;
        index index.html;
        try_files $uri $uri/ /index.html;
    }
}
```

---

## 5. Reverse Proxy & Compose Registration

1. In `docker-compose.yml`, define `student-N-frontend`:
   ```yaml
   student-N-frontend:
     build:
       context: .
       dockerfile: student-N/frontend/Dockerfile
     networks:
       - internal
   ```
2. In `shared/frontend/nginx.conf`, uncomment or add the proxy block:
   ```nginx
   location /feature/ {
       proxy_pass http://student-N-frontend:80/;
       proxy_set_header Host $host;
       proxy_set_header X-Real-IP $remote_addr;
   }
   ```
3. In `shared/ui-kit/src/services.ts`, toggle your service to `live: true` and specify its route.
4. In `shared/frontend/src/data/tiles.ts`, verify the tile icon, label, and description.
