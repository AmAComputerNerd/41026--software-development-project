// Canonical list of Better Canvas microservices, shared by the dashboard
// shell (tile grid) and every microservice's navbar (cross-service links).
export type ServiceId =
  | 'notifications'
  | 'deadlines-tasks'
  | 'grades-progress'
  | 'automations'
  | 'account-settings'

export interface Service {
  id: ServiceId
  name: string
  route: string | null
  live: boolean
}

export const SERVICES: Service[] = [
  {
    id: 'notifications',
    name: 'Notifications',
    route: '/notifications/',
    live: true,
  },
  {
    id: 'deadlines-tasks',
    name: 'Deadlines & Tasks',
    route: '/deadlines/',
    live: true,
  },
  {
    id: 'grades-progress',
    name: 'Grades & Progress',
    route: null,
    live: false,
  },
  {
    id: 'automations',
    name: 'Automations',
    route: null,
    live: false,
  },
  {
    id: 'account-settings',
    name: 'Account & Settings',
    route: '/account/',
    live: true,
  },
]
