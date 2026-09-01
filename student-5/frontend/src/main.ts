import { createApp } from 'vue'

import App from './App.vue'
import router from './router'
import '@better-canvas/ui-kit/styles/tokens.css'
import '@better-canvas/ui-kit/styles/primitives.css'
import './styles/grades.scss'

createApp(App).use(router).mount('#app')
