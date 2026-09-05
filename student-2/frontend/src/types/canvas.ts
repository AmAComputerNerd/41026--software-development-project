export interface CanvasCourse {
  id: number
  name: string
  courseCode: string | null
  workflowState: string
}

export interface CanvasRecipient {
  id: string
  name: string
  category: string
  avatarUrl: string | null
}