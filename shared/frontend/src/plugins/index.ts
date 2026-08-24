/**
 * plugins/index.ts
 *
 * Automatically included in `./src/main.ts`
 */

// Types
import type { App } from 'vue'

// Plugins
import router from '../router'

export function registerPlugins (app: App) {
  app.use(router)
}