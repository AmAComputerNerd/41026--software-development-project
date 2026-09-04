# @better-canvas/ui-kit

The canonical Neobrutalism design system, shared CSS primitives, design tokens, and reusable Vue 3 components for the Better Canvas platform.

---

## 1. Installation & Setup

`@better-canvas/ui-kit` is an npm workspace package located at `shared/ui-kit`.

To use it in any frontend microservice:

1. Add it to `package.json`:
   ```json
   "dependencies": {
     "@better-canvas/ui-kit": "*"
   }
   ```
2. Import the stylesheets in `main.ts`:
   ```ts
   import '@better-canvas/ui-kit/styles/tokens.css'
   import '@better-canvas/ui-kit/styles/primitives.css'
   ```

---

## 2. Design Tokens Reference

All design tokens are defined in `src/styles/tokens.css` and use `--nb-*` custom properties:

### Colors
- Surface / Background: `var(--nb-color-surface)`, `var(--nb-color-bg)`
- Text / Ink: `var(--nb-color-ink)`
- Brand / Accent: `var(--nb-color-primary)`, `var(--nb-color-accent)`
- Status Colors: `var(--nb-color-danger)`, `var(--nb-color-success)`, `var(--nb-color-warning)`

### Borders & Shadows
- Borders: `var(--nb-border-width-md) solid var(--nb-color-ink)` (default 4px solid black)
- Shadows: `var(--nb-shadow-offset-md) var(--nb-shadow-offset-md) 0 var(--nb-shadow-color)` (hard 4px drop shadow)
- Border Radius: `0px` (strict sharp corners)

---

## 3. Shared Components

- **`TopNav`**: Global navigation header with breadcrumbs, service switcher, theme toggle, and SSE-driven live unread notification badge.
- **`ModalDialog`**: Neobrutalist modal container with thick borders and focus trap.
- **`Badge`**: Status badge tags (`info`, `warning`, `danger`, `success`, `ai`).
- **`Button`**: Neobrutalist button with active translation states.
