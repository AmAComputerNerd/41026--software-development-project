const GRADES_BASE_URL = import.meta.env.VITE_GRADES_API_BASE_URL || '/api/grades'

export interface StudentMarkDto {
  assignmentId: string
  assignmentName?: string
  mark?: number | null
  temporaryMark?: number | null
  totalMarks?: number
  weight?: number
}

export async function getStudentMarks(studentId: string): Promise<StudentMarkDto[]> {
  const response = await fetch(`${GRADES_BASE_URL}/api/assignment/marks/${studentId}`)
  if (!response.ok) {
    throw new Error(`Failed to fetch student marks: ${response.status}`)
  }
  return response.json()
}

export async function updateTemporaryMark(
  studentId: string,
  assignmentId: string,
  temporaryMark: number,
) {
  const response = await fetch(`${GRADES_BASE_URL}/api/assignment/marks/`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ studentId, assignmentId, temporaryMark }),
  })
  if (!response.ok) {
    throw new Error(`Failed to update temporary mark: ${response.status}`)
  }
  return response.json()
}
