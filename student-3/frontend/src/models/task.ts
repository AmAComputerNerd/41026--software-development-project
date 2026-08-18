// This file defines frontend models for the DTOs in the backend API.
// It must be kept in sync with backend models.

import type { Course } from './course'

export enum TaskStatus {
  Todo = 'Todo',
  InProgress = 'InProgress',
  Completed = 'Completed',
}

export enum TaskPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
}

export interface Task {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  priority: TaskPriority
  dueDateUtc: Date | null
  course: Course | null
  parentTask: Task | null
}
