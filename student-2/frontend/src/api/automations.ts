import { request } from './http'
import type { Automation, AutomationRun, SaveAutomationInput } from '@/types/automation'

export const getAutomations = (studentId?: string) =>
  request<Automation[]>(`/automations/${studentId ? `?studentId=${studentId}` : ''}`)

export const getAutomation = (id: string) => request<Automation>(`/automations/${id}`)

export const createAutomation = (input: SaveAutomationInput) =>
  request<Automation>('/automations/', {
    method: 'POST',
    body: JSON.stringify(input),
  })

export const updateAutomation = (id: string, input: SaveAutomationInput) =>
  request<Automation>(`/automations/${id}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })

export const deleteAutomation = (id: string) =>
  request<void>(`/automations/${id}`, { method: 'DELETE' })

export const getAutomationRuns = (studentId?: string) =>
  request<AutomationRun[]>(`/automation-runs/${studentId ? `?studentId=${studentId}` : ''}`)