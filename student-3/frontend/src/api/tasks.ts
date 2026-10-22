import { request } from './http'
import type {
  CanvasSyncResult,
  Course,
  CreateTaskInput,
  GeneratedTaskDescription,
  GenerateTaskBreakdownInput,
  GenerateTaskDescriptionInput,
  TaskItem,
  UpdateTaskInput,
  BreakdownProposal,
  BreakdownReviewInput,
  BreakdownReviewResult,
  BreakdownRevisionInput,
} from '@/types/task'

export const getTasks = () => request<TaskItem[]>('/tasks/')
export const getCourses = () => request<Course[]>('/courses/')
export const getAiModeCapabilities = () =>
  request<{ aiModeEnabled: boolean; multiAgentEnabled: boolean }>('/integrations/capabilities', { cache: 'no-store' })

export const createTask = (input: CreateTaskInput) =>
  request<TaskItem>('/tasks/', {
    method: 'POST',
    body: JSON.stringify(input),
  })

export const updateTask = (id: string, input: UpdateTaskInput) =>
  request<TaskItem>(`/tasks/${id}`, {
    method: 'PUT',
    body: JSON.stringify(input),
  })

export const deleteTask = (id: string) =>
  request<void>(`/tasks/${id}`, { method: 'DELETE' })

export const generateTaskBreakdown = (id: string, input: GenerateTaskBreakdownInput) =>
  request<BreakdownProposal | TaskItem[]>(`/tasks/${id}/ai-breakdown`, {
    method: 'POST',
    body: JSON.stringify(input),
  })

export const reviewTaskBreakdown = (id: string, input: BreakdownReviewInput) =>
  request<BreakdownReviewResult>(`/tasks/${id}/ai-breakdown/review`, {
    method: 'POST',
    body: JSON.stringify(input),
  })

export const reviseTaskBreakdown = (id: string, input: BreakdownRevisionInput) =>
  request<BreakdownProposal>(`/tasks/${id}/ai-breakdown/revise`, {
    method: 'POST',
    body: JSON.stringify(input),
  })

export const generateTaskDescription = (input: GenerateTaskDescriptionInput) =>
  request<GeneratedTaskDescription>('/tasks/ai-description', {
    method: 'POST',
    body: JSON.stringify(input),
  })

export const syncCanvas = () =>
  request<CanvasSyncResult>('/canvas-sync', { method: 'POST' })
