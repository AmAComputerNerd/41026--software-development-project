# Plugging into the shared shell

The shell (`shared/frontend`) is a dashboard at `/` with a tile per feature. Wiring your
frontend in takes 4 steps.

## 1. Use the design tokens

Import `shared/frontend/src/styles/tokens.css` in your app and use its custom
properties instead of hardcoding colours, borders, or shadows:

```css
background: var(--color-surface);
border: var(--border-width-md) solid var(--border-color);
box-shadow: var(--shadow-offset-md) var(--shadow-offset-md) 0 var(--shadow-color);
```

See the file for the full list (colours, border widths, shadow offsets, fonts, spacing scale).

## 2. Add your nginx route

In `shared/frontend/nginx.conf`, uncomment/add your `location` block(s), pointing
`proxy_pass` at your docker-compose service name(s). Example already stubbed for grades:

```nginx
location /grades/ {
    proxy_pass http://student-2-frontend:80/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}

location /api/grades/ {
    proxy_pass http://student-2-backend:8080/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

Route prefix must match your tile's `route` in `tiles.ts` (step 4) and your API prefix
must match what your frontend calls.

## 3. Add your service to docker-compose.yml

Follow the `student-1-frontend` pattern at repo root `docker-compose.yml`:

```yaml
student-2-frontend:
    build:
        context: ./student-2/frontend
        dockerfile: Dockerfile
```

Add your container name to `shared-shell`'s `depends_on` so it starts before the shell.

## 4. Flip your tile live

In `shared/frontend/src/data/tiles.ts`, find your tile and set:

```ts
route: 'http://localhost:5173/grades',  // your route prefix from step 2
live: true,
```

That's it — rebuild (`docker compose up --build`) and your tile links out to your app
through the shell's nginx proxy.
