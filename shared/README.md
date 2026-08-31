# Shared services

## Shared backend

The shared backend owns third-party API integrations. Other microservices call
its HTTP API and must not read its database directly.

### Local setup

Set these values in the repository root `.env` file:

```dotenv
CANVAS_BASE_URL=https://your-institution.instructure.com
CANVAS_API_TOKEN=your-token
```

The service searches parent directories for `.env` when run outside Docker:

```powershell
dotnet run --project shared\backend\Api
```

Canvas endpoints are available under `/api/canvas`. The SQLite database stores
request audit records only, it is not used for caching Canvas data.

`CanvasFacade` holds an in-memory cache (`IMemoryCache`, in-process only, not
persisted anywhere) for courses, assignments, and enrolled users, each keyed
by request parameters (e.g. course ID) with a 3 minute TTL. This means
responses may be up to 3 minutes stale rather than always live, and cuts
down on repeated identical Canvas API calls across the multiple backends
that call this service.

## Plugging into the shared shell

The shell (`shared/frontend`) is a dashboard at `/` with a tile per feature. Wiring your
frontend in takes 4 steps.

### 1. Depend on @better-canvas/ui-kit

Once your app is on Vue, depend on the shared `@better-canvas/ui-kit` workspace
package (`shared/ui-kit`). Add it to
your frontend's `package.json`:

```json
"dependencies": {
    "@better-canvas/ui-kit": "*"
}
```

Since it's an npm workspace package, `npm install` at the repo root links it in.
Import its stylesheets once in your app entrypoint, and use its custom
properties instead of hardcoding colours, borders, or shadows:

```ts
// main.ts
import '@better-canvas/ui-kit/styles/tokens.css'
import '@better-canvas/ui-kit/styles/primitives.css'
```

```css
background: var(--nb-color-bg);
border: var(--nb-border-width-md) solid var(--nb-color-ink);
box-shadow: var(--nb-shadow);
```

See `shared/ui-kit/src/styles/tokens.css` for the full list (colours, border
widths, shadow offsets, fonts, spacing scale). `student-1/frontend` is the
reference implementation for consuming the kit — check its `package.json` and
`src/main.ts` for the wiring.

### 2. Add your nginx route

In `shared/frontend/nginx.conf`, uncomment/add your `location` block(s), pointing
`proxy_pass` at your docker-compose service name(s). Example for automations:

```nginx
location /automations/ {
    proxy_pass http://student-2-frontend:80/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}

location /api/automations/ {
    proxy_pass http://student-2-backend:8080/api/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

Route prefix must match your tile's `route` in `tiles.ts` (step 4) and your API prefix
must match what your frontend calls.

### 3. Add your service to docker-compose.yml

Follow the `student-1-frontend` pattern at repo root `docker-compose.yml`:

```yaml
student-2-frontend:
    build:
        context: .
        dockerfile: student-2/frontend/Dockerfile
```

Add your container name to `shared-shell`'s `depends_on` so it starts before the shell.

### 4. Flip your tile live

In `shared/ui-kit/src/services.ts`, find your service and set:

```ts
route: '/automations/',
live: true,
```

That's it — rebuild (`docker compose up --build`) and your tile links out to your app
through the shell's nginx proxy.
