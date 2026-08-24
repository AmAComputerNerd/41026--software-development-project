// Dashboard tile config. Base id/name/route/live come from the shared
// service registry; icon + description are dashboard-grid-specific and
// live here only.
import { SERVICES } from '@better-canvas/ui-kit'
import type { ServiceId } from '@better-canvas/ui-kit'

export type TileIcon = 'bell' | 'clock' | 'chart' | 'gear' | 'user'

export interface Tile {
  id: ServiceId
  name: string
  description: string
  icon: TileIcon
  route: string | null
  live: boolean
}

const ICONS: Record<ServiceId, TileIcon> = {
  notifications: 'bell',
  'deadlines-tasks': 'clock',
  'grades-progress': 'chart',
  automations: 'gear',
  'account-settings': 'user',
}

const DESCRIPTIONS: Record<ServiceId, string> = {
  notifications: 'Alerts, digests, and preferences for deadlines, grades, and account activity.',
  'deadlines-tasks': 'Track upcoming deadlines and manage your task list.',
  'grades-progress': 'View grades and track progress across courses.',
  automations: 'Configure automated workflows and triggers.',
  'account-settings': 'Manage your account details and preferences.',
}

export const TILES: Tile[] = SERVICES.map((service) => ({
  ...service,
  icon: ICONS[service.id],
  description: DESCRIPTIONS[service.id],
}))
