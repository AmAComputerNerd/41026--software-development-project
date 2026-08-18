// This file defines frontend models for the DTOs in the backend API.
// It must be kept in sync with backend models.

import type { TaskPriority, TaskStatus } from './task'

export interface CourseDto {
  id: string
  code: string
  name: string
}

export interface TaskDto {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  priority: TaskPriority
  dueDate: string | null
  courseId: string | null
  courseName: string | null
  parentTaskId: string | null
  parentTaskTitle: string | null
}

export interface TaskFilterDto {
  status?: TaskStatus | null
  priority?: TaskPriority | null
  courseId?: string | null
  parentTaskId?: string | null
  overdue?: boolean | null
}

export interface CreateTaskRequestDto {
  title: string
  description?: string | null
  priority: TaskPriority
  dueDate?: string | null
  courseId?: string | null
  parentTaskId?: string | null
}

export interface ModifyTaskRequestDto {
  newTitle?: string | null
  updateDescription: boolean
  newDescription?: string | null
  newDueDate?: string | null
  newPriority?: TaskPriority
  newStatus?: TaskStatus
}
