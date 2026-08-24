// Dashboard tile config.
// A tile is "live" only when BOTH a backend and a frontend exist for it in
// this repo. Flip `live: true` and set `route` once a frontend ships —
// no other change needed.
export type TileIcon = 'bell' | 'clock' | 'chart' | 'gear' | 'user'

export interface Tile {
  id: string
  name: string
  description: string
  icon: TileIcon
  route: string | null
  live: boolean
}

export const TILES: Tile[] = [
  {
    id: 'notifications',
    name: 'Notifications',
    description: 'Alerts, digests, and preferences for deadlines, grades, and account activity.',
    icon: 'bell',
    route: '/notifications/',
    live: true,
  },
  {
    id: 'deadlines-tasks',
    name: 'Deadlines & Tasks',
    description: 'Track upcoming deadlines and manage your task list.',
    icon: 'clock',
    route: null,
    live: false,
  },
  {
    id: 'grades-progress',
    name: 'Grades & Progress',
    description: 'View grades and track progress across courses.',
    icon: 'chart',
    route: null,
    live: false,
  },
  {
    id: 'automations',
    name: 'Automations',
    description: 'Configure automated workflows and triggers.',
    icon: 'gear',
    route: null,
    live: false,
  },
  {
    id: 'account-settings',
    name: 'Account & Settings',
    description: 'Manage your account details and preferences.',
    icon: 'user',
    route: null,
    live: false,
  },
]
