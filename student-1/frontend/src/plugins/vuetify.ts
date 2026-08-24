import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'

import { createVuetify } from 'vuetify'
import type { ThemeDefinition } from 'vuetify'

// Neobrutalist theme mirroring the design tokens in src/styles/neobrutalism.scss
const neobrutalism: ThemeDefinition = {
  dark: false,
  colors: {
    background: '#EDEBE6',
    surface: '#EDEBE6',
    primary: '#111111',
    secondary: '#FF5A1F',
    accent: '#F2C94C',
    error: '#FF5A1F',
    'on-background': '#111111',
    'on-surface': '#111111',
    'on-primary': '#EDEBE6',
    'on-secondary': '#111111',
  },
}

export default createVuetify({
  theme: {
    defaultTheme: 'neobrutalism',
    themes: { neobrutalism },
  },
  defaults: {
    global: {
      rounded: 0,
    },
  },
})
