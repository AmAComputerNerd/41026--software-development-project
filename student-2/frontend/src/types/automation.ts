interface AutomationBase {
  id: string
  studentId: string
  enabled: boolean
}

export type AssignmentExtensionReason =
  | 'UNW'
  | 'ACL'
  | 'NMT'
  | 'FAM'
  | 'CAR'
  | 'REL'
  | 'WRK'
  | 'TEC'
  | 'BRV'
  | 'OTH'

export interface AssignmentExtensionAutomation extends AutomationBase {
  $type: 'assignmentExtension'
  subjectId: number | null
  bufferMinutes: number
  reason: AssignmentExtensionReason
  furtherDetails: string
}

export interface ScheduledPostAutomation extends AutomationBase {
  $type: 'scheduledPost'
  postTime: string
  contextCode: string
  recipients: string[]
  subject: string
  body: string
  groupConversation: boolean
}

export interface QuizFillerAutomation extends AutomationBase {
  $type: 'quizFiller'
  subjectId: number | null
  multipleChoice: boolean
  shortAnswer: boolean
  numberOfAttemptsRequired: number
  allowForNoTimeLimit: boolean
}

export type Automation =
  | AssignmentExtensionAutomation
  | ScheduledPostAutomation
  | QuizFillerAutomation
export type AutomationDiscriminator = Automation['$type']

interface SaveAutomationInputBase {
  studentId: string
  enabled: boolean
}

export interface SaveAssignmentExtensionAutomationInput extends SaveAutomationInputBase {
  $type: 'assignmentExtension'
  subjectId: number | null
  bufferMinutes: number
  reason: AssignmentExtensionReason
  furtherDetails: string
}

export interface SaveScheduledPostAutomationInput extends SaveAutomationInputBase {
  $type: 'scheduledPost'
  postTime: string
  contextCode: string
  recipients: string[]
  subject: string
  body: string
  groupConversation: boolean
}

export interface SaveQuizFillerAutomationInput extends SaveAutomationInputBase {
  $type: 'quizFiller'
  subjectId: number | null
  multipleChoice: boolean
  shortAnswer: boolean
  numberOfAttemptsRequired: number
  allowForNoTimeLimit: boolean
}

export type SaveAutomationInput =
  | SaveAssignmentExtensionAutomationInput
  | SaveScheduledPostAutomationInput
  | SaveQuizFillerAutomationInput

interface AutomationFormDataBase {
  enabled: boolean
}

export interface AssignmentExtensionAutomationFormData extends AutomationFormDataBase {
  $type: 'assignmentExtension'
  subjectId: number | null
  bufferMinutes: number
  reason: AssignmentExtensionReason
  furtherDetails: string
}

export interface ScheduledPostAutomationFormData extends AutomationFormDataBase {
  $type: 'scheduledPost'
  postTime: string
  contextCode: string
  recipients: string[]
  subject: string
  body: string
  groupConversation: boolean
}

export interface QuizFillerAutomationFormData extends AutomationFormDataBase {
  $type: 'quizFiller'
  subjectId: number | null
  multipleChoice: boolean
  shortAnswer: boolean
  numberOfAttemptsRequired: number
  allowForNoTimeLimit: boolean
}

export type AutomationFormData =
  | AssignmentExtensionAutomationFormData
  | ScheduledPostAutomationFormData
  | QuizFillerAutomationFormData

interface AutomationRunBase {
  id: string
  automationId: string
  executionTimeStamp: string
  result: 'RUN' | 'SUC' | 'FAI'
}

export interface AssignmentExtensionAutomationRun extends AutomationRunBase {
  $type: 'assignmentExtension'
  assignmentId: string
}

export interface ScheduledPostAutomationRun extends AutomationRunBase {
  $type: 'scheduledPost'
  postTime: string
  contextCode: string
  recipients: string[]
  subject: string
  body: string
  groupConversation: boolean
}

export interface QuizFillerAutomationRun extends AutomationRunBase {
  $type: 'quizFiller'
  courseId: number
  quizId: number
  quizTitle: string
  questionCount: number
}

export type AutomationRun =
  | AssignmentExtensionAutomationRun
  | ScheduledPostAutomationRun
  | QuizFillerAutomationRun