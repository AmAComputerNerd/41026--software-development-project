import type { Automation, AutomationRun } from '@/types/automation'
import type { CanvasCourse } from '@/types/canvas'

export function formatCourseLabel(
  courseId: number | null | undefined,
  courses: CanvasCourse[],
) {
  if (!courseId) return 'Any course'

  const course = courses.find((option) => option.id === courseId)
  if (!course) return `Course ${courseId}`
  const name = withoutSemester(course.name)
  const code = course.courseCode ? withoutSemester(course.courseCode) : null
  if (!code) return name
  if (code !== name) return `${code} — ${name}`

  const matchingCodeAndTitle = /^(\d+(?:\s+\d+)*)\s+(.+)$/.exec(name)
  return matchingCodeAndTitle
    ? `${matchingCodeAndTitle[1]} — ${matchingCodeAndTitle[2]}`
    : name
}

function withoutSemester(value: string) {
  return value.replace(/\s+-\s+(?:spring|summer|autumn|fall|winter)\s+\d{4}$/i, '')
}

export function getAutomationCourseId(automation: Automation) {
  if (automation.$type === 'scheduledPost') return getContextCourseId(automation.contextCode)
  return automation.subjectId
}

export function getRunCourseId(run: AutomationRun) {
  if (run.$type === 'scheduledPost') return getContextCourseId(run.contextCode)
  return run.courseId
}

export function getContextCourseId(contextCode: string) {
  const match = /^course_(\d+)$/.exec(contextCode)
  return match ? Number(match[1]) : null
}