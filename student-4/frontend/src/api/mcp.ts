import { request } from './http'

export type ReadinessStatus = 'ready' | 'needs_attention' | 'unavailable'

export interface AccountReadinessCheck {
  code: 'profile' | 'role' | 'canvas' | 'notifications'
  label: string
  status: ReadinessStatus
  message: string
  action: 'edit_profile' | null
}

export interface AccountReadiness {
  userId: string
  overallStatus: ReadinessStatus
  checkedAtUtc: string
  checks: AccountReadinessCheck[]
}

export interface McpReadinessResult {
  status: 'success'
  tool: 'accounts_check_readiness'
  data: AccountReadiness
  error: null
}

export function checkAccountReadiness(userId: string): Promise<McpReadinessResult> {
  return request(`/users/${encodeURIComponent(userId)}/mcp-readiness`, { method: 'POST' })
}
