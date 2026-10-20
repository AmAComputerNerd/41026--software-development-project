import type { MultiAgentWorkflow } from './multiAgent'

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

export interface GenerateTaskBreakdownInput {
  prompt: string
  priority: TaskPriority
}

export interface GeneratedSubtask {
  title: string
  description: string | null
}

export interface BreakdownProposal {
  workflow: MultiAgentWorkflow
  tasks: GeneratedSubtask[]
}

export interface BreakdownReviewInput {
  workflowId: string
  reject: boolean
  reviewerName: string
  comments: string
  tasks: (GeneratedSubtask & { index: number })[]
  revision: number
  decision: 'Approve' | 'PartialAccept' | 'Reject'
}

export interface BreakdownRevisionInput {
  workflowId: string
  requestId: string
  revision: number
  reviewerName: string
  feedback: string
}

export interface BreakdownReviewResult {
  workflow: MultiAgentWorkflow
  tasks: TaskItem[]
}

export interface GenerateTaskDescriptionInput {
  title: string
  courseId: string | null
  parentTaskId: string | null
}

export interface GeneratedTaskDescription {
  description: string
}
