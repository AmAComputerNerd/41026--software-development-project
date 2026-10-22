export interface Student {
  studentId: string
  name: string | null
  idealMark: number | null
}

export interface Course {
  courseId: string
  code: string
  name: string
  canvasCourseId: number | null
  canvasIsActive: boolean | null
  lastCanvasSyncAt: string | null
}

export interface Assignment {
  assignmentId: string
  courseId: string
  name: string
  weight: number | null
  maxMark: number | null
  completed: boolean | null
}

export interface StudentAssignment {
  studentId: string
  assignmentId: string
  tempMark: number | null
  finalMark: number | null
}

export interface WeightingItem {
  assignmentId: string
  name: string
  weight: number | null
  maxMark: number | null
}

export interface WeightingsResponse {
  weight: number
  count: number
  generatedAtUtc: string
  items: WeightingItem[]
}

export interface McpResponse {
  status: string
  tool: string
  data: WeightingsResponse | null
  error: { code: string; message: string } | null
}

export interface RagCitation {
  sourceId: string
  title: string
  heading: string
  score: number
}

export interface RagRetrievalSummary {
  matchedChunks: number
  consideredChunks: number
}

export interface RagAnswerResponse {
  status: string
  answer: string
  confidence: string
  citations: RagCitation[]
  retrieval: RagRetrievalSummary
}

export interface MultiAgentPlanner {
  objective: string
  requiredEvidence: string[]
  steps: string[]
  completedAtUtc: string
}

export interface MultiAgentWorker {
  evidenceSummary: string
  draftOutput: string
  recommendedActions: string[]
  citations: string[]
  completedAtUtc: string
}

export interface MultiAgentReviewer {
  verdict: string
  confidence: string
  critique: string
  identifiedRisks: string[]
  recommendations: string[]
  completedAtUtc: string
}

export interface MultiAgentHumanReview {
  decision: string
  reviewerName: string
  comments: string
  finalOutput: string
  reviewedAtUtc: string
}

export interface MultiAgentAuditEntry {
  timestampUtc: string
  agent: string
  action: string
  summary: string
}

export interface MultiAgentRevision {
  revision: number
  requestId: string
  reviewerName: string
  feedback: string
  requestedAtUtc: string
  planner: MultiAgentPlanner
  worker: MultiAgentWorker
  reviewer: MultiAgentReviewer
}

export interface MultiAgentWorkflow {
  id: string
  owner: string
  studentId: string
  goal: string
  status: string
  createdAtUtc: string
  updatedAtUtc: string
  planner: MultiAgentPlanner
  worker: MultiAgentWorker
  reviewer: MultiAgentReviewer
  humanReview: MultiAgentHumanReview | null
  auditLog: MultiAgentAuditEntry[]
  revision: number
  revisions: MultiAgentRevision[] | null
}

export interface MultiAgentReviewRequest {
  decision: string
  reviewerName: string
  comments: string
  correctedOutput?: string
  expectedRevision?: number
}

export interface MultiAgentRevisionRequest {
  requestId: string
  expectedRevision: number
  reviewerName: string
  feedback: string
}

const API_BASE = (import.meta.env.VITE_GRADES_API_BASE_URL || '/api/grades').replace(/\/$/, '')

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  })

  if (!response.ok) {
    const detail = await response.text()
    throw new Error(detail || `Request failed (${response.status})`)
  }

  if (response.status === 204 || response.headers.get('content-length') === '0') {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export const gradesApi = {
  getStudents: () => request<Student[]>('/api/students/'),
  getStudent: (studentId: string) => request<Student>(`/api/students/${studentId}`),
  getCourses: () => request<Course[]>('/api/courses/'),
  getStudentAssignments: (studentId: string) =>
    request<Assignment[]>(`/api/assignment/student/${studentId}`),
  getCourseAssignments: (courseId: string) =>
    request<Assignment[]>(`/api/assignment/course/${courseId}`),
  getStudentMarks: (studentId: string) =>
    request<StudentAssignment[]>(`/api/assignment/marks/${studentId}`),
  setIdealMark: (studentId: string, idealMark: number, exists: boolean) =>
    request<Student>('/api/students/', {
      method: exists ? 'PUT' : 'POST',
      body: JSON.stringify({ studentId, idealMark }),
    }),
  setTemporaryMark: (
    studentId: string,
    assignmentId: string,
    tempMark: number,
    exists: boolean,
  ) =>
    request<StudentAssignment>('/api/assignment/marks/', {
      method: exists ? 'PUT' : 'POST',
      body: JSON.stringify({ studentId, assignmentId, tempMark }),
    }),
  deleteTemporaryMark: (studentId: string, assignmentId: string) =>
    request<void>(`/api/assignment/marks/${studentId}/${assignmentId}`, {
      method: 'DELETE',
    }),
  generateRecommendation: (assignments: Assignment[]) =>
    request<{ recommendation: string }>('/api/ai/generate-recommendation', {
      method: 'POST',
      body: JSON.stringify({ assignments }),
    }),
  getMcpWeightings: (weight: number, limit: number) =>
    request<McpResponse>('/api/integrations/mcp/assignment-weightings', {
      method: 'POST',
      body: JSON.stringify({ weight, limit }),
    }),
  getRagAnswer: (question: string) =>
    request<RagAnswerResponse>('/api/integrations/rag/answers', {
      method: 'POST',
      body: JSON.stringify({ question }),
    }),
  runMultiAgentWorkflow: () =>
    request<MultiAgentWorkflow>('/api/ai/multi-agent', {
      method: 'POST',
    }),
}
