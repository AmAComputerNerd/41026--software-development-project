import { createApp } from 'vue'

import App from './App.vue'
import router from './router'
import '@better-canvas/ui-kit/styles/tokens.css'
import '@better-canvas/ui-kit/styles/primitives.css'
import './styles/neobrutalism.scss'

const app = createApp(App)

app.use(router)

app.mount('#app')
