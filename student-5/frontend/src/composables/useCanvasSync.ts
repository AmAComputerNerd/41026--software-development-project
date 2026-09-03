import { computed, ref } from 'vue'
import { gradesApi, type CanvasSyncResult } from '@/api/grades'

export type SyncStatus = 'idle' | 'syncing' | 'synced' | 'failed'

// Module-level singleton state: every consumer across the app reads the
// same status / result / error. We only auto-fire once per app session.
const status = ref<SyncStatus>('idle')
const lastResult = ref<CanvasSyncResult | null>(null)
const lastError = ref<string | null>(null)
const lastSyncedAt = ref<Date | null>(null)
let autoSyncStarted = false

async function sync(): Promise<void> {
  if (status.value === 'syncing') return
  status.value = 'syncing'
  lastError.value = null
  try {
    lastResult.value = await gradesApi.syncCanvas()
    lastSyncedAt.value = new Date()
    status.value = 'synced'
  } catch (error) {
    lastError.value = error instanceof Error ? error.message : 'Canvas sync failed.'
    status.value = 'failed'
  }
}

function ensureAutoSync() {
  // One shot per app session. Subsequent SPA navigations do not retrigger.
  if (autoSyncStarted || status.value === 'syncing') return
  autoSyncStarted = true
  // Fire and forget; UI reads status reactively.
  void sync()
}

export function useCanvasSync() {
  const isSyncing = computed(() => status.value === 'syncing')
  const isFailed = computed(() => status.value === 'failed')

  return {
    status,
    isSyncing,
    isFailed,
    lastResult,
    lastError,
    lastSyncedAt,
    sync,
    ensureAutoSync,
  }
}