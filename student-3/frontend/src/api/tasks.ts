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
} from '@/types/task'

export const getTasks = () => request<TaskItem[]>('/tasks/')
export const getCourses = () => request<Course[]>('/courses/')

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
  request<TaskItem[]>(`/tasks/${id}/ai-breakdown`, {
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
