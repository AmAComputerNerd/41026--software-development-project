export type AutomationType = 'AssignmentExtension' | 'ScheduledPost'

export interface Automation {
  id: string
  studentId: string
  type: AutomationType
  enabled: boolean
  bufferMinutes: number | null
  reason: string | null
  furtherDetails: string | null
  postTime: string | null
  recipients: string[] | null
  subject: string | null
  body: string | null
}

export interface SaveAutomationInput {
  studentId: string
  type: AutomationType
  enabled: boolean
  bufferMinutes: number | null
  reason: string | null
  furtherDetails: string | null
  postTime: string | null
  recipients: string[] | null
  subject: string | null
  body: string | null
}

export interface AutomationRun {
  id: string
  automationId: string
  type: AutomationType
  executionTimeStamp: string
  result: 'SUC' | 'FAI'
  assignmentId: string | null
  recipients: string[] | null
  subject: string | null
  body: string | null
}