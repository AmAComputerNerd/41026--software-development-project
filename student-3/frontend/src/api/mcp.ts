import { request } from './http'
import type { McpDeadlineResult } from '@/types/mcp'

export const getUpcomingDeadlines = (days: number, limit: number) =>
  request<McpDeadlineResult>('/integrations/mcp/upcoming-deadlines', {
    method: 'POST',
    body: JSON.stringify({ days, limit }),
  })
