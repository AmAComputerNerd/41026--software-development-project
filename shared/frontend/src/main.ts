/**
 * main.ts
 *
 * Bootstraps plugins then mounts the App
 */

// Composables
import { createApp } from 'vue'

// Plugins
import { registerPlugins } from '@/plugins'

// Components
import App from './App.vue'

// Styles
import '@better-canvas/ui-kit/styles/tokens.css'
import '@better-canvas/ui-kit/styles/primitives.css'

const app = createApp(App)

registerPlugins(app)

app.mount('#app')
