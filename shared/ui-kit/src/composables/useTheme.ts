import { ref, watchEffect } from 'vue'

export type Theme = 'light' | 'dark'

const STORAGE_KEY = 'nb-theme'

function getInitialTheme(): Theme {
  if (typeof window === 'undefined') return 'light'
  const stored = window.localStorage.getItem(STORAGE_KEY)
  if (stored === 'light' || stored === 'dark') return stored
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

// Module-level so every component sharing this composable reads/writes one theme.
const theme = ref<Theme>(getInitialTheme())

if (typeof document !== 'undefined') {
  watchEffect(() => {
    document.documentElement.setAttribute('data-theme', theme.value)
    window.localStorage.setItem(STORAGE_KEY, theme.value)
  })
}

export function useTheme() {
  function toggleTheme() {
    theme.value = theme.value === 'dark' ? 'light' : 'dark'
  }

  return { theme, toggleTheme }
}
