export type TaskPriority = 'Low' | 'Medium' | 'High'
export type TaskStatus = 'Todo' | 'InProgress' | 'Completed'

export interface Course {
  id: string
  code: string
  name: string
  canvasCourseId: number | null
  canvasWorkflowState: string | null
  canvasIsActive: boolean | null
  lastCanvasSyncAt: string | null
}

export interface TaskItem {
  id: string
  title: string
  description: string | null
  dueDate: string | null
  priority: TaskPriority
  status: TaskStatus
  courseId: string | null
  courseName: string | null
  parentTaskId: string | null
  parentTaskTitle: string | null
  canvasAssignmentId: number | null
  canvasUpdatedAt: string | null
  canvasWorkflowState: string | null
  canvasSubmissionState: string | null
  canvasIsActive: boolean | null
}

export interface CreateTaskInput {
  title: string
  description: string | null
  dueDate: string | null
  priority: TaskPriority
  courseId: string | null
  parentTaskId: string | null
}

export interface UpdateTaskInput {
  newTitle: string | null
  updateDescription: boolean
  newDescription: string | null
  updateDueDate: boolean
  newDueDate: string | null
  newPriority: TaskPriority | null
  newStatus: TaskStatus | null
}

export interface CanvasSyncResult {
  coursesCreated: number
  coursesUpdated: number
  tasksCreated: number
  tasksUpdated: number
}
