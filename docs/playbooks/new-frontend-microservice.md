# Playbook: adding a new student frontend

Follow this when scaffolding student-2, student-4, or student-5's
frontend, using student-1/frontend as the reference implementation.

## 1. Scaffold

Vue 3, no Vuetify, plain SCSS. Use create-vue the same way
student-1/frontend was set up (check its `package.json` for the exact
dependency versions to match — currently `vue ^3.5.41`, `vue-router
^5.2.0`, `sass-embedded`, `vue-tsc` for type-checking).

## 2. Add to the npm workspace

Add your frontend's path to the root `package.json`'s `"workspaces"`
array (alongside `shared/frontend`, `shared/ui-kit`, `student-1/frontend`).

## 3. Depend on @better-canvas/ui-kit

In your frontend's `package.json`:
```json
"dependencies": {
    "@better-canvas/ui-kit": "*"
}
```

In your app entrypoint (`main.ts`):
```ts
import '@better-canvas/ui-kit/styles/tokens.css'
import '@better-canvas/ui-kit/styles/primitives.css'
```

Use the CSS custom properties from ui-kit instead of hardcoding colours,
borders, or shadows (note the actual token names are prefixed `--nb-`):
```css
background: var(--nb-color-bg);
border: var(--nb-border-width-md) solid var(--nb-color-ink);
```

Use the shared TopNav component from ui-kit rather than building your own
nav bar, so the header stays identical across every feature.

## 4. Dockerfile

Copy `student-1/frontend/Dockerfile` as your starting point: multi-stage
build. Build stage (`node:22-alpine`) copies root `package.json` +
`package-lock.json`, `shared/ui-kit`, and your own `student-N/frontend`
directory, runs `npm ci`, then `npm run build --workspace=<your-workspace-name>`.
If your app needs its backend's base URL baked in at build time, set it
via an `ENV VITE_<NAME>_API_BASE_URL=/api/<name>` line before the build
step (student-1 does this for `VITE_NOTIFICATIONS_API_BASE_URL`). Serve
stage (`nginx:1.27-alpine`) copies your own `nginx.conf` to
`/etc/nginx/conf.d/default.conf` and the `dist/` output from the build
stage.

Your own `nginx.conf` inside your service only needs a Vue Router
history-mode fallback:
```
location / {
    try_files $uri $uri/ /index.html;
}
```
The actual cross-service routing happens in `shared/frontend/nginx.conf`,
not yours.

## 5. Add yourself to docker-compose.yml

Add a service block matching `student-1-frontend`'s pattern: build context
`.` (repo root, needed so the Dockerfile can `COPY shared/ui-kit`), your
Dockerfile path, no host `ports` entry (you're only reached through the
shell).

## 6. Uncomment your route in shared/frontend/nginx.conf

Commented-out stubs already exist for `/grades` (student-2),
`/automations` (student-4), and `/account` (student-5). Uncomment yours,
replace the placeholder container names (e.g. `student-2-frontend`,
`student-2-backend`) with your actual service names from
`docker-compose.yml`, and add `depends_on` entries for your
frontend/backend in the `shared-shell` service block (it currently
depends on `student-1-frontend` and `student-1-backend`).

## 7. Flip your tile to live

The dashboard tile grid is data-driven, not hardcoded per-tile in
`DashboardGrid.vue`. The live/dead switch lives in the canonical service
registry: `shared/ui-kit/src/services.ts`. Find your `ServiceId` entry in
the `SERVICES` array and set `route` to your shared-shell path (e.g.
`/grades/`) and `live: true` (student-1's `notifications` entry is the
only one currently live; student-3's `deadlines-tasks` entry is still
`route: null, live: false` as the placeholder pattern to follow until
built). `shared/frontend/src/data/tiles.ts` merges each `SERVICES` entry
with a dashboard-specific icon and description — add/check yours there
too if it's missing.
