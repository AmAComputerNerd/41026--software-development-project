import { ref } from 'vue'
import { createPreference, getPreferences, updatePreference } from '@/api/preferences'
import { CURRENT_STUDENT_ID } from '@/config'
import { NOTIFICATION_TYPES } from './useNotifications'

export const NOTIFICATION_CHANNELS = [
  { value: 'InApp', label: 'IN-APP' },
  { value: 'Email', label: 'EMAIL' },
]

interface PreferenceDto {
  id: string
  studentId: string
  type: string
  channel: string
  enabled: boolean
  updatedAtUtc: string
}

interface PreferenceCell {
  id: string | null
  enabled: boolean
}

// type -> channel -> cell. Rows with no backend record default to enabled,
// and are lazily created on the first toggle.
type PreferenceGrid = Record<string, Record<string, PreferenceCell>>

function emptyGrid(): PreferenceGrid {
  const grid: PreferenceGrid = {}
  for (const type of NOTIFICATION_TYPES) {
    grid[type.value] = {}
    for (const channel of NOTIFICATION_CHANNELS) {
      grid[type.value][channel.value] = { id: null, enabled: true }
    }
  }
  return grid
}

export function usePreferences() {
  const grid = ref<PreferenceGrid>(emptyGrid())
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchPreferences() {
    loading.value = true
    error.value = null
    try {
      const preferences: PreferenceDto[] = await getPreferences(CURRENT_STUDENT_ID)
      const next = emptyGrid()
      for (const p of preferences) {
        if (next[p.type]?.[p.channel]) {
          next[p.type][p.channel] = { id: p.id, enabled: p.enabled }
        }
      }
      grid.value = next
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load preferences'
    } finally {
      loading.value = false
    }
  }

  async function toggle(type: string, channel: string) {
    const cell = grid.value[type][channel]
    const previous = { ...cell }
    const nextEnabled = !cell.enabled
    cell.enabled = nextEnabled

    try {
      if (cell.id) {
        await updatePreference(cell.id, {
          studentId: CURRENT_STUDENT_ID,
          type,
          channel,
          enabled: nextEnabled,
        })
      } else {
        const created = await createPreference({
          studentId: CURRENT_STUDENT_ID,
          type,
          channel,
          enabled: nextEnabled,
        })
        cell.id = created.id
      }
    } catch (err) {
      grid.value[type][channel] = previous
      error.value = err instanceof Error ? err.message : 'Failed to update preference'
    }
  }

  return {
    grid,
    loading,
    error,
    fetchPreferences,
    toggle,
  }
}
