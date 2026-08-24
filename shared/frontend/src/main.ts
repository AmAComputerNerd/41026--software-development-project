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
import './styles/tokens.css'
import './styles/shell.css'

const app = createApp(App)

registerPlugins(app)

app.mount('#app')
