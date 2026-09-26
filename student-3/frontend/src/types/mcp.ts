export interface UpcomingDeadline {
  id: string
  title: string
  dueDate: string
  priority: 'Low' | 'Medium' | 'High'
  status: 'Todo' | 'InProgress' | 'Completed'
  courseName: string | null
}

export interface UpcomingDeadlinesData {
  days: number
  count: number
  generatedAtUtc: string
  items: UpcomingDeadline[]
}

export interface McpToolError {
  code: string
  message: string
}

export interface McpDeadlineResult {
  status: 'success' | 'error'
  tool: 'deadlines_list_upcoming'
  data: UpcomingDeadlinesData | null
  error: McpToolError | null
}
