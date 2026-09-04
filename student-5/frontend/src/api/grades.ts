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
}
