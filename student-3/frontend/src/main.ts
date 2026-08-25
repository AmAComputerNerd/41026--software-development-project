import { createApp } from 'vue'
import { createVuetify } from 'vuetify'

import App from './App.vue'
import router from './router'
import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'
import '@better-canvas/ui-kit/styles/tokens.css'
import '@better-canvas/ui-kit/styles/primitives.css'
import './styles/deadlines.scss'

const app = createApp(App)
const vuetify = createVuetify({
  defaults: {
    VBtn: { rounded: 0, elevation: 0 },
    VCard: { rounded: 0, elevation: 0 },
    VTextField: { variant: 'outlined', density: 'comfortable' },
    VSelect: { variant: 'outlined', density: 'comfortable' },
    VTextarea: { variant: 'outlined', density: 'comfortable' },
  },
})

app.use(router)
app.use(vuetify)
app.mount('#app')
